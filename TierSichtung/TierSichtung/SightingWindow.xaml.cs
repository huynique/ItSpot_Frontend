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
using System.Windows.Shapes;

namespace TierSichtung
{
    /// <summary>
    /// Interaktionslogik für SightingWindow.xaml
    /// </summary>
    public partial class SightingWindow : Window
    {
        public SightingWindow(string trivialname)
        {
            InitializeComponent();
            this.Title = "Sichtung: " + trivialname;
        }
    }
}
