using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace TierSichtung
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadRecent();
        }

        private void Search(object sender, TextChangedEventArgs e)  //verknüpft
        {
            TextBox textBox = sender as TextBox;

            if (textBox.Text == "")
            {
                grid.Children.Clear();
                LoadRecent();
            }
            else
            {
                grid.Children.Clear();
            }

            try
            {

                Animal[] animals = getAnimals();

                if (textBox.Text != "")
                {
                    for (int i = 0; i < animals.Length; i++)
                    {
                        if (animals[i].trivialname.ToLower().Contains(textBox.Text.ToLower()))
                        {
                            Button newBtn = new Button();
                            newBtn.Content = animals[i].trivialname;
                            newBtn.Name = "";
                            grid.Children.Add(newBtn);
                        }
                    }
                }






            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex);
            }

            ManageGrid();
        }

        public void LoadRecent() //verknüpft
        {
            try
            {
                Animal[] animals = getAnimals();

                for (int i = animals.Length - 1; i > animals.Length - 4; i--)
                {
                    Button newBtn = new Button();
                    newBtn.Content = animals[i].trivialname;
                    newBtn.Name = "";
                    grid.Children.Add(newBtn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex);
            }
        }

        private void WindowSizeChange(object sender, SizeChangedEventArgs e)
        {
            ManageGrid();
        }

        void ManageGrid()
        {
            if (grid.Children.Count / 3 <= 3)
            {
                grid.Rows = 3;
            }
            else
            {
                grid.Rows = (grid.Children.Count / 3) + 1;
            }
            if (grid.Rows == 3)
            {
                grid.Height = scroll.ActualHeight;
            }
            else
            {
                grid.Height = scroll.ActualHeight + ((scroll.ActualHeight / 3) * grid.Rows);
            }
        }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            Window1 login = new Window1();
            login.Show();
        }

        private void OpenFilter(object sender, RoutedEventArgs e)
        {
            if (filterListe.Visibility == Visibility.Visible)
            {
                filterListe.Visibility = Visibility.Hidden;
            }
            else
            {
                filterListe.Visibility = Visibility.Visible;
            }
        }

        private void FilterChange(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = sender as ListBox;

            foreach (ListBoxItem selection in list.Items)
            {

            }
        }

        public Animal[] getAnimals()        //Holt die animals aus json und liefer animal array
        {
            string url = "http://localhost/ItSpot_Backend/restAPI.php/animal";

            HttpClient client = new HttpClient();
            var response = client.GetAsync(url).Result;

            string responseString = response.Content.ReadAsStringAsync().Result;

            Animal[] animals = JsonSerializer.Deserialize<Animal[]>(responseString);

            return animals;
        }
    }
}