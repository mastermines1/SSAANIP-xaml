using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace SSAANIP{
    public partial class MainWindow : Page{
        public string socket;
        public string username;
        public string version;
        public string appName;
        readonly RequestMethods req;
        public master parent;
        private string password;
        public Dictionary<string, string> availableArtists = new();
        public Dictionary<string,string> availableAlbums = new();
        public MainWindow(master master, string username, string password){
        InitializeComponent();
            parent = master;
            this.username = username;
            this.password = password;
            req = new(username, password);
            getArtists();
        }
        public async void getArtists(){
            List<string> artistsID = new();
            IEnumerable<XElement> indexes = await req.sendGetIndexes();
            foreach (XElement element in indexes.Elements().Elements().Elements()){ //get every artist id
                foreach (XAttribute attribute in element.Attributes()){
                    if (attribute.Name == "id"){
                        artistsID.Add(attribute.Value.ToString());
                    }
                }
            }
            foreach (string id in artistsID){ //gets the artist name
                IEnumerable<XElement> artistData = await req.sendGetArtist(id);
                string artistName = artistData.Elements().ElementAt(0).FirstAttribute.NextAttribute.Value;
                availableArtists.Add(artistName, id);
            }
            lsArtist.ItemsSource = this.availableArtists.Keys;
        }
        public async void getAlbums(object sender, RoutedEventArgs e){
            string selectedArtist = (sender as ListBox).SelectedItem.ToString();
            string artistID = availableArtists[selectedArtist];
            var artistData = await req.sendGetArtist(artistID);
            foreach (XElement element in artistData.Elements().Elements()){
                availableAlbums.Add(element.Attributes().First().NextAttribute.NextAttribute.NextAttribute.NextAttribute.Value, element.Attributes().First().Value);
            }
            lsAlbums.ItemsSource = this.availableAlbums.Keys;
        }
    }
}
