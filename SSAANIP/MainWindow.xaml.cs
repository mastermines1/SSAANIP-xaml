using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace SSAANIP
{
    public partial class MainWindow : Page{
        public string socket;
        public string username;
        public string version;
        public string appName;
        readonly RequestMethods req;
        public master parent;
        private string password;
        public Dictionary<string, string> availableArtists = new();
        public Dictionary<string,Dictionary<string,string>> availableAlbums = new();
        public string connectionString = "Data source=tracks.db";
        public MainWindow(master master, string username, string password){
            InitializeComponent();
            parent = master;
            this.username = username;
            this.password = password;
            req = new(username, password);
            updateDB();
        }
        public void updateDB()
        {
            using(SQLiteConnection conn = new(connectionString))
            using(var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "DELETE FROM tblAblumlink;DELETE FROM tblAlbums;DELETE FROM tblArtists;DELETE FROM tblSongs;";
                cmd.ExecuteScalar();
            }
            getArtists();
        }
        public async void getArtists(){
            List<string> artistsID = new();
            IEnumerable<XElement> indexes = await req.sendGetIndexes();
            foreach (XElement element in indexes.Elements().Elements().Elements()){ //get every artist id
                artistsID.Add(element.FirstAttribute.Value);
            }

            foreach (string id in artistsID){ //gets the artist name
                IEnumerable<XElement> responce = await req.sendGetArtist(id);
                string artistName = responce.Elements().ElementAt(0).FirstAttribute.NextAttribute.Value;
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblArtists VALUES (@id,@name)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", id));
                    cmd.Parameters.Add(new SQLiteParameter("@name", artistName));
                    cmd.ExecuteScalar();
                }
                getAlbums(id);
            }
        }
        public async void getAlbums(string artistID){
            var artistData = await req.sendGetArtist(artistID);
            foreach (XElement album in artistData.Elements().Elements()){
                string currentAlbumId = album.FirstAttribute.Value.ToString();
                int newLinkId = 1;

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbums VALUES (@id,@name)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", currentAlbumId));
                    cmd.Parameters.Add(new SQLiteParameter("@name", album.Attribute("name").Value));
                    cmd.ExecuteScalar();
                }

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT linkId FROM tblAblumlink ORDER BY linkId DESC";
                    try{
                        newLinkId = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                    }
                    catch { }
                }

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){ 
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAblumlink VALUES (@id,@artistId,@albumId)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", newLinkId.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter("@artistId", artistID));
                    cmd.Parameters.Add(new SQLiteParameter("@albumId", currentAlbumId));
                    cmd.ExecuteScalar();
                }
                getTrack(currentAlbumId);
            }
        }
        public async void getTrack(string albumID){
            var albumData = await req.sendGetAlbum(albumID);
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblSongs VALUES (@id,@name,@duration,@albumId)";
                cmd.Parameters.Add(new SQLiteParameter("@id", newLinkId.ToString()));
                cmd.Parameters.Add(new SQLiteParameter("@artistId", artistID));
                cmd.Parameters.Add(new SQLiteParameter("@albumId", currentAlbumId));
                cmd.ExecuteScalar();
            }

        }
    }
}