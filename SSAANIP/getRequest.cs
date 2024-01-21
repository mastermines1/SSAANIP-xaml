using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
namespace SSAANIP{
    public class Request{
        private readonly string socket;  //the socket that the subsonic server is located at
		public readonly string username; //the username that the user is logged in with
		private readonly string version; //the version of the server
        private readonly string clientName; //the name of the client in use
        private readonly string password;
        public Request(string username, string password){
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.password = password;
        }
        public Request(string username, string password, string socket){
            this.socket = socket;
            this.username = username;
            this.version = "";
            this.clientName = "SocketTest";
            this.password = password;
        }
        public async Task<IEnumerable<XElement>> sendRequest(string request, string extraParms){
            HttpClient client = new();
            using HttpResponseMessage responseMessage = await client.GetAsync(createURL(request,extraParms));
            responseMessage.EnsureSuccessStatusCode();
            IEnumerable<XElement> collection = XDocument.Parse(await responseMessage.Content.ReadAsStringAsync()).Elements();
            return collection;
        }
        private static string createSalt(int size){ //generates a salt of set size 
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[size];
            rng.GetBytes(salt);
            return Convert.ToHexString(salt);
        }
        public string createURL(string request, string extraParms){
            MD5 md5 = MD5.Create();
            string salt = createSalt(16);
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(password + salt);
            byte[] hashed = md5.ComputeHash(inputBytes);
            string authToken = Convert.ToHexString(hashed).ToLower();
            return $@"http://{socket}/rest/{request}?u={username}&t={authToken}&s={salt}&v={version}&c={clientName}{extraParms}";
        }
        public async void sendCreateUser(string username, string password, bool isAdmin){
            HttpClient client = new();
            string socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            string token = await getTokenJson(socket);
            if (token != null){
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
        public async Task sendDeleteUser(string username){
            if (username == "self") username = this.username;
            string socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            HttpClient client = new();
            string token = await getTokenJson(socket);
            if (token != null){
                string delete_user_url = $"http://{socket}/api/user/{await getIdJson(socket, token, username)}";
                client.DefaultRequestHeaders.Add("x-nd-authorization", "Bearer " + token);
                await client.DeleteAsync(delete_user_url);
            }
        }
        public async Task sendUpdateUser(string newUsername, string newPassword, string newIsAdmin){
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
        public async Task<string> getTokenJson(string socket){
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
        public async Task<string> getIdJson(string socket, string token, string username){
            HttpClient client = new();
            if (username != this.username){
                string fetch_url = $"http://{socket}/api/user";
                client.DefaultRequestHeaders.Add("x-nd-authorization", "Bearer " + token);
                HttpResponseMessage fetch_response = await client.GetAsync(fetch_url);
                fetch_response.EnsureSuccessStatusCode();
                string responseBody = await fetch_response.Content.ReadAsStringAsync();
                JArray response = JArray.Parse(responseBody);
                foreach (JToken response_object in response.Children()){
                    if (response_object["userName"].ToString() == username){
                        return response_object["id"].ToString();
                    }
                }
            }
            else{
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
        private class create_data{
            public string userName { get; set; }
            public string password { get; set; }
            public bool isAdmin { get; set; }
            public string name { get; set; }
        }
        private class update_data{
            public string userName { get; set; }
            public bool isAdmin { get; set; }
            public string password { get; set; }
        }
    }
}