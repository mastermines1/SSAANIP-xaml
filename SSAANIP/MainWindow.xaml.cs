using System;
using System.Collections.Generic;
using System.Linq;
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

namespace SSAANIP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window{


        public MainWindow(){

            InitializeComponent();

        }

        private void showTheme(object sender, RoutedEventArgs e){
            if(lbTheme.Visibility == Visibility.Visible){
                lbTheme.Visibility = Visibility.Collapsed;
            }
            else{
                lbTheme.Visibility = Visibility.Visible;
            }

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            getRequest index = new("100.73.164.110:4533","admin","1.16","getIndexes","test");
            var result = await index.sendRequestAsync();


            if (result.Contains($"\"ok\""))
            {
                List<int> quoteLocation = new List<int>;
                foreach (char i in result){
                    if (i == '"'){
                        quoteLocation.Append(i);
                    }
                }
                


            }




            //string id = 

            //getRequest directory = new("100.73.164.110:4533", "admin", "1.16", "getMusicDirectory", "test", $"id=\"{id}\"");


            //165142354302

        }
    }
}
