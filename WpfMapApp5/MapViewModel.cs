using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology; // For SimpleLineSymbol
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls; // Make sure to include this for SceneView
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows; // For System.Windows.Point
using System.Windows.Controls;
using System.Drawing; // For System.Drawing.Color
using Class;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using NetTopologySuite.Index.Quadtree;
using NetTopologySuite.Operation.Overlay;
using static ArcGIS_App.CollisionReportWindow;
using System.Text;

namespace ArcGIS_App
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private Scene _scene;
        private SceneView _sceneView;
        private GraphicsOverlay _graphicsOverlay;
        public Dictionary<string, Graphic> _planeGraphics = new Dictionary<string, Graphic>();
        public Dictionary<string, Graphic> _planeLabelGraphics = new Dictionary<string, Graphic>();
        private DateTime _simulationStartTime;
        private DateTime _simulationEndTime;
        private DispatcherTimer _movementTimer;
        public DateTime _startTime;
        private bool _isPlaying = false;
        private Label _timeLabel;
        private Slider _timelineSlider;
        private bool _areLabelsVisible = false; // Track visibility state
        private bool _arePlaneLabelsVisible = false;
        private bool areFlightPlansVisible = true;
        private System.Windows.Point? _startPoint; // Nullable Point for WPF
        public bool IsPlaying => _isPlaying;
        public List<WaypointGIS> _waypoints;
        public FlightPlanListGIS _flightplans;
        public double _speedMultiplier = 1.0;
        private ModelSceneSymbol _farPlaneSymbol;
        private ModelSceneSymbol _closePlaneSymbol;
        private Quadtree<string> _planeSpatialIndex;
        private DispatcherTimer _orbitTimer;
        private double _currentAngle = 0;
        private const double FAR_THRESHOLD = 10000; // meters
        private bool _isOrbiting = true;
        private int safetyDistance;
        private GraphicsOverlay _securityDistanceOverlay;
        // This dictionary will track the safety distance graphics for each plane by their callsign
        private Dictionary<string, Graphic> _safetyDistanceGraphics = new Dictionary<string, Graphic>();
        private bool isSafetyVisible=false;


        // Constructor
        public MapViewModel(SceneView sceneView, Label timeLabel, Slider timelineSlider, List<WaypointGIS> waypoints, FlightPlanListGIS flightplans)
        {
            _sceneView = sceneView;
            _timeLabel = timeLabel;
            _timelineSlider = timelineSlider;
            _waypoints = waypoints;
            _flightplans = flightplans;
            _sceneView.ViewpointChanged += SceneView_ViewpointChanged;

            _scene = new Scene(BasemapStyle.ArcGISImagery)
            {
                BaseSurface = new Surface()
            };

            // Add terrain layer (if necessary)
            AddTerrainLayer();
            InitializeModelSymbols();
            InitializeSpatialIndex();

            // Set the scene to the SceneView
            _sceneView.Scene = _scene;

            // Create a GraphicsOverlay for 3D models and add it to the SceneView
            _graphicsOverlay = new GraphicsOverlay
            {
                SceneProperties = new LayerSceneProperties(SurfacePlacement.Absolute)
            };
            _sceneView.GraphicsOverlays.Add(_graphicsOverlay);

            // Initialize time label
            _timeLabel.Content = "Time: --:--:--";
            _securityDistanceOverlay = new GraphicsOverlay
            {
                SceneProperties = new LayerSceneProperties(SurfacePlacement.Absolute)
            };
            _sceneView.GraphicsOverlays.Add(_securityDistanceOverlay);

            // Subscribe to the timeline slider value changed event
            _timelineSlider.ValueChanged += TimelineSlider_ValueChanged;
            _orbitTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Update every 50ms
            };
            _sceneView.MouseDoubleClick += SceneView_UserInteractionStarted;
            _sceneView.MouseLeftButtonDown += SceneView_UserInteractionStarted;
            _sceneView.MouseRightButtonDown += SceneView_UserInteractionStarted;
            _sceneView.MouseWheel+= SceneView_UserInteractionStarted;

            // Initialize orbiting
            InitializeOrbiting();
        }

        public void LoadParameters(int valor)
        {
            safetyDistance=valor;
        }
        public List<CollisionData> ConvertReportToCollisionData(string report)
        {
            List<CollisionData> collisionDataList = new List<CollisionData>();

            // Split the report into lines
            string[] lines = report.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // Parse each line and convert it to CollisionData
            foreach (var line in lines)
            {
                // Split each line by the delimiter (assumed to be ' - ')
                var parts = line.Split(new[] { " - " }, StringSplitOptions.None);

                if (parts.Length == 6)
                {
                    // Create a new CollisionData object
                    var collisionData = new CollisionData
                    {
                        Callsign1 = parts[0],
                        Callsign2 = parts[1],
                        CollisionStart = parts[2],  // This will be in "HH:mm:ss" format
                        CollisionEnd = parts[3],    // This will be in "HH:mm:ss" format
                        FLcallsign1 = parts[4],
                        FLcallsign2 = parts[5]
                    };

                    // Add the collision data to the list
                    collisionDataList.Add(collisionData);
                }
            }

            return collisionDataList;
        }
        public async void GenerateReport()
        {
            try
            {
                // Create and show the collision report window immediately
                CollisionReportWindow reportWindow = new CollisionReportWindow();
                reportWindow.Show();

                // Run the collision detection in the background
                await Task.Run(() => GenerateReportInBackground(reportWindow));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while generating the report: {ex.Message}");
            }
        }
        private void GenerateReportInBackground(CollisionReportWindow reportWindow)
        {
            try
            {
                // Initialize the progress variable
                double totalPairs = (_flightplans.FlightPlans.Count * (_flightplans.FlightPlans.Count - 1)) / 2; // Number of unique pairs
                double progressIncrement = 100.0 / totalPairs;  // Calculate how much to increment each time

                StringBuilder collisionReport = new StringBuilder();
                double progress = 0; // Initial progress value

                reportWindow.UpdateProgress(progress);  // Initialize progress bar

                List<CollisionData> collisionDataList = new List<CollisionData>();  // To store collision data

                // Loop through each pair of flight plans
                for (int i = 0; i < _flightplans.FlightPlans.Count; i++)
                {
                    var flightPlan1 = _flightplans.FlightPlans[i];

                    // Check collisions with all subsequent flight plans
                    for (int j = i + 1; j < _flightplans.FlightPlans.Count; j++)
                    {
                        var flightPlan2 = _flightplans.FlightPlans[j];

                        // Detect collisions between the two planes
                        List<Tuple<string, string, DateTime, DateTime>> collisions = CollisionDetection.DetectCollisionsBetweenPlanes(flightPlan1, flightPlan2, _waypoints, safetyDistance * 1852);

                        // Format the detected collisions and add to collisionDataList
                        foreach (var collision in collisions)
                        {
                            CollisionData collisionData = new CollisionData
                            {
                                Callsign1 = flightPlan1.Callsign,
                                Callsign2 = flightPlan2.Callsign,
                                CollisionStart = collision.Item3.ToString("HH:mm:ss"),
                                CollisionEnd = collision.Item4.ToString("HH:mm:ss"),
                                FLcallsign1 = collision.Item1,
                                FLcallsign2 = collision.Item2
                            };
                            collisionDataList.Add(collisionData);
                        }

                        // Update progress bar after each pair is processed
                        progress += progressIncrement;
                        reportWindow.UpdateProgress(progress);
                    }
                }

                // Once all collision detection is done, pass the collected data to the window
                reportWindow.LoadCollisionData(collisionDataList);

            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the background task
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void InitializeOrbiting()
        {
            _orbitTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _orbitTimer.Tick += OrbitCamera;
            _orbitTimer.Start();
        }
        private void SceneView_UserInteractionStarted(object sender, EventArgs e)
        {
            // Only stop orbiting if we're currently in orbital mode
            if (_isOrbiting)
            {
                _orbitTimer.Stop();
                _isOrbiting = false;
            }
        }
        private bool IsPlaneFlying(DateTime currentSimulationTime, FlightPlanGIS flightPlan)
        {
            DateTime flightStartTime = flightPlan.StartTime;
            DateTime flightEndTime = flightPlan.StartTime.AddSeconds(flightPlan.TotalDuration);

            // A plane is flying only if the current simulation time is within the flight duration
            return currentSimulationTime >= flightStartTime && currentSimulationTime <= flightEndTime;
        }

        public void ToggleSecurityDistanceCylinders()
        {
            // Toggle the global visibility flag
            isSafetyVisible = !isSafetyVisible;

            // Apply the visibility setting to all planes (currently flying and newly departing)
            foreach (var planeGraphic in _planeGraphics)
            {
                string callsign = planeGraphic.Key; // Get the plane's callsign
                if (planeGraphic.Value.Geometry is MapPoint planePosition)
                {
                    if (isSafetyVisible)
                    {
                        // Add or update the safety distance circle when visibility is enabled
                        AddOrUpdateSafetyDistanceCircle(planePosition, callsign);
                    }
                    else
                    {
                        // Remove the safety distance circle when visibility is disabled
                        RemoveSafetyDistanceCircle(callsign);
                    }
                }
            }
        }

        private void OrbitCamera(object sender, EventArgs e)
        {
            // Increment the angle more smoothly
            _currentAngle += 0.0005; // Adjust this speed for slower/faster orbit
            if (_currentAngle >= 360) _currentAngle = 0;

            // Convert angle to radians
            double angleRad = _currentAngle * Math.PI / 180;

            // Coordinates for the center of the Earth (approximately)
            double earthLat = 41.3;  // Latitude of the Earth's center (Equator)
            double earthLon = 100;  // Longitude of the Earth's center (Prime Meridian)

            // Orbit radius: A very large orbit for a far-away view (e.g., 10,000 km above the surface)
            double orbitRadius = 10000000; // Orbit radius in meters (10,000 km for a far orbit)

            // Calculate orbital position using trigonometric functions (Circular path around Earth)
            double orbitLat = earthLat; // Keep latitude fixed for a global view (i.e., equator view)
            double orbitLon = earthLon + orbitRadius * Math.Cos(angleRad); // Longitude changes as we orbit

            // Altitude of the camera (in meters) to simulate being far away in space
            double orbitAltitude = 14000000; // 10,000 km altitude for a high orbit

            // Create the camera at orbital position
            Camera orbitCamera = new Camera(
                orbitLat,            // Latitude of the camera
                orbitLon,            // Longitude of the camera
                orbitAltitude,       // Altitude (simulating the far-away orbit)
                GetHeadingToEarth(orbitLat, orbitLon),  // Heading always pointing towards Earth
                0,                  // Pitch (adjust for better visibility)
                270                     // Roll (set to zero)
            );

            // Define a large Envelope around the Earth to simulate a global view
            Envelope orbitEnvelope = new Envelope(
                earthLon - orbitRadius,         // West longitude
                earthLat - orbitRadius,         // South latitude
                earthLon + orbitRadius,         // East longitude
                earthLat + orbitRadius,         // North latitude
                SpatialReferences.Wgs84
            );

            // Create a Viewpoint using the calculated Envelope and Camera
            Viewpoint orbitViewpoint = new Viewpoint(orbitEnvelope, orbitCamera);

            // Update the SceneView with the new viewpoint to simulate orbiting the planet
            _sceneView.SetViewpoint(orbitViewpoint);
        }

        private double GetHeadingToEarth(double currentLat, double currentLon)
        {
            // Coordinates for Earth's center (lat = 0, lon = 0)
            double earthLat = 0.0;
            double earthLon = 0.0;

            // Calculate bearing (heading) from current position to the Earth's center
            double dLon = earthLon - currentLon;
            double dLat = earthLat - currentLat;

            double bearing = Math.Atan2(dLon, dLat) * 180 / Math.PI;

            // Normalize the bearing to a range of 0-360 degrees
            return (bearing + 360) % 360;
        }


        private void InitializeSpatialIndex()
        {
            _planeSpatialIndex = new Quadtree<string>();
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
        private async Task InitializeModelSymbols()
        {
            // Get the base directory of the application
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            // Construct URIs for your model files located in the Resources folder
            Uri detailedModelUri = new Uri(Path.Combine(basePath, "RecursosChulos", "A320model.obj"));

            try
            {
                // Load models with specified sizes
                _closePlaneSymbol = await ModelSceneSymbol.CreateAsync(detailedModelUri, 1.5);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading models: {ex.Message}");
            }
        }

        public void LoadFlightPlanFunc(FlightPlanListGIS plan)
        {
            _flightplans = plan;
        }
        private void SceneView_ViewpointChanged(object sender, EventArgs e)
        {
            UpdateTextSize();
        }
        public void IncreaseSimulationSpeed()
        {
            _speedMultiplier = Math.Min(_speedMultiplier * 2, 512); // Cap at 32x
            UpdateTimerInterval();
        }
        public void ResetMultiplier()
        {
            _speedMultiplier = 1;
            UpdateTimerInterval();
        }

        public void DecreaseSimulationSpeed()
        {
            _speedMultiplier = Math.Max(_speedMultiplier / 2, 0.125); // Min at 1/8x
            UpdateTimerInterval();
        }
        private void UpdateTimerInterval()
        {
            if (_movementTimer != null && _movementTimer.IsEnabled)
            {
                _movementTimer.Interval = TimeSpan.FromMilliseconds(50 / _speedMultiplier);

                // Adjust _startTime to reflect the speed change
                _startTime = DateTime.Now.AddSeconds(-_timelineSlider.Value / _speedMultiplier);
            }
        }

        private async Task InitializeSimulation()
        {
            if (_flightplans == null || !_flightplans.FlightPlans.Any())
            {
                MessageBox.Show("No flight plans available.");
                return;
            }

            // Clear all existing plane graphics
            foreach (var graphic in _planeGraphics.Values)
            {
                _graphicsOverlay.Graphics.Remove(graphic);
            }
            _planeGraphics.Clear();

            _simulationStartTime = _flightplans.FlightPlans.Min(fp => fp.StartTime);
            _simulationEndTime = _flightplans.FlightPlans.Max(fp => fp.StartTime.AddSeconds(fp.TotalDuration));

            foreach (var flightPlan in _flightplans.FlightPlans)
            {
                if (flightPlan.Waypoints != null && flightPlan.Waypoints.Any())
                {
                    var startPoint = new MapPoint(
                        flightPlan.Waypoints[0].Longitude,
                        flightPlan.Waypoints[0].Latitude,
                        ParseAltitude(flightPlan.FlightLevels[0]),
                        SpatialReferences.Wgs84
                    );
                    await AddPlaneGraphic(startPoint, flightPlan.Callsign);
                }
            }

            _timelineSlider.Minimum = 0;
            _timelineSlider.Maximum = (_simulationEndTime - _simulationStartTime).TotalSeconds;
            _timelineSlider.Value = 0;
        }

        private void UpdateTextSize()
        {
            // Get the current viewpoint (camera position) and target scale
            Viewpoint viewpoint = _sceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            MapPoint cameraPosition = viewpoint.TargetGeometry as MapPoint;

            // If the camera position is null, return (don't update text size)
            if (cameraPosition == null) return;

            // For each graphic, calculate the distance to the camera and adjust text size
            foreach (var graphic in _graphicsOverlay.Graphics)
            {
                if (graphic.Symbol is TextSymbol textSymbol)
                {
                    MapPoint labelPosition = graphic.Geometry as MapPoint;

                    // If the label has a valid position (MapPoint), calculate the distance
                    if (labelPosition != null)
                    {
                        // Calculate the 3D distance between the camera and the label
                        double distance = GeometryEngine.Distance(cameraPosition, labelPosition);

                        // Map the distance to a fixed text size that appears consistent in world space
                        double textSize = CalculateTextSizeBasedOnDistance(distance);

                        // Update the font size of the label based on the distance
                        textSymbol.Size = textSize;
                    }
                }
            }
        }

        // This method calculates the text size based on distance from the camera to the label
        private double CalculateTextSizeBasedOnDistance(double distance)
        {
            // The real-world size of the label in meters (for example, 1 meter)
            double realWorldSize = 1.0; // You can adjust this to make the labels physically larger or smaller in world units

            // Calculate the scaling factor (size should decrease as distance increases)
            double sizeFactor = realWorldSize / distance;  // Inverse relation to distance

            // You can optionally limit the range of sizes to prevent the text from becoming too small or too large
            double minSize = 10;  // Minimum size in pixels
            double maxSize = 0; // Maximum size in pixels

            // Apply a scaling factor, ensuring the size is within the desired range
            double textSize = sizeFactor * 200; // Multiply by 1000 to scale the size to visible range

            // Clamp the size to ensure it doesn't become too small or too large
            textSize = Math.Max(minSize, Math.Min(maxSize, textSize));

            return textSize;
        }

        public List<WaypointGIS> GetCurrentWaypoints()
        {
            return _waypoints;
        }
        public void UpdateWaypoints(List<WaypointGIS> waypoints)
        {
            _waypoints = waypoints;
            _graphicsOverlay.Graphics.Clear();

            // Initialize bounds for the envelope
            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLon = double.MaxValue, maxLon = double.MinValue;

            foreach (var waypoint in _waypoints)
            {
                double height = waypoint.ID.Length == 3 ? 5000 : 20000; // Set height based on waypoint ID length
                MapPoint basePoint = new MapPoint(waypoint.Longitude, waypoint.Latitude, 0, SpatialReferences.Wgs84);
                MapPoint topPoint = new MapPoint(waypoint.Longitude, waypoint.Latitude, height, SpatialReferences.Wgs84);

                // Determine line color based on waypoint ID length
                Color lineColor = waypoint.ID.Length == 3 ? Color.DarkRed : Color.White;
                SimpleLineSymbol lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.FromArgb(100, lineColor.R, lineColor.G, lineColor.B), 2); // Transparent line

                // Create a graphic for the vertical line
                Graphic lineGraphic = new Graphic(new Polyline(new List<MapPoint> { basePoint, topPoint }));
                lineGraphic.Symbol = lineSymbol;
                _graphicsOverlay.Graphics.Add(lineGraphic);

                // Create a text symbol for the label positioned slightly above the top point
                TextSymbol labelSymbol = new TextSymbol
                {
                    Text = waypoint.ID,
                    Color = _areLabelsVisible ? (waypoint.ID.Length == 3 ? Color.DarkRed : Color.White) : Color.Transparent,
                    Size = 12,
                    HorizontalAlignment = Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                    FontFamily = "Roboto",
                    FontStyle = Esri.ArcGISRuntime.Symbology.FontStyle.Normal,
                };

                // Position the label slightly above the top point
                MapPoint labelPosition = new MapPoint(topPoint.X, topPoint.Y, topPoint.Z + 1000, SpatialReferences.Wgs84); // Adjust Z value for positioning

                // Create a graphic for the label
                Graphic labelGraphic = new Graphic(labelPosition, labelSymbol);
                _graphicsOverlay.Graphics.Add(labelGraphic);

                // Create a point at the top of the waypoint in the same color as the line
                var pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.FromArgb(100, lineColor.R, lineColor.G, lineColor.B), 3); // Circle with specified color and size
                Graphic pointGraphic = new Graphic(topPoint, pointSymbol);
                _graphicsOverlay.Graphics.Add(pointGraphic);

                // Update the bounds of the envelope
                minLat = Math.Min(minLat, waypoint.Latitude);
                maxLat = Math.Max(maxLat, waypoint.Latitude);
                minLon = Math.Min(minLon, waypoint.Longitude);
                maxLon = Math.Max(maxLon, waypoint.Longitude);
            }

            // Create the envelope based on the min/max latitude and longitude of the waypoints
            Envelope waypointEnvelope = new Envelope(minLon, minLat, maxLon, maxLat, SpatialReferences.Wgs84);

            // Create a viewpoint for the envelope
            Viewpoint waypointViewpoint = new Viewpoint(waypointEnvelope);
            _orbitTimer.Stop();
            // Pan the camera to the new viewpoint asynchronously
            _sceneView.SetViewpointAsync(waypointViewpoint);
        }


        public void ToggleWaypointLabels()
        {
            _areLabelsVisible = !_areLabelsVisible; // Toggle visibility state

            foreach (var graphic in _graphicsOverlay.Graphics)
            {
                if (graphic.Symbol is TextSymbol textSymbol)
                {
                    textSymbol.Color = _areLabelsVisible ? Color.White : Color.Transparent; // Change color based on visibility
                }
            }

        }

        public void TogglePlaneLabels()
        {
            _arePlaneLabelsVisible = !_arePlaneLabelsVisible;
            TogglePlaneLabels(_arePlaneLabelsVisible);
        }
        public async Task StartSimulation()

        {
            if (!_isPlaying)
            {
                try
                {
                    // Ensure the simulation is only initialized once
                    if (_flightplans == null || !_flightplans.FlightPlans.Any())
                    {
                        await InitializeSimulation(); // Only initialize if not done before
                    }

                    // Initialize the DispatcherTimer if it doesn't exist
                    if (_movementTimer == null)
                    {
                        _movementTimer = new DispatcherTimer();
                        _movementTimer.Tick += MovePlanesAlongPaths;
                    }

                    // Set the rendering interval to a fixed 30 FPS
                    _movementTimer.Interval = TimeSpan.FromMilliseconds(33.33);

                    // Adjust the start time based on the current timeline slider value
                    if (_timelineSlider.Value > 0)
                    {
                        _startTime = DateTime.Now.AddSeconds(-_timelineSlider.Value / _speedMultiplier);
                    }
                    else
                    {
                        _startTime = DateTime.Now;
                    }

                    _movementTimer.Start();
                    _isPlaying = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting simulation: {ex.Message}");
                }
            }
        }

        public void PauseSimulation()
        {
            try
            {
                if (_movementTimer != null)
                {

                    _movementTimer.Stop();
                }

                _isPlaying = false;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pausing simulation: {ex.Message}");
            }
        }


        // Add terrain layer to the scene
        private void AddTerrainLayer()
        {
            var terrainLayer = new ArcGISTiledElevationSource(new Uri("http://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/TopoBathy3D/ImageServer"));
            _scene.BaseSurface.ElevationSources.Add(terrainLayer);
        }
        public void MovePlanesAlongPaths(object sender, EventArgs e)
        {
            try
            {
                // Calculate elapsed time
                TimeSpan elapsedTime = DateTime.Now - _startTime;
                double elapsedSeconds = elapsedTime.TotalSeconds * _speedMultiplier;
                var currentSimulationTime = _simulationStartTime.AddSeconds(elapsedSeconds);

                _timeLabel.Content = $"Time: {currentSimulationTime:HH:mm:ss}";
                _timelineSlider.Value = elapsedSeconds;

                bool simulationActive = false;

                foreach (var flightPlan in _flightplans.FlightPlans)
                {
                    if (_planeGraphics.TryGetValue(flightPlan.Callsign, out Graphic planeGraphic))
                    {
                        if (IsPlaneFlying(currentSimulationTime, flightPlan))
                        {
                            simulationActive = true;

                            // Calculate the position and orientation of the plane
                            double flightElapsedTime = (currentSimulationTime - flightPlan.StartTime).TotalSeconds;

                            // Get the current position along the path
                            var currentPosition = GetPositionAlongPath(flightPlan, flightElapsedTime);
                            var nextPosition = GetPositionAlongPath(flightPlan, flightElapsedTime + 1);
                            double heading = CalculateHeading(currentPosition, nextPosition);
                            double pitch = CalculatePitch(currentPosition, nextPosition);
                            double roll = CalculateRoll(flightPlan, flightElapsedTime, heading);

                            // Update the plane's position and orientation
                            planeGraphic.Geometry = currentPosition;
                            UpdatePlaneModel(planeGraphic, _sceneView.Camera, heading, pitch, roll, isSafetyVisible);

                            // Make the plane visible
                            planeGraphic.IsVisible = true;

                            // Update label position and visibility
                            if (_planeLabelGraphics.TryGetValue(flightPlan.Callsign, out Graphic labelGraphic))
                            {
                                labelGraphic.Geometry = currentPosition;
                                labelGraphic.IsVisible = _arePlaneLabelsVisible;
                            }

                            // Update the safety distance circle visibility and position if isSafetyVisible is true
                            if (isSafetyVisible)
                            {
                                // Update or add the safety distance circle at the new position of the plane
                                AddOrUpdateSafetyDistanceCircle(currentPosition, flightPlan.Callsign);
                            }
                            else
                            {
                                // Remove the safety distance circle if not visible
                                RemoveSafetyDistanceCircle(flightPlan.Callsign);
                            }
                        }
                        else
                        {
                            // Hide planes and labels when not flying
                            planeGraphic.IsVisible = false;

                            // Remove or hide the safety distance disk
                            if (_safetyDistanceGraphics.ContainsKey(flightPlan.Callsign))
                            {
                                _safetyDistanceGraphics[flightPlan.Callsign].IsVisible = false;
                            }
                        }
                    }
                }

                // Stop simulation if no planes are active and time is past the end
                if (!simulationActive && currentSimulationTime > _simulationEndTime)
                {
                    _movementTimer.Stop();
                    _isPlaying = false;
                    MessageBox.Show("Simulation Completed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Simulation error: {ex.Message}");
                MessageBox.Show($"Simulation error: {ex.Message}");
            }
        }


        private MapPoint GetPositionAlongPath(FlightPlanGIS flightPlan, double elapsedTime)
        {
            if (flightPlan.Waypoints.Count < 2)
            {
                return null;
            }

            double segmentDuration = 0;
            for (int i = 0; i < flightPlan.Waypoints.Count - 1; i++)
            {
                var startPoint = new MapPoint(flightPlan.Waypoints[i].Longitude, flightPlan.Waypoints[i].Latitude, SpatialReferences.Wgs84);
                var endPoint = new MapPoint(flightPlan.Waypoints[i + 1].Longitude, flightPlan.Waypoints[i + 1].Latitude, SpatialReferences.Wgs84);

                GeodeticDistanceResult distanceResult = GeometryEngine.DistanceGeodetic(
                    startPoint,
                    endPoint,
                    LinearUnits.Kilometers,
                    AngularUnits.Degrees,
                    GeodeticCurveType.Geodesic
                );

                double segmentDistance = distanceResult.Distance;
                double segmentSpeed = double.Parse(flightPlan.Speeds[i]);
                double segmentTime = (segmentDistance / segmentSpeed) * 3600; // Tiempo en segundos

                segmentDuration += segmentTime;

                if (elapsedTime <= segmentDuration)
                {
                    double timeInSegment = elapsedTime - (segmentDuration - segmentTime);
                    double segmentFraction = timeInSegment / segmentTime;

                    MapPoint interpolatedPoint = (MapPoint)GeometryEngine.MoveGeodetic(
                        new[] { startPoint },
                        segmentDistance * segmentFraction,
                        LinearUnits.Kilometers,
                        distanceResult.Azimuth1,
                        AngularUnits.Degrees,
                        GeodeticCurveType.Geodesic
                    )[0];

                    double startAltitude = ConvertFlightLevelToMeters(flightPlan.FlightLevels[i]);
                    double endAltitude = ConvertFlightLevelToMeters(flightPlan.FlightLevels[i + 1]);
                    double interpolatedAltitude = startAltitude + segmentFraction * (endAltitude - startAltitude);

                    return new MapPoint(interpolatedPoint.X, interpolatedPoint.Y, interpolatedAltitude, SpatialReferences.Wgs84);
                }
            }

            var lastWaypoint = flightPlan.Waypoints.Last();
            double lastAltitude = ConvertFlightLevelToMeters(flightPlan.FlightLevels.Last());
            return new MapPoint(lastWaypoint.Longitude, lastWaypoint.Latitude, lastAltitude, SpatialReferences.Wgs84);
        }

        private double ConvertFlightLevelToMeters(string flightLevel)
        {
            // Check if the flight level is in the FLXX format (e.g., FL100, FL350)
            if (flightLevel.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
            {
                string levelStr = flightLevel.Substring(2); // Remove "FL"
                if (double.TryParse(levelStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double flightLevelFeet))
                {
                    // Convert from FLXX (feet) to meters
                    return flightLevelFeet * 100 * 0.3048; // 1 foot = 0.3048 meters
                }
                else
                {
                    throw new FormatException($"Invalid flight level format: {flightLevel}");
                }
            }
            // Check if the flight level is in meters (e.g., "3000m" or "3500.5m")
            else if (flightLevel.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                // Remove the "m" and parse the remaining value
                string levelStr = flightLevel.Substring(0, flightLevel.Length - 1); // Remove "m"
                if (double.TryParse(levelStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double altitudeMeters))
                {
                    // If it's already in meters, return as-is
                    return altitudeMeters;
                }
                else
                {
                    throw new FormatException($"Invalid altitude format: {flightLevel}");
                }
            }
            else
            {
                throw new FormatException($"Invalid flight level or altitude format: {flightLevel}");
            }
        }


        private double CalculateHeading(MapPoint currentPosition, MapPoint nextPosition)
        {
            // Use the Geodesic to calculate bearing (heading) from current position to next position
            double lat1 = currentPosition.Y * Math.PI / 180.0; // Convert to radians
            double lon1 = currentPosition.X * Math.PI / 180.0;
            double lat2 = nextPosition.Y * Math.PI / 180.0;
            double lon2 = nextPosition.X * Math.PI / 180.0;

            double deltaLon = lon2 - lon1;
            double deltaLat = lat2 - lat1;

            double x = Math.Sin(deltaLon) * Math.Cos(lat2);
            double y = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

            double azimuth = 180+Math.Atan2(x, y) * 180.0 / Math.PI; // Convert from radians to degrees

            return (azimuth + 360) % 360; // Normalize to 0-360 degrees
        }

        private double CalculatePitch(MapPoint currentPosition, MapPoint nextPosition)
        {
            // Calculate pitch based on the altitude difference between current and next position
            double deltaZ = nextPosition.Z - currentPosition.Z;
            double horizontalDistance = GeometryEngine.DistanceGeodetic(
                currentPosition,
                nextPosition,
                LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic).Distance;

            // Invert the sign to correct the direction
            return -Math.Atan2(deltaZ, horizontalDistance) * (180 / Math.PI); // Pitch in degrees
        }
        private double CalculateRoll(FlightPlanGIS flightPlan, double elapsedTime, double currentHeading)
        {
            // Use the heading difference between two points to calculate roll
            double nextHeading = CalculateHeading(
                GetPositionAlongPath(flightPlan, elapsedTime),
                GetPositionAlongPath(flightPlan, elapsedTime + 1)
            );

            double deltaHeading = nextHeading - currentHeading;
            deltaHeading = (deltaHeading + 360) % 360; // Normalize to 0-360
            if (deltaHeading > 180) deltaHeading -= 360; // Normalize to -180 to 180

            return deltaHeading * 0.1; // Adjust roll sensitivity
        }
        private void AddOrUpdateSafetyDistanceCircle(MapPoint planePosition, string callsign)
        {
            try
            {
                double safetyDistanceMeters = safetyDistance * 1852; // Convert nautical miles to meters

                // Get the plane's current altitude (Z value)
                double planeAltitude = planePosition.Z;

                // Create the geodesic buffer (circle) around the plane's position
                var circleGeometry = GeometryEngine.BufferGeodetic(
                    planePosition,                         // Plane's position
                    safetyDistanceMeters,                  // Radius (in meters)
                    LinearUnits.Meters,                    // Radius units
                    Double.NaN,                             // No curve offset
                    GeodeticCurveType.Geodesic             // Geodesic curve type (snapped to the surface)
                );

                // Create a simple symbol for the safety distance circle (red outline)
                var circleSymbol = new SimpleFillSymbol(
                    SimpleFillSymbolStyle.Solid,
                    Color.FromArgb(100, 255, 0, 0), // Semi-transparent red
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 2) // Red outline
                );

                // Check if we already have a safety distance circle for this plane
                if (_safetyDistanceGraphics.ContainsKey(callsign))
                {
                    // If it exists, update the geometry (move it to the new position)
                    _safetyDistanceGraphics[callsign].Geometry = circleGeometry;
                }
                else
                {
                    // If the circle doesn't exist, create a new graphic
                    var circleGraphic = new Graphic(circleGeometry, circleSymbol);

                    // Add the graphic to the overlay
                    _graphicsOverlay.Graphics.Add(circleGraphic);

                    // Store the graphic in the dictionary for future updates or removal
                    _safetyDistanceGraphics[callsign] = circleGraphic;
                }

                // Set visibility based on `isSafetyVisible`
                _safetyDistanceGraphics[callsign].IsVisible = isSafetyVisible;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding safety distance circle: {ex.Message}");
            }
        }

        private void RemoveSafetyDistanceCircle(string callsign)
        {
            try
            {
                // If a circle exists for this plane, remove it
                if (_safetyDistanceGraphics.ContainsKey(callsign))
                {
                    // Remove the circle graphic from the overlay
                    _graphicsOverlay.Graphics.Remove(_safetyDistanceGraphics[callsign]);

                    // Remove it from the dictionary
                    _safetyDistanceGraphics.Remove(callsign);
                }
            }
            catch (Exception ex)
            {
            }
        }


        private void UpdatePlaneModel(Graphic planeGraphic, Camera camera, double heading, double pitch, double roll, bool isSafetyVisible)
        {
            // Assume you have a way to get the CallSign for each plane, which is stored in your _planeGraphics dictionary
            string planeCallSign = GetPlaneCallSign(planeGraphic);

            if (planeGraphic.Geometry is MapPoint planePosition)
            {
                // Calculate the distance between the camera and the plane
                double distance = GeometryEngine.DistanceGeodetic(
                    planePosition, camera.Location,
                    LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic).Distance;

                // Update the plane model based on the distance to the camera
                if (distance > FAR_THRESHOLD / 2)
                {
                    // Marker behavior: fades from red to white as it gets closer
                    double size = Math.Clamp(10 - (FAR_THRESHOLD - distance) / 1000, 3, 10);
                    int fadeValue = (int)Math.Clamp((FAR_THRESHOLD - distance) / 10, 0, 255); // Red fades to white
                    var color = Color.FromArgb(255, 255, fadeValue, fadeValue); // Red fades to white

                    var pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, color, size);
                    planeGraphic.Symbol = pointSymbol;
                }
                else
                {
                    // Switch to the detailed 3D model when close
                    planeGraphic.Symbol = _closePlaneSymbol;

                    if (planeGraphic.Symbol is ModelSceneSymbol modelSymbol)
                    {
                        modelSymbol.Heading = heading;
                        modelSymbol.Pitch = pitch;
                        modelSymbol.Roll = roll;
                    }
                }

                // Add or update the safety distance circle if isSafetyVisible is true
                if (isSafetyVisible)
                {
                    // Add or update the red safety distance circle for this plane
                    AddOrUpdateSafetyDistanceCircle(planePosition, planeCallSign);
                }
                else
                {
                    // If safety is not visible, remove the safety distance circle
                    RemoveSafetyDistanceCircle(planeCallSign);
                }
            }
        }


        private string GetPlaneCallSign(Graphic planeGraphic)
        {
            // Assuming you have a dictionary that tracks the CallSigns of planes
            foreach (var plane in _planeGraphics)
            {
                if (plane.Value == planeGraphic)
                {
                    return plane.Key; // Return the CallSign (or ID) associated with this Graphic
                }
            }
            return null; // Return null if no matching plane found
        }


        private double ParseAltitude(string altitudeStr, double terrainHeight = 0)
        {
            // Check if the altitude string has 'FL' (flight level), 'm' (meters), or 'AGL' (above ground level)
            if (altitudeStr.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
            {
                // Flight level (e.g., FL120) - convert from hundreds of feet to meters
                string levelStr = altitudeStr.Substring(2); // Remove "FL"
                if (double.TryParse(levelStr, out double flightLevelFeet))
                {
                    double altitudeMeters = flightLevelFeet * 30.48; // Convert feet to meters (1 foot = 0.3048 meters)
                    return altitudeMeters; // Flight Level (AMSL)
                }
                else
                {
                    throw new FormatException($"Invalid flight level format: {altitudeStr}");
                }
            }
            else if (altitudeStr.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                // Altitude in meters (e.g., 1200.0m) - could be AMSL or AGL, need context
                string metersStr = altitudeStr.Substring(0, altitudeStr.Length - 1); // Remove "m"
                if (double.TryParse(metersStr, out double altitudeMeters))
                {
                    // If no explicit context, assume it's AMSL (Above Mean Sea Level)
                    return altitudeMeters; // Return meters, assuming AMSL if no context
                }
                else
                {
                    throw new FormatException($"Invalid altitude in meters format: {altitudeStr}");
                }
            }
            else if (altitudeStr.EndsWith("AGL", StringComparison.OrdinalIgnoreCase))
            {
                // Altitude in meters Above Ground Level (AGL)
                string aglStr = altitudeStr.Substring(0, altitudeStr.Length - 3); // Remove "AGL"
                if (double.TryParse(aglStr, out double altitudeAGL))
                {
                    // Add terrain height to convert from AGL to AMSL
                    return altitudeAGL + terrainHeight; // Return adjusted altitude (AMSL)
                }
                else
                {
                    throw new FormatException($"Invalid altitude AGL format: {altitudeStr}");
                }
            }
            else
            {
                throw new FormatException($"Unrecognized altitude format: {altitudeStr}");
            }
        }

        public void AddFlightPathGraphic(Graphic flightPathGraphic)
        {
            // Add the flight path graphic to the overlay
            _graphicsOverlay.Graphics.Add(flightPathGraphic);

            // Check if flight plans are loaded properly
            if (_flightplans != null)
            {
                if (_flightplans.FlightPlans.Any())
                {
                    // Initialize the simulation if flight plans exist
                    InitializeSimulation();
                }
                else
                {
                    MessageBox.Show("Flight plans list is empty. Cannot start simulation.");
                }
            }
            else
            {
                MessageBox.Show("Flight plans are not initialized. Cannot start simulation.");
            }
        }
        // Create a simpler approach if ModelSceneSymbol is unavailable
        private async Task<Graphic> AddPlaneGraphic(MapPoint startingPoint, string callsign)
        {
            try
            {
                // Remove existing plane graphic and label graphic if they exist
                if (_planeGraphics.TryGetValue(callsign, out Graphic existingGraphic))
                {
                    _graphicsOverlay.Graphics.Remove(existingGraphic);
                }
                if (_planeLabelGraphics.TryGetValue(callsign, out Graphic existingLabelGraphic))
                {
                    _graphicsOverlay.Graphics.Remove(existingLabelGraphic);
                }

                // Create the plane graphic
                Graphic planeGraphic = new Graphic(startingPoint, _farPlaneSymbol);
                _graphicsOverlay.Graphics.Add(planeGraphic);
                _planeGraphics[callsign] = planeGraphic;

                // Create the label graphic
                TextSymbol labelSymbol = new TextSymbol
                {
                    Text = callsign,
                    Color = System.Drawing.Color.Black,
                    Size = 12,
                    FontWeight = Esri.ArcGISRuntime.Symbology.FontWeight.Bold,
                    HorizontalAlignment = Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                    VerticalAlignment = Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom,
                    BackgroundColor = System.Drawing.Color.White,
                    OutlineColor = System.Drawing.Color.Black,
                    OutlineWidth = 1
                };

                Graphic labelGraphic = new Graphic(startingPoint, labelSymbol);
                labelGraphic.IsVisible = _arePlaneLabelsVisible;
                _graphicsOverlay.Graphics.Add(labelGraphic);
                _planeLabelGraphics[callsign] = labelGraphic;

                // Add the safety distance circle for the new plane if visibility is enabled
                if (isSafetyVisible)
                {
                    AddOrUpdateSafetyDistanceCircle(startingPoint, callsign);  // This ensures the circle is added for new planes
                }

                return planeGraphic;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding plane graphic: {ex.Message}");
                return null;
            }
        }


        private void TogglePlaneLabels(bool isVisible)
        {
            _arePlaneLabelsVisible = isVisible;
            _arePlaneLabelsVisible = isVisible;
            foreach (var planeGraphic in _planeGraphics.Values)
            {
                if (planeGraphic is Graphic labelGraphic)
                {
                    labelGraphic.IsVisible = isVisible;
                }
            }
        }

        public Graphic GetPlaneGraphicForTracking(string callsign)
        {
            if (_planeGraphics.ContainsKey(callsign))
            {
                return _planeGraphics[callsign]; // Return the graphic if the callsign matches
            }

            return null; // Return null if no graphic is found for the callsign
        }
        public void ToggleFlightPlanVisibility()
        {
            areFlightPlansVisible = !areFlightPlansVisible; // Toggle the state

            // Iterate through the graphics and change visibility based on the state
            foreach (var graphic in _graphicsOverlay.Graphics)
            {
                // You can choose to hide only specific graphics that belong to the flight path (if necessary)
                if (graphic.Symbol is SimpleLineSymbol || graphic.Symbol is SimpleMarkerSymbol) // flight path graphics
                {
                    graphic.IsVisible = areFlightPlansVisible; // Set visibility of flight path graphics
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
