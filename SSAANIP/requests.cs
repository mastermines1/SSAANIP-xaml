using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Windows.Themes;

namespace SSAANIP{
    public class RequestMethods{ //different requests
        readonly public string username;
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
            Request request = new(username, password, requestType, id);
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
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendGetAlbum(string id){
            Request request = new(username, password, "getAlbum", "&id=" + id);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendGetArtist(string id){
            Request request = new(username, password, "getArtist", "&id=" + id);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendStartScan(){
            Request request = new(username, password, "startScan");
            return await request.sendRequestAsync();
        }
        public string createUrl(string id){
            MD5 md5 = MD5.Create();
            string salt = createSalt(16);
            byte[] inputBytes = Encoding.ASCII.GetBytes(password + salt);
            byte[] hashed = md5.ComputeHash(inputBytes);
            string authToken = Convert.ToHexString(hashed).ToLower();
            return $@"http://{File.ReadAllLines("config.txt")[0].Split("=")[1]}/rest/stream?u={username}&t={authToken}&s={salt}&v={File.ReadAllLines("config.txt")[2].Split("=")[1]}&c={File.ReadAllLines("config.txt")[1].Split("=")[1]}&id={id}";
        }
        public static string createSalt(int size) { //generates a salt of set size 
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[size];
            rng.GetBytes(salt);
            return Convert.ToHexString(salt);
        }
        public async Task<IEnumerable<XElement>> sendGetUser(string username) {
            string extraParms = "&username=" + username;
            if (username == null) extraParms = "&username=" + this.username;
            Request request = new(this.username, password, "getUser", extraParms);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendGetUsers() {
            Request request = new(username, password, "getUsers");
            return await request.sendRequestAsync();
        }
        public async void sendCreateUser(string username, string password, bool isAdmin) {
            HttpClient client = new();
            string socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            string token = await getTokenJson(socket);
            if (token != null) {
                string create_user_url = $"http://{socket}/api/user";
                client.DefaultRequestHeaders.Add("x-nd-authorization", "Bearer " + token);
                create_data create_data = new(){
                    userName = username,
                    name = username,
                    isAdmin = isAdmin,
                    password = password
                };
                string json = JsonConvert.SerializeObject(create_data);
                StringContent stringContent = new(json);
                await client.PostAsync(create_user_url, stringContent);
            }
        }
        public async Task sendDeleteUser(string username) {
            if (username == "self") username = this.username;
            string socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            HttpClient client = new();
            string token = await getTokenJson(socket);
            if (token != null) {
                string delete_user_url = $"http://{socket}/api/user/{await getIdJson(socket, token, username)}";
                client.DefaultRequestHeaders.Add("x-nd-authorization", "Bearer " + token);
                await client.DeleteAsync(delete_user_url);
            }
        }
        public async Task sendUpdateUser(string newUsername,string newPassword, string newIsAdmin){
            string socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            HttpClient client = new();
            string token = await getTokenJson(socket);
            if (token != null){
                string update_user_url = $"http://{socket}/api/user/{await getIdJson(socket, token, newUsername)}";
                client.DefaultRequestHeaders.Add("x-nd-authorization", "Bearer " + token);
                update_data update_data = new();
                if (newIsAdmin == "true") update_data.isAdmin = true;
                else if (newIsAdmin == "false") update_data.isAdmin = false;
                if (newUsername != null) update_data.userName = username;
                if (newPassword != null) update_data.password = newPassword;
                string json = JsonConvert.SerializeObject(update_data);
                StringContent stringContent = new(json);
                await client.PutAsync(update_user_url, stringContent);
            }
        }
        public async Task<string> getTokenJson(string socket) {
            HttpClient client = new();
            string login_url = $"http://{socket}/auth/login";
            Dictionary<string, string> login_data = new(){
                {"username",this.username}, {"password",this.password},
            };
            string json = JsonConvert.SerializeObject(login_data);
            StringContent stringContent = new(json);
            HttpResponseMessage login_response = await client.PostAsync(login_url, stringContent);
            login_response.EnsureSuccessStatusCode();
            string responseBody = await login_response.Content.ReadAsStringAsync();
            JObject response = (JObject)JToken.Parse(responseBody);
            return response.SelectToken("token").ToString();
        }
        public async Task<string> getIdJson(string socket, string token, string username) {
            HttpClient client = new();
            if (username != this.username) {
                string fetch_url = $"http://{socket}/api/user";
                client.DefaultRequestHeaders.Add("x-nd-authorization", "Bearer " + token);
                HttpResponseMessage fetch_response = await client.GetAsync(fetch_url);
                fetch_response.EnsureSuccessStatusCode();
                string responseBody = await fetch_response.Content.ReadAsStringAsync();
                JArray response = JArray.Parse(responseBody);

                foreach (JToken response_object in response.Children()) {
                    if (response_object["userName"].ToString() == username) {
                        return response_object["id"].ToString();
                    }
                }
            }
            else {
                string fetch_url = $"http://{socket}/auth/login";
                Dictionary<string, string> userInfo = new(){
                    {"username",this.username},{"password",this.password}
                };
                StringContent stringContent = new(JsonConvert.SerializeObject(userInfo));
                HttpResponseMessage fetch_response = await client.PostAsync(fetch_url, stringContent);
                fetch_response.EnsureSuccessStatusCode();
                string responseBody = await fetch_response.Content.ReadAsStringAsync();
                JObject response = JObject.Parse(responseBody);
                return response["id"].ToString();
            }
            return "error";
        }
        public class create_data{
            public string userName { get; set; }
            public string password { get; set; }
            public bool isAdmin { get; set; }
            public string name { get; set; }
        }
        public class update_data {
            public string userName { get; set; }
            public bool isAdmin { get; set; }
            public string password { get; set; }
        }
    }
}