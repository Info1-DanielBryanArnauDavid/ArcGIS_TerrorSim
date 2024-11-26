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

namespace ArcGIS_App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            // Create an instance of MapViewModel and pass in the SceneView
            var viewModel = new MapViewModel(MySceneView);

            // Set DataContext if necessary (for MVVM pattern)
            this.DataContext = viewModel;
            this.Closed += MainWindow_Closed; // Subscribe to Closed event
            WelcomeWindow welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
        }
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // Ensure application exits when MainWindow is closed
            Application.Current.Shutdown();
        }
        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("New File created!", "File Menu");
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open File dialog triggered!", "File Menu");
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // Close the application
        }

        private void AddTerrain_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Terrain clicked!", "Add Menu");
        }

        private void AddPolyline_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Polyline clicked!", "Add Menu");
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("View reset to default!", "View Menu");
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zoom In clicked!", "View Menu");
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zoom Out clicked!", "View Menu");
        }
    }
}
