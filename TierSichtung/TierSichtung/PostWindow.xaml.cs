using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;


namespace TierSichtung
{
    public partial class PostSightingWindow : Window
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly JsonSerializerOptions _js = new JsonSerializerOptions { PropertyNamingPolicy = null };

        private GMapControl _map;
        private GMapMarker _pin;
        private double? _selectedLat;
        private double? _selectedLng;

        public PostSightingWindow()
        {
            InitializeComponent();

            // Einmal User-Agent setzen (Nominatim verlangt das)
            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", "ItSpot/1.0 (can.elezi1@gmail.com)");

            this.Loaded += async (_, __) =>
            {
                // ... dein vorhandener Map + Animal-Lade Code ...
                try
                {
                    AnimalCombo.ItemsSource = await GetAnimalsAsync();  // animal als vorwahlen laden

                    _map = new GMapControl
                    {
                        CanDragMap = true,
                        MouseWheelZoomEnabled = true,
                        MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter,
                        MinZoom = 2,
                        MaxZoom = 19,
                        Zoom = 12
                    };
                    MapHost.Child = _map;

                    GMaps.Instance.Mode = AccessMode.ServerOnly;
                    _map.MapProvider = OpenStreetMapProvider.Instance;

                    _map.Position = new PointLatLng(51.7189, 8.7575); // Paderborn als Standard
                    _map.MouseLeftButtonUp += Map_MouseLeftButtonUp;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Laden der Tiere oder Initialisieren der Karte: " + ex.Message,
                                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }


        private async void Post_Click(object sender, RoutedEventArgs e)    // Sighting posten
        {
            ValidationText.Text = "";

            int animalId = 0;
            if (AnimalCombo.SelectedValue is int id) animalId = id;

            var date = DatePicker.SelectedDate?.ToString("yyyy-MM-dd");
            var ort = string.IsNullOrWhiteSpace(OrtBox.Text) ? null : OrtBox.Text.Trim();
            int.TryParse(CountBox.Text, out var count);

            if (animalId <= 0) { ValidationText.Text = "Bitte ein Tier auswählen."; return; }
            if (string.IsNullOrWhiteSpace(date)) { ValidationText.Text = "Bitte ein Datum wählen."; return; }
            if (string.IsNullOrWhiteSpace(ort)) { ValidationText.Text = "Bitte einen Ort/Adresse eingeben."; return; }

            // DTO zusammenbauen
            var dto = new
            {
                animalid = animalId,
                date = date,
                ort = ort,
                positive = 0,
                negative = 0,
                status = 0,
                count = count,
                lat = _selectedLat,     // null wenn nicht gewählt
                lng = _selectedLng
            };

            try
            {
                var url = "http://localhost/ItSpot_Backend/restAPI.php/sighting";
                var content = new StringContent(JsonSerializer.Serialize(dto, _js), Encoding.UTF8, "application/json");
                var res = await _http.PostAsync(url, content);
                res.EnsureSuccessStatusCode();

                MessageBox.Show("Sighting erfolgreich gepostet :D", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ValidationText.Text = "Fehler beim Posten: " + ex.Message;
            }
        }

        private void Map_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(_map);
            var ll = _map.FromLocalToLatLng((int)p.X, (int)p.Y);

            _selectedLat = ll.Lat;
            _selectedLng = ll.Lng;

            // UI anzeigen
            LatText.Text = _selectedLat.Value.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
            LngText.Text = _selectedLng.Value.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

            // Marker (ersetzt vorhandenen)
            _map.Markers.Clear();
            _pin = new GMapMarker(new PointLatLng(_selectedLat.Value, _selectedLng.Value))
            {
                Shape = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = Brushes.Red,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                },
                Offset = new System.Windows.Point(-7, -7)
            };
            _map.Markers.Add(_pin);
        }

        private async void PreviewMap_Click(object sender, RoutedEventArgs e)
        {
            if (_map == null) return;

            var address = OrtBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Bitte zuerst einen Ort/Adresse eingeben.", "Hinweis",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var geo = await GeocodeAsync(address);
                if (geo == null)
                {
                    MessageBox.Show("Adresse konnte nicht gefunden werden.", "Hinweis",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _selectedLat = geo.Value.lat;
                _selectedLng = geo.Value.lng;

                LatText.Text = _selectedLat.Value.ToString("0.000000", CultureInfo.InvariantCulture);
                LngText.Text = _selectedLng.Value.ToString("0.000000", CultureInfo.InvariantCulture);

                var pos = new PointLatLng(_selectedLat.Value, _selectedLng.Value);
                _map.Position = pos;
                _map.Zoom = Math.Max(_map.Zoom, 14);

                // Marker erneuern
                _map.Markers.Clear();
                _pin = new GMapMarker(pos)
                {
                    Shape = new Ellipse
                    {
                        Width = 14,
                        Height = 14,
                        Fill = Brushes.Red,
                        Stroke = Brushes.White,
                        StrokeThickness = 2
                    },
                    Offset = new System.Windows.Point(-7, -7)
                };
                _map.Markers.Add(_pin);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geocoding-Fehler: " + ex.Message, "Fehler",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static DateTime _lastGeoCall = DateTime.MinValue;

        private async Task<(double lat, double lng)?> GeocodeAsync(string address)  // Nominatim OSM Geocoding für MAP, voll idk
        {
            if (string.IsNullOrWhiteSpace(address)) return null;

            // Throttle: max. 1 Anfrage pro Sekunde
            var delta = DateTime.UtcNow - _lastGeoCall;
            if (delta.TotalMilliseconds < 1100)
                await Task.Delay(1100 - (int)delta.TotalMilliseconds);

            var url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(address)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            // (Header sind bereits in DefaultRequestHeaders gesetzt)

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            _lastGeoCall = DateTime.UtcNow;

            if ((int)res.StatusCode == 429)
                throw new Exception("Zu viele Anfragen (429). Bitte kurz warten und erneut versuchen.");

            if ((int)res.StatusCode == 403)
                throw new Exception("Zugriff verweigert (403). Prüfe User-Agent/Referer/Rate-Limit gemäß Nominatim-Richtlinien.");

            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement;
            if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0) return null;

            var first = arr[0];
            var latStr = first.GetProperty("lat").GetString();
            var lonStr = first.GetProperty("lon").GetString();

            if (double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                return (lat, lng);
            }
            return null;
        }


        public async Task<Animal[]> GetAnimalsAsync()  // holt alle animals aus der datenbank
        {
            string url = "http://localhost/ItSpot_Backend/restAPI.php/animal";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var animals = JsonSerializer.Deserialize<Animal[]>(json);
            return animals ?? Array.Empty<Animal>();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}