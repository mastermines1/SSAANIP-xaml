using System.Windows.Controls;
using System.Windows;
using System.IO;
using System.Linq;

namespace SSAANIP{
    public partial class loginPage : Page{
        protected master parent;
        protected Request req;
        protected string version;
        public loginPage(master master){
            InitializeComponent();
            parent = master;
        }
        public async void login(object sender, RoutedEventArgs e) {
            string username = usrBox.Text.ToString();
            string password = pwdBox.Password.ToString();
            req = new(username, password);
            try{
                var response = await req.sendRequest("ping","");           
                if (response.First().Attribute("status").Value.ToString() == "ok"){ //valid username and password
                    parent.Frame.Content = new MainWindow(parent,req);
                }else{
                    output.Content = "Invalid username or password";
                    pwdBox.Clear();
                }
            }catch{
                output.Content = "Cant access server";
            }            
        }
        public void changeServer(object sender, RoutedEventArgs e){
            File.Delete("config.txt");
            this.parent.Frame.Content = new SocketPage(this.parent);
        }
    }
}
