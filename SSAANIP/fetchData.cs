using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Xml.Linq;
using System.Linq;

namespace SSAANIP{
    public class fetchData{
        string connectionString;
        RequestMethods req;
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
        public async void updateArtists(){
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
                updateAlbums(id);
            }
        }
        public async void updateAlbums(string artistID){
            var artistData = await req.sendGetArtist(artistID);
            foreach (XElement album in artistData.Elements().Elements()){
                string currentAlbumId = album.FirstAttribute.Value.ToString();
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbums VALUES (@id,@name,@duration)";
                    cmd.Parameters.Add(new SQLiteParameter("@id", currentAlbumId));
                    cmd.Parameters.Add(new SQLiteParameter("@name", album.Attribute("name").Value));
                    cmd.Parameters.Add(new SQLiteParameter("@duration", album.Attribute("duration").Value));

                    cmd.ExecuteScalar();
                }

                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbumArtistLink (artistId,albumId) VALUES (@artistId,@albumId)";
                    cmd.Parameters.Add(new SQLiteParameter("@artistId", artistID));
                    cmd.Parameters.Add(new SQLiteParameter("@albumId", currentAlbumId));
                    cmd.ExecuteScalar();
                }
                updateTracks(currentAlbumId);
            }
        }
        public async void updateTracks(string albumID){
            var albumData = await req.sendGetAlbum(albumID);
            int index = 0;
            foreach (XElement track in albumData.Elements().Elements()){
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblSongs VALUES (@id,@name,@duration,@index)";
                    cmd.Parameters.Add(new SQLiteParameter("@index", index));
                    cmd.Parameters.Add(new SQLiteParameter("@id", track.FirstAttribute.Value));
                    cmd.Parameters.Add(new SQLiteParameter("@name", track.Attribute("title").Value));
                    cmd.Parameters.Add(new SQLiteParameter("@duration", track.Attribute("duration").Value));
                    cmd.ExecuteScalar();
                }
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblAlbumSongLink (albumId,songId) VALUES (@albumId,@songId)";
                    cmd.Parameters.Add(new SQLiteParameter("@albumId", albumID));
                    cmd.Parameters.Add(new SQLiteParameter("@songId", track.FirstAttribute.Value));
                    cmd.ExecuteScalar();
                }
                index += 1;
            }
        }
    }
}