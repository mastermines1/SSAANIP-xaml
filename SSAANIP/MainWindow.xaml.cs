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

            output.Content = result;

            if (result.Contains($"\"ok\""))
            {                    
                string key = string.Empty;
                string value = string.Empty;
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
                    for (int j = 1; j < 100; j++)
                    {
                        if (result[equalsLocation[i] - j] != " ") {
                            key.Prepend(result[equalsLocation[i] - j]);
                            continue;
                        }
                        else
                        {
                            keyFlag = true;
                        }
                        if (result[equalsLocation[i]+j] != ' ')
                        {
                            value.Append(result[equalsLocation[i]+j]);
                        }
                        else
                        {
                            valueFlag = true;
                        }
                        if(keyFlag && valueFlag)
                        {
                            break;
                        }

                        continue;
                    }
                }
                output.Content = data["id"]; 

            }




            //string id = 

            //getRequest directory = new("100.73.164.110:4533", "admin", "1.16", "getMusicDirectory", "test", $"id=\"{id}\"");


            //165142354302

        }
    }
}
