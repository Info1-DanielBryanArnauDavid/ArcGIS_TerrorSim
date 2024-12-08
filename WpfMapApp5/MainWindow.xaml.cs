using System;
using System.Windows;
using System.Drawing;
using Esri.ArcGISRuntime.UI.Controls;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;
using Class;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System.Linq;
using System.Text;
using Esri.ArcGISRuntime.Mapping;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ArcGIS_App
{
    public partial class MainWindow : Window
    {
        public MapViewModel _viewModel;
        private FlightPlanListGIS flightplanlist; //la creme de la creme
        private DispatcherTimer _movementTimer;
        private bool _isTrackingPlane = false;
        private FlightPlanGIS _currentFlightPlan;
        private OrbitGeoElementCameraController _orbitCameraController;
        private string _currentCallsign; // To store the callsign of the plane being tracked
        public static MainWindow Current { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            Current = this;

            // Initialize the MapViewModel with an empty list of waypoints
            _viewModel = new MapViewModel(MySceneView, TimeLabel, TimelineSlider, new List<WaypointGIS>(), flightplanlist);
            this.DataContext = _viewModel;

            // Initialize the movement timer with an interval to update position in real-time
            _movementTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Update every 100ms (adjust as needed)
            };
            _movementTimer.Tick += CameraTrackingTimer_Tick;

            MySceneView.ViewpointChanged += MySceneView_ViewpointChanged;

            // Subscribe to the closed event to ensure the app exits when the window is closed
            this.Closed += MainWindow_Closed;

            // Optionally, show a welcome window
            WelcomeWindow welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
        }
        private void MySceneView_ViewpointChanged(object sender, EventArgs e)
        {
        }
        private void UpdateCameraViewpoint(MapPoint planePosition)
        {
            var viewpoint = new Viewpoint(planePosition, 100000); // Set zoom level (10000 can be adjusted)
            MySceneView.SetViewpointAsync(viewpoint);
        }

        public void StartTrackingPlane(FlightPlanGIS selectedFlightPlan)
        {
            if (selectedFlightPlan == null || selectedFlightPlan == _currentFlightPlan)
            {
                StopTracking(); // Stop tracking if the plane is already being tracked or no plane is selected.
            }
            else
            {
                _currentFlightPlan = selectedFlightPlan;
                _currentCallsign = selectedFlightPlan.Callsign;
                _isTrackingPlane = true;

                if (_viewModel._planeGraphics.TryGetValue(_currentCallsign, out Graphic planeGraphic))
                {
                    // Configure the orbit camera controller to track the plane
                    _orbitCameraController = new OrbitGeoElementCameraController(planeGraphic, 300)
                    {
                        CameraPitchOffset = 45,
                        MinCameraDistance = 100,
                        MaxCameraDistance = 10000
                    };

                    MySceneView.CameraController = _orbitCameraController;

                    // Subscribe to the ViewpointChanged event if needed (optional)
                    MySceneView.ViewpointChanged += MySceneView_ViewpointChanged;
                }

                // Start listening to mouse click events to stop tracking
                MySceneView.MouseLeftButtonDown += SceneView_MouseLeftButtonDown;
            }
        }

        private void SceneView_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Check if we're tracking a plane
            if (_isTrackingPlane)
            {
                // Stop tracking the plane
                StopTracking();  // Stop tracking the plane when the left mouse click occurs

                // Optionally: Reset other properties or behaviors when stopping the tracking.
            }
        }


        private void StopTracking()
        {
            _currentFlightPlan = null;
            _currentCallsign = null;
            _isTrackingPlane = false;

            // Reset to default viewpoint or another desired viewpoint
            var viewpoint = new Viewpoint(new Envelope(-9.6, 36.0, 3.5, 43.8, SpatialReferences.Wgs84));
            MySceneView.SetViewpointAsync(viewpoint);

            // Reset camera controller to default (optional)
            MySceneView.CameraController = new GlobeCameraController();

            // Clean up or reset other tracking-related settings as needed
            _orbitCameraController = null;

            // Unsubscribe from the ViewpointChanged event if needed
            MySceneView.ViewpointChanged -= MySceneView_ViewpointChanged;

            // Unsubscribe from MouseLeftButtonDown to stop further tracking
            MySceneView.MouseLeftButtonDown -= SceneView_MouseLeftButtonDown;
        }

        private void UpdatePlanePosition()
        {
            // Get the plane's graphic from the MapViewModel
            var planeGraphic = _viewModel.GetPlaneGraphicForTracking(_currentCallsign);
            if (planeGraphic == null)
                return;  // If no graphic is found, exit

            // Get the current position of the plane (from the graphic)
            var planePosition = planeGraphic.Geometry as MapPoint;
            if (planePosition == null)
                return;

            // Update the camera's viewpoint to follow the plane's position
            UpdateCameraViewpoint(planePosition);
        }

        private void CameraTrackingTimer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("TICK");
            if (_isTrackingPlane)
            {
                UpdatePlanePosition();
            }
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
                PlayPauseText.Text = "▶";
                _viewModel.PauseSimulation();
                _viewModel.ResetMultiplier();
                UpdateSpeedLabel();
            }
            else
            {
                // Switch to Pause icon ('||')
                PlayPauseText.Text = "❚❚";
                _viewModel.StartSimulation();
            }
        }

        private void ResetButton_Click(object sender,RoutedEventArgs e)
        {
            _viewModel.ResetSimulation();
            ResetButton.IsEnabled = false;
        }

        // TimelineSlider value changed (update plane position and time)
        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TimelineSlider.Value > 0)
            {
                ResetButton.IsEnabled = true;
            }
        }

        private void LoadWaypoints_Click(object sender, RoutedEventArgs e)
        {
            List<WaypointGIS> loadedWaypoints = LoadWaypoints();
            if (loadedWaypoints != null && loadedWaypoints.Count > 0)
            {
                _viewModel.UpdateWaypoints(loadedWaypoints);
                MessageBox.Show($"Loaded {loadedWaypoints.Count} waypoints successfully!", "File Open");
                WaypointsClick.IsEnabled=true;
            }
        }
        private void ToggleWaypointLabels_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleWaypointLabels(); // Call the method to toggle labels
        }
        private void ReportGenerate_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.GenerateReportAsync();
            UpdateSpeedLabel();
        }
        private async void Autosolve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int maxAttempts = 10;
                int attempts = 0;
                bool messageBoxShown = false; // Flag to track if message box is shown

                while (attempts < maxAttempts || _viewModel.Solved!=true)
                {
                    attempts++;

                    // Trigger GenerateReport to detect collisions
                    Debug.WriteLine($"[INFO] Attempt {attempts}: Running GenerateReport...");
                    await _viewModel.GenerateReportAsync(); // Wait for the report to finish

                    // Wait for CollisionReportWindow to appear and for ProgressBar to reach max
                    CollisionReportWindow collisionWindow = null;
                    while (true)
                    {
                        // Check if CollisionReportWindow is present
                        collisionWindow = Application.Current.Windows.OfType<CollisionReportWindow>().FirstOrDefault();
                        if (collisionWindow != null && collisionWindow.IsLoaded)
                        {
                            // Check if ProgressBar has reached maximum
                            if (collisionWindow.CollisionProgressBar.Value == collisionWindow.CollisionProgressBar.Maximum)
                            {
                                Debug.WriteLine("[INFO] CollisionReportWindow progress complete. Automatically resolving collisions...");
                                await Task.Delay(300); // Wait 300ms after progress bar reaches max to ensure everything settles

                                // Trigger the collision resolution logic programmatically
                                collisionWindow.FixCollisionsButton_Click(sender, e); // Automatically resolve collisions
                                break; // Break from the loop once collision resolution is triggered
                            }
                        }
                        await Task.Delay(200); // Poll every 200ms to check the progress bar
                    }

                    // Wait briefly for the fixes to apply and give time for processing
                    await Task.Delay(1000); // Allow time for collision resolution (adjust as needed)

                    // Check if collisions have been resolved
                    if (_viewModel.Solved || collisionWindow.Solved)
                    {
                        Debug.WriteLine("[INFO] All collisions resolved.");
                        break; // Exit the loop if all collisions are resolved
                    }
                    else
                    {
                        Debug.WriteLine("[INFO] Collisions still present. Continuing...");
                        // Continue the process for another attempt
                    }

                    // If no collisions and no message box has been shown
                    if (!messageBoxShown && collisionWindow.CollisionProgressBar.Value == collisionWindow.CollisionProgressBar.Maximum)
                    {
                        // Show message box for safe to publish
                        MessageBox.Show("No collision data, safe to publish.", "No Collisions", MessageBoxButton.OK, MessageBoxImage.Information);
                        messageBoxShown = true; // Set the flag to prevent further message boxes
                        break; // Exit the loop after showing the message box
                    }
                }

                // Notify the user when all collisions are resolved
                if (_viewModel.Solved)
                {
                    MessageBox.Show("All collisions have been resolved.", "Autosolve Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Autosolve reached the maximum number of attempts (10). Some collisions may still be unresolved.", "Autosolve Incomplete", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // Log error if an exception occurs during the process
                Debug.WriteLine($"[ERROR] {ex.Message}");
            }
        }


        private void OpenGithubRepo_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/Info1-DanielBryanArnauDavid/ArcGIS_TerrorSim/tree/Final";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        private void TogglePlaneLabels_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.TogglePlaneLabels(); // Call the method to toggle labels
        }
        private void GoodLuck_Click(object sender, RoutedEventArgs e)
        {
            var welcome = new WelcomeWindow();
            welcome.Show();
        }
        private void Safety_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleSecurityDistanceCylinders();
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
                    _viewModel.LoadFlightPlanFunc(flightplanlist);
                    FlightPlansClick.IsEnabled = true;
                    MessageBox.Show("We encourage the user to check for collisions by generating a manual report");
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

            VisualizeFlightPlans();
        }

        public void LoadUpdatedAlso(FlightPlanListGIS nuevalista)
        {
            flightplanlist = nuevalista;
            VisualizeFlightPlans();
        }
        private void SaveFlightPlans_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SaveFlightPlans();
        }
        private void SaveWaypoints_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SaveWaypoints();
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
                        if (int.TryParse(level.Substring(2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out int fl))
                        {
                            // Convert FL to meters (1 FL = 100 ft -> meters), multiply by 30.48
                            heights.Add(fl * 30.48); // Correct conversion from flight level to meters
                        }
                    }
                    else if (level.EndsWith("m")) // Altitude in meters
                    {
                        if (double.TryParse(level.Replace("m", ""), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,out double altitudeMeters))
                        {
                            heights.Add(altitudeMeters); // Use altitude directly in meters
                        }
                    }
                }

                // Ensure there are enough heights to match the number of waypoints
                // If there are fewer heights than waypoints, we can replicate the last height for remaining waypoints
                while (heights.Count < flightPlan.Waypoints.Count)
                {
                    heights.Add(heights.Last()); // Duplicate the last height
                }

                // Create a Polyline for the flight path using great circle interpolation
                List<MapPoint> flightPathPoints = new List<MapPoint>();
                for (int j = 0; j < flightPlan.Waypoints.Count - 1; j++)
                {
                    var startWaypoint = flightPlan.Waypoints[j];
                    var endWaypoint = flightPlan.Waypoints[j + 1];

                    // Calculate great circle points between waypoints with elevation
                    var segmentPoints = CalculateGreatCircleWithElevation(
                        new MapPoint(startWaypoint.Longitude, startWaypoint.Latitude, heights.ElementAtOrDefault(j), SpatialReferences.Wgs84),
                        new MapPoint(endWaypoint.Longitude, endWaypoint.Latitude, heights.ElementAtOrDefault(j + 1), SpatialReferences.Wgs84),
                        50 // Number of interpolation points
                    );

                    flightPathPoints.AddRange(segmentPoints);
                }

                // Create a polyline from the calculated points
                var path = new Polyline(flightPathPoints);
                var pathSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, GetRandomColor(), 1);
                var flightPathGraphic = new Graphic(path, pathSymbol);

                // Add the flight path to the view model
                _viewModel.AddFlightPathGraphic(flightPathGraphic);

                // Optional: Add some metadata to the graphic if needed
                flightPathGraphic.Attributes["CompanyName"] = flightPlan.CompanyName;
                flightPathGraphic.Attributes["StartTime"] = flightPlan.StartTime;
            }
        }

        private void IncreaseSpeedButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IncreaseSimulationSpeed();
            UpdateSpeedLabel();
        }

        private void DecreaseSpeedButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DecreaseSimulationSpeed();
            UpdateSpeedLabel();
        }

        public void UpdateSpeedLabel()
        {
            SpeedMultiplierLabel.Content = $"Speed: {_viewModel._speedMultiplier}x";
        }
        private void EnableControlButtons(bool enable)
        {
            PlayPauseButton.IsEnabled = enable;
            IncreaseSpeedButton.IsEnabled = enable;
            DecreaseSpeedButton.IsEnabled = enable;
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
        private void ShowFlightPlanDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the new FlightPlanDetailsWindow and pass the loaded flight plans to it, along with the MainWindow reference
            var flightPlanDetailsWindow = new FlightPlanDetailsWindow(flightplanlist.FlightPlans, this);
            flightPlanDetailsWindow.Show();
        }

        // Helper method to generate random colors for flight paths
        private Color GetRandomColor()
        {
            Random random = new Random();

            // Generate a random alpha (transparency) value between 0 (transparent) and 255 (opaque)
            byte alpha = 200;

            // Generate random red, green, and blue color values
            byte red = (byte)random.Next(256);
            byte green = (byte)random.Next(256);
            byte blue = (byte)random.Next(256);

            // Return a color with random RGBA values, where A (alpha) is the transparency
            return Color.FromArgb(alpha, red, green, blue);
        }
        private void LoadParameters_Click(object sender, RoutedEventArgs e)
        {
            Window dialog = new Window
            {
                Title = "Safety Distance",
                Width = 300,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            panel.Children.Add(new TextBlock { Text = "Safety Distance (NM):" });

            ComboBox parameterBox = new ComboBox
            {
                Margin = new Thickness(5),
                IsEditable = true // Permitir entrada manual
            };

            // Agregar elementos predefinidos
            parameterBox.Items.Add("1");
            parameterBox.Items.Add("2");
            parameterBox.Items.Add("3");
            parameterBox.Items.Add("4");
            parameterBox.Items.Add("5");
            parameterBox.Items.Add("8");
            parameterBox.Items.Add("10");

            // Crear los botones
            Button okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(5), IsEnabled = false };
            Button cancelButton = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(5) };

            // Control de botón OK: habilitar solo con entrada válida
            parameterBox.Loaded += (s, args) =>
            {
                // Obtener el TextBox interno asociado al ComboBox
                if (parameterBox.Template.FindName("PART_EditableTextBox", parameterBox) is TextBox editableTextBox)
                {
                    // Evento para detectar cambios en el texto
                    editableTextBox.TextChanged += (textSender, textArgs) =>
                    {
                        okButton.IsEnabled = IsPositiveTwoDigitInteger(editableTextBox.Text); // Habilitar si válido
                    };
                }
            };

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Eventos para los botones
            okButton.Click += (s, args) => { dialog.DialogResult = true; };
            cancelButton.Click += (s, args) => { dialog.DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(parameterBox);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;

            if (dialog.ShowDialog() == true)
            {
                string param1 = parameterBox.Text;
                if (int.TryParse(param1, out int safetyDistance) && safetyDistance >= 1 && safetyDistance <= 99)
                {
                    _viewModel.LoadParameters(safetyDistance);
                    EnableControlButtons(true);
                    ViewMenuItem.IsEnabled = true;
                    OptionsMenuItem.IsEnabled = true;
                }
            }
        }

        // Método auxiliar para validar entrada: entero positivo con máximo 2 dígitos
        private bool IsPositiveTwoDigitInteger(string text)
        {
            return int.TryParse(text, out int number) && number >= 1 && number <= 99;
        }


    }
}
