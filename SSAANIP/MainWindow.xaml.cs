using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;
using System.Reflection;
using System.Diagnostics.Eventing.Reader;
using System.ComponentModel;

namespace SSAANIP {
    public partial class MainWindow : Page {
        readonly RequestMethods req;
        public master parent;
        public string connectionString = "Data source=tracks.db";
        string selectedArtistId;
        string selectedAlbumId;
        string selectedSongId;
        string[] currentSongIds;
        Queue<int> songIndex = new();
        public Queue<string> queue = new();
        public Queue<string> shuffledUpNext = new();
        public Stack<string> prevPlayed = new();
        public Boolean isPaused = false;
        public string currentSongId;
        public string playingAlbumId;
        public Boolean updatePosition = false;
        public bool isChangedByProgram = false;
        public int currentSongDuration;
        public int loopMode = 0;  //0=no loop, 1=regular loop, 2=loop current song
        public Boolean fromQueue = false;
        public Boolean isShuffled = false;
        int currentSongIndex;

        public MainWindow(master master, string username, string password) {
            InitializeComponent();
            parent = master;

            
            req = new(username, password);
            if (!File.Exists("tracks.db")) { //checks if file exists
                File.Create("tracks.db");
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "CREATE TABLE \"tblAlbumlink\" (\r\n\t\"linkId\"\tTEXT,\r\n\t\"artistId\"\tTEXT,\r\n\t\"AlbumId\"\tTEXT,\r\n\tPRIMARY KEY(\"linkId\"),\r\n\tFOREIGN KEY(\"AlbumId\") REFERENCES \"tblAlbums\"(\"albumID\"),\r\n\tFOREIGN KEY(\"artistId\") REFERENCES \"tblArtists\"(\"artistId\")\r\n);CREATE TABLE \"tblAlbums\" (\r\n\t\"albumID\"\tTEXT,\r\n\t\"albumName\"\tTEXT,\r\n\t\"albumDuration\"\tTEXT,\r\n\tPRIMARY KEY(\"albumID\")\r\n);CREATE TABLE \"tblArtists\" (\r\n\t\"artistId\"\tTEXT,\r\n\t\"artistName\"\tTEXT,\r\n\tPRIMARY KEY(\"artistId\")\r\n);CREATE TABLE \"tblSongs\" (\r\n\t\"songId\"\tTEXT,\r\n\t\"songName\"\tTEXT,\r\n\t\"songDuration\"\tTEXT,\r\n\t\"albumId\"\tTEXT,\r\n\t\"songIndex\"\tTEXT,\r\n\tPRIMARY KEY(\"songId\"),\r\n\tFOREIGN KEY(\"albumId\") REFERENCES \"tblAlbums\"(\"albumID\")\r\n)";
                    cmd.ExecuteScalar();
                }
                updateDB();
            }
            displayArtists();
        }

        public async void nextSong(){
            fromQueue = false;
            isPaused = false;
            btnPlayPause.Content = "||";
            if (loopMode == 2){
                mediaElement.Position = new TimeSpan(0);
            }
            else if(queue.Count > 0){
                fromQueue = true;
                currentSongId = queue.Dequeue();
                mediaElement.Source = new Uri(req.createUrl(currentSongId));
                lblSongPlaying.Content = await getSongName(currentSongId);
            }
            else if (songIndex.Count > 0 ){
                currentSongIndex = songIndex.Dequeue();
                currentSongId = currentSongIds[currentSongIndex];
                mediaElement.Source = new Uri(req.createUrl(currentSongId));
                lblSongPlaying.Content = await getSongName(currentSongId);
            }
            else if(songIndex.Count == 0 && loopMode == 1){
                playAlbum();
            }
            sdrPosition.Visibility = Visibility.Visible;
            updatePosition = false;
            updateSdr();
        }

        public void playAlbum(){

            List<string> songIds = getSongIdsFromAlbumId(playingAlbumId);

            songIndex.Clear();
            for (int i = 0; i < songIds.Count; i++){
                songIndex.Enqueue(i);
            }
            if (isShuffled){
                songIndex = shuffle(songIndex);
            }
            currentSongIds = songIds.ToArray();
            nextSong();
            mediaElement.Play();
        }


        public List<string> getSongIdsFromAlbumId(string albumId){
            List<string> songIds = new();

            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT songID FROM tblSongs WHERE albumID = @id";
                cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
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
                cmd.Parameters.Add(new SQLiteParameter("@id", tempSongId));
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
            var songData = await req.Browsing("getSong", songId);
            return songData.Elements().First().Attribute("title").Value.ToString();
        }

        public static Queue<int> shuffle(Queue<int> Queue){
            int[] Array = Queue.ToArray();
            Random rng = new();
            int length = Array.Length;
            for(int i=0; i < length; i++){
                int ranNum = rng.Next(0,length);
                var temp = Array[i];
                Array[i] = Array[ranNum];
                Array[ranNum] = temp;
            }
            Queue.Clear();

            for (int i = 0; i < Array.Length; i++){
                Queue.Enqueue(Array[i]);
            }
            return Queue;
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

            List<string> songNames = new ();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songName FROM tblSongs WHERE albumId = @id";
                cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
                using SQLiteDataReader reader = cmd.ExecuteReader(); {
                    while (reader.Read()) {
                        songNames.Add(reader.GetString(0));
                    }
                }
            }
            lsSongs.ItemsSource = songNames;
        }


        // Methods to fetch data from server to local database
        public async void updateDB(){
            await req.sendStartScan();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "DELETE FROM tblAlbumLink;DELETE FROM tblAlbums;DELETE FROM tblArtists;DELETE FROM tblSongs;";
                cmd.ExecuteScalar();
            }
            updateArtists();
        }
        public async void updateArtists() {
            List<string> artistsID = new();
            IEnumerable<XElement> indexes = await req.sendGetIndexes();
            foreach (XElement element in indexes.Elements().Elements().Elements()) { //get every artist id
                artistsID.Add(element.FirstAttribute.Value);
            }

            foreach (string id in artistsID) { //gets the artist name
                IEnumerable<XElement> responce = await req.sendGetArtist(id);
                string artistName = responce.Elements().ElementAt(0).FirstAttribute.NextAttribute.Value;
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblArtists VALUES (@id,@name)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", id));
                    cmd.Parameters.Add(new SQLiteParameter("@name", artistName));
                    cmd.ExecuteScalar();
                }
                updateAlbums(id);
            }
        }
        public async void updateAlbums(string artistID) {
            var artistData = await req.sendGetArtist(artistID);
            foreach (XElement album in artistData.Elements().Elements()) {
                string currentAlbumId = album.FirstAttribute.Value.ToString();
                int newLinkId = 1;

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbums VALUES (@id,@name,@duration)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", currentAlbumId));
                    cmd.Parameters.Add(new SQLiteParameter("@name", album.Attribute("name").Value));
                    cmd.Parameters.Add(new SQLiteParameter("@duration", album.Attribute("duration").Value));

                    cmd.ExecuteScalar();
                }

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT linkId FROM tblAlbumLink ORDER BY linkId DESC";
                    try {
                        newLinkId = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                    }
                    catch { }
                }

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbumLink VALUES (@id,@artistId,@albumId)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", newLinkId.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter("@artistId", artistID));
                    cmd.Parameters.Add(new SQLiteParameter("@albumId", currentAlbumId));
                    cmd.ExecuteScalar();
                }
                updateTracks(currentAlbumId);
            }
        }
        public async void updateTracks(string albumID) {
            var albumData = await req.sendGetAlbum(albumID);
            int index = 0;
            foreach (XElement track in albumData.Elements().Elements()) {

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblSongs VALUES (@id,@name,@duration,@albumId,@index)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", track.FirstAttribute.Value));
                    cmd.Parameters.Add(new SQLiteParameter("@name", track.Attribute("title").Value));
                    cmd.Parameters.Add(new SQLiteParameter("@duration", track.Attribute("duration").Value));
                    cmd.Parameters.Add(new SQLiteParameter("@albumId", albumID));
                    cmd.Parameters.Add(new SQLiteParameter("@index", index.ToString()));
                    cmd.ExecuteScalar();
                }
                index += 1;
            }
        }
        //Event handlers
        private void btnUpdateDb_clicked(object sender, System.Windows.RoutedEventArgs e) {
            updateDB();
            displayArtists();
        }
        private void lsArtist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            btnPlayAlbum.Visibility = Visibility.Hidden;
            btnQueueAlbum.Visibility = Visibility.Hidden;
            btnPlaySong.Visibility = Visibility.Hidden;
            btnPlaySong.Visibility = Visibility.Hidden;
            lsSongs.ItemsSource = null;
            lsAlbums.ItemsSource = null;

            string selectedArtist = (sender as ListBox).SelectedItem.ToString();
            selectedArtistId = string.Empty;
            List<string> albumNames = new();
            List<string> albumIds = new();

            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT artistId FROM tblArtists WHERE artistName = @name";
                cmd.Parameters.Add(new SQLiteParameter("@name", selectedArtist));
                selectedArtistId = cmd.ExecuteScalar().ToString();
            }

            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT albumID FROM tblAlbumLink WHERE artistId = @id";
                cmd.Parameters.Add(new SQLiteParameter("@id", selectedArtistId));
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
                cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
                albumNames.Add(cmd.ExecuteScalar().ToString());
                
            }

            lsAlbums.ItemsSource = albumNames;
        }
        private void lsAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            btnPlayAlbum.Visibility = Visibility.Visible;
            btnQueueAlbum.Visibility = Visibility.Visible;
            btnPlaySong.Visibility = Visibility.Hidden;
            btnPlaySong.Visibility = Visibility.Hidden;
            if ((sender as ListBox).SelectedItem != null){
                string selectedAlbumName = (sender as ListBox).SelectedItem.ToString();

                List<string> albumIds = new();

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT albumId FROM tblAlbums WHERE albumName = @name";
                    cmd.Parameters.Add(new SQLiteParameter("@name", selectedAlbumName));
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
                    cmd.CommandText = "SELECT artistID FROM tblAlbumLink WHERE albumID = @id";
                    cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
                    object result = cmd.ExecuteScalar();
                    if (result.ToString() == selectedArtistId){
                        this.selectedAlbumId = albumId;
                        this.playingAlbumId = albumId;
                        displayAlbums(albumId);
                    }
                }
            }
        }
        private void lsSongs_SelectionChanged(object sender, SelectionChangedEventArgs e){
            btnPlaySong.Visibility = Visibility.Visible;
            btnQueueSong.Visibility = Visibility.Visible;
            if(lsSongs.SelectedItem != null){
                string currentSongName = lsSongs.SelectedItem.ToString();
                using SQLiteConnection conn = new(connectionString);
                using SQLiteCommand cmd = conn.CreateCommand(); {
                    conn.Open();
                    cmd.CommandText = "SELECT songID FROM tblSongs WHERE songName = @name";
                    cmd.Parameters.Add(new SQLiteParameter("@name", currentSongName));
                    selectedSongId = cmd.ExecuteScalar().ToString();
                }
            }

        }
        private void btnPlayAlbum_clicked(object sender, RoutedEventArgs e){
            playAlbum();
        }
        private void btnQueueAlbum_Click(object sender, RoutedEventArgs e){
            List<string> songIds = getSongIdsFromAlbumId(playingAlbumId);
            foreach (string songId in songIds){
                queue.Enqueue(songId);
            }
        }
        private void btnPlaySong_Click(object sender, RoutedEventArgs e){
            int selectedSongIndex;
            using (SQLiteConnection conn = new(connectionString))
            using (SQLiteCommand cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songIndex FROM tblSongs WHERE songId = @id";
                cmd.Parameters.Add(new SQLiteParameter("@id",selectedSongId));
                selectedSongIndex = Convert.ToInt32(cmd.ExecuteScalar());
            }
            playingAlbumId = selectedAlbumId;
            List<string> currentSongIdsList = getSongIdsFromAlbumId(playingAlbumId);
            currentSongIds = currentSongIdsList.ToArray();
            songIndex.Clear();
            for(int i = selectedSongIndex; i < currentSongIds.Length; i++){
                songIndex.Enqueue(i);
            }
            nextSong();
            mediaElement.Play();
            if(isShuffled) songIndex = shuffle(songIndex);
        }
        private void btnQueueSong_Click(object sender, RoutedEventArgs e){
            queue.Enqueue(currentSongId);
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e){
            if (!fromQueue){
            prevPlayed.Push(currentSongId);
            }
            nextSong();
        }
        private void btnNextSong_click(object sender, RoutedEventArgs e){
            prevPlayed.Push(currentSongId);
            nextSong();
        }
        private void btnPrevSong_Click(object sender, RoutedEventArgs e){
            if(prevPlayed.Count > 0){
                songIndex.Enqueue(currentSongIndex);
                queue.Enqueue(prevPlayed.Pop());
                nextSong();
            }
        }
        private void btnPlayPause_Click(object sender, RoutedEventArgs e){
            if(mediaElement.Source != null){
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
            updatePosition = false;
            mediaElement.Stop();
            //upNext.Clear();
            queue.Clear();
            prevPlayed.Clear();
            btnPlayPause.Content = "⏵";
            lblSongPlaying.Content = "";
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
            if(loopMode != 2){
                loopMode += 1;
            }
            else{
                loopMode = 0;
            }

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
            if (isShuffled){
                isShuffled = false;
                btnShuffleToggle.Background = Brushes.Red;
            }
            else if(loopMode != 1){
                isShuffled = true;
                btnShuffleToggle.Background = Brushes.GreenYellow;
                songIndex = shuffle(songIndex);
            }
            else{
                isShuffled = true;
                btnShuffleToggle.Background = Brushes.GreenYellow;
                int length = songIndex.Count;
                songIndex.Clear();
                for(int i=0; i < length; i++){
                    if(i != currentSongIndex) songIndex.Enqueue(i);
                }
                songIndex = shuffle(songIndex);
            }
        }

    }
}