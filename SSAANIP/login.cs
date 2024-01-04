using System.Windows.Controls;
using System.Windows;
using System.Xml.Linq;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace SSAANIP{
    public partial class loginPage : Page{
        public master parent;
        public RequestMethods req;
        public string version;
        SQLiteConnection conn;
        public string[] hashedpassword;
        public loginPage(master master){

            InitializeComponent();
            parent = master;
            conn = new SQLiteConnection("Data source=usrDB.db");
        }
        public async void login(object sender, RoutedEventArgs e) {
            string username = usrBox.Text.ToString();
            string password = pwdBox.Password.ToString();

            Request request = new(username, password, "ping");
            var response = await request.sendRequestAsync();
            
            if (response.First().Attribute("status").Value.ToString() == "ok"){ //valid username and password
                parent.Frame.Content = new MainWindow(parent, username, pwdBox.Password.ToString());
            }
            else{
                output.Content = "Invalid username or password";
                pwdBox.Clear();
            }
            
        }
        public void changeServer(object sender, RoutedEventArgs e){
            File.Delete("config.txt");
            this.parent.Frame.Content = new SocketPage(this.parent);
        }
    }
}
