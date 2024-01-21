using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Media;
namespace SSAANIP {
    public partial class MainWindow : Page {
        protected readonly fetchData fetch;
        protected readonly RequestMethods req;
        protected master parent;
        protected string connectionString = "Data source=data.db";
        protected string selectedArtistId;
        protected string selectedAlbumId;
        protected string selectedSongId;
        protected string[] currentSongIds;
        protected int currentSongIndex;
        protected Queue<int> songIndex = new();
        protected Queue<string> queue = new();
        protected Queue<string> shuffledUpNext = new();
        protected Stack<string> prevPlayed = new();
        protected Boolean isPaused = true;
        protected string currentSongId;
        protected string playingAlbumId;
        protected Boolean updatePosition = false;
        protected bool isChangedByProgram = false;
        protected int currentSongDuration;
        protected int loopMode = 0;  //0=no loop, 1=regular loop, 2=loop current song
        protected Boolean fromQueue = false;
        protected string prioNextUp = null;
        public MainWindow(master master,RequestMethods req) {
            InitializeComponent();
            parent = master;
            this.req = req;
            fetch = new(connectionString, req);
            fetch.updateUsers();


            if (!File.Exists("data.db")) { //checks if file exists
                File.Create("data.db");
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "CREATE TABLE \"tblAlbumArtistlink\" (\r\n\t\"linkId\"\tTEXT,\r\n\t\"artistId\"\tTEXT,\r\n\t\"AlbumId\"\tTEXT,\r\n\tPRIMARY KEY(\"linkId\"),\r\n\tFOREIGN KEY(\"AlbumId\") REFERENCES \"tblAlbums\"(\"albumID\"),\r\n\tFOREIGN KEY(\"artistId\") REFERENCES \"tblArtists\"(\"artistId\")\r\n);CREATE TABLE \"tblAlbums\" (\r\n\t\"albumID\"\tTEXT,\r\n\t\"albumName\"\tTEXT,\r\n\t\"albumDuration\"\tTEXT,\r\n\tPRIMARY KEY(\"albumID\")\r\n);CREATE TABLE \"tblArtists\" (\r\n\t\"artistId\"\tTEXT,\r\n\t\"artistName\"\tTEXT,\r\n\tPRIMARY KEY(\"artistId\")\r\n);CREATE TABLE \"tblSongs\" (\r\n\t\"songId\"\tTEXT,\r\n\t\"songName\"\tTEXT,\r\n\t\"songDuration\"\tTEXT,\r\n\t\"albumId\"\tTEXT,\r\n\t\"songIndex\"\tTEXT,\r\n\tPRIMARY KEY(\"songId\"),\r\n\tFOREIGN KEY(\"albumId\") REFERENCES \"tblAlbums\"(\"albumID\")\r\n)CREATE TABLE \"tblPlaylistAlbumLink\" (\r\n\t\"linkId\"\tINTEGER,\r\n\t\"playlistId\"\tTEXT,\r\n\t\"artistID\"\tTEXT,\r\n\tPRIMARY KEY(\"linkId\" AUTOINCREMENT),\r\n\tFOREIGN KEY(\"artistID\") REFERENCES \"tblAlbums\"(\"albumId\"),\r\n\tFOREIGN KEY(\"playlistId\") REFERENCES \"tblPlaylists\"(\"playlistId\")\r\n)CREATE TABLE \"tblPlaylistSongLink\" (\r\n\t\"linkId\"\tINTEGER,\r\n\t\"playlistId\"\tTEXT,\r\n\t\"songId\"\tTEXT,\r\n\tPRIMARY KEY(\"linkId\" AUTOINCREMENT),\r\n\tFOREIGN KEY(\"songId\") REFERENCES \"tblSongs\"(\"songId\"),\r\n\tFOREIGN KEY(\"playlistId\") REFERENCES \"tblPlaylists\"(\"playlistId\")\r\n);CREATE TABLE \"tblPlaylists\" (\r\n\t\"playlistId\"\tTEXT,\r\n\t\"playlistName\"\tTEXT,\r\n\t\"playlistDuration\"\tTEXT,\r\n\tPRIMARY KEY(\"playlistId\")\r\n)";
                    cmd.ExecuteScalar();
                }
            }
            //fetch.updateDB();
            displayArtists();
        }
        public async void nextSong(){
            fromQueue = false;
            if (loopMode == 2){
                mediaElement.Position = new TimeSpan(0);
            }
            else if(prioNextUp != null){
                mediaElement.Source = new Uri(req.createUrl(prioNextUp));
                prioNextUp = null;
                lblSongPlaying.Content = await getSongName(currentSongId) + " / " + await getArtistNameFromSong(currentSongId);
            }
            else if(queue.Count > 0){
                fromQueue = true;
                currentSongId = queue.Dequeue();
                mediaElement.Source = new Uri(req.createUrl(currentSongId));
                lblSongPlaying.Content = await getSongName(currentSongId) + " / " + await getArtistNameFromSong(currentSongId);
                lsQueue.Items.RemoveAt(0);
            }
            else if (songIndex.Count > 0 ){
                currentSongIndex = songIndex.Dequeue();
                currentSongId = currentSongIds[currentSongIndex];
                mediaElement.Source = new Uri(req.createUrl(currentSongId));
                lblSongPlaying.Content = await getSongName(currentSongId) + " / " + await getArtistNameFromSong(currentSongId);
            }
            else if(songIndex.Count == 0 && loopMode == 1){
                playAlbum();
            }
            updatePosition = false;
            updateSdr();
        }
        public void updateQueue(string[] newSongIds){
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
        public void playAlbum(){

            List<string> songIds = getSongIdsFromAlbumId(playingAlbumId);

            songIndex.Clear();
            for (int i = 0; i < songIds.Count; i++){
                songIndex.Enqueue(i);
            }
            if (btnShuffleToggle.Background == Brushes.GreenYellow){
                songIndex = shuffle(songIndex);
            }
            currentSongIds = songIds.ToArray();
            nextSong();
            mediaElement.Play();
        }
        public List<string> getSongIdsFromAlbumId(string albumId){
            List<string> songIds = new();

            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();

                cmd.CommandText = "SELECT songID FROM tblAlbumSongLink WHERE albumID = @id";
                cmd.Parameters.Add(new("@id", albumId));
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read()){
                    songIds.Add(reader.GetString(0));
                }
            }
            return songIds;
        }
        public async void updateSdr(){
            string tempSongId = this.currentSongId;
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
        public async Task<string> getSongName(string songId){
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songName FROM tblSongs WHERE songId = @id";
                cmd.Parameters.Add(new("@id", songId));
                return cmd.ExecuteScalar().ToString();
            }
        }
        public async Task<string> getArtistNameFromSong(string songId){
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
        public static Queue<int> shuffle(Queue<int> Queue){
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
        public void stop(){
            updatePosition = false;
            mediaElement.Stop();
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
        public void displayArtists() {
            List<string> artistNames = new();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT artistName FROM tblArtists ORDER by artistName DESC"; //Fetch all artist's names from local DB
                using SQLiteDataReader reader = cmd.ExecuteReader(); {
                    while (reader.Read())
                    {
                        artistNames.Add(reader.GetString(0));
                    }
                }
            }
            lsArtist.ItemsSource = artistNames;
        }
        public void displayAlbums(string albumId){
            List<string> songIds = new();
            List<string> songNames = new ();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT songId FROM tblAlbumSongLink WHERE albumid = @id";
                cmd.Parameters.Add(new("@id", albumId));
                using SQLiteDataReader reader = cmd.ExecuteReader();{
                    while (reader.Read()){
                        songIds.Add(reader.GetString(0));
                    }
                }
            }

            foreach (string s in songIds){
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT songName FROM tblSongs WHERE songId = @id";
                    cmd.Parameters.Add(new("@id", s));
                    using SQLiteDataReader reader = cmd.ExecuteReader();{
                        while (reader.Read()){
                            songNames.Add(reader.GetString(0));
                        }
                    }
                }
            }
            lsSongs.ItemsSource = songNames;
        }
        private void btnUpdateDb_clicked(object sender, System.Windows.RoutedEventArgs e) {
            stop();
            lsArtist.ItemsSource = null;
            lsAlbums.ItemsSource = null;
            lsSongs.ItemsSource = null;
            fetch.updateDB();
            displayArtists();
        }
        private void lsArtist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            btnPlayAlbum.Visibility = Visibility.Hidden;
            btnQueueAlbum.Visibility = Visibility.Hidden;
            btnPlaySong.Visibility = Visibility.Hidden;
            btnQueueSong.Visibility = Visibility.Hidden;
            lsSongs.ItemsSource = null;
            lsAlbums.ItemsSource = null;
            selectedArtistId = string.Empty;
            List<string> albumNames = new();
            List<string> albumIds = new();
            if (lsArtist.SelectedItem != null){
                string selectedArtist = lsArtist.SelectedItem.ToString();
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT artistId FROM tblArtists WHERE artistName = @name";
                    cmd.Parameters.Add(new("@name", selectedArtist));
                    selectedArtistId = cmd.ExecuteScalar().ToString();
                }
            }
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT albumID FROM tblAlbumArtistLink WHERE artistId = @id";
                cmd.Parameters.Add(new("@id", selectedArtistId));
                using SQLiteDataReader reader = cmd.ExecuteReader(); {
                    while (reader.Read()) {
                        albumIds.Add(reader.GetString(0));
                    }
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
        private void lsAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            btnPlaySong.Visibility = Visibility.Hidden;
            btnQueueSong.Visibility = Visibility.Hidden;
            if ((sender as ListBox).SelectedItem != null){
                string selectedAlbumName = (sender as ListBox).SelectedItem.ToString();
                List<string> albumIds = new();
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT albumId FROM tblAlbums WHERE albumName = @name";
                    cmd.Parameters.Add(new("@name", selectedAlbumName));
                    using SQLiteDataReader reader = cmd.ExecuteReader(); {
                        while (reader.Read()){
                            albumIds.Add(reader.GetString(0));
                        }
                    }
                }
                foreach (string albumId in albumIds){
                    SQLiteConnection conn = new(connectionString);
                    var cmd = conn.CreateCommand();
                    conn.Open();
                    cmd.CommandText = "SELECT artistID FROM tblAlbumArtistLink WHERE albumID = @id";
                    cmd.Parameters.Add(new("@id", albumId));
                    object result = cmd.ExecuteScalar();
                    if (result.ToString() == selectedArtistId){
                        this.selectedAlbumId = albumId;
                        this.playingAlbumId = albumId;
                        displayAlbums(albumId);
                    }
                }
                btnPlayAlbum.Visibility = Visibility.Visible;
                btnQueueAlbum.Visibility = Visibility.Visible;
            }
        }
        private void lsSongs_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if(lsSongs.SelectedItem != null){
                string currentSongName = lsSongs.SelectedItem.ToString();
                using SQLiteConnection conn = new(connectionString);
                using SQLiteCommand cmd = conn.CreateCommand(); {
                    conn.Open();
                    cmd.CommandText = "SELECT songID FROM tblSongs WHERE songName = @name";
                    cmd.Parameters.Add(new("@name", currentSongName));
                    selectedSongId = cmd.ExecuteScalar().ToString();
                }
                btnPlaySong.Visibility = Visibility.Visible;
                btnQueueSong.Visibility = Visibility.Visible;
            }
        }
        private void btnPlayAlbum_clicked(object sender, RoutedEventArgs e){
            playAlbum();
        }
        private void btnQueueAlbum_Click(object sender, RoutedEventArgs e){
            List<string> songIds = getSongIdsFromAlbumId(selectedAlbumId);
            foreach (string songId in songIds){
                queue.Enqueue(songId);
            }
            updateQueue(songIds.ToArray());
        }
        private void btnPlaySong_Click(object sender, RoutedEventArgs e){
            int selectedSongIndex;
            currentSongId = selectedSongId;
            using (SQLiteConnection conn = new(connectionString))
            using (SQLiteCommand cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songIndex FROM tblSongs WHERE songId = @id";
                cmd.Parameters.Add(new("@id",selectedSongId));
                selectedSongIndex = Convert.ToInt32(cmd.ExecuteScalar());
            }
            playingAlbumId = selectedAlbumId;
            List<string> currentSongIdsList = getSongIdsFromAlbumId(playingAlbumId);
            currentSongIds = currentSongIdsList.ToArray();
            songIndex.Clear();
            for(int i = selectedSongIndex+1; i < currentSongIds.Length; i++){
                songIndex.Enqueue(i);
            }
            prioNextUp = currentSongId;
            nextSong();
            mediaElement.Play();
            if(btnShuffleToggle.Background == Brushes.GreenYellow) songIndex = shuffle(songIndex);
            btnPlayPause.Content = "||";
            isPaused = false;
        }
        private void btnQueueSong_Click(object sender, RoutedEventArgs e){
            queue.Enqueue(selectedSongId);
            updateQueue(new string[] {selectedSongId});
        }
        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e){
            if (!fromQueue) prevPlayed.Push(currentSongId);
            nextSong();
        }
        private void btnNextSong_click(object sender, RoutedEventArgs e){
            if (!fromQueue) prevPlayed.Push(currentSongId);
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
                currentSongId = prevPlayed.Pop();
                prioNextUp = currentSongId;
                nextSong();
            }
        }
        private void btnPlayPause_Click(object sender, RoutedEventArgs e){
            if(mediaElement.Source != null || queue.Count > 0){
                if (isPaused) { 
                    mediaElement.Play();
                    btnPlayPause.Content = "||";
                    isPaused = false;
                }
                else{
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
            }
            else{
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
            }
            else{
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
            }
        }
        private async void btnQueueDown_Click(object sender, RoutedEventArgs e){
            string[] queueIds = queue.ToArray();
            if (lsQueue.SelectedIndex >= 0 && lsQueue.SelectedIndex < queue.Count-1){
                int selectedIndex = lsQueue.SelectedIndex;
                (queueIds[selectedIndex], queueIds[selectedIndex + 1]) = (queueIds[selectedIndex + 1], queueIds[selectedIndex]);
                (lsQueue.Items[selectedIndex], lsQueue.Items[selectedIndex + 1]) = (lsQueue.Items[selectedIndex + 1], lsQueue.Items[selectedIndex]);
                queue.Clear();
                foreach (string id in queueIds){
                    queue.Enqueue(id);
                }
            }
        }
        private async void btnQueueDelete_Click(object sender, RoutedEventArgs e){
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
                grdAlbums.Visibility = Visibility.Visible;
            } else{ //toggle to playlists
                grdAlbums.Visibility = Visibility.Hidden;
            }
        }
    }
}