using System.Net;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Data.SQLite;

namespace SSAANIP
{
    public partial class loginPage : Page{
        public master parent;
        public RequestMethods req;
        public string socket;
        public string version;
        public string appName;
        SQLiteConnection conn;
        public loginPage(master master){

            InitializeComponent();
            parent = master;
            socket = "100.73.164.110:4533";
            version = "1.16";
            appName = "test";
            conn = new SQLiteConnection("Data source=usrDB.db");
        }
        public async void login(object sender, RoutedEventArgs e)
        {
            string username = usrBox.Text.ToString();
            string password = pwdBox.Password.ToString();
            req = new(socket, username, version, appName);

            IEnumerable<XElement> response = await req.System("ping");

            if (response.Attributes("status").First().Value.ToString() == "\"ok\"")//valid username and password
            {
                login(username, password);
            }
        }
        public void login(string username,string password)
        {
           conn.Open();
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT ";
            
        }
    }
}
