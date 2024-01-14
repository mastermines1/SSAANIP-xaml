using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Cryptography;
using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace SSAANIP{
    public class Request{
        private string socket;  //the socket that the subsonic server is located at
		private string username; //the username that the user is logged in with
		private string version; //the version of the server
        private string request; //the type of request
        private string clientName; //the name of the client in use
        private string extraParms;
        private string password;
        SQLiteConnection conn;
        
        public Request(string username, string password, string request){
            conn = new SQLiteConnection("Data source=usrDB.db");
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.request = request;
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.extraParms = "";
            this.password = password;
        }
        public Request(string username, string password, string request, string extraParms){
            conn = new SQLiteConnection("");
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.request = request;
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.extraParms = extraParms;
            this.password = password;
        }
        public Request(string username, string password, string request, string socket, string extraParms){
            conn = new SQLiteConnection("");
            this.socket = socket;
            this.username = username;
            this.version = "";
            this.request = request;
            this.clientName = "SocketTest";
            this.extraParms = extraParms;
            this.password = password;

        }

        public async Task<IEnumerable<XElement>> sendRequestAsync(){
            HttpClient client = new();
            MD5 md5 = MD5.Create();
            string salt = createSalt(16);
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(password + salt);
            byte[] hashed = md5.ComputeHash(inputBytes);
            string authToken = Convert.ToHexString(hashed).ToLower();

            string url = $@"http://{socket}/rest/{request}?u={username}&t={authToken}&s={salt}&v={version}&c={clientName}{extraParms}";

            using HttpResponseMessage responseMessage = await client.GetAsync(url);
            responseMessage.EnsureSuccessStatusCode();
            IEnumerable<XElement> collection = XDocument.Parse(await responseMessage.Content.ReadAsStringAsync()).Elements();
            return collection;
        }
        public async void sendCreateUser(string newUsername, string newPassword, string IsAdmin){
            HttpClient client = new HttpClient();

            string login_url = $"http://{socket}/auth/login";
            Dictionary<string, string> login_data = new(){
                {"username",this.username}, {"password",this.password},
            };
            string json = JsonConvert.SerializeObject(login_data);
            StringContent stringContent = new(json, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage login_response = await client.PostAsync(login_url, stringContent);
            login_response.EnsureSuccessStatusCode();
            string responseBody = await login_response.Content.ReadAsStringAsync();
            JObject response = (JObject)JToken.Parse(responseBody);
            var token = response.SelectToken("token").ToString();


            if (token != null){
                string create_user_url = $"http://100.73.164.110:4533/api/user";

                Dictionary<string, string> user_data = new(){
                    {"isAdmin", IsAdmin},
                    {"userName", newUsername},
                    {"name", newUsername},
                    {"password", newPassword},
                };

                client.DefaultRequestHeaders.Add("x-nd-authorization", "Bearer " + token);
                //json = $"{{\"isAdmin\":{IsAdmin},\"username\":\"{newUsername}\",\"name\":\"{newUsername}\",\"password\":\"{newPassword}\"}}";
                //json = JsonConvert.SerializeObject (user);
                var content = new FormUrlEncodedContent(user_data);
                //stringContent = new(json, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage create_user_response = await client.PostAsync(create_user_url, content);
                create_user_response.EnsureSuccessStatusCode();
                responseBody = await create_user_response.Content.ReadAsStringAsync();
            }
        }
        public static string createSalt(int size)
        { //generates a salt of set size 
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[size];
            rng.GetBytes(salt);
            return Convert.ToHexString(salt);
        }
    }
}
