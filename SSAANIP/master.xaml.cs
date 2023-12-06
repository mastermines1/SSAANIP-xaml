using System.Windows;

namespace SSAANIP
{
    public partial class master : Window{

        public master(){
            InitializeComponent();

            Frame.Content = new loginPage(this);
        }
    }
}
