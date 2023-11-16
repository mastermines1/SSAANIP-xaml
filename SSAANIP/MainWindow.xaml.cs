using System;
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
    public partial class MainWindow : Window{


        public MainWindow(){

            InitializeComponent();

        }

        private void changeTheme(object sender, RoutedEventArgs e) {
            if(this.Background == Brushes.White) {
                this.Background = Brushes.Black; // "#1F1B24";
            }
            else{
                this.Background = Brushes.White;
            }
        }

        private void showSettings(object sender, RoutedEventArgs e)
        {
           if (lbSettings.Visibility == Visibility.Visible)
            {
                lbSettings.Visibility = Visibility.Collapsed;
            }
            else
            {
                lbSettings.Visibility = Visibility.Visible;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Request index = new("100.73.164.110:4533","admin","1.16","getArtist", "test","&id=61dbf9c1dc2a19880b44ed9fc93be334" );
            var xmlDoc = await index.sendRequestAsync();
            
            foreach(XElement element in xmlDoc.Elements().Elements())
            {
                   
            }


            /* if (result.Contains($"\"ok\""))
            {                    

                char[] resultlist = result.ToCharArray();
                List<int> equalsLocation = new List<int>();
                Dictionary<string, string> data = new Dictionary<string, string>();
                for (int i = 0; i < resultlist.Count();i++){
                    if (resultlist[i] == '='){
                        equalsLocation.Add(i);
                    }
                }
                output.Content = result;
                for(int i = 0; i < equalsLocation.Count(); i++) {
                    bool keyFlag = false;
                    bool valueFlag = false;
                    string value = string.Empty;
                    string key = string.Empty;
                    for (int j = 1; j < 1000; j++)
                    {
                        if (!keyFlag)
                        {
                            if (result[equalsLocation[i] - j] != ' ' && result[equalsLocation[i] - j] != '<')
                            {
                                key = (result[equalsLocation[i] - j]) + key;
                                continue;
                            }
                            else
                            {
                                keyFlag = true;
                                break;
                            }
                            break;
                        }
                    }
                    for (int j = 1; j <1000; j++) { 
                        if(!valueFlag){
                            if (result[equalsLocation[i]+1+j] != '\"')
                            {
                                value = value+result[equalsLocation[i]+1+j];
                            }
                            else
                            {
                                valueFlag = true;
                            }
                        }
                        
                        if(keyFlag && valueFlag)
                        {
                            data.Add(key, value);
                            break;
                        }

                        continue;
                    }
                }
                output.Content = data["id"]; 

            } */




            //string id = 

            //getRequest directory = new("100.73.164.110:4533", "admin", "1.16", "getMusicDirectory", "test", $"id=\"{id}\"");


            //165142354302

        }



        public async Task<IEnumerable<XElement>> System(string requestType)
        {
            Request request = new("100.73.164.110:4533", "admin", "1.16", requestType, "test");
            return await request.sendRequestAsync();

        }
        public async Task<IEnumerable<XElement>> Browsing(string requestType,string id)
        {
            if (id != null){
                id = $"&id={id}";
            }
            Request request = new("100.73.164.110:4533", "admin", "1.16", requestType, "test",id);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendCreatePlaylist(string name, string[] songId)
        {
            string extras = $"&name={name}";
            foreach (string sID in songId){
                extras += $"&songID={sID}";
            }
            Request request = new("100.73.164.110:4533", "admin", "1.16", "createPlaylist", "test",extras);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendUpdatePlaylist(string playlistID, string name, string comment, string isPublic, string[] songID, string[] songIndex)
        {
            string extras = string.Empty;
            if (name != null){
                extras += $"&name={name}";
            }
            else if (comment != null){
                extras += $"&comment={comment}";
            }

            Request request = new("100.73.164.110:4533", "admin", "1.16", "updatePlaylist", "test", extras);
            return await request.sendRequestAsync();

        }


    }
}
