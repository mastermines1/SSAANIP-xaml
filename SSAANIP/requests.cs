using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Text;




namespace SSAANIP{
    public class RequestMethods{ //different requests
        public string username;
        protected string password;
        public RequestMethods(string username, string password){
            this.username = username;
            this.password = password;
        }
        public async Task<IEnumerable<XElement>> System(string requestType){
            Request request = new(username, password, requestType);
            return await request.sendRequestAsync();
            
        }
        public async Task<IEnumerable<XElement>> Browsing(string requestType, string id){
            if (id != null){
                id = $"&id={id}";
            }
            Request request = new(username,password, requestType, id);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendCreatePlaylist(string name, string[] songId){
            string extras = $"&name={name}";
            foreach (string sID in songId){
                extras += $"&songID={sID}";
            }
            Request request = new(username, password, "createPlaylist", extras);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendUpdatePlaylist(string playlistID, string name, string comment, string isPublic, string[] songID, string[] songIndex){
            string extras = string.Empty;
            if (name != null){
                extras += $"&name={name}";
            }
            else if (comment != null){
                extras += $"&comment={comment}";
            }
            Request request = new(username, password, "updatePlaylist", extras);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendGetIndexes(){
            Request request = new(username, password, "getIndexes");
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public async Task<IEnumerable<XElement>> sendGetAlbum(string id){
            Request request = new(username, password, "getAlbum", "&id="+id);
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public async Task<IEnumerable<XElement>> sendGetArtist(string id){
            Request request = new(username, password, "getArtist", "&id=" + id);
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public async Task<IEnumerable<XElement>> sendStartScan(){
            Request request = new(username, password, "startScan");
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public string createUrl(string id){
            HttpClient httpClient = new();

            MD5 md5 = MD5.Create();
            string salt = createSalt(16);
            byte[] inputBytes = Encoding.ASCII.GetBytes(password + salt);
            byte[] hashed = md5.ComputeHash(inputBytes);
            string authToken = Convert.ToHexString(hashed).ToLower();


            return $@"http://{File.ReadAllLines("config.txt")[0].Split("=")[1]}/rest/stream?u={username}&t={authToken}&s={salt}&v={File.ReadAllLines("config.txt")[2].Split("=")[1]}&c={File.ReadAllLines("config.txt")[1].Split("=")[1]}&id={id}";

        }
        public static string createSalt(int size){ //generates a salt of set size 
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[size];
            rng.GetBytes(salt);
            return Convert.ToHexString(salt);
        }
        public async Task<IEnumerable<XElement>> sendGetUser(string username){
            if (username == null) username = this.username;
            Request request = new(username, password, "getUser");
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public async Task<IEnumerable<XElement>> sendDeleteUser(string username){
            if (username == "self") username = this.username;
            Request request = new(username, password, "deleteUser",$"&username={username}");
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public async void sendCreateUser(string newUserName,string newPassword, string isAdmin){
            Request request = new(this.username, this.password,"createUser");
            request.sendCreateUser(newUserName,newPassword,isAdmin);
        }
    }
}