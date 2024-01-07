using System.Windows.Controls;

namespace SSAANIP{
    /// <summary>
    /// Interaction logic for userMgmt.xaml
    /// </summary>
    public partial class userMgmt : Page{
        master master;
        public userMgmt(master master){
            InitializeComponent();
            this.master = master;

        }
        
    }
}
