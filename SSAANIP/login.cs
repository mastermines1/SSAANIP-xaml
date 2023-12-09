using System.Net;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Data.SQLite;
using System.Security.Cryptography;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SSAANIP{
    public partial class loginPage : Page{
        public master parent;
        public RequestMethods req;
        public string version;
        SQLiteConnection conn;
        public string[] hashedpassword;
        public loginPage(master master){

            InitializeComponent();
            parent = master;
            conn = new SQLiteConnection("Data source=usrDB.db");
        }
        public async void login(object sender, RoutedEventArgs e) {
            string username = usrBox.Text.ToString();
            string password = pwdBox.Password.ToString();

            Request request = new(username, password, "ping");
            var response = await request.sendRequestAsync();
            
            foreach( XAttribute attribute in response.Attributes() ){
                if (attribute.Name == "status" && attribute.Value.ToString() == "ok"){ //valid username and password

                    parent.Frame.Content = new MainWindow(parent, username, pwdBox.Password.ToString());
                    break;
                }
                else{
                    output.Content = "Invalid username or password";
                }
            }

        }
        public async void login(string username,string password){
            //Request request = new(username, "getUser", hashedpassword[0], hashedpassword[1]);
            //conn.Open();
            //SQLiteCommand cmd = conn.CreateCommand();
            //cmd.CommandText = "SELECT * FROM tblUsers WHERE Username ='@u'";
            //cmd.Parameters.Add(new SQLiteParameter("@u",username));
            //if (cmd.ExecuteNonQuery() == null){ //checks if user is already in local database and if not adds it
            //    string adminRole =string.Empty;
            //    var xmlDoc = await request.sendRequestAsync(); //finds if the user is an admin or not
            //    foreach (XElement element in xmlDoc.Elements()){
            //        foreach(XAttribute attribute in element.Attributes()){
            //            if (attribute.Name == "adminRole"){
            //                adminRole = attribute.Value;
            //                break;
            //            }
            //        }
            //    }

            //    //hashes the password + salt
            //    string[] hashed = hashPassword(password);
            //    string authToken = hashed[0];
            //    string salt = hashed[1];

            //    cmd.CommandText = "INSERT INTO tblUsers (Username,PassHash,Salt,IsAdmin) VALUES (@u,@p,@s,@a)"; //Inserts new user into local DB
            //    cmd.Parameters.Add(new SQLiteParameter("@u",username));
            //    cmd.Parameters.Add(new SQLiteParameter("@p",authToken));
            //    cmd.Parameters.Add(new SQLiteParameter("@s",salt));
            //    cmd.Parameters.Add(new SQLiteParameter("@a",adminRole));
            //    cmd.ExecuteScalar();

            //}
             
            //conn.Close();
            //parent.Frame.Content = new MainWindow(parent,username,password);

        }
        public void changeServer(object sender, RoutedEventArgs e)
        {

            File.Delete("config.txt");
            this.parent.Frame.Content = new loginPage(this.parent);
        }

        //public static string[] hashPassword(string password){
        //    string salted = createSalt(16);
        //    return new string[] { Convert.ToHexString(MD5.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(password + salted))).ToLower(), salted };
        //}
        //public static string createSalt(int size){ //generates a salt of set size
        //    RandomNumberGenerator rng = RandomNumberGenerator.Create();
        //    byte[] salt = new byte[size];
        //    rng.GetBytes(salt);
        //    return Convert.ToHexString(salt);
        //}

    }
}
