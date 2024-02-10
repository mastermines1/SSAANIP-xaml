using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace SSAANIP;
public partial class SocketPage : Page{
    public masterWindow parent;
    public SocketPage(masterWindow master){
        InitializeComponent();
        parent = master;
    }
    private async void btnConfirm_Click(object sender, RoutedEventArgs e){
        File.Create("./assets/config.txt").Close();
        string[] newConfig = new string[3];
        Request request = new("test","test", socketBox.Text);
        try{
            var xmlDoc = await request.sendRequestAsync("ping",""); //test if valid socket
            newConfig[0] = "socket=" + socketBox.Text;
            newConfig[1] = "appName=" + namebox.Text;
            newConfig[2] = "version=" + xmlDoc.ElementAt(0).Attribute("version").Value;
            File.WriteAllLines("./assets/config.txt",newConfig);
            parent.Frame.Content = new loginPage(parent);
        }catch{
            outputlbl.Content = "invalid socket";
        }
    }
}