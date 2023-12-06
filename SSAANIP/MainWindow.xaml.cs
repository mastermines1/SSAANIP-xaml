﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace SSAANIP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Page{
        public string socket;
        public string username;
        public string version;
        public string appName;
        readonly RequestMethods req;
        public master parent;
        public MainWindow(master master,string username){

            InitializeComponent();
            parent = master;
            socket = "100.73.164.110:4533";
            username = username;
            version = "1.16";
            appName = "test";
            req = new(socket, username, version, appName);
        }



        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<XElement> xmlDoc = await req.sendGetIndexes();
            
            foreach(XElement element in xmlDoc.Elements().Elements().Elements())
            {

                foreach(XAttribute attribute in element.Attributes())
                {
                    if(attribute.Name == "id")
                    {
                        string id = attribute.Value;

                        var xml = await req.Browsing("getArtist",id);
                    }
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            parent.Frame.Content = new loginPage(parent);
        }
    }
}
