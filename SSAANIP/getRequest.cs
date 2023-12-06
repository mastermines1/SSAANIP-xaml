using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SSAANIP{
    public class Request{
        private string socket;  //the socket that the subsonic server is located at
		private string username; //the username that the user is logged in with
		private string version; //the version of the server
        private string request; //the type of request
        private string clientName; //the name of the client in use
        private HttpClient client = new HttpClient();
        private string extraParms;
        private string authToken;
        private string salt;
        SQLiteConnection conn;
        
        
        public Request(string username, string request){
            conn = new SQLiteConnection("Data source=usrDB.db");
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.request = request;
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.extraParms = "";
            conn.Open();
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT passHash,Salt FROM tblUsers WHERE Username = @u";
            cmd.Parameters.Add(new SQLiteParameter("@u", username));
            using (var reader = cmd.ExecuteReader()){
                this.authToken = reader.GetString(0);
                this.salt = reader.GetString(1);
            }
            conn.Close();
        }
        public Request(string username, string request, string extraParms){
            conn = new SQLiteConnection("Data source=usrDB.db");
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.request = request;
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.extraParms = extraParms;
            conn.Open();
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT passHash,Salt FROM tblUsers WHERE Username = @u";
            cmd.Parameters.Add(new SQLiteParameter("@u", username));
            using (var reader = cmd.ExecuteReader()){
                this.authToken = reader.GetString(0);
                this.salt = reader.GetString(1);
            }
            conn.Close();
        }
        public Request(string username, string request, string authToken, string salt){
            conn = new SQLiteConnection("");
            this.socket = File.ReadAllLines("config.txt")[0].Split("=")[1];
            this.username = username;
            this.version = File.ReadAllLines("config.txt")[2].Split("=")[1];
            this.request = request;
            this.clientName = File.ReadAllLines("config.txt")[1].Split("=")[1];
            this.extraParms = extraParms;
            this.authToken = authToken;
            this.salt = salt;

        }

        public async Task<IEnumerable<XElement>> sendRequestAsync(){

            string url = $@"http://{socket}/rest/{request}?u={username}&t={authToken}&s={salt}&v={version}&c={clientName}{extraParms}";


            using HttpResponseMessage responseMessage = await client.GetAsync(url);
            responseMessage.EnsureSuccessStatusCode();
            XmlDocument data = new XmlDocument();
            string output = (await responseMessage.Content.ReadAsStringAsync());
            XDocument xele = XDocument.Parse(output);
            IEnumerable<XElement> collection = xele.Elements();

            return collection;
        }
    }
}
