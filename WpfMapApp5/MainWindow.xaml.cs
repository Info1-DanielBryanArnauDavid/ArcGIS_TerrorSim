using System;
using System.Windows;
using Esri.ArcGISRuntime.UI.Controls;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ArcGIS_App
{
    public partial class MainWindow : Window
    {
        private MapViewModel _viewModel;
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the MapViewModel and pass the required UI elements (SceneView, TimeLabel, TimelineSlider)
            _viewModel = new MapViewModel(MySceneView, TimeLabel, TimelineSlider);
            this.DataContext = _viewModel;

            // Subscribe to the closed event to ensure the app exits when the window is closed
            this.Closed += MainWindow_Closed;

            // Optionally, show a welcome window
            WelcomeWindow welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
        }

        // Ensure application exits when MainWindow is closed
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        // File Menu: New file click handler
        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("New File created!", "File Menu");
        }

        // File Menu: Open file click handler
        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open File dialog triggered!", "File Menu");
        }

        // File Menu: Exit click handler
        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // Close the application
        }

        // Add Menu: Add Terrain click handler
        private void AddTerrain_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Terrain clicked!", "Add Menu");
        }

        // Add Menu: Add Polyline click handler
        private void AddPolyline_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Polyline clicked!", "Add Menu");
        }

        // View Menu: Reset View click handler
        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("View reset to default!", "View Menu");
        }

        // View Menu: Zoom In click handler
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zoom In clicked!", "View Menu");
        }

        // View Menu: Zoom Out click handler
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zoom Out clicked!", "View Menu");
        }

        // Play/Pause Button click handler
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsPlaying)
            {
                // Switch to Play icon ('>')
                PlayPauseText.Text = ">";
                _viewModel.PauseSimulation();
            }
            else
            {
                // Switch to Pause icon ('||')
                PlayPauseText.Text = "||";
                _viewModel.StartSimulation();
            }
        }

        // TimelineSlider value changed (update plane position and time)
        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Update simulation time when the slider is changed
            _viewModel.UpdateSimulationFromSlider(e.NewValue);
        }
    }
}
