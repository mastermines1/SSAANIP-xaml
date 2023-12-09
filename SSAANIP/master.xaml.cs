using System.Windows;
using System.IO;

namespace SSAANIP
{
    public partial class master : Window{

        public master(){
            InitializeComponent();

            if (File.ReadAllLines("config.txt")[0].Split("=")[1] == "")
            {
                Frame.Content = new SocketPage(this);
            }
            else
            {
                Frame.Content = new loginPage(this);
            }

        }
    }
}
