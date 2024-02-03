using System.Collections.Generic;
using System.Data.SQLite;
using System.Xml.Linq;
using System.Linq;
using System.Threading.Tasks;
namespace SSAANIP;
public class updateData{
    readonly string connectionString;
    readonly Request req;
    public updateData(string connectionString, Request req){
        this.connectionString = connectionString;
        this.req = req;
    }
    // Methods to update data in the local database
    public async Task updateDB(){
        await req.sendRequestAsync("startScan", "");
        using (SQLiteConnection conn = new(connectionString))
        using (var cmd = conn.CreateCommand()){
            conn.Open();
            cmd.CommandText = "DELETE FROM tblAlbumArtistLink;DELETE FROM tblAlbumSongLink;DELETE FROM tblAlbums;DELETE FROM tblArtists;DELETE FROM tblSongs;UPDATE sqlite_sequence SET seq=0 WHERE (name=\"tblAlbumArtistLink\" OR name=\"tblAlbumSongLink\")";
            cmd.ExecuteScalar();
        }
        updateArtists();
    }
    private async Task updateArtists(){
        List<string> artistsID = new();
        IEnumerable<XElement> indexes = await req.sendRequestAsync("getIndexes", "");
        foreach (XElement element in indexes.Elements().Elements().Elements()){ //get every artist id
            artistsID.Add(element.FirstAttribute.Value);
        }
        foreach (string id in artistsID){ //gets the artist name
            IEnumerable<XElement> response = await req.sendRequestAsync("getArtist","&id=" + id);
            string artistName = response.Elements().ElementAt(0).FirstAttribute.NextAttribute.Value;
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
            updateTracks(currentAlbumId);
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
    private async Task updatePlaylists(){
        IEnumerable<XElement> playlistsData = await req.sendRequestAsync("getPlaylists", "");
        foreach (XElement playlist in playlistsData.Elements().Elements()){
            IEnumerable<XElement> playlistData = await req.sendRequestAsync("getPlaylist", "&id=" + playlist.Attribute("id").Value.ToString());
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "UPDATE tblPlaylists SET(playlistId=@id, playlistName=@name, playlistDuration=@duration, isPublic=@isPublic) WHERE playlistId=@id";
                cmd.Parameters.Add(new("@id", playlist.Attribute("id").Value));
                cmd.Parameters.Add(new("@name",playlist.Attribute("name").Value));
                cmd.Parameters.Add(new("@duration",playlist.Attribute("duration").Value));
                cmd.Parameters.Add(new("@isPublic", playlist.Attribute("public").Value));
                cmd.ExecuteScalar();
            }
            int index = 0;
            foreach(XElement song in playlistData.Elements().Elements()){
                if(song.Name == "entry"){
                    using (SQLiteConnection conn = new(connectionString))
                    using (var cmd = conn.CreateCommand()){
                        conn.Open();
                        cmd.CommandText = "INSERT INTO tblPlaylistSongLink (playlistId, songId, songIndex) VALUES (@playlistId, @songId, @songIndex)";
                        cmd.Parameters.Add(new("@playlistId", playlist.Attribute("id").Value));
                        cmd.Parameters.Add(new("songId", song.FirstAttribute.Value));
                        cmd.Parameters.Add(new("songIndex", index));
                        cmd.ExecuteScalar();
                    }
                    index ++;
                }
            }
        }
    }        
    public async Task updateUsers(){
        var test = await req.sendGetUserDataAsync();
        IEnumerable<XElement> usersInfo;
        var authenticatedUserInfo = await req.sendRequestAsync("getUser", "&id=" + req.username);
        if(authenticatedUserInfo.Elements().First().Attribute("adminRole").Value.ToString() == "true"){
            usersInfo = (await req.sendRequestAsync("getUsers","")).Elements();
            using (SQLiteConnection conn = new(connectionString))
            using (var cmd = conn.CreateCommand()){
                conn.Open();
                cmd.CommandText = "DELETE FROM tblUsers";
                cmd.ExecuteScalar();
            }
        } else usersInfo = authenticatedUserInfo;
        foreach(XElement user in usersInfo.Elements()){
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