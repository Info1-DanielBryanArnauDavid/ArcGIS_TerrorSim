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
using System.Text;

namespace ArcGIS_App
{
    public partial class MainWindow : Window
    {
        private MapViewModel _viewModel;
        private FlightPlanListGIS _flightPlanList;
        private GraphicsOverlay _graphicsOverlay;
        private bool _areLabelsVisible = false; // Track visibility state
        private bool _areFlightPlansVisible = true; // Track visibility state
        private FlightPlanListGIS flightplanlist; //la creme de la creme
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the MapViewModel with an empty list of waypoints
            _viewModel = new MapViewModel(MySceneView, TimeLabel, TimelineSlider, new List<WaypointGIS>(), flightplanlist);
            this.DataContext = _viewModel;

            // Subscribe to the closed event to ensure the app exits when the window is closed
            this.Closed += MainWindow_Closed;

            // Optionally, show a welcome window
            WelcomeWindow welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
        }

        private void ToggleFlightPlans_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleFlightPlanVisibility();
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
                        MessageBox.Show(
                            "Please load waypoints first before loading flight plans.",
                            "Waypoints Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    // Load flight plans into the FlightPlanListGIS object
                    flightplanlist = FlightPlanListGIS.LoadFlightPlansFromFile(flightPlansFilePath, loadedWaypoints);

                    if (flightplanlist != null && flightplanlist.FlightPlans.Count > 0)
                    {
                        // Create a detailed message with all the flight plans' information
                        StringBuilder detailsMessage = new StringBuilder();
                        detailsMessage.AppendLine($"Successfully loaded {flightplanlist.FlightPlans.Count} flight plans.");

                        foreach (var flightPlan in flightplanlist.FlightPlans)
                        {
                            detailsMessage.AppendLine($"Company: {flightPlan.CompanyName}");
                            detailsMessage.AppendLine($"Callsign: {flightPlan.Callsign}");
                            detailsMessage.AppendLine($"Aircraft: {flightPlan.Aircraft}");
                            detailsMessage.AppendLine($"Start Time: {flightPlan.StartTime.ToString("HH:mm:ss")}");
                            detailsMessage.AppendLine("Waypoints and Flight Levels:");

                            for (int i = 0; i < flightPlan.Waypoints.Count; i++)
                            {
                                detailsMessage.AppendLine($"  - Waypoint: {flightPlan.Waypoints[i].ID}, Flight Level: {flightPlan.FlightLevels[i]}, Speed: {flightPlan.Speeds[i]}");
                            }

                            detailsMessage.AppendLine(); // Add extra space between flight plans
                        }

                        // Show the detailed information in a message box
                        MessageBox.Show(detailsMessage.ToString(), "Flight Plans Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "No flight plans could be loaded from the file.",
                            "No Flight Plans",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error loading flight plans: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }

            // Visualize the flight plans (this can be handled in a separate method)
            VisualizeFlightPlans();
        }



        private void VisualizeFlightPlans()
        {
            if (flightplanlist == null || flightplanlist.FlightPlans.Count == 0)
            {
                MessageBox.Show("No flight plans available to visualize. Please load flight plans first.", "No Flight Plans", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var flightPlan in flightplanlist.FlightPlans)
            {
                // Create a path for the flight plan
                List<double> heights = new List<double>();

                foreach (var level in flightPlan.FlightLevels)
                {
                    // Parse the altitude
                    if (level.StartsWith("FL")) // Flight Level format
                    {
                        if (int.TryParse(level.Substring(2), out int fl))
                        {
                            heights.Add((fl * 10) / 0.3048); // Convert FL to meters (1 FL = 100 ft -> meters)
                        }
                    }
                    else if (level.EndsWith("m")) // Altitude in meters
                    {
                        if (double.TryParse(level.Replace("m", ""), out double altitudeMeters))
                        {
                            heights.Add(altitudeMeters); // Use altitude directly in meters
                        }
                    }
                }

                // Create a Polyline for the flight path using great circle interpolation
                List<MapPoint> flightPathPoints = new List<MapPoint>();
                for (int j = 0; j < flightPlan.Waypoints.Count - 1; j++)
                {
                    var startWaypoint = flightPlan.Waypoints[j];
                    var endWaypoint = flightPlan.Waypoints[j + 1];

                    // Calculate great circle points between waypoints
                    var segmentPoints = CalculateGreatCircleWithElevation(
                        new MapPoint(startWaypoint.Longitude, startWaypoint.Latitude, heights.ElementAtOrDefault(j), SpatialReferences.Wgs84),
                        new MapPoint(endWaypoint.Longitude, endWaypoint.Latitude, heights.ElementAtOrDefault(j + 1), SpatialReferences.Wgs84),
                        50 // Number of interpolation points
                    );

                    flightPathPoints.AddRange(segmentPoints);
                }

                // Create a polyline from the calculated points
                var path = new Polyline(flightPathPoints);
                var pathSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, GetRandomColor(), 1); // Blue path
                var flightPathGraphic = new Graphic(path, pathSymbol);

                // Add the flight path to the view model
                _viewModel.AddFlightPathGraphic(flightPathGraphic);

                // Optional: Add some metadata to the graphic if needed
                flightPathGraphic.Attributes["CompanyName"] = flightPlan.CompanyName;
                flightPathGraphic.Attributes["StartTime"] = flightPlan.StartTime;
            }
        }

        private List<MapPoint> CalculateGreatCircleWithElevation(MapPoint startPoint, MapPoint endPoint, int numPoints)
        {
            List<MapPoint> points = new List<MapPoint>();

            double lat1 = startPoint.Y;
            double lon1 = startPoint.X;
            double lat2 = endPoint.Y;
            double lon2 = endPoint.X;

            double dLat = Math.PI * (lat2 - lat1) / 180;
            double dLon = Math.PI * (lon2 - lon1) / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(Math.PI * lat1 / 180) * Math.Cos(Math.PI * lat2 / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Asin(Math.Sqrt(a));

            double radiusEarthKm = 6371; // Radius of Earth in kilometers
            double distanceKm = radiusEarthKm * c;

            for (int i = 0; i <= numPoints; i++)
            {
                double fractionOfDistance = (double)i / numPoints;

                // Interpolate latitude and longitude using spherical interpolation
                double A = Math.Sin((1 - fractionOfDistance) * c) / Math.Sin(c);
                double B = Math.Sin(fractionOfDistance * c) / Math.Sin(c);

                double xLat = A * Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lon1 * Math.PI / 180) +
                              B * Math.Cos(lat2 * Math.PI / 180) * Math.Cos(lon2 * Math.PI / 180);

                double xLon = A * Math.Cos(lat1 * Math.PI / 180) * Math.Sin(lon1 * Math.PI / 180) +
                              B * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(lon2 * Math.PI / 180);

                double yLat = A * Math.Sin(lat1 * Math.PI / 180) + B * Math.Sin(lat2 * Math.PI / 180);

                double interpolatedLat = Math.Atan2(yLat, Math.Sqrt(xLat * xLat + xLon * xLon)) * (180 / Math.PI);
                double interpolatedLon = Math.Atan2(xLon, xLat) * (180 / Math.PI);

                // Add interpolated point with elevation based on its position
                points.Add(new MapPoint(interpolatedLon, interpolatedLat, startPoint.Z + (endPoint.Z - startPoint.Z) * fractionOfDistance, SpatialReferences.Wgs84));
            }

            return points;
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
