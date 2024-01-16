﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
        string connectionString = "Data source=data.db";
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
                        await req.sendDeleteUser("self");
                        using (SQLiteConnection conn = new(connectionString))
                        using (var cmd = conn.CreateCommand()){
                            conn.Open();
                            cmd.CommandText = "DELETE FROM tblUsers WHERE username = @username";
                            cmd.Parameters.Add(new("@username", req.username));
                            cmd.ExecuteScalar();
                        }

                        master.Frame.Content = new loginPage(master);
                    }
                }
                else{ //incorrect password
                    lblConfirm.Text = "Incorrect password";
                }
            }
            else{
                btnDeleteSelf.Content = "Continue";
                lblConfirm.Text = "Please confirm your password";
            }

        }
        private async void btnAdmin_Click(object sender, RoutedEventArgs e){
            if(adminPanel.Visibility == Visibility.Visible){
                adminPanel.Visibility = Visibility.Hidden;
            }else {
                if (await confirmPassword(confirmPass)){
                    adminPanel.Visibility = Visibility.Visible;
                    lblConfirm.Text = "";
                    confirmPass.Password = "";
                    confirmPass.Visibility = Visibility.Hidden;
                    btnAdmin.Content = "Admin panel";
                } else{
                    confirmPass.Visibility = Visibility.Visible;
                    lblConfirm.Text = "Confirm your password.";
                    btnAdmin.Content = "Continue";
                }
                lsUserNames.Items.Clear();
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT userName FROM tblUsers";
                    using (SQLiteDataReader reader = cmd.ExecuteReader()){
                        while (reader.Read()){
                            string newUserName = reader.GetString(0);
                            if(newUserName.ToLower() != req.username) lsUserNames.Items.Add(newUserName);
                        }
                    }
                }
            }
        }
        private void btnback_Click(object sender, RoutedEventArgs e){
            master.Frame.Content = new MainWindow(master, req);
        }
        private void btnAddUser_Click(object sender, RoutedEventArgs e){
            if(pwdPassword.Password == pwdConfirmPassword.Password){
                string username = txtUserName.Text;
                string password = pwdPassword.Password;
                string isAdmin = checkBoxAdmin.IsChecked.ToString();
                req.sendCreateUser(username, password, isAdmin);
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "INSERT INTO tblUsers VALUES (@username,@isAdmin)";
                    cmd.Parameters.Add(new("@username", username));
                    cmd.Parameters.Add(new("@isAdmin", isAdmin));
                    cmd.ExecuteScalar();
                }
                lsUserNames.Items.Add(username);
                lblOutput.Content = $"User {username} Created";
            }
            else{
                lblOutput.Content = "Passwords do not match";
            }
        }
        private void lsUserNames_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if(lsUserNames.SelectedItem != null){
                userPanel.Visibility = Visibility.Visible;
                txtDisplayUserName.Text = lsUserNames.SelectedItem.ToString();
                string isAdmin;
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "SELECT isAdmin FROM tblUsers WHERE username = @username";
                    cmd.Parameters.Add(new("@username", lsUserNames.SelectedItem.ToString()));
                    isAdmin = cmd.ExecuteScalar().ToString();
                }
                if (isAdmin == "true") ckbIsAdmin.IsChecked = true;
                else ckbIsAdmin.IsChecked = false;
            }
        }
        private async void btnDeleteUser_Click(object sender, RoutedEventArgs e){
            if (lsUserNames.SelectedItem != null && MessageBox.Show("Are you sure? \n This is a permenant change.", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK){
                await req.sendDeleteUser(lsUserNames.SelectedItem.ToString());
                using (SQLiteConnection conn = new(connectionString))
                using (var cmd = conn.CreateCommand()){
                    conn.Open();
                    cmd.CommandText = "DELETE FROM tblUsers WHERE username = @username";
                    cmd.Parameters.Add(new("@username", lsUserNames.SelectedItem.ToString()));
                    cmd.ExecuteScalar();
                }
                userPanel.Visibility = Visibility.Hidden;
                lsUserNames.Items.Remove(lsUserNames.SelectedItem);
            }
        }
    }
}