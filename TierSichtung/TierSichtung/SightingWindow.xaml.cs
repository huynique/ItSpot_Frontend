using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TierSichtung
{
    /// <summary>
    /// Interaktionslogik für SightingWindow.xaml
    /// </summary>
    public partial class SightingWindow : Window
    {

        public SightingWindow(Animal animal)
        {
            InitializeComponent();
            this.Title = animal.sciencename;

            trivial_lbl.Content = animal.trivialname;
            science_lbl.Content = animal.sciencename;
           
        }

        public Animal[] getAnimals()    //Holt die animals aus json und liefer animal array
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
