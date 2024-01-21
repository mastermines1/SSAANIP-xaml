using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;

namespace SSAANIP{
    public partial class SocketPage : Page{
        public master parent;
        public SocketPage(master master){
            InitializeComponent();
            parent = master;
        }
        private async void Button_Click(object sender, RoutedEventArgs e){
            string[] newConfig = new string[3];
            Request request = new("test","test", socketBox.Text);
            try{
                var xmlDoc = await request.sendRequest("ping",""); //test if valid socket

                newConfig[0] = "socket=" + socketBox.Text;
                newConfig[1] = "appName=" + namebox.Text;
                newConfig[2] = "version=" + xmlDoc.ElementAt(0).Attribute("version").Value;

                File.WriteAllLines("config.txt",newConfig);
                parent.Frame.Content = new loginPage(parent);
            } catch{
                outputlbl.Content = "invalid socket";
            }
        }
    }
}
