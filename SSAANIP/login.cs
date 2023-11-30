using System.Net.Sockets;
using System;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SSAANIP
{
    public partial class loginPage : Page{
        public master parent;
        public RequestMethods req;
        public string socket;
        public string username;
        public string version;
        public string appName;
        public loginPage(master master){

            InitializeComponent();
            parent = master;

        }
        public async void login(object sender, RoutedEventArgs e)
        {
            socket = "100.73.164.110:4533";
            version = "1.16";
            appName = "test";
            username = usrBox.Text.ToString();
            string password = pwdBox.Text.ToString();
            req = new(socket, username, version, appName);

            IEnumerable<XElement> response = await req.System("ping");

            output.Content=response;


        }
    }
}
