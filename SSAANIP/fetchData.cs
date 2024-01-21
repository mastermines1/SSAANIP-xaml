using System.Collections.Generic;
using System.Data.SQLite;
using System.Xml.Linq;
using System.Linq;
namespace SSAANIP{
    public class fetchData{
        readonly string connectionString;
        readonly RequestMethods req;
        public fetchData(string connectionString, RequestMethods req){
            this.connectionString = connectionString;
            this.req = req;
        }
        // Methods to fetch data from server to local database
        public async void updateDB(){
            await req.sendStartScan();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "DELETE FROM tblAlbumArtistLink;DELETE FROM tblAlbumSongLink;DELETE FROM tblplaylistArtistLink;DELETE FROM tblPlaylistSongLink;DELETE FROM tblAlbums;DELETE FROM tblArtists;DELETE FROM tblSongs;DELETE FROM tblPlaylists";
                cmd.ExecuteScalar();
            }
            updateArtists();
        }
        private async void updateArtists(){
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
                    cmd.Parameters.Add(new("@id", id));
                    cmd.Parameters.Add(new("@name", artistName));
                    cmd.ExecuteScalar();
                }
                updateAlbums(id);
            }
        }
        private async void updateAlbums(string artistID){
            var artistData = await req.sendGetArtist(artistID);
            foreach (XElement album in artistData.Elements().Elements()){
                string currentAlbumId = album.FirstAttribute.Value.ToString();
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbums VALUES (@id,@name,@duration)";
                    cmd.Parameters.Add(new("@id", currentAlbumId));
                    cmd.Parameters.Add(new("@name", album.Attribute("name").Value));
                    cmd.Parameters.Add(new("@duration", album.Attribute("duration").Value));

                    cmd.ExecuteScalar();
                }
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbumArtistLink (artistId,albumId) VALUES (@artistId,@albumId)";
                    cmd.Parameters.Add(new("@artistId", artistID));
                    cmd.Parameters.Add(new("@albumId", currentAlbumId));
                    cmd.ExecuteScalar();
                }
                updateTracks(currentAlbumId);
            }
        }
        private async void updateTracks(string albumID){
            var albumData = await req.sendGetAlbum(albumID);
            int index = 0;
            foreach (XElement track in albumData.Elements().Elements()){
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblSongs VALUES (@id,@name,@duration,@index)";
                    cmd.Parameters.Add(new("@index", index));
                    cmd.Parameters.Add(new("@id", track.FirstAttribute.Value));
                    cmd.Parameters.Add(new("@name", track.Attribute("title").Value));
                    cmd.Parameters.Add(new("@duration", track.Attribute("duration").Value));
                    cmd.ExecuteScalar();
                }
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbumSongLink (albumId,songId) VALUES (@albumId,@songId)";
                    cmd.Parameters.Add(new("@albumId", albumID));
                    cmd.Parameters.Add(new("@songId", track.FirstAttribute.Value));
                    cmd.ExecuteScalar();
                }
                index += 1;
            }
        }
        public async void updateUsers(){
            IEnumerable<XElement> usersInfo;
            var authenticatedUserInfo = await req.sendGetUser(null);
            if(authenticatedUserInfo.Elements().First().Attribute("adminRole").Value.ToString() == "true"){
                usersInfo = await req.sendGetUsers();
                usersInfo = usersInfo.Elements().Elements();
                using SQLiteConnection conn = new(connectionString);
                using var cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandText = "DELETE FROM tblUsers";
                cmd.ExecuteScalar();
            } else usersInfo = authenticatedUserInfo.Elements();
            foreach(XElement user in usersInfo){
                bool alrExists = false;
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT userName FROM tblUsers WHERE userName = @username";
                    cmd.Parameters.Add(new("@username", user.Attribute("username").Value.ToString().ToLower()));
                    if (cmd.ExecuteNonQuery() != 0) alrExists = true;
                }
                if (!alrExists){
                    using (SQLiteConnection conn = new(connectionString))
                    using (var cmd = conn.CreateCommand()){
                        conn.Open();
                        cmd.CommandText = "INSERT INTO tblUsers VALUES (@userName,@isAdmin)";
                        cmd.Parameters.Add(new("@userName", user.Attribute("username").Value.ToString().ToLower()));
                        cmd.Parameters.Add(new("@isAdmin", user.Attribute("adminRole").Value.ToString()));
                        cmd.ExecuteScalar();
                    }
                }
            }
        }
    }
}