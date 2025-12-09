using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void Search(object sender, TextChangedEventArgs e)
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
                string[] tiere = File.ReadAllLines("Tiere.csv");

                if (textBox.Text != "")
                {
                    for (int i = 0; i < tiere.Length; i++)
                    {
                        string[] data = tiere[i].Split(';');

                        if (data[0].ToLower().Contains(textBox.Text.ToLower()))
                        {
                            Button newBtn = new Button();
                            newBtn.Content = data[0];
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

        public void LoadRecent()
        {
            try
            {
                string[] tiere = File.ReadAllLines("Tiere.csv");

                for (int i = tiere.Length - 1; i > tiere.Length - 4; i--)
                {
                    string[] data = tiere[i].Split(';');

                    Button newBtn = new Button();
                    newBtn.Content = data[0];
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
    }
}