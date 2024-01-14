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

namespace SSAANIP {
    public partial class MainWindow : Page {
        readonly fetchData fetch;
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
        public Boolean isPaused = true;
        public string currentSongId;
        public string playingAlbumId;
        public Boolean updatePosition = false;
        public bool isChangedByProgram = false;
        public int currentSongDuration;
        public int loopMode = 0;  //0=no loop, 1=regular loop, 2=loop current song
        public Boolean fromQueue = false;
        public Boolean isShuffled = false;
        int currentSongIndex;
        string prioNextUp = null;
        string[] queueNames;
        public MainWindow(master master,RequestMethods req) {
            InitializeComponent();
            parent = master;
            this.req = req;
            fetch = new(connectionString, req);

            req.sendCreateUser("test1231231","test","false");

            if (!File.Exists("tracks.db")) { //checks if file exists
                File.Create("tracks.db");
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
                lblSongPlaying.Content = await getSongName(currentSongId);
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
            updatePosition = false;
            updateSdr();

            displayQueue();
        }
        public async void displayQueue(){
            string[] queueIds = queue.ToArray();
            queueNames = await getNamesFromIDs(queueIds);
            lsQueue.ItemsSource = queueNames;
            btnClearQueue.Visibility = Visibility.Visible;

        }
        public async Task<string[]> getNamesFromIDs(string[] ids){
            List<string> names = new();
            for (int i = 0; i < ids.Length; i++){
                if(ids[i]!=null) names.Add(await getSongName(ids[i]));
            }
            return names.ToArray();
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
            using (var cmd = conn.CreateCommand()){
                conn.Open();

                cmd.CommandText = "SELECT songID FROM tblAlbumSongLink WHERE albumID = @id";
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

        public void stop(){
            updatePosition = false;
            mediaElement.Stop();
            queue.Clear();
            prevPlayed.Clear();
            btnPlayPause.Content = "⏵";
            lblSongPlaying.Content = "";
            lsQueue.ItemsSource = null;
            mediaElement.Source = null;
            lblTime.Content = "";
            sdrPosition.Value = 0;
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
                cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
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
                    cmd.Parameters.Add(new SQLiteParameter("@id", s));
                    using SQLiteDataReader reader = cmd.ExecuteReader();{
                        while (reader.Read()){
                            songNames.Add(reader.GetString(0));
                        }
                    }
                }
            }
            lsSongs.ItemsSource = songNames;
        }


        //Event handlers
        private void btnUpdateDb_clicked(object sender, System.Windows.RoutedEventArgs e) {
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
                    cmd.Parameters.Add(new SQLiteParameter("@name", selectedArtist));
                    selectedArtistId = cmd.ExecuteScalar().ToString();
                }
            }


            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT albumID FROM tblAlbumArtistLink WHERE artistId = @id";
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
            btnPlaySong.Visibility = Visibility.Hidden;
            btnQueueSong.Visibility = Visibility.Hidden;
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
                    cmd.CommandText = "SELECT artistID FROM tblAlbumArtistLink WHERE albumID = @id";
                    cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
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
                    cmd.Parameters.Add(new SQLiteParameter("@name", currentSongName));
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
            List<string> songIds = getSongIdsFromAlbumId(playingAlbumId);
            foreach (string songId in songIds){
                queue.Enqueue(songId);
            }
            displayQueue();
        }
        private void btnPlaySong_Click(object sender, RoutedEventArgs e){
            int selectedSongIndex;
            currentSongId = selectedSongId;
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
            for(int i = selectedSongIndex+1; i < currentSongIds.Length; i++){
                songIndex.Enqueue(i);
            }
            prioNextUp = selectedSongId;
            nextSong();
            mediaElement.Play();
            if(isShuffled) songIndex = shuffle(songIndex);
            btnPlayPause.Content = "||";
            isPaused = false;
        }
        private void btnQueueSong_Click(object sender, RoutedEventArgs e){
            queue.Enqueue(selectedSongId);
            displayQueue();
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
        private void lsQueue_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if(lsQueue.SelectedItem == null){
                btnQueueUp.Visibility = Visibility.Hidden;
                btnQueueDelete.Visibility = Visibility.Hidden;
                btnQueueDown.Visibility = Visibility.Hidden;
            }
            else{
                btnQueueUp.Visibility = Visibility.Visible;
                btnQueueDelete.Visibility = Visibility.Visible;
                btnQueueDown.Visibility = Visibility.Visible;
            }
        }
        private void btnQueueUp_Click(object sender, RoutedEventArgs e){
            string[] queueIds = queue.ToArray();
            if(lsQueue.SelectedIndex > 0){
                int selectedIndex = lsQueue.SelectedIndex;
                (queueIds[selectedIndex], queueIds[selectedIndex - 1]) = (queueIds[selectedIndex - 1], queueIds[selectedIndex]);
                (queueNames[selectedIndex], queueNames[selectedIndex - 1]) = (queueNames[selectedIndex - 1], queueNames[selectedIndex]);
                lsQueue.ItemsSource = queueNames;

                queue.Clear();
                foreach (string id in queueIds){
                    if(id!=null) queue.Enqueue(id);
                }
            }
        }
        private async void btnQueueDown_Click(object sender, RoutedEventArgs e){
            string[] queueIds = queue.ToArray();
            if (lsQueue.SelectedIndex > 0 && lsQueue.SelectedIndex < queue.Count-1){
                int selectedIndex = lsQueue.SelectedIndex;
                (queueIds[selectedIndex], queueIds[selectedIndex + 1]) = (queueIds[selectedIndex + 1], queueIds[selectedIndex]);
                (queueNames[selectedIndex], queueNames[selectedIndex + 1]) = (queueNames[selectedIndex + 1], queueNames[selectedIndex]);
                lsQueue.ItemsSource = queueNames;
                queue.Clear();
                foreach (string id in queueIds){
                    queue.Enqueue(id);
                }

            }
        }
        private async void btnQueueDelete_Click(object sender, RoutedEventArgs e){
            string[] queueIds = queue.ToArray();

            if (lsQueue.SelectedIndex > 0){
                int selectedIndex = lsQueue.SelectedIndex;
                for(int i = selectedIndex; i < queueIds.Length-1; i++){
                    queueIds[i] = queueIds[i+1];
                    queueNames[i] = queueNames[i+1];
                }
                queueIds[queueIds.Length-1] = null;
                queue.Clear();
                foreach (string id in queueIds){
                    queue.Enqueue(id);
                }
                lsQueue.ItemsSource = queueNames;
            }
        }
        private void btnUserManage_Click(object sender, RoutedEventArgs e){
            stop();
            parent.Frame.Content = new userMgmt(parent, req);
        }
        private void btnClearQueue_Click(object sender, RoutedEventArgs e){
            queue.Clear();
            lsQueue.ItemsSource = null;
        }
    }
}