using System;
using System.Windows;
using System.Drawing;
using Esri.ArcGISRuntime.UI.Controls;
using System.Windows.Controls;
using System.Windows.Threading;
using Class;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System.Linq;

namespace ArcGIS_App
{
    public partial class MainWindow : Window
    {
        private MapViewModel _viewModel;
        private FlightPlanListGIS _flightPlanList;
        private GraphicsOverlay _graphicsOverlay;
        private bool _areLabelsVisible = false; // Track visibility state
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
        private void ToggleWaypointLabels_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleWaypointLabels(); // Call the method to toggle labels
        }

        private void LoadFlightPlans_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select flight plans file
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Flight Plan Files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                string flightPlansFilePath = openFileDialog.FileName;

                try
                {
                    // First, load the waypoints 
                    List<WaypointGIS> loadedWaypoints = _viewModel.GetCurrentWaypoints();

                    if (loadedWaypoints == null || loadedWaypoints.Count == 0)
                    {
                        MessageBox.Show("Please load waypoints first before loading flight plans.", "Waypoints Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Load flight plans using the method in FlightPlanListGIS
                    List<FlightPlanGIS> flightPlans = FlightPlanListGIS.LoadFlightPlansFromFile(flightPlansFilePath, loadedWaypoints);

                    if (flightPlans != null && flightPlans.Count > 0)
                    {
                        // Plot each flight plan
                        foreach (var flightPlan in flightPlans)
                        {
                            // Create a path for the flight plan
                            List<double> heights = new List<double>();

                            foreach (var level in flightPlan.FlightLevels)
                            {
                                // Parse the FLXXX format to get the altitude
                                if (level.StartsWith("FL"))
                                {
                                    if (int.TryParse(level.Substring(2), out int altitude))
                                    {
                                        heights.Add((altitude*10)/0.3048); // Add altitude directly if parsing is successful
                                    }
                                }
                            }

                            // Create a Polyline for the flight path
                            List<MapPoint> flightPathPoints = flightPlan.Waypoints.Select((wp, index) =>
                                new MapPoint(wp.Longitude, wp.Latitude, heights[index], SpatialReferences.Wgs84)).ToList();

                            var flightPath = new Polyline(flightPathPoints);

                            // Create a graphic for the flight path with a unique color for each flight plan
                            var pathSymbol = new SimpleLineSymbol(
                                SimpleLineSymbolStyle.Solid,
                                Color.FromArgb(150, GetRandomColor().R, GetRandomColor().G, GetRandomColor().B),
                                3
                            );

                            var flightPathGraphic = new Graphic(flightPath, pathSymbol);

                            // Add the flight path to the view model
                            _viewModel.AddFlightPathGraphic(flightPathGraphic);

                            // Optional: Add some metadata to the graphic if needed
                            flightPathGraphic.Attributes["CompanyName"] = flightPlan.CompanyName;
                            flightPathGraphic.Attributes["StartTime"] = flightPlan.StartTime;
                        }

                        MessageBox.Show($"Loaded and plotted {flightPlans.Count} flight plans.", "Flight Plans Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("No flight plans could be loaded from the file.", "No Flight Plans", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading flight plans: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // Helper method to generate random colors for flight paths
        private Color GetRandomColor()
        {
            Random random = new Random();
            return Color.FromArgb(
                (byte)random.Next(256),
                (byte)random.Next(256),
                (byte)random.Next(256)
            );
        }


        private void LoadParameters_Click(object sender, RoutedEventArgs e)
        {
            // Code to handle loading parameters
            MessageBox.Show("Loading Parameters");
        }
    }
}
