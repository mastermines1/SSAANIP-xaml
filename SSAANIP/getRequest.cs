using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;
namespace SSAANIP{
    public class Request{
        private readonly string socket;  //the socket that the subsonic server is located at
		private readonly string username; //the username that the user is logged in with
		private readonly string version; //the version of the server
        private readonly string request; //the type of request
        private readonly string clientName; //the name of the client in use
        private readonly string extraParms;
        private readonly string password;
        public Request(string username, string password, string request){
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.request = request;
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.extraParms = "";
            this.password = password;
        }
        public Request(string username, string password, string request, string extraParms){
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.request = request;
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.extraParms = extraParms;
            this.password = password;
        }
        public Request(string username, string password, string request, string socket, string extraParms){
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
        public static string createSalt(int size){ //generates a salt of set size 
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[size];
            rng.GetBytes(salt);
            return Convert.ToHexString(salt);
        }
    }
}