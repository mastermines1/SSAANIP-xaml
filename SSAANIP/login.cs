using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
namespace SSAANIP;
public partial class loginPage : Page{
    protected masterWindow parent;
    protected Request req;
    protected string version;
    public loginPage(masterWindow master){
        InitializeComponent();
        parent = master;
    }
    public async void login(object sender, RoutedEventArgs e) {
        string username = usrBox.Text;
        string password = pwdBox.Password;
        req = new(username, password);
        try{
            var response = await req.sendRequestAsync("ping", "");
            if (response.First().Attribute("status").Value.ToString() == "ok"){ //valid username and password
                parent.Frame.Content = new MainWindow(parent, req);
            }
            else{
                output.Content = "Invalid username or password";
                pwdBox.Clear();
            }
        }catch(HttpRequestException){
            output.Content = "Cant access server";
        }catch{
            output.Content = "Unknown error";
        }  
    }
    public void changeServer(object sender, RoutedEventArgs e){
        File.Delete("./assets/config.txt");
        this.parent.Frame.Content = new SocketPage(this.parent);
    }
}