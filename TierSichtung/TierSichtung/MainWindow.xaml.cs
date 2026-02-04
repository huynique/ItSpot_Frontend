using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TierSichtung
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient _http = new HttpClient();

        // Für schnell wechselnde Suchanfragen: alte Requests abbrechen
        private CancellationTokenSource _searchCts;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private string filter = "";

        public MainWindow()
        {
            InitializeComponent();

            // Asynchron erst laden, wenn Window sichtbar ist
            this.Loaded += async (_, __) =>
            {
                await LoadRecentAsync();
            };
        }

        // =========================
        // === Such-/Filter-Flow ===
        // =========================

        private async void Search(object sender, TextChangedEventArgs e)
        {
            // Jede neue Eingabe: alte Suche abbrechen
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            try
            {
                await FinalSearchAsync(_searchCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignorieren – ein neuer Request läuft schon
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message);
            }
        }


        private async void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Content is string text)
            {
                filter = text switch
                {
                    "Mammal" => "family=mammal",
                    "Bird" => "family=bird",
                    "Fish" => "family=fish",
                    "Reptile" => "family=reptile",
                    "(Alle)" => "",
                    _ => ""
                };

                _searchCts?.Cancel();
                _searchCts = new CancellationTokenSource();

                try
                {
                    await FinalSearchAsync(_searchCts.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { MessageBox.Show("Fehler: " + ex.Message); }
            }
        }


        /// <summary>
        /// Zentraler Such-/Anzeige-Flow (früher: FinalSearch)
        /// </summary>
        private async Task FinalSearchAsync(CancellationToken ct = default)
        {
            grid.Children.Clear();

            // Sightings per Backend sortieren lassen (empfohlen)
            var sightings = await GetSightingsAsync(
                filterlist: filter,
                orderBy: "date",
                orderDir: "DESC",
                ct: ct);

            // Wenn kein Suchtext → gleich rendern
            if (string.IsNullOrWhiteSpace(searchBox.Text))
            {
                await RenderSightingsAsync(sightings, ct);
                return;
            }

            // Mit Suchtext: clientseitig nach trivialname filtern
            try
            {
                string query = searchBox.Text.Trim().ToLowerInvariant();

                foreach (var s in sightings)
                {
                    ct.ThrowIfCancellationRequested();

                    // Nur wenn Backend trivialname mitliefert (JOIN)
                    var name = s?.trivialname ?? string.Empty;
                    if (name.ToLowerInvariant().StartsWith(query))
                    {
                        CreateButton(s);
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message);
            }
        }

        private async Task RenderSightingsAsync(Sightings[] sightings, CancellationToken ct = default)
        {
            try
            {
                foreach (var s in sightings)
                {
                    ct.ThrowIfCancellationRequested();
                    CreateButton(s);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message);
            }
        }

        private async Task LoadRecentAsync(CancellationToken ct = default)
        {
            try
            {
                // jüngste zuerst über Backend
                var sightings = await GetSightingsAsync(orderBy: "date", orderDir: "DESC", ct: ct);

                foreach (var s in sightings)
                {
                    ct.ThrowIfCancellationRequested();
                    CreateButton(s);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message);
            }
        }

        // =========================
        // === UI / Buttons etc. ===
        // =========================

        private void CreateButton(Sightings s)
        {
            var label = s.trivialname ?? $"Tier #{s.animalid}";
            var dateText = s.date?.ToString("dd.MM.yyyy") ?? "";
            // Datum prüfen ob weniger als 7 Tage her
            if (s.date != null)
            {
                DateTime sightingDate = s.date.Value;
                DateTime currentDate = DateTime.Now;
                TimeSpan difference = currentDate - sightingDate;
                if (difference.TotalDays <= 7)
                {
                    if (difference.TotalDays < 1)
                    {
                        dateText = "Today";
                    }
                    else if ((int)difference.TotalDays == 1)
                    {
                        dateText = "Yesterday";
                    } else 
                        dateText = (int)difference.TotalDays + " Days Ago";
                }
                else
                {
                    dateText = sightingDate.ToString("dd.MM.yyyy");
                }
            }
            else
            {
                dateText = "Date: Unknown";
            }

            var newBtn = new Button
            {
            Content = string.IsNullOrEmpty(dateText) ? label : $"{label} • {dateText}",
                Margin = new Thickness(10),
                MinHeight = 100,
                MinWidth = 100,
                FontSize = 16,
                FontFamily = new FontFamily("Goudy Std"),
                Style = (Style)FindResource("GridItemButton"),

            };

            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri("pack://application:,,,/assets/buttons.jpg", UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();

            newBtn.Background = new ImageBrush(img)
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 0.9
            };


            newBtn.Click += (s_, ev) =>
            {
                var win = new SightingWindow(s);
                win.Show();
            };

            grid.Children.Add(newBtn);
        }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            var login = new Window1();
            login.Show();
        }

        private void PostClick(object sender, RoutedEventArgs e)
        {
            // Aufrufende Stelle
            var dlg = new PostSightingWindow
            {
                Owner = this // z. B. dein Hauptfenster
            };
            var ok = dlg.ShowDialog() == true;
            if (ok)
            {
                // Weiterverarbeiten / Refresh etc.
            }
        }

        private void OpenFilter(object sender, RoutedEventArgs e)
        {
            filterListe.Visibility = (filterListe.Visibility == Visibility.Visible)
                ? Visibility.Hidden
                : Visibility.Visible;
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            var about = new AboutUs();
            about.Show();
        }

        // =========================
        // === HTTP / Back-End  ===
        // =========================

        public async Task<Animal[]> GetAnimalsAsync(string filterlist = "", CancellationToken ct = default)
        {
            string url = string.IsNullOrWhiteSpace(filterlist)
                ? "http://localhost/ItSpot_Backend/restAPI.php/animal"
                : $"http://localhost/ItSpot_Backend/restAPI.php/animal/getFilteredAnimal?{filterlist}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);

            var animals = JsonSerializer.Deserialize<Animal[]>(json, _jsonOptions);
            return animals ?? Array.Empty<Animal>();
        }

        public async Task<Sightings[]> GetSightingsAsync(
    string filterlist = "",
    string orderBy = "date",
    string orderDir = "DESC",
    CancellationToken ct = default)
        {
            string baseUrl = "http://localhost/ItSpot_Backend/restAPI.php/sighting";

            // Query zusammenbauen
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(filterlist))
                parts.Add(filterlist);
            if (!string.IsNullOrWhiteSpace(orderBy))
                parts.Add($"orderBy={Uri.EscapeDataString(orderBy)}");
            if (!string.IsNullOrWhiteSpace(orderDir))
                parts.Add($"orderDir={Uri.EscapeDataString(orderDir)}");

            string url = parts.Count > 0 ? $"{baseUrl}?{string.Join("&", parts)}" : baseUrl;

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var sightings = JsonSerializer.Deserialize<Sightings[]>(json, _jsonOptions);
            return sightings ?? Array.Empty<Sightings>();
        }

    }


}



/*
 * using System.IO;
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


                for (int i = animals.Length - 1; i >= 0; i--)
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
        

private void CreateButton(Animal animal)
        {
            Button newBtn = new Button();
            newBtn.Content = animal.trivialname;
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

        //string url = "http://localhost/ItSpot_Backend/restAPI.php/animal/getFilteredAnimal?family=mammal"; test

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


        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (MammalRadio.IsChecked == true) filter = "family=mammal";
            else if (BirdRadio.IsChecked == true) filter = "family=bird";
            else if (FishRadio.IsChecked == true) filter = "family=fish";
            else if (ReptileRadio.IsChecked == true) filter = "family=reptile";
            else filter = ""; // (Alle)

            grid.Children.Clear();
            FinalSearch();
        }


    }
}
*/