using System.Windows;
using System.IO;
namespace SSAANIP;
public partial class masterWindow : Window{
    public masterWindow(){
        InitializeComponent();
        try{
            Frame.Content = string.IsNullOrEmpty(File.ReadAllLines("config.txt")[0].Split("=")[1]) ? new SocketPage(this) : new loginPage(this);
        }catch{
            File.WriteAllText("config.txt", "");
            Frame.Content = new SocketPage(this);
        }
    }
}