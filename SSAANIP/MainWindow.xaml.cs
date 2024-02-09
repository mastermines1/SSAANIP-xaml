using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
namespace SSAANIP;
public partial class MainWindow : Page {
    protected readonly updateData update;
    protected readonly Request req;
    protected masterWindow parent;
    protected string connectionString = "Data source=data.db";
    protected string[] currentSongIds;
    protected int currentSongIndex;
    protected Queue<int> songIndex = new();
    protected Queue<string> queue = new();
    protected Queue<string> shuffledUpNext = new();
    protected Stack<string> prevPlayed = new();
    protected bool isPaused = true;
    protected string playingSourceId;
    protected string playingSourceType;
    protected bool updatePosition = false;
    protected bool isChangedByProgram = false;
    protected int currentSongDuration;
    protected int loopMode = 0;  //0=no loop, 1=regular loop, 2=loop current song
    protected bool fromQueue = false;
    protected string prioNextUp = null;
    protected string playlistEdited;
    public MainWindow(masterWindow master,Request req) {
        InitializeComponent();
        parent = master;
        this.req = req;
        update = new(connectionString, req);
        if (!File.Exists("data.db")) { //checks if file exists and if not creates one
            File.Create("data.db").Close();            
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open(); 
                cmd.CommandText = "CREATE TABLE \"tblAlbumArtistLink\" (\"linkId\"\tINTEGER,\"artistId\"TEXT,\"AlbumId\"TEXT,FOREIGN KEY(\"artistId\") REFERENCES \"tblArtists\"(\"artistId\"),PRIMARY KEY(\"linkId\" AUTOINCREMENT));" +
                    "CREATE TABLE \"tblAlbumSongLink\" (\"linkid\"INTEGER,\"albumId\"TEXT,\"songId\"TEXT,\"songIndex\"INTEGER,PRIMARY KEY(\"linkid\" AUTOINCREMENT),FOREIGN KEY(\"songId\") REFERENCES \"tblSongs\"(\"songId\"),FOREIGN KEY(\"albumId\") REFERENCES \"tblAlbums\"(\"albumId\"));" +
                    "CREATE TABLE \"tblAlbums\" (\"albumId\"\tTEXT,\"albumName\"\tTEXT,\"albumDuration\"\tTEXT,PRIMARY KEY(\"albumId\"));" +
                    "CREATE TABLE \"tblArtists\" (\"artistId\"\tTEXT,\"artistName\"\tTEXT,PRIMARY KEY(\"artistId\"));" +
                    "CREATE TABLE \"tblPlaylistSongLink\" (\"linkId\"\tINTEGER,\"playlistId\"\tTEXT,\"songId\"\tTEXT,\"songIndex\"\tINTEGER,PRIMARY KEY(\"linkId\" AUTOINCREMENT),FOREIGN KEY(\"songId\") REFERENCES \"tblSongs\"(\"songId\"),FOREIGN KEY(\"playlistId\") REFERENCES \"tblPlaylists\"(\"playlistId\"));" +
                    "CREATE TABLE \"tblPlaylistUserLink\" (\"linkId\"\tINTEGER,\"playlistId\"\tTEXT,\"userName\"\tTEXT,PRIMARY KEY(\"linkId\" AUTOINCREMENT),FOREIGN KEY(\"playlistId\") REFERENCES \"tblPlaylists\"(\"playlistId\"),FOREIGN KEY(\"userName\") REFERENCES \"tblUsers\"(\"userName\"));" +
                    "CREATE TABLE \"tblPlaylists\" (\"playlistId\"\tTEXT,\"playlistName\"\tTEXT,\"playlistDuration\"\tTEXT,\"isPublic\"\tTEXT,\"playlistDescription\"\tTEXT,PRIMARY KEY(\"playlistId\"));" +
                    "CREATE TABLE \"tblSongs\" (\"songId\"\tTEXT,\"songName\"\tTEXT,\"songDuration\"\tTEXT,PRIMARY KEY(\"songId\"));" +
                    "CREATE TABLE \"tblUsers\" (\"userName\"\tTEXT,\"isAdmin\"\tTEXT,PRIMARY KEY(\"userName\"));";
                cmd.ExecuteScalar();
            }
        }
        update.updateUsers(); 
        cobPlaylists.ItemsSource = getSourceNamesFromType("Playlist");
        displayArtists();
    }
    private async Task nextSong(){
        fromQueue = false;
        string currentSongId = string.Empty;
        if (loopMode == 2){
            mediaElement.Position = new TimeSpan(0);
        }
        else if(prioNextUp != null){
            mediaElement.Source = req.createURL("stream", "&id=" + prioNextUp);
            lblSongPlaying.Content = await getSongName(prioNextUp) + " | " + await getArtistNameFromSong(prioNextUp);
            prioNextUp = null;
        }
        else if(queue.Count > 0){
            fromQueue = true;
            currentSongId = queue.Dequeue();
            mediaElement.Source = req.createURL("stream", "&id=" + currentSongId);
            lblSongPlaying.Content = await getSongName(currentSongId) + " | " + await getArtistNameFromSong(currentSongId);
            lsQueue.Items.RemoveAt(0);
        }
        else if (songIndex.Count > 0 ){
            currentSongIndex = songIndex.Dequeue();
            currentSongId = currentSongIds[currentSongIndex];
            mediaElement.Source = req.createURL("stream", "&id=" + currentSongId);
            lblSongPlaying.Content = await getSongName(currentSongId) + " | " + await getArtistNameFromSong(currentSongId);
        }
        else if(songIndex.Count == 0 && loopMode == 1){
            playSongsFromList(getSongIdsFromSourceId(playingSourceId, playingSourceType));
        }
        updatePosition = false;
        updateSdr(currentSongId);
    }
    private void updateQueue(string[] newSongIds){
        if(newSongIds.Length > 0){
            foreach (string id in newSongIds){
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT songName FROM tblSongs WHERE songId = @id";
                    cmd.Parameters.Add(new("@id", id));
                    string output = cmd.ExecuteScalar().ToString();
                    lsQueue.Items.Add(output);
                }
            }
            btnClearQueue.Visibility = Visibility.Visible;
        }
    }
    private async Task playSongsFromList(List<string> songIds){
        if(songIds.Count > 0){
            songIndex.Clear();
            for (int i = 0; i < songIds.Count; i++){
                songIndex.Enqueue(i);
            }
            if (btnShuffleToggle.Background == Brushes.GreenYellow){
                songIndex = shuffle(songIndex);
            }
            currentSongIds = songIds.ToArray();
            await nextSong();
            mediaElement.Play();
        }
    }
    private async Task playSong(string songId, string sourceType){
        int selectedSongIndex;
        using (SQLiteConnection conn = new(connectionString))
        using (SQLiteCommand cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = $"SELECT songIndex FROM tbl{sourceType}SongLink WHERE songId = @id";
            cmd.Parameters.Add(new("@id", songId));
            selectedSongIndex = Convert.ToInt32(cmd.ExecuteScalar());
        }
        playingSourceId = getSourceIdFromName(lsAlbums.SelectedItem.ToString(), sourceType);
        List<string> currentSongIdsList = getSongIdsFromSourceId(playingSourceId, sourceType);
        currentSongIds = currentSongIdsList.ToArray();
        songIndex.Clear();
        for (int i = selectedSongIndex + 1; i < currentSongIds.Length; i++){
            songIndex.Enqueue(i);
        }
        prioNextUp = songId;
        await nextSong();
        mediaElement.Play();
        if (btnShuffleToggle.Background == Brushes.GreenYellow) songIndex = shuffle(songIndex);
        btnPlayPause.Content = "||";
        isPaused = false;
        updateSdr(songId);
    }
    private List<string> getSongIdsFromSourceId(string sourceId, string sourceType){
        List<string> songIds = new();    
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = $"SELECT songId FROM tbl{sourceType}SongLink WHERE {sourceType.ToLower()}Id = @id";
            cmd.Parameters.Add(new("@id", sourceId));
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()){
                songIds.Add(reader.GetString(0));
            }
        }
        return songIds;
    }
    private string getSourceIdFromName(string sourceName, string sourceType){
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = $"SELECT {sourceType.ToLower()}Id FROM tbl{sourceType}s WHERE {sourceType.ToLower()}Name = @name";
            cmd.Parameters.Add(new("@name", sourceName));
            return cmd.ExecuteScalar().ToString();
        }
    }
    private async Task updateSdr(string tempSongId){
        double currentPosition = 0;
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT songDuration FROM tblSongs WHERE songId = @id";
            cmd.Parameters.Add(new("@id", tempSongId));
            currentSongDuration = Convert.ToInt32(cmd.ExecuteScalar());
        }
        await Task.Delay(1000);
        updatePosition = true;
        while (currentPosition < currentSongDuration && updatePosition){
            currentPosition = mediaElement.Position.TotalSeconds;
            lblTime.Content = $"({((int)currentPosition/60).ToString().PadLeft(2, '0')}:{((int)currentPosition%60).ToString().PadLeft(2, '0')}/{((int)currentSongDuration/60).ToString().PadLeft(2, '0')}:{((int)currentSongDuration%60).ToString().PadLeft(2, '0')})";
            var currentRatio = (currentPosition*100)/currentSongDuration;
            isChangedByProgram = true;
            sdrPosition.Value = (double) currentRatio;
            isChangedByProgram = false;
            await Task.Delay(1000);
        }
    }
    private static string getIdFromUrl(string url){
        return url.Split('=').Last();    
    }
    private async Task<string> getSongName(string songId){
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT songName FROM tblSongs WHERE songId = @id";
            cmd.Parameters.Add(new("@id", songId));
            return cmd.ExecuteScalar().ToString();
        }
    }
    private async Task<string> getArtistNameFromSong(string songId){
        string albumId = string.Empty;
        string artistId = string.Empty;
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT albumId FROM tblAlbumSongLink WHERE songId = @id";
            cmd.Parameters.Add(new("@id", songId));
            albumId = cmd.ExecuteScalar().ToString();
        }
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT artistId FROM tblAlbumArtistLink WHERE albumId = @id";
            cmd.Parameters.Add(new("@id", albumId));
            artistId = cmd.ExecuteScalar().ToString();
        }
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT artistName FROM tblArtists WHERE artistId = @id";
            cmd.Parameters.Add(new("@id", artistId));
            return cmd.ExecuteScalar().ToString();
        }
    }
    private static Queue<int> shuffle(Queue<int> Queue){
        int[] Array = Queue.ToArray();
        Random rng = new();
        int length = Array.Length;
        for(int i=0; i < length; i++){
            int ranNum = rng.Next(0,length);
            (Array[ranNum], Array[i]) = (Array[i], Array[ranNum]);
        }
        Queue.Clear();

        for (int i = 0; i < Array.Length; i++){
            Queue.Enqueue(Array[i]);
        }
        return Queue;
    }
    private List<string> getSourceNamesFromType(string sourceType){
        List<string> sourceNames = new();
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = $"SELECT {sourceType.ToLower()}Name FROM tbl{sourceType}s ORDER by {sourceType.ToLower()}Name ASC";
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()){
                sourceNames.Add(reader.GetString(0));
            }
        }
        return sourceNames;
    }
    private void stop(){
        updatePosition = false;
        mediaElement.Stop();
        songIndex.Clear();
        queue.Clear();
        prevPlayed.Clear();
        btnPlayPause.Content = "⏵";
        lblSongPlaying.Content = "";
        lsQueue.Items.Clear();
        mediaElement.Source = null;
        lblTime.Content = "";
        sdrPosition.Value = 0;
        btnClearQueue.Visibility = Visibility.Hidden;
    }
    //Methods to display data
    private void displayArtists() {
        lsArtist.ItemsSource = getSourceNamesFromType("Artist");
    }
    private void displaySongs(string albumId){
        List<string> songIds = new();
        List<string> songNames = new ();
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()) {
            conn.Open();
            cmd.CommandText = "SELECT songId FROM tblAlbumSongLink WHERE albumid = @id";
            cmd.Parameters.Add(new("@id", albumId));
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()){
                songIds.Add(reader.GetString(0));
            } 
        }
        foreach (string s in songIds){
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songName FROM tblSongs WHERE songId = @id";
                cmd.Parameters.Add(new("@id", s));
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read()){
                    songNames.Add(reader.GetString(0));
                }
                    
            }
        }
        lsSongs.ItemsSource = songNames;
    }
    private void displayPlaylists(){
        List<string> playlistNames = getSourceNamesFromType("Playlist");
        foreach(string playlistName in playlistNames){

            lsPlaylists.Items.Add(playlistName);
        }
    }
    private async void btnUpdateDb_clicked(object sender, RoutedEventArgs e) {
        stop(); 
        lsArtist.ItemsSource = null;
        lsAlbums.ItemsSource = null;
        lsSongs.ItemsSource = null;
        await update.updateDB();
        displayArtists();
    }
    private void lsArtist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        btnPlayAlbum.Visibility = Visibility.Hidden;
        btnQueueAlbum.Visibility = Visibility.Hidden;
        btnPlaySong.Visibility = Visibility.Hidden;
        btnQueueSong.Visibility = Visibility.Hidden;
        btnAddAlbumToPlaylist.Visibility = Visibility.Hidden;
        if (lsArtist.SelectedItem != null){
            lsSongs.ItemsSource = null;
            lsAlbums.ItemsSource = null;
            List<string> albumNames = new();
            List<string> albumIds = new();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT albumID FROM tblAlbumArtistLink WHERE artistId = @id";
                cmd.Parameters.Add(new("@id", getSourceIdFromName(lsArtist.SelectedItem.ToString(), "Artist")));
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read()){
                    albumIds.Add(reader.GetString(0));
                }
            }
            foreach (string albumId in albumIds) {
                SQLiteConnection conn = new(connectionString);
                var cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandText = "SELECT albumName FROM tblAlbums WHERE albumId = @id ORDER by albumName DESC";
                cmd.Parameters.Add(new("@id", albumId));
                albumNames.Add(cmd.ExecuteScalar().ToString());
                
            }
            lsAlbums.ItemsSource = albumNames;
        }
    }
    private void lsAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        btnPlaySong.Visibility = Visibility.Hidden;
        btnQueueSong.Visibility = Visibility.Hidden;
        btnAddSongToPlaylist.Visibility = Visibility.Hidden;
        if ((sender as ListBox).SelectedItem != null){
            if (cobPlaylists.SelectedItem != null) btnAddAlbumToPlaylist.Visibility = Visibility.Visible;
            string selectedAlbumName = (sender as ListBox).SelectedItem.ToString();
            List<string> albumIds = new();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT albumId FROM tblAlbums WHERE albumName = @name";
                cmd.Parameters.Add(new("@name", selectedAlbumName));
                using SQLiteDataReader reader = cmd.ExecuteReader(); 
                while (reader.Read()){
                    albumIds.Add(reader.GetString(0));
                }
            }
            foreach (string albumId in albumIds){
                SQLiteConnection conn = new(connectionString);
                var cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandText = "SELECT artistID FROM tblAlbumArtistLink WHERE albumID = @id";
                cmd.Parameters.Add(new("@id", albumId));
                object result = cmd.ExecuteScalar();
                if (result.ToString() == getSourceIdFromName(lsArtist.SelectedItem.ToString(), "Artist")){
                    displaySongs(albumId);
                }
            }
            btnPlayAlbum.Visibility = Visibility.Visible;
            btnQueueAlbum.Visibility = Visibility.Visible;
        }
    }
    private void lsSongs_SelectionChanged(object sender, SelectionChangedEventArgs e){
        if(lsSongs.SelectedItem != null){
            if (cobPlaylists.SelectedItem != null) btnAddSongToPlaylist.Visibility = Visibility.Visible;
            btnPlaySong.Visibility = Visibility.Visible;
            btnQueueSong.Visibility = Visibility.Visible;
        }
    }
    private void btnPlayAlbum_clicked(object sender, RoutedEventArgs e){
        if (loopMode == 2) loopMode = 1;
        playingSourceType = "Album";
        playingSourceId = getSourceIdFromName(lsAlbums.SelectedItem.ToString(), "Album");
        List<string> songIds = getSongIdsFromSourceId(playingSourceId, "Album");
        prioNextUp = songIds[0];
        songIds.RemoveAt(0);
        playSongsFromList(songIds);
    }
    private void btnQueueAlbum_Click(object sender, RoutedEventArgs e){
        List<string> songIds = getSongIdsFromSourceId(getSourceIdFromName(lsAlbums.SelectedItem.ToString(), "Album"), "Album");
        foreach (string songId in songIds){
            queue.Enqueue(songId);
        }
        updateQueue(songIds.ToArray());
    }
    private void btnPlaySong_Click(object sender, RoutedEventArgs e){
        if(loopMode == 2) loopMode = 1;
        playingSourceType = "Album";
        playingSourceId = getSourceIdFromName(lsSongs.SelectedItem.ToString(), "Song");
        playSong(playingSourceId, "Album");
    }
    private void btnQueueSong_Click(object sender, RoutedEventArgs e){
        queue.Enqueue(getSourceIdFromName(lsSongs.SelectedItem.ToString(), "Song"));
        updateQueue(new string[] {getSourceIdFromName(lsSongs.SelectedItem.ToString(), "Song")});
    }
    private void mediaElement_MediaEnded(object sender, RoutedEventArgs e){
        if (!fromQueue) prevPlayed.Push(getIdFromUrl(mediaElement.Source.ToString()));
        nextSong();
    }
    private void btnNextSong_click(object sender, RoutedEventArgs e){
        if (!fromQueue && mediaElement.Source != null) prevPlayed.Push(getIdFromUrl(mediaElement.Source.ToString()));
        nextSong();
    }
    private void btnPrevSong_Click(object sender, RoutedEventArgs e){
        if(prevPlayed.Count > 0){
            if (!fromQueue){
                int[] tempSongIndex = songIndex.ToArray();
                int length = songIndex.Count;
                songIndex.Clear();
                songIndex.Enqueue(currentSongIndex);
                for(int i =0; i < length; i++) {
                        songIndex.Enqueue(tempSongIndex[i]);
                }
            }
            prioNextUp = prevPlayed.Pop();
            nextSong();
        }
    }
    private void btnPlayPause_Click(object sender, RoutedEventArgs e){
        if(mediaElement.Source != null || queue.Count > 0){
            if (isPaused) { 
                mediaElement.Play();
                btnPlayPause.Content = "||";
                isPaused = false;
            }else{
                mediaElement.Pause();
                btnPlayPause.Content = "⏵";
                isPaused = true;
            }
        }
    }
    private void btnStop_Click(object sender, RoutedEventArgs e){
        stop();
    }
    private void sdrVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e){
        mediaElement.Volume = e.NewValue / 10;
    }
    private void sdrPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e){
        if(!isChangedByProgram){
            TimeSpan temp = new((long)e.NewValue*currentSongDuration*100000);
            mediaElement.Position = temp;
        }
    }
    private void btnLoopToggle_Click(object sender, RoutedEventArgs e){
        if(loopMode != 2) loopMode += 1;
        else loopMode = 0;
        switch(loopMode){
            case 0:
                btnLoopToggle.Content = "🔁";
                btnLoopToggle.Background = Brushes.Red;
                break;
            case 1:
                btnLoopToggle.Content = "🔁";
                btnLoopToggle.Background = Brushes.GreenYellow;
                break;
            case 2:
                btnLoopToggle.Content = "🔂";
                break;
        }
    }
    private void btnShuffleToggle_Click(object sender, RoutedEventArgs e){
        if (btnShuffleToggle.Background == Brushes.GreenYellow){
            btnShuffleToggle.Background = Brushes.Red;
        }
        else if(loopMode != 1){
            btnShuffleToggle.Background = Brushes.GreenYellow;
            songIndex = shuffle(songIndex);
        }else{
            btnShuffleToggle.Background = Brushes.GreenYellow;
            int length = songIndex.Count;
            songIndex.Clear();
            for(int i=0; i < length; i++){
                if(i != currentSongIndex) songIndex.Enqueue(i);
            }
            songIndex = shuffle(songIndex);
        }
    }
    private void lsQueue_SelectionChanged(object sender, SelectionChangedEventArgs e){
        if(lsQueue.SelectedItem == null){
            btnQueueUp.Visibility = Visibility.Hidden;
            btnQueueDelete.Visibility = Visibility.Hidden;
            btnQueueDown.Visibility = Visibility.Hidden;
            btnPlaySongFromQueue.Visibility = Visibility.Hidden;
            btnClearQueue.Visibility = Visibility.Hidden;
        }else{
            btnQueueUp.Visibility = Visibility.Visible;
            btnQueueDelete.Visibility = Visibility.Visible;
            btnQueueDown.Visibility = Visibility.Visible;
            btnPlaySongFromQueue.Visibility = Visibility.Visible;
        }
    }
    private void btnQueueUp_Click(object sender, RoutedEventArgs e){
        string[] queueIds = queue.ToArray();
        if(lsQueue.SelectedIndex > 0){
            int selectedIndex = lsQueue.SelectedIndex;
            (queueIds[selectedIndex], queueIds[selectedIndex - 1]) = (queueIds[selectedIndex - 1], queueIds[selectedIndex]);
            (lsQueue.Items[selectedIndex], lsQueue.Items[selectedIndex - 1]) = (lsQueue.Items[selectedIndex - 1], lsQueue.Items[selectedIndex]);                
            queue.Clear();
            foreach (string id in queueIds){
                if(id!=null) queue.Enqueue(id);
            }
            lsQueue.SelectedIndex = selectedIndex-1;
        }
    }
    private void btnQueueDown_Click(object sender, RoutedEventArgs e){
        string[] queueIds = queue.ToArray();
        if (lsQueue.SelectedIndex >= 0 && lsQueue.SelectedIndex < queue.Count-1){
            int selectedIndex = lsQueue.SelectedIndex;
            (queueIds[selectedIndex], queueIds[selectedIndex + 1]) = (queueIds[selectedIndex + 1], queueIds[selectedIndex]);
            (lsQueue.Items[selectedIndex], lsQueue.Items[selectedIndex + 1]) = (lsQueue.Items[selectedIndex + 1], lsQueue.Items[selectedIndex]);
            queue.Clear();
            foreach (string id in queueIds){
                queue.Enqueue(id);
            }
            lsQueue.SelectedIndex = selectedIndex+1;
        }
    }
    private void btnQueueDelete_Click(object sender, RoutedEventArgs e){
        string[] queueIds = queue.ToArray();
        if (lsQueue.SelectedIndex >= 0){
            lsQueue.Items.RemoveAt(lsQueue.SelectedIndex);
            queue.Clear();
            for (int i= 0;i < lsQueue.Items.Count; i++){
                if (i != lsQueue.SelectedIndex) queue.Enqueue(queueIds[i]);
            }
        }
    }
    private void btnUserManage_Click(object sender, RoutedEventArgs e){
        stop();
        parent.Frame.Content = new userMgmt(parent, req);
    }
    private void btnClearQueue_Click(object sender, RoutedEventArgs e){
        queue.Clear();
        lsQueue.Items.Clear();
        btnClearQueue.Visibility = Visibility.Hidden;
    }
    private void btnPlaySongFromQueue_Click(object sender, RoutedEventArgs e){
        if (lsQueue.SelectedIndex >= 0){
            int i = 0;
            while (i != lsQueue.SelectedIndex){
                queue.Dequeue();
                lsQueue.Items.RemoveAt(0);
            }
            nextSong();
            mediaElement.Play();
            btnPlayPause.Content = "||";
        }
    }
    private void btnPlaylistsToggle_Click(object sender, RoutedEventArgs e){
        if (grdAlbums.Visibility == Visibility.Hidden){ //toggle to albums
            btnPlaylistsToggle.Content = "Show playlists";
            grdAlbums.Visibility = Visibility.Visible;
            grdPlaylists.Visibility = Visibility.Hidden;
        }else{ //toggle to playlists
            btnPlaylistsToggle.Content = "Show albums";
            grdAlbums.Visibility = Visibility.Hidden;
            grdPlaylists.Visibility = Visibility.Visible;
            btnPlayPlaylist.Visibility = Visibility.Hidden;
            btnQueuePlaylist.Visibility = Visibility.Hidden;
            btnPlayPlaylistSong.Visibility = Visibility.Hidden;
            btnQueuePlaylistSong.Visibility = Visibility.Hidden;
            lsPlaylists.Items.Clear();
            lsPlaylistsSongs.Items.Clear(); 
            displayPlaylists();
        }
    }
    private async void lsPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e){
        if(lsPlaylists.SelectedItem != null){
            lsPlaylistsSongs.Items.Clear();
            btnPlayPlaylist.Visibility = Visibility.Visible;
            btnQueuePlaylist.Visibility = Visibility.Visible;
            btnEditPlaylist.Visibility = Visibility.Visible;

            string playlistId = string.Empty;
            List<string> songIds = new();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT playlistId FROM tblPlaylists WHERE playlistName = @name";
                cmd.Parameters.Add(new("@name", lsPlaylists.SelectedItem));
                playlistId = cmd.ExecuteScalar().ToString();
            }
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songId FROM tblPlaylistSongLink WHERE playlistId= @id";
                cmd.Parameters.Add(new("@id", playlistId));
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read()){
                    songIds.Add(reader.GetString(0));
                }
            }
            foreach(string songId in songIds){
                lsPlaylistsSongs.Items.Add(await getSongName(songId));
            }
        }else{
            btnPlayPlaylist.Visibility = Visibility.Hidden;
            btnQueuePlaylist.Visibility = Visibility.Hidden;
            btnEditPlaylist.Visibility = Visibility.Hidden;
        }
    }
    private void btnNewPlaylist_Click(object sender, RoutedEventArgs e){
        txtPlaylistDescription.Text = "";
        txtPlaylistName.Text = "";
        lsPlaylistEditSongs.Items.Clear();
        playlistEdited = "";
        grdPlaylistEdit.Visibility = Visibility.Visible;
    }
    private async void btnSavePlaylist_Click(object sender, RoutedEventArgs e){
        string playlistName = txtPlaylistName.Text;
        string playlistDescription = txtPlaylistDescription.Text;
        string playlistIsPublic = ckbIsPublic.IsChecked.Value.ToString();
        string playlistId = string.Empty;
        bool alrExists = false;

        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT playlistId FROM tblPlaylists WHERE playlistName = @name";
            cmd.Parameters.Add(new("@name", playlistEdited));
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()){
                playlistId = reader.GetString(0);
                alrExists = true;
            }
        }
        if (!alrExists){
            playlistId = (await req.sendRequestAsync("createPlaylist", "&name=" + playlistName)).Elements().First().Attribute("id").Value;
        }
        await req.sendRequestAsync("updatePlaylist", $"&playlistId={playlistId}&name={playlistName}&comment={playlistDescription}&public={playlistIsPublic}");
        IEnumerable<XElement> playlistData = await req.sendRequestAsync("getPlaylist", "&id=" + playlistId);

        if (!alrExists){
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblPlaylists VALUES (@id, @name, @duration, @isPublic, @description)";
                cmd.Parameters.Add(new("@id", playlistId));
                cmd.Parameters.Add(new("@name", playlistName));
                cmd.Parameters.Add(new("@duration", playlistData.Elements().First().Attribute("duration").Value.ToString()));
                cmd.Parameters.Add(new("@isPublic", playlistIsPublic.ToLower()));
                cmd.Parameters.Add(new("@description", playlistDescription));
                cmd.ExecuteScalar();
            }
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblPlaylistUserLink (playlistId, userName) VALUES (@id, @name)";
                cmd.Parameters.Add(new("@id", playlistId));
                cmd.Parameters.Add(new("@name", req.username));
                cmd.ExecuteScalar();
            }
            lsPlaylists.Items.Add(playlistName);
        }else{
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "UPDATE tblPlaylists SET playlistName=@name, isPublic=@isPublic, playlistDescription=@description WHERE playlistId = @id";
                cmd.Parameters.Add(new("@id", playlistId));
                cmd.Parameters.Add(new("@name", playlistName));
                cmd.Parameters.Add(new("@duration", playlistData.Elements().First().Attribute("duration").Value));
                cmd.Parameters.Add(new("@isPublic", playlistIsPublic));
                cmd.Parameters.Add(new("@description", playlistDescription));
                cmd.ExecuteScalar();
            }
        }
        int noOfSongs = 0;
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT * FROM tblPlaylistSongLink WHERE playlistId = @id";
            cmd.Parameters.Add(new("@id", playlistId));
            using SQLiteDataReader reader = cmd.ExecuteReader() ;
            while (reader.Read()) noOfSongs++;
        }

        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "DELETE FROM tblPlaylistSongLink WHERE playlistId = @id";
            cmd.Parameters.Add(new("@id", playlistId));
            cmd.ExecuteScalar();
        }
        //request to remove all songs from playlist
        for(int i=0; i< noOfSongs; i++){
            req.sendRequestAsync("updatePlaylist", $"&playlistId={playlistId}&songIndexToRemove={i}");
        }
        int index = 0;
        string songId = string.Empty;
        foreach (string songName in lsPlaylistEditSongs.Items){
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songID FROM tblSongs WHERE songName = @name";
                cmd.Parameters.Add(new("@name", songName));
                songId = cmd.ExecuteScalar().ToString();
            }
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblPlaylistSongLink (playlistId,songId,songIndex) VALUES (@playlistId, @songId, @index) ";
                cmd.Parameters.Add(new("@playlistId", playlistId));
                cmd.Parameters.Add(new("@songId", songId));
                cmd.Parameters.Add(new("@index", index));
                cmd.ExecuteScalar();
            }
            //send request to add in the new song
            await req.sendRequestAsync("updatePlaylist",$"&playlistId={playlistId}&songIdToAdd={songId}");
            index++;
        }
        lsPlaylistsSongs.SelectedItem = null;
        lsPlaylists.Items.Clear();
        lsPlaylistsSongs.Items.Clear();
        displayPlaylists();
        cobPlaylists.ItemsSource = getSourceNamesFromType("Playlist");
        grdPlaylistEdit.Visibility = Visibility.Hidden;
    }
    private async void btnEditPlaylist_Click(object sender, RoutedEventArgs e){
        txtPlaylistName.Text = lsPlaylists.SelectedItem.ToString();
        playlistEdited = lsPlaylists.SelectedItem.ToString();
        lsPlaylistEditSongs.Items.Clear();
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT playlistDescription FROM tblPlaylists WHERE playlistName = @name";
            cmd.Parameters.Add(new("@name", lsPlaylists.SelectedItem));
            txtPlaylistDescription.Text = cmd.ExecuteScalar().ToString();
        }
        string playlistId = string.Empty;
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT playlistId FROM tblPlaylists WHERE playlistName = @name";
            cmd.Parameters.Add(new("@name", lsPlaylists.SelectedItem.ToString()));
            playlistId = cmd.ExecuteScalar().ToString();
        }
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT songId FROM tblPlaylistSongLink WHERE playlistId = @id ORDER BY songIndex ASC";
            cmd.Parameters.Add(new("@id", playlistId));
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()){
                lsPlaylistEditSongs.Items.Add(await getSongName(reader.GetString(0)));
            }
        }
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "DELETE FROM tblPlaylists WHERE playlistName = @name";
            cmd.Parameters.Add(new("@name", lsPlaylists.SelectedItem));
        }
        grdPlaylistEdit.Visibility = Visibility.Visible;
    }
    private void btnPlaylistUp_Click(object sender, RoutedEventArgs e){
        int selectedIndex = lsPlaylistEditSongs.SelectedIndex;
        if(selectedIndex > 0){
            (lsPlaylistEditSongs.Items[selectedIndex], lsPlaylistEditSongs.Items[selectedIndex + 1]) = (lsPlaylistEditSongs.Items[selectedIndex + 1], lsPlaylistEditSongs.Items[selectedIndex]);
        }
    }
    private void btnPlaylistDelete_Click(object sender, RoutedEventArgs e){
        lsPlaylistEditSongs.Items.Remove(lsPlaylistEditSongs.SelectedItem);
    }
    private void btnPlaylistDown_Click(object sender, RoutedEventArgs e){
        int selectedIndex = lsPlaylistEditSongs.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < lsPlaylistEditSongs.Items.Count){
            (lsPlaylistEditSongs.Items[selectedIndex], lsPlaylistEditSongs.Items[selectedIndex - 1]) = (lsPlaylistEditSongs.Items[selectedIndex - 1], lsPlaylistEditSongs.Items[selectedIndex]);
        }
    }
    private void btnAddSong_Click(object sender, RoutedEventArgs e){
        string songName = txtSongToAdd.Text;
        txtSongToAdd.Text = "";
        string songId = string.Empty;
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT songId FROM tblSongs WHERE songName = @name";
            cmd.Parameters.Add(new("@name", songName));
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()){
                songId = reader.GetString(0);
            }
        }
        if (songId != string.Empty){
            lsPlaylistEditSongs.Items.Add(songName);
        }
    }
    private void lsPlaylistsSongs_SelectionChanged(object sender, SelectionChangedEventArgs e){
        if(lsPlaylistsSongs.SelectedItem != null){
            btnPlayPlaylistSong.Visibility = Visibility.Visible;
            btnQueuePlaylistSong.Visibility = Visibility.Visible;
        }else{
            btnPlayPlaylistSong.Visibility = Visibility.Hidden;
            btnQueuePlaylistSong.Visibility = Visibility.Hidden;
        }
    }
    private void btnPlayPlaylist_Click(object sender, RoutedEventArgs e){
        if (lsPlaylists.SelectedItem != null){
            playingSourceType = "Playlist";
            playingSourceId = getSourceIdFromName(lsPlaylists.SelectedItem.ToString(), "Playlist");
            playSongsFromList(getSongIdsFromSourceId(playingSourceId, "Playlist"));
        }
    }
    private void btnQueuePlaylist_Click(object sender, RoutedEventArgs e){
        if(lsPlaylists.SelectedItem != null){
            List<string> songIds = getSongIdsFromSourceId(getSourceIdFromName(lsPlaylists.SelectedItem.ToString(), "Playlist"), "Playlist");
            foreach (string songId in songIds){
                queue.Enqueue(songId);
            }
            updateQueue(songIds.ToArray());
        }
    }
    private void btnPlayPlaylistSong_Click(object sender, RoutedEventArgs e){
        if (lsPlaylists.SelectedItem != null){
            playingSourceType = "Playlist";
            playingSourceId = getSourceIdFromName(lsPlaylists.SelectedItem.ToString(), "Playlist");
            playSongsFromList(getSongIdsFromSourceId(playingSourceId, "Playlist"));
        }
    }
    private void btnQueuePlaylistSong_Click(object sender, RoutedEventArgs e){
        queue.Enqueue(getSourceIdFromName(lsPlaylistsSongs.SelectedItem.ToString(), "Song"));
        updateQueue(new string[] { getSourceIdFromName(lsPlaylistsSongs.SelectedItem.ToString(), "Song") });
    }
    private void cobPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e){
        if(cobPlaylists.SelectedItem != null && lsAlbums.SelectedItem != null){
            btnAddAlbumToPlaylist.Visibility = Visibility.Visible;
        }
        else if (cobPlaylists.SelectedItem != null && lsSongs.SelectedItem != null){
            btnAddSongToPlaylist.Visibility = Visibility.Visible;
        }else{
            btnAddAlbumToPlaylist.Visibility = Visibility.Hidden;
            btnAddSongToPlaylist.Visibility = Visibility.Hidden;
        }
    }
    private async void btnAddAlbumToPlaylist_Click(object sender, RoutedEventArgs e){
        string playlistId = getSourceIdFromName(cobPlaylists.SelectedItem.ToString(), "Playlist");
        string albumId = getSourceIdFromName(lsAlbums.SelectedItem.ToString(), "Album");
        List<string> songIds = new();
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT songId FROM tblAlbumSongLink WHERE albumId = @id";
            cmd.Parameters.Add(new("@id", albumId));
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()){
                songIds.Add(reader.GetString(0));
            }
        }
        foreach(string songId in songIds){
            await req.sendRequestAsync("updatePlaylist", $"&playlistId={playlistId}&songIdToAdd={songId}");
            int index = 0;
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songIndex from tblPlaylistSongLink";
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read()) index++;
            }
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblPlaylistSongLink (playlistId, songId, songIndex) VALUES (@playlistId, @songId, @index)";
                cmd.Parameters.Add(new("@playlistId", playlistId));
                cmd.Parameters.Add(new("@songId", songId));
                cmd.Parameters.Add(new("@index", index));
                cmd.ExecuteScalar();
            }
        }
    }
    private async void btnAddSongToPlaylist_Click(object sender, RoutedEventArgs e){
        string playlistId = getSourceIdFromName(cobPlaylists.SelectedItem.ToString(), "Playlist");
        string songId = getSourceIdFromName(lsSongs.SelectedItem.ToString(), "Song");
        await req.sendRequestAsync("updatePlaylist", $"&playlistId={playlistId}&songIdToAdd={songId}");
        int index = 0;
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "SELECT songIndex from tblPlaylistSongLink";
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read()) index++;
        }
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "INSERT INTO tblPlaylistSongLink (playlistId, songId, songIndex) VALUES (@playlistId, @songId, @index)";
            cmd.Parameters.Add(new("@playlistId", playlistId));
            cmd.Parameters.Add(new("@songId", songId));
            cmd.Parameters.Add(new("@index", index));
            cmd.ExecuteScalar();
        }
    }
    private async void btnDeletePlaylist_Click(object sender, RoutedEventArgs e){
        if (MessageBox.Show("Are you sure?\n This is a permanent change","Confirm",MessageBoxButton.OKCancel) == MessageBoxResult.OK){
            //Delete playlist
            string playlistId = getSourceIdFromName(txtPlaylistName.Text, "Playlist");
            await req.sendRequestAsync("deletePlaylist", $"&id={playlistId}");
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "DELETE FROM tblPlaylists WHERE playlistId = @playlistId;" +
                    " DELETE FROM tblPlaylistSongLink WHERE playlistId = @playlistId;" +
                    " DELETE FROM tblPlaylistUserLink WHERE playlistId = @playlistId";
                cmd.Parameters.Add(new("@playlistId", playlistId));
                cmd.ExecuteScalar();
            }
            lsPlaylists.Items.Remove(txtPlaylistName.Text);
            txtPlaylistName.Text = "";
            txtPlaylistDescription.Text = "";
            lsPlaylistEditSongs.Items.Clear();
            lsPlaylistsSongs.SelectedItem = null;
            lsPlaylists.Items.Clear();
            lsPlaylistsSongs.Items.Clear();
            displayPlaylists();
            grdAlbums.Visibility = Visibility.Hidden;
        }
    }
}