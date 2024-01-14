using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            IEnumerable<XElement> userData = await req.sendGetUser(null);
            lblUserName.Content = "Username: " + userData.Elements().First().FirstAttribute.Value;
            if (userData.Elements().First().Attribute("adminRole").Value == "true") btnAdmin.Visibility = Visibility.Visible;
        }
        public async Task<Boolean> confirmPassword(PasswordBox pwdBox){
            string password = pwdBox.Password;
            RequestMethods tempReq = new(req.username, password);
            IEnumerable<XElement> data = await tempReq.System("ping");
            if(data.First().Attribute("status").Value == "ok"){
                return true;
            }
            else{
                return false;
            }
        }
        private void btnLogout_Click(object sender, RoutedEventArgs e){
            master.Frame.Content = new loginPage(master);
        }
        private async void btnDeleteSelf_Click(object sender, RoutedEventArgs e){
            confirmPass.Visibility = Visibility.Visible;
            if (confirmPass.Password != ""){
                if (await confirmPassword(confirmPass)){
                    if (MessageBox.Show("Are you sure? \n This is a permenant change.", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK){
                        string test = "test";
                        //req.sendDeleteUser("self");
                        lblConfirm.Text = "";
                        confirmPass.Visibility = Visibility.Hidden;
                    }
                }
                else{ //incorrect password
                    lblConfirm.Text = "Incorrect password";
                }
            }
            else{
                lblConfirm.Text = "Please confirm your password";
            }

        }
        private async void btnAdmin_Click(object sender, RoutedEventArgs e){
            if (await confirmPassword(confirmPass)){
                adminPanel.Visibility = Visibility.Visible;
            }
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
        private void btnAddUser_Click(object sender, RoutedEventArgs e){
            if(pwdPassword.Password == pwdConfirmPassword.Password){
                string username = txtUserName.Text;
                string password = pwdPassword.Password;
                string isAdmin = checkBoxAdmin.IsChecked.ToString();
                //req.sendCreateUser(username, password, isAdmin);
                lblOutput.Content = $"User {username} Created";
            }
            else{
                lblOutput.Content = "Passwords dont match";
            }
        }
    }
}