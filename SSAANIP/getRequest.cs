using System.Security.Cryptography;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System;

namespace SSAANIP{
    public class getRequest{
        private string socket;  //the socket that the subsonic server is located at
		private string username; //the username that the user is logged in with
		private string version; //the version of the server
        private string request; //the type of request
        private string clientName; //the name of the client in use
        private HttpClient client = new HttpClient();
        private string extraParms;
        
        
        public getRequest(string socket,string username,string version,string request,string clientName){
            this.socket = socket;
            this.username = username;
            this.version = version;
            this.request = request;
            this.clientName = clientName;
        }
        public getRequest(string socket,string username,string version,string request,string clientName,string extraParms){
            this.socket = socket;
            this.username = username;
            this.version = version;
            this.request = request;
            this.clientName = clientName;
            this.extraParms = extraParms;        
        }

        public async Task<string> sendRequestAsync(){
            string password = string.Empty;
            MD5 md5 = MD5.Create();
            try{
				password = File.ReadAllText("password.txt");
			}
			catch{
				Console.WriteLine("No password file found");

			}
            string salt = createSalt(16);

			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(password+salt);
			byte[] hashed = md5.ComputeHash(inputBytes);
			string authToken = Convert.ToHexString(hashed).ToLower();

            string url = $@"http://{socket}/rest/{request}?u={username}&t={authToken}&s={salt}&v={version}&c={client}{extraParms}";


            using HttpResponseMessage responseMessage = await client.GetAsync(url);
            responseMessage.EnsureSuccessStatusCode();
			string data = await responseMessage.Content.ReadAsStringAsync();
            return data;
        }


		public static string createSalt(int size){ //generates a salt of set size TO-DO make custom one
			RandomNumberGenerator rng = RandomNumberGenerator.Create();
			byte[] salt = new byte[size];
			rng.GetBytes(salt);
			return Convert.ToHexString(salt);
		}
    }
}
