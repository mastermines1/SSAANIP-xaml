using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace SSAANIP{
    /// <summary>
    /// Interaction logic for userMgmt.xaml
    /// </summary>
    public partial class userMgmt : Page{
        master master;
        RequestMethods req;
        public userMgmt(master master, RequestMethods req){
            InitializeComponent();
            this.master = master;
            this.req = req;
            fetchUserInfo();
        }
        public async void fetchUserInfo(){
            IEnumerable<XElement> userData = await req.sendGetUser();
            lblUserName.Content = "Username: " + userData.Elements().First().FirstAttribute.Value;
            if (userData.Elements().First().Attribute("adminRole").Value == "true") btnAdmin.Visibility = Visibility.Visible;
        }
        //public bool confirmPassword(){

        //}
        private void btnLogout_Click(object sender, RoutedEventArgs e){
            master.Frame.Content = new loginPage(master);
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e){
            if (MessageBox.Show("Are you sure? \n This is a permenant change.", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK){
                //req.sendDeleteUser("self");
            }
        }
        private void btnAdmin_Click(object sender, RoutedEventArgs e){
            
        }
        private void btnback_Click(object sender, RoutedEventArgs e){
            master.Frame.Content = new MainWindow(master, req);
        }
        private void btnDeleteSelectedUser_Click(object sender, RoutedEventArgs e){
            if(lsUsers.SelectedItem != null){
                if (MessageBox.Show("Are you sure? \n This is a permenant change.", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK){
                    req.sendDeleteUser(lsUsers.SelectedItem.ToString());
                }
            }

        }
        private void btnMakeAdmin_Click(object sender, RoutedEventArgs e){

        }

    }
}