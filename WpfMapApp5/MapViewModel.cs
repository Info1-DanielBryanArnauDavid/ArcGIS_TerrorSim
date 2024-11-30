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
using System.Threading.Tasks;

namespace ArcGIS_App
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private Scene _scene;
        private SceneView _sceneView;
        private GraphicsOverlay _graphicsOverlay;
        public Dictionary<string, Graphic> _planeGraphics = new Dictionary<string, Graphic>();
        private DateTime _simulationStartTime;
        private DateTime _simulationEndTime;
        private DispatcherTimer _movementTimer;
        public DateTime _startTime;
        private bool _isPlaying = false;
        private Label _timeLabel;
        private Slider _timelineSlider;
        private bool _areLabelsVisible = false; // Track visibility state
        private bool areFlightPlansVisible = true;
        private System.Windows.Point? _startPoint; // Nullable Point for WPF
        public bool IsPlaying => _isPlaying;
        private List<WaypointGIS> _waypoints;
        public FlightPlanListGIS _flightplans;
        public double _speedMultiplier = 1.0;

        // Constructor
        public MapViewModel(SceneView sceneView, Label timeLabel, Slider timelineSlider, List<WaypointGIS> waypoints, FlightPlanListGIS flightplans)
        {
            _sceneView = sceneView;
            _timeLabel = timeLabel;
            _timelineSlider = timelineSlider;
            _waypoints = waypoints;
            _flightplans = flightplans;

            _sceneView.ViewpointChanged += SceneView_ViewpointChanged;

            // Initialize the scene with a Basemap
            _scene = new Scene(BasemapStyle.ArcGISImagery)
            {
                InitialViewpoint = new Viewpoint(new Envelope(-9.6, 36.0, 3.5, 43.8, SpatialReferences.Wgs84))
            };

            // Add terrain layer (if necessary)
            AddTerrainLayer();

            // Set the scene to the SceneView
            _sceneView.Scene = _scene;

            // Create a GraphicsOverlay for 3D models and add it to the SceneView
            _graphicsOverlay = new GraphicsOverlay
            {
                // We can configure layer scene properties if necessary
                SceneProperties = new LayerSceneProperties(SurfacePlacement.Absolute)
            };
            _sceneView.GraphicsOverlays.Add(_graphicsOverlay);

            // Initialize time label
            _timeLabel.Content = "Time: 00:00:00";

            // Subscribe to the timeline slider value changed event
            _timelineSlider.ValueChanged += TimelineSlider_ValueChanged;

            // Test: Add a sample plane graphic at a fixed location
            AddPlaneGraphic(new MapPoint(-3.7, 40.4, 0), "TestFlight");
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
        public async Task StartSimulation()
        {
            if (!_isPlaying)
            {
                await InitializeSimulation();
                try
                {
                    if (_movementTimer == null)
                    {
                        _movementTimer = new DispatcherTimer();
                        _movementTimer.Tick += MovePlanesAlongPaths;
                    }

                    _movementTimer.Interval = TimeSpan.FromMilliseconds(50 / _speedMultiplier);

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
            var terrainLayer = new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
            _scene.BaseSurface.ElevationSources.Add(terrainLayer);
        }

        // Add the path with elevation and calculate great circle path

        public void MovePlanesAlongPaths(object sender, EventArgs e)
        {
            try
            {
                TimeSpan elapsedTime = DateTime.Now - _startTime;
                double elapsedSeconds = elapsedTime.TotalSeconds * _speedMultiplier;
                var currentSimulationTime = _simulationStartTime.AddSeconds(elapsedSeconds);

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
                        var flightElapsedTime = (currentSimulationTime - flightStartTime).TotalSeconds;
                        var currentPosition = GetPositionAtTime(flightPlan, flightElapsedTime);

                        if (_planeGraphics.TryGetValue(flightPlan.Callsign, out Graphic planeGraphic))
                        {
                            planeGraphic.Geometry = currentPosition;

                            // Update heading
                            if (planeGraphic.Symbol is ModelSceneSymbol modelSymbol)
                            {
                                double heading = CalculateHeading(flightPlan, flightElapsedTime);
                                modelSymbol.Heading = heading;
                            }

                            planeGraphic.IsVisible = true;
                        }
                    }
                    else if (currentSimulationTime > flightEndTime)
                    {
                        var lastWaypoint = flightPlan.Waypoints.Last();
                        double altitude = ParseAltitude(flightPlan.FlightLevels.Last());
                        var finalPosition = new MapPoint(lastWaypoint.Longitude, lastWaypoint.Latitude, altitude, SpatialReferences.Wgs84);

                        if (_planeGraphics.TryGetValue(flightPlan.Callsign, out Graphic planeGraphic))
                        {
                            planeGraphic.Geometry = finalPosition;
                        }
                    }
                }

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

        private double CalculateHeading(FlightPlanGIS flightPlan, double elapsedTime)
        {
            MapPoint currentPosition = GetPositionAtTime(flightPlan, elapsedTime);
            MapPoint nextPosition = GetPositionAtTime(flightPlan, elapsedTime + 1); // 1 second in the future

            if (currentPosition != null && nextPosition != null)
            {
                double dx = nextPosition.X - currentPosition.X;
                double dy = nextPosition.Y - currentPosition.Y;
                return 180+Math.Atan2(dx, dy) * 180 / Math.PI;
            }

            return 0; // Default heading if calculation is not possible
        }

        private MapPoint GetPositionAtTime(FlightPlanGIS flightPlan, double elapsedTime)
        {
            // Ensure waypoints exist
            if (flightPlan.Waypoints.Count < 2)
            {
                return null; // Can't calculate position with less than two waypoints
            }

            // Find the segment of the flight we're currently on
            double segmentDuration = 0;
            for (int i = 0; i < flightPlan.Waypoints.Count - 1; i++)
            {
                // Calculate duration for each segment based on the distance and speed
                double segmentDistance = CalculateDistance(flightPlan.Waypoints[i], flightPlan.Waypoints[i + 1]);
                double segmentSpeed = double.Parse(flightPlan.Speeds[i]);  // Assuming the speeds are in km/h or other units
                double segmentTime = (segmentDistance / segmentSpeed) * 3600; // Time in seconds for this segment

                segmentDuration += segmentTime;

                if (elapsedTime <= segmentDuration)
                {
                    // Interpolate position and altitude for this segment

                    // Calculate the time elapsed on this segment (relative to the start of the segment)
                    double timeInSegment = elapsedTime - (segmentDuration - segmentTime);

                    // Interpolate position along the segment
                    double segmentFraction = timeInSegment / segmentTime;

                    // Get the current waypoint and the next waypoint
                    var startWaypoint = flightPlan.Waypoints[i];
                    var endWaypoint = flightPlan.Waypoints[i + 1];

                    // Interpolate the position (latitude, longitude)
                    double currentLatitude = startWaypoint.Latitude + segmentFraction * (endWaypoint.Latitude - startWaypoint.Latitude);
                    double currentLongitude = startWaypoint.Longitude + segmentFraction * (endWaypoint.Longitude - startWaypoint.Longitude);

                    // Directly use the flight plan altitude values for interpolation
                    double startAltitude = ParseAltitude(flightPlan.FlightLevels[i]); // This is AMSL (Above Mean Sea Level)
                    double endAltitude = ParseAltitude(flightPlan.FlightLevels[i + 1]); // This is AMSL (Above Mean Sea Level)

                    // Interpolate altitude between the two waypoints based on time
                    double currentAltitude = startAltitude + segmentFraction * (endAltitude - startAltitude);

                    // Return the calculated position
                    return new MapPoint(currentLongitude, currentLatitude, currentAltitude, SpatialReferences.Wgs84);
                }
            }

            // If no segment matched, return the last waypoint's position
            var lastWaypoint = flightPlan.Waypoints.Last();
            double lastAltitude = ParseAltitude(flightPlan.FlightLevels.Last());
            return new MapPoint(lastWaypoint.Longitude, lastWaypoint.Latitude, lastAltitude, SpatialReferences.Wgs84);
        }




        private double CalculateDistance(WaypointGIS startWaypoint, WaypointGIS endWaypoint)
        {
            const double R = 6371; // Earth radius in kilometers

            // Convert degrees to radians
            double lat1 = startWaypoint.Latitude * (Math.PI / 180);
            double lon1 = startWaypoint.Longitude * (Math.PI / 180);
            double lat2 = endWaypoint.Latitude * (Math.PI / 180);
            double lon2 = endWaypoint.Longitude * (Math.PI / 180);

            // Haversine formula
            double dlat = lat2 - lat1;
            double dlon = lon2 - lon1;

            double a = Math.Sin(dlat / 2) * Math.Sin(dlat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dlon / 2) * Math.Sin(dlon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // Distance in kilometers
            double distance = R * c;

            return distance;
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
                // Remove existing graphic if it exists
                if (_planeGraphics.TryGetValue(callsign, out Graphic existingGraphic))
                {
                    _graphicsOverlay.Graphics.Remove(existingGraphic);
                }

                Uri modelUri = new Uri("C:\\Users\\bolty\\Desktop\\A320model.obj");
                ModelSceneSymbol planeSymbol = await ModelSceneSymbol.CreateAsync(modelUri, 1.5); // Adjust scale as needed

                planeSymbol.AnchorPosition = SceneSymbolAnchorPosition.Bottom;

                Graphic planeGraphic = new Graphic(startingPoint, planeSymbol);
                _graphicsOverlay.Graphics.Add(planeGraphic);

                // Update the dictionary with the new graphic
                _planeGraphics[callsign] = planeGraphic;

                Console.WriteLine($"Plane graphic for {callsign} added successfully.");
                return planeGraphic;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding plane graphic: {ex.Message}");
                return null;
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
