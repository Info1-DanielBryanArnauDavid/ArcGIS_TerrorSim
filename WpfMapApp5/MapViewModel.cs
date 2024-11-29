using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology; // For SimpleLineSymbol
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls; // Make sure to include this for SceneView
using System;
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

namespace ArcGIS_App
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private Scene _scene;
        private SceneView _sceneView;
        private GraphicsOverlay _graphicsOverlay;
        private Dictionary<string, Graphic> _planeGraphics = new Dictionary<string, Graphic>();
        private DateTime _simulationStartTime;
        private DateTime _simulationEndTime;
        private List<MapPoint> _interpolatedPoints;  // Interpolated path points
        private DispatcherTimer _movementTimer;
        private int _currentWaypointIndex = 0;
        private DateTime _startTime;
        private bool _isPlaying = false;
        private Label _timeLabel;
        private Slider _timelineSlider;
        private bool _areLabelsVisible = false; // Track visibility state
        private bool areFlightPlansVisible = true;
        private System.Windows.Point? _startPoint; // Nullable Point for WPF
        public bool IsPlaying => _isPlaying;
        private List<WaypointGIS> _waypoints;
        private FlightPlanListGIS _flightplans;

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
                InitialViewpoint = new Viewpoint(new Envelope(-3.7038, 40.4168, 2.1734, 41.3851, SpatialReferences.Wgs84))
            };

            // Add terrain layer
            AddTerrainLayer();

            _sceneView.Scene = _scene;

            _graphicsOverlay = new GraphicsOverlay
            {
                SceneProperties = new LayerSceneProperties(SurfacePlacement.Absolute)
            };
            _sceneView.GraphicsOverlays.Add(_graphicsOverlay);

            _timeLabel.Content = "Time: 00:00:00";

            _timelineSlider.ValueChanged += TimelineSlider_ValueChanged;
        }
        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Console.WriteLine($"Slider Value: {_timelineSlider.Value}"); // Check if the slider value changes
        }

        public void LoadFlightPlanFunc(FlightPlanListGIS plan)
        {
            _flightplans = plan;
        }
        private void SceneView_ViewpointChanged(object sender, EventArgs e)
        {
            UpdateTextSize();
        }
        private void InitializeSimulation()
        {
            if (_flightplans == null || !_flightplans.FlightPlans.Any())
            {
                MessageBox.Show("No flight plans available.");
                return;
            }

            // Clear existing plane graphics
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
                    AddPlaneGraphic(startPoint, flightPlan.Callsign);
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

            foreach (var waypoint in _waypoints)
            {
                double height = waypoint.ID.Length == 3 ? 5000 : 20000; // Set height based on waypoint ID length
                MapPoint basePoint = new MapPoint(waypoint.Longitude, waypoint.Latitude, 0, SpatialReferences.Wgs84);
                MapPoint topPoint = new MapPoint(waypoint.Longitude, waypoint.Latitude, height, SpatialReferences.Wgs84);

                Polyline verticalLine = new Polyline(new List<MapPoint> { basePoint, topPoint });

                // Determine line color based on waypoint ID length
                Color lineColor = waypoint.ID.Length == 3 ? Color.DarkRed : Color.White; // Dark red for three-letter names
                SimpleLineSymbol lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.FromArgb(100, lineColor.R, lineColor.G, lineColor.B), 2); // Transparent line

                // Create a graphic for the vertical line
                Graphic lineGraphic = new Graphic(verticalLine);
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
            
            }
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
        public void StartSimulation()
        {
            if (!_isPlaying)
            {
                try
                {
                    _movementTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(50)
                    };
                    _movementTimer.Tick += MovePlanesAlongPaths;
                    _movementTimer.Start();
                    _startTime = DateTime.Now;
                    _isPlaying = true;
                    MessageBox.Show("Simulation Started");
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
                _movementTimer.Stop(); // Stop the timer
                _isPlaying = false;
                MessageBox.Show("Simulation Paused");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pausing simulation: {ex.Message}");
            }
        }

        // Add terrain layer to the scene
        private void AddTerrainLayer()
        {
            var terrainLayer = new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
            _scene.BaseSurface.ElevationSources.Add(terrainLayer);
        }

        // Add the path with elevation and calculate great circle path

        private void MovePlanesAlongPaths(object sender, EventArgs e)
        {
            try
            {
                // Calculate elapsed time and current simulation time
                TimeSpan elapsedTime = DateTime.Now - _startTime;
                double elapsedSeconds = elapsedTime.TotalSeconds;
                var currentSimulationTime = _simulationStartTime.AddSeconds(elapsedSeconds);

                // Update time label and slider
                _timeLabel.Content = $"Time: {currentSimulationTime:HH:mm:ss}";
                _timelineSlider.Value = elapsedSeconds;

                bool simulationActive = false;

                foreach (var flightPlan in _flightplans.FlightPlans)
                {
                    var flightStartTime = flightPlan.StartTime;
                    var flightEndTime = flightPlan.StartTime.AddSeconds(flightPlan.TotalDuration);
                    if (currentSimulationTime >= flightStartTime && currentSimulationTime <= flightEndTime)
                    {
                        simulationActive = true;

                        // Calculate flight-specific elapsed time
                        var flightElapsedTime = (currentSimulationTime - flightStartTime).TotalSeconds;

                        // Get current position
                        var currentPosition = GetPositionAtTime(flightPlan, flightElapsedTime);
                        // Ensure plane graphic exists and update
                        if (_planeGraphics.TryGetValue(flightPlan.Callsign, out Graphic planeGraphic))
                        {
                            planeGraphic.Geometry = currentPosition;
                            planeGraphic.IsVisible = true;
                        }
                        else
                        {
                            Debug.WriteLine($"WARNING: No plane graphic found for {flightPlan.Callsign}");
                        }
                    }
                    else if (currentSimulationTime > flightEndTime)
                    {
                        // Position plane at final destination
                        var lastWaypoint = flightPlan.Waypoints.Last();
                        double altitude = ParseAltitude(flightPlan.FlightLevels.Last());
                        var finalPosition = new MapPoint(
                            lastWaypoint.Longitude,
                            lastWaypoint.Latitude,
                            altitude,
                            SpatialReferences.Wgs84
                        );

                        if (_planeGraphics.TryGetValue(flightPlan.Callsign, out Graphic planeGraphic))
                        {
                            planeGraphic.Geometry = finalPosition;
                            Debug.WriteLine($"Flight {flightPlan.Callsign} reached final destination");
                        }
                    }
                }

                // Only stop if no flights are active and we've passed the end time
                if (!simulationActive && currentSimulationTime > _simulationEndTime)
                {
                    _movementTimer.Stop();
                    _isPlaying = false;
                    Debug.WriteLine("=== Simulation Completed ===");
                    MessageBox.Show("Simulation Completed");
                }

                Debug.WriteLine("=== End of Simulation Tick ===\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Simulation error: {ex.Message}");
                MessageBox.Show($"Simulation error: {ex.Message}");
            }
        }
        private MapPoint GetPositionAtTime(FlightPlanGIS flightPlan, double elapsedSeconds)
        {
            double totalDistance = 0;
            double coveredDistance = 0;

            for (int i = 1; i < flightPlan.Waypoints.Count; i++)
            {
                var start = flightPlan.Waypoints[i - 1];
                var end = flightPlan.Waypoints[i];

                var segmentDistance = GeometryEngine.Distance(
                    new MapPoint(start.Longitude, start.Latitude, 0, SpatialReferences.Wgs84),
                    new MapPoint(end.Longitude, end.Latitude, 0, SpatialReferences.Wgs84)
                );

                totalDistance += segmentDistance;
            }

            double speed = Convert.ToDouble(flightPlan.Speeds[0]);
            coveredDistance = speed * elapsedSeconds / 3600;

            if (coveredDistance >= totalDistance)
            {
                var lastWaypoint = flightPlan.Waypoints.Last();
                double altitude = ParseAltitude(flightPlan.FlightLevels.Last());
                return new MapPoint(lastWaypoint.Longitude, lastWaypoint.Latitude, altitude, SpatialReferences.Wgs84);
            }

            double currentDistance = 0;
            for (int i = 1; i < flightPlan.Waypoints.Count; i++)
            {
                var start = flightPlan.Waypoints[i - 1];
                var end = flightPlan.Waypoints[i];

                var segmentDistance = GeometryEngine.Distance(
                    new MapPoint(start.Longitude, start.Latitude, 0, SpatialReferences.Wgs84),
                    new MapPoint(end.Longitude, end.Latitude, 0, SpatialReferences.Wgs84)
                );

                if (currentDistance + segmentDistance > coveredDistance)
                {
                    double t = (coveredDistance - currentDistance) / segmentDistance;
                    double lat = start.Latitude + t * (end.Latitude - start.Latitude);
                    double lon = start.Longitude + t * (end.Longitude - start.Longitude);

                    double startAltitude = ParseAltitude(flightPlan.FlightLevels[i - 1]);
                    double endAltitude = ParseAltitude(flightPlan.FlightLevels[i]);
                    double altitude = startAltitude + t * (endAltitude - startAltitude);

                    return new MapPoint(lon, lat, altitude, SpatialReferences.Wgs84);
                }

                currentDistance += segmentDistance;
            }

            return new MapPoint(flightPlan.Waypoints.Last().Longitude, flightPlan.Waypoints.Last().Latitude, ParseAltitude(flightPlan.FlightLevels.Last()), SpatialReferences.Wgs84);
        }
        private double ParseAltitude(string altitudeStr)
        {
            // Check if the altitude string has 'FL' (flight level) or 'm' (meters)
            if (altitudeStr.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
            {
                // Flight level (e.g., FL120) - convert from hundreds of feet to meters
                string levelStr = altitudeStr.Substring(2); // Remove "FL"
                if (double.TryParse(levelStr, out double flightLevelFeet))
                {
                    return flightLevelFeet * 30.48; // Convert feet to meters (1 foot = 0.3048 meters)
                }
                else
                {
                    throw new FormatException($"Invalid flight level format: {altitudeStr}");
                }
            }
            else if (altitudeStr.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                // Altitude in meters (e.g., 1200.0m)
                string metersStr = altitudeStr.Substring(0, altitudeStr.Length - 1); // Remove "m"
                if (double.TryParse(metersStr, out double altitudeMeters))
                {
                    return altitudeMeters; // Already in meters, return as is
                }
                else
                {
                    throw new FormatException($"Invalid altitude in meters format: {altitudeStr}");
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

        private void AddPlaneGraphic(MapPoint startingPoint, string callsign)
        {
            var planeGraphic = new Graphic(startingPoint);
            var planeSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Red, 10);
            planeGraphic.Symbol = planeSymbol;

            // Add the graphic to the overlay
            _graphicsOverlay.Graphics.Add(planeGraphic);

            // Store the graphic in the dictionary using the callsign as the key
            if (!_planeGraphics.ContainsKey(callsign))
            {
                _planeGraphics.Add(callsign, planeGraphic);  // Add to dictionary
            }
            else
            {
                // Optionally handle the case where the callsign already exists in the dictionary
                Console.WriteLine($"Graphic for callsign {callsign} already exists.");
            }
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
