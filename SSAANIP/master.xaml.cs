using System.IO;
using System.Windows;
namespace SSAANIP;
public partial class masterWindow : Window{
    public masterWindow(){
        InitializeComponent();
        try{
            Frame.Content = string.IsNullOrEmpty(File.ReadAllLines("./assets/config.txt")[0].Split("=")[1]) ? new SocketPage(this) : new loginPage(this);
        }catch{
            Frame.Content = new SocketPage(this);
        }
    }
}