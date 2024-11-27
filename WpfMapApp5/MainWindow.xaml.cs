using System;
using System.Windows;
using Esri.ArcGISRuntime.UI.Controls;
using System.Windows.Controls;
using System.Windows.Threading;
using Class;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;

namespace ArcGIS_App
{
    public partial class MainWindow : Window
    {
        private MapViewModel _viewModel;
        private FlightPlanListGIS _flightPlanList;
        private GraphicsOverlay _graphicsOverlay;
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the MapViewModel with an empty list of waypoints
            _viewModel = new MapViewModel(MySceneView, TimeLabel, TimelineSlider, new List<WaypointGIS>());
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
        private List<WaypointGIS> LoadWaypoints()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                return FlightPlanListGIS.LoadWaypointsFromFile(filePath);
            }

            return null;
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

        private void LoadWaypoints_Click(object sender, RoutedEventArgs e)
        {
            List<WaypointGIS> loadedWaypoints = LoadWaypoints();
            if (loadedWaypoints != null && loadedWaypoints.Count > 0)
            {
                _viewModel.UpdateWaypoints(loadedWaypoints);
                MessageBox.Show($"Loaded {loadedWaypoints.Count} waypoints successfully!", "File Open");
            }
        }

        private void LoadFlightPlans_Click(object sender, RoutedEventArgs e)
        {
            // Code to handle loading flight plans
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Flight Plan Files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                // Assuming each flight plan is in a file and contains a list of waypoints
                List<WaypointGIS> loadedWaypoints = FlightPlanListGIS.LoadWaypointsFromFile(filePath); // Implement this function

                if (loadedWaypoints != null && loadedWaypoints.Count > 0)
                {
                    _viewModel.UpdateWaypoints(loadedWaypoints);

                    // Now plot the flight path as a polyline on the map

                    MessageBox.Show($"Loaded {loadedWaypoints.Count} waypoints and plotted the flight path.", "Flight Plan Loaded");
                }
            }
        }



        private void LoadParameters_Click(object sender, RoutedEventArgs e)
        {
            // Code to handle loading parameters
            MessageBox.Show("Loading Parameters");
        }
    }
}
