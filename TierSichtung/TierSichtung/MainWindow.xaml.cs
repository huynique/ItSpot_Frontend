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

        string filter = "";
        public MainWindow()
        {
            InitializeComponent();
            LoadRecent();
        }

        private void Search(object sender, TextChangedEventArgs e)  //verknüpft
        {
            FinalSearch();

        }

        public void FinalSearch()
        {
            if (searchBox.Text == "")
            {
                grid.Children.Clear();
                LoadAnimals(getAnimals(filter));
            }
            else
            {
                grid.Children.Clear();
            }

            try
            {

                Animal[] animals = getAnimals(filter);

                if (searchBox.Text != "")
                {
                    for (int i = 0; i < animals.Length; i++)
                    {
                        if (animals[i].trivialname.ToLower().StartsWith(searchBox.Text.ToLower()))
                        {
                            CreateButton(animals[i]);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex);
            }

            //ManageGrid();
        }

        public void LoadRecent() //verknüpft
        {
            try
            {
                Animal[] animals = getAnimals();

                for (int i = animals.Length - 1; i > animals.Length || i >= 0; i--)
                {
                    CreateButton(animals[i]);
                }
               // MessageBox.Show("Filter: " + filter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex);
            }
        }

        public void LoadAnimals(Animal[] animals) 
        {
            try
            {
                for (int i = 0; i < animals.Length; i++)
                {
                    CreateButton(animals[i]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex);
            }
        }

        /*
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
        */

        private void CreateButton(Animal animal)
        {
            Button newBtn = new Button();
            newBtn.Content = animal.trivialname + "\n(" + animal.sciencename + ")";
            newBtn.Name = "";
            newBtn.Click += (s, ev) =>
            {
                SightingWindow sightingWindow = new SightingWindow(animal);
                sightingWindow.Show();
            };
            newBtn.Margin = new Thickness(10);
            newBtn.MinHeight = 100;
            newBtn.MinWidth = 100;
            newBtn.FontSize = 16;
            newBtn.FontFamily = new FontFamily("Goudy Std");
            newBtn.Style = (Style)FindResource("GridItemButton");
            grid.Children.Add(newBtn);
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

        public Animal[] getAnimals(string filterlist)    //Holt die animals aus json und liefer animal array
        {
            string url = "http://localhost/ItSpot_Backend/restAPI.php/animal/getFilteredAnimal?" + filterlist;

            HttpClient client = new HttpClient();

            var response = client.GetAsync(url).Result;

            string responseString = response.Content.ReadAsStringAsync().Result;

            Animal[] animals = JsonSerializer.Deserialize<Animal[]>(responseString);

            return animals;
        }

        public Sightings[] getSightings()    //Holt die animals aus json und liefer animal array
        {
            string url = "http://localhost/ItSpot_Backend/restAPI.php/sightings";

            HttpClient client = new HttpClient();

            var response = client.GetAsync(url).Result;

            string responseString = response.Content.ReadAsStringAsync().Result;

            Sightings[] sightings = JsonSerializer.Deserialize<Sightings[]>(responseString);

            return sightings;
        }

        //string url = "http://localhost/ItSpot_Backend/restAPI.php/animal/getFilteredAnimal?family=mammal";

        public Animal[] getAnimals()    //Holt die animals aus json und liefer animal array
        {
            string url = "http://localhost/ItSpot_Backend/restAPI.php/animal";

            HttpClient client = new HttpClient();

            var response = client.GetAsync(url).Result;

            string responseString = response.Content.ReadAsStringAsync().Result;

            Animal[] animals = JsonSerializer.Deserialize<Animal[]>(responseString);

            return animals;
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            AboutUs about = new AboutUs();
            about.Show();
        }

        private void MammalCheck(object sender, RoutedEventArgs e)
        {
            ClearFilter(0);
            if (MammalCheckBox.IsChecked == false)
            {
                filter = "";
                grid.Children.Clear();
                FinalSearch();
                return;
            }
            filter = "family=mammal";
            grid.Children.Clear();
            FinalSearch();
        }

        private void BirdCheck(object sender, RoutedEventArgs e)
        {
            ClearFilter(1);
            if (BirdCheckBox.IsChecked == false)
            {
                filter = "";
                grid.Children.Clear();
                FinalSearch();
                return;
            }
            filter = "family=bird";

            grid.Children.Clear();
            FinalSearch();
        }

        private void FishCheck(object sender, RoutedEventArgs e)
        {
            ClearFilter(2);
            if (FishCheckBox.IsChecked == false)
            {
                filter = "";
                grid.Children.Clear();
                FinalSearch();
                return;
            }
            filter = "family=fish";
            grid.Children.Clear();
            FinalSearch();
        }

        private void ReptileCheck(object sender, RoutedEventArgs e)
        {
            ClearFilter(3);
            if (ReptileCheckBox.IsChecked == false)
            {
                filter = "";
                grid.Children.Clear();
                FinalSearch();
                return;
            }
            filter = "family=reptile";
            grid.Children.Clear();
            FinalSearch();
        }

        public void ClearFilter(int it)
        {
            for (int i = 0; i < filterListe.Items.Count; i++)
            {
                if (filterListe.Items[i] is CheckBox checkBox)
                { 
                    if (i != it)
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
        }
    }
}