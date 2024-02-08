using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace SSAANIP;
public class updateData{
    readonly string connectionString;
    readonly Request req;
    SemaphoreSlim slim = new(1, 1);
    public updateData(string connectionString, Request req){
        this.connectionString = connectionString;
        this.req = req;
    }
    // Methods to update data in the local database
    public async Task updateDB(){
        try{
            await slim.WaitAsync();
            await req.sendRequestAsync("startScan", "");
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "DELETE FROM tblAlbumArtistLink;DELETE FROM tblAlbumSongLink;DELETE FROM tblAlbums;DELETE FROM tblArtists;DELETE FROM tblSongs;UPDATE sqlite_sequence SET seq=0 WHERE (name=\"tblAlbumArtistLink\" OR name=\"tblAlbumSongLink\")";
                cmd.ExecuteScalar();
            }
            await updateArtists();
        }finally{slim.Release();}
    }
    private async Task updateArtists(){
        IEnumerable<XElement> indexes = await req.sendRequestAsync("getIndexes", "");
        foreach (XElement element in indexes.Elements().Elements().Elements()){ //get every artist id
            string artistId = element.FirstAttribute.Value.ToString();  
            IEnumerable<XElement> response = await req.sendRequestAsync("getArtist","&id=" + artistId);
            string artistName = response.Elements().ElementAt(0).FirstAttribute.NextAttribute.Value;
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblArtists VALUES (@id,@name)";
                cmd.Parameters.Add(new("@id", artistId));
                cmd.Parameters.Add(new("@name", artistName));
                cmd.ExecuteScalar();
            }
            await updateAlbums(artistId);
        }
    }
    private async Task updateAlbums(string artistID){
        var artistData = await req.sendRequestAsync("getArtist","&id=" + artistID);
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
            await updateTracks(currentAlbumId);
        }
    }
    private async Task updateTracks(string albumID){
        var albumData = await req.sendRequestAsync("getAlbum", "&id=" + albumID);
        int index = 0;
        foreach (XElement track in albumData.Elements().Elements()){
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblSongs VALUES (@id,@name,@duration)";
                cmd.Parameters.Add(new("@id", track.FirstAttribute.Value));
                cmd.Parameters.Add(new("@name", track.Attribute("title").Value));
                cmd.Parameters.Add(new("@duration", track.Attribute("duration").Value));
                cmd.ExecuteScalar();
            }
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "INSERT INTO tblAlbumSongLink (albumId,songId,songIndex) VALUES (@albumId,@songId, @index)";
                cmd.Parameters.Add(new("@albumId", albumID));
                cmd.Parameters.Add(new("@songId", track.FirstAttribute.Value));
                cmd.Parameters.Add(new("@index", index));
                cmd.ExecuteScalar();
            }
            index += 1;
        }
    }
    public async Task updateUsers(){
        var authenticatedUserInfo = await req.sendRequestAsync("getUser", "&id=" + req.username);
        if(authenticatedUserInfo.Elements().First().Attribute("adminRole").Value.ToString() == "true"){
            JArray usersInfo = await req.sendGetUserDataAsync();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "DELETE FROM tblUsers";
                cmd.ExecuteScalar();
            }
            foreach(JToken user in usersInfo.Children()){
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblUsers VALUES (@userName,@isAdmin)";
                    cmd.Parameters.Add(new("@userName", user.SelectToken("userName").ToString().ToLower()));
                    cmd.Parameters.Add(new("@isAdmin", user.SelectToken("isAdmin").ToString().ToLower()));
                    cmd.ExecuteScalar();
                }
            }
        }
        else{
            foreach(XElement user in authenticatedUserInfo.Elements()){
                bool alrExists = false;
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT userName FROM tblUsers WHERE userName = @username";
                    cmd.Parameters.Add(new("@username", user.Attribute("username").Value.ToString().ToLower()));
                    using SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read()){
                        alrExists = true;
                    }  
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