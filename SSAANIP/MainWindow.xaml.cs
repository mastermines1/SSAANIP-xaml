using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows;
using System.Threading.Tasks;

namespace SSAANIP {
    public partial class MainWindow : Page {
        public string username;
        public string appName;
        readonly RequestMethods req;
        public master parent;
        public string password;
        public List<string> artistNames = new List<string>();
        public string connectionString = "Data source=tracks.db";
        public List<string> albumIds;
        string selectedArtistId;
        string selectedAlbumId;
        public Queue<string> upNext = new Queue<string>();
        public Queue<string> queue = new Queue<string>();
        public Stack<string> prevPlayed = new Stack<string>();
        public Boolean isPaused = false;
        public string currentSongId;
        public string currentAlbumId;

        public MainWindow(master master, string username, string password) {
            InitializeComponent();
            parent = master;
            this.username = username;
            this.password = password;
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

            prevPlayed.Push(currentSongId);
            if(queue.Count > 0){
                currentSongId = queue.Dequeue();
                mediaElement.Source = new Uri(req.createUrl(currentSongId));
                lblSongPlaying.Content = await getSongName(currentSongId);
            }
            else if (upNext.Count > 0){ 
                currentSongId = upNext.Dequeue();
                mediaElement.Source = new Uri(req.createUrl(currentSongId));
                lblSongPlaying.Content = await getSongName(currentSongId);
            }

        }


        public async Task<string> getSongName(string songId){
            var songData = await req.Browsing("getSong", songId);
            return songData.Elements().First().Attribute("title").Value.ToString();
        }


        //Methods to display data
        public void displayArtists() {
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = "SELECT artistName FROM tblArtists ORDER by artistName DESC"; //Fetch all artist's names from local DB
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read())
                    {
                        artistNames.Add(reader.GetString(0));
                    }
                }
            }
            lsArtist.ItemsSource = artistNames;
        }
        public void displayAlbums(string albumId){

            List<string> songNames = new List<string>();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songName FROM tblSongs WHERE albumId = @id";
                cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        songNames.Add(reader.GetString(0));
                    }
                }
            }
            lsSongs.ItemsSource = songNames;
        }







        // Methods to fetch data from server to local database
        public async void updateDB(){
            var scanStatus = await req.sendStartScan();
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
            lsSongs.ItemsSource = null;
            lsAlbums.ItemsSource = null;

            string selectedArtist = (sender as ListBox).SelectedItem.ToString();
            selectedArtistId = string.Empty;
            List<string> albumNames = new List<string>();
            List<string> albumIds = new List<string>();

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
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        albumIds.Add(reader.GetString(0));
                    }
                }
            }

            foreach (string albumId in albumIds) {
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT albumName FROM tblAlbums WHERE albumId = @id ORDER by albumName DESC";
                    cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
                    albumNames.Add(cmd.ExecuteScalar().ToString());
                }
            }

            lsAlbums.ItemsSource = albumNames;
        }

        private void lsAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            btnPlayAlbum.Visibility = Visibility.Visible;
            if ((sender as ListBox).SelectedItem != null){
                string selectedAlbumName = (sender as ListBox).SelectedItem.ToString();

                List<string> albumIds = new List<string>();
                List<string> allAlbumIdsFromArtist = new List<string>();

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT albumId FROM tblAlbums WHERE albumName = @name";
                    cmd.Parameters.Add(new SQLiteParameter("@name", selectedAlbumName));
                    using (SQLiteDataReader reader = cmd.ExecuteReader()){
                        while (reader.Read()){
                            albumIds.Add(reader.GetString(0));
                        }
                    }
                }

                foreach (string albumId in albumIds){
                    using (SQLiteConnection conn = new(connectionString))
                    using (var cmd = conn.CreateCommand()){
                        conn.Open();
                        cmd.CommandText = "SELECT artistID FROM tblAlbumLink WHERE albumID = @id";
                        cmd.Parameters.Add(new SQLiteParameter("@id", albumId));
                        object result = cmd.ExecuteScalar();
                        if (result.ToString() == selectedArtistId){
                            this.selectedAlbumId=albumId;
                            displayAlbums(albumId);
                        }
                    }
                }
            }
        }

        private void playAlbum(object sender, RoutedEventArgs e){
            string selectedAlbumName = lsAlbums.SelectedItem.ToString();
            List<string> songIds = new List<string>();

            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "SELECT songID FROM tblSongs WHERE albumID = @id";
                cmd.Parameters.Add(new SQLiteParameter("@id", this.selectedAlbumId));
                using (SQLiteDataReader reader = cmd.ExecuteReader()){
                    while (reader.Read()){
                        songIds.Add(reader.GetString(0));
                    }
                }
            }
            upNext.Clear();
            for(int i = 0; i< songIds.Count; i++){
                upNext.Enqueue(songIds[i]);
            }

            nextSong();
            mediaElement.Play();
            isPaused = false;
            btnPlayPause.Content = "||";
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e){
            nextSong();
        }

        private void btnNextSong_click(object sender, RoutedEventArgs e){
            nextSong();
        }

        private void btnPrevSong_Click(object sender, RoutedEventArgs e){
            if(prevPlayed.Count > 0){
                upNext.Enqueue(currentSongId);
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
            mediaElement.Stop();
            upNext.Clear();
            queue.Clear();
            prevPlayed.Clear();
            btnPlayPause.Content = "⏵";
            lblSongPlaying.Content = "";
        }

        private void sdrVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e){
            double value = sdrVolume.Value;
        }
    }
}