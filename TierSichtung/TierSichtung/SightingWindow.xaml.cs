// GMap.NET (WPF)
using GMap.NET;                              // PointLatLng
using GMap.NET.MapProviders;                 // OpenStreetMapProvider
using GMap.NET.WindowsPresentation;          // GMapControl, GMapMarker
using System;
using System.Runtime.InteropServices;      // DwmSetWindowAttribute
using System.ComponentModel;                 // DesignerProperties
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;                 // Ellipse

namespace TierSichtung
{
    public partial class SightingWindow : Window
    {
        private readonly Sightings _sighting;
        private GMapControl _map;

        public SightingWindow(Sightings sighting)
        {
            InitializeComponent();
            _sighting = sighting ?? throw new ArgumentNullException(nameof(sighting));

            // Deine Labels wie gehabt
            trivial_lbl.Content = _sighting.trivialname ?? $"Tier #{_sighting.animalid}";
            science_lbl.Content = _sighting.sciencename ?? "";
            date_lbl.Content = $"Posted: {FormatDate(_sighting.date)}";
            lastseen_lbl.Content = $"last seen: {FormatDate(_sighting.lastseen)}";
            location_lbl.Content = $"Location: {_sighting.ort ?? "-"}";
            pos_lbl.Content = $"Positive: {_sighting.positive}";
            neg_lbl.Content = $"Negative: {_sighting.negative}";

            if (_sighting.status == 0)
                status_lbl.Content = "pending";
            else if (_sighting.status == 2)
                status_lbl.Content = "approved";
            else
                status_lbl.Content = "rejected";

            // Map zur Laufzeit erstellen (Designer-sicher)
            Loaded += OnLoadedCreateMap;
        }

        private void OnLoadedCreateMap(object sender, RoutedEventArgs e)
        {
            // Im Designer NICHT initialisieren (verhindert SQLite-Interop-Error)
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // Map-Control programmatic erstellen
            _map = new GMapControl
            {
                CanDragMap = true,
                MouseWheelZoomEnabled = true,
                MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter,
                MinZoom = 2,
                MaxZoom = 18,
                Zoom = 12
            };

            // In den Border einhängen
            MapHost.Child = _map;

            // Kein Cache (vermeidet native SQLite im ersten Schritt)
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            _map.MapProvider = OpenStreetMapProvider.Instance;

            // Position bestimmen (Fallback Paderborn)
            var pos = TryGetLatLng(_sighting, out var p) ? p : new PointLatLng(51.7189, 8.7575);
            _map.Position = pos;

            // Marker direkt zur Markers-Collection hinzufügen (kein Overlay in WPF!)
            var marker = new GMapMarker(pos)
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
            _map.Markers.Add(marker);
        }

        private static string FormatDate(object dt)
        {
            if (dt is DateTime d) return d.ToString("dd.MM.yyyy");
            if (dt is string s && DateTime.TryParse(s, out var parsed))
                return parsed.ToString("dd.MM.yyyy");
            return "-";
        }

        /// <summary>
        /// Liest Koordinaten flexibel aus dem Sightings-Objekt.
        /// Erlaubt lat/lng oder latitude/longitude, als double? oder string.
        /// </summary>
        private static bool TryGetLatLng(Sightings s, out PointLatLng p)
        {
            p = default;
            if (s == null) return false;

            var t = s.GetType();
            var latProp = t.GetProperty("lat") ?? t.GetProperty("latitude");
            var lngProp = t.GetProperty("lng") ?? t.GetProperty("longitude");
            if (latProp == null || lngProp == null) return false;

            var latObj = latProp.GetValue(s);
            var lngObj = lngProp.GetValue(s);

            if (latObj is double la && lngObj is double lo)
            {
                p = new PointLatLng(la, lo);
                return true;
            }
            if (latObj is float lfa && lngObj is float lfo)
            {
                p = new PointLatLng(lfa, lfo);
                return true;
            }
            if (latObj is string las && lngObj is string los &&
                double.TryParse(las, NumberStyles.Float, CultureInfo.InvariantCulture, out var la2) &&
                double.TryParse(los, NumberStyles.Float, CultureInfo.InvariantCulture, out var lo2))
            {
                p = new PointLatLng(la2, lo2);
                return true;
            }
            return false;
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private void EnableBlur()
        {
            var windowHelper = new System.Windows.Interop.WindowInteropHelper(this);
            int trueValue = 1;
            // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE / alternative blur flags möglich
            DwmSetWindowAttribute(windowHelper.Handle, 20, ref trueValue, sizeof(int));
        }

    }


}