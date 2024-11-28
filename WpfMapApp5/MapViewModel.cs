using Esri.ArcGISRuntime.Data;
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

namespace ArcGIS_App
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private double _minTextSize = 10; // Minimum font size
        private double _maxTextSize = 50; // Maximum font size
        private double _minScale = 10000000; // Minimum scale (zoomed out)
        private double _maxScale = 1000; // Maximum scale (zoomed in)
        private Scene _scene;
        private SceneView _sceneView;
        private GraphicsOverlay _graphicsOverlay;
        private Graphic _planeGraphic;  // Plane graphic
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
                InitialViewpoint = new Viewpoint(new Envelope(-3.7038, 40.4168, 2.1734, 41.3851, SpatialReferences.Wgs84)) // Initial view around Madrid and Barcelona
            };

            // Add terrain layer
            AddTerrainLayer();

            _sceneView.Scene = _scene;

            _graphicsOverlay = new GraphicsOverlay
            {
                SceneProperties = new LayerSceneProperties(SurfacePlacement.Absolute) // Use absolute placement
            };
            _sceneView.GraphicsOverlays.Add(_graphicsOverlay);

            // Call the method to add the path
            AddPathWithElevationAndGreatCircle();

            // Add the plane graphic at the starting point
            AddPlaneGraphic(_interpolatedPoints[0]);

            // Initialize the time label
            _timeLabel.Content = "Time: 00:00:00";

            // Bind the slider event to update the plane's position
            _timelineSlider.ValueChanged += TimelineSlider_ValueChanged;
        }
        private void SceneView_ViewpointChanged(object sender, EventArgs e)
        {
            UpdateTextSize();
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



        // Start the simulation (play the plane's movement)
        public void StartSimulation()
        {
            if (!_isPlaying)
            {
                // Set up the timer and start the animation
                _movementTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)  // Update every 100 ms
                };

                _movementTimer.Tick += MovePlaneAlongPath;  // The function to update the plane's position
                _movementTimer.Start();

                _startTime = DateTime.Now;  // Record the start time
                _isPlaying = true;  // Update the state to "playing"
            }
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


        // Pause the simulation (pause the plane's movement)
        public void PauseSimulation()
        {
            if (_isPlaying)
            {
                // Pause the animation
                _movementTimer.Stop();
                _isPlaying = false;  // Update the state to "paused"
            }
        }

        // Add terrain layer to the scene
        private void AddTerrainLayer()
        {
            var terrainLayer = new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
            _scene.BaseSurface.ElevationSources.Add(terrainLayer);
        }

        // Add the path with elevation and calculate great circle path
        private void AddPathWithElevationAndGreatCircle()
        {
            List<(double Longitude, double Latitude, double Height)> waypoints = new List<(double, double, double)>
            {
                (-3.7038, 40.4168, 650), // Madrid
                (-1.0, 41.0, 9000),      // Example intermediate point
                (1.0, 41.2, 10200),      // Example intermediate point
                (2.1734, 41.3851, 5)     // Barcelona
            };

            _interpolatedPoints = new List<MapPoint>();

            // Interpolate between waypoints
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                var startPoint = waypoints[i];
                var endPoint = waypoints[i + 1];

                var segmentPoints = CalculateGreatCircleWithElevation(
                    new MapPoint(startPoint.Longitude, startPoint.Latitude, startPoint.Height, SpatialReferences.Wgs84),
                    new MapPoint(endPoint.Longitude, endPoint.Latitude, endPoint.Height, SpatialReferences.Wgs84),
                    50 // Interpolation points
                );

                _interpolatedPoints.AddRange(segmentPoints);
            }

            // Optionally, add the last waypoint
            _interpolatedPoints.Add(new MapPoint(2.1734, 41.3851, 5, SpatialReferences.Wgs84));

            // Create and add the polyline path
            var path = new Polyline(_interpolatedPoints);
            var pathSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.FromArgb(100, 0, 255, 255), 2); // Blue path
            var pathGraphic = new Graphic(path, pathSymbol);
            _graphicsOverlay.Graphics.Add(pathGraphic);

            var polylineExtent = path.Extent;
            _sceneView.SetViewpoint(new Viewpoint(polylineExtent.GetCenter(), 5000000)); // Adjust scale
        }

        // Calculate great circle with elevation between two points
        private List<MapPoint> CalculateGreatCircleWithElevation(MapPoint startPoint, MapPoint endPoint, int numPoints)
        {
            List<MapPoint> points = new List<MapPoint>();
            double lat1 = startPoint.Y * Math.PI / 180;
            double lon1 = startPoint.X * Math.PI / 180;
            double lat2 = endPoint.Y * Math.PI / 180;
            double lon2 = endPoint.X * Math.PI / 180;
            double h1 = startPoint.Z;
            double h2 = endPoint.Z;

            for (int i = 0; i <= numPoints; i++)
            {
                double fraction = (double)i / numPoints;

                double delta = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((lat2 - lat1) / 2), 2) +
                                                       Math.Cos(lat1) * Math.Cos(lat2) *
                                                       Math.Pow(Math.Sin((lon2 - lon1) / 2), 2)));
                double A = Math.Sin((1 - fraction) * delta) / Math.Sin(delta);
                double B = Math.Sin(fraction * delta) / Math.Sin(delta);

                double x = A * Math.Cos(lat1) * Math.Cos(lon1) + B * Math.Cos(lat2) * Math.Cos(lon2);
                double y = A * Math.Cos(lat1) * Math.Sin(lon1) + B * Math.Cos(lat2) * Math.Sin(lon2);
                double z = A * Math.Sin(lat1) + B * Math.Sin(lat2);

                double newLat = Math.Atan2(z, Math.Sqrt(x * x + y * y)) * 180 / Math.PI;
                double newLon = Math.Atan2(y, x) * 180 / Math.PI;

                double newHeight = h1 + fraction * (h2 - h1); // Linear interpolation for height

                points.Add(new MapPoint(newLon, newLat, newHeight, SpatialReferences.Wgs84));
            }

            return points;
        }

        // Update plane's position when the timeline slider changes
        public void UpdateSimulationFromSlider(double value)
        {
            int waypointIndex = (int)value;

            if (waypointIndex >= 0 && waypointIndex < _interpolatedPoints.Count)
            {
                var currentPoint = _interpolatedPoints[waypointIndex];
                UpdatePlanePosition(currentPoint);
                _timeLabel.Content = $"Time: {TimeSpan.FromSeconds(waypointIndex)}"; // Adjust time display
            }
        }

        // Move the plane along the path at each tick of the timer
        private void MovePlaneAlongPath(object sender, EventArgs e)
        {
            if (_currentWaypointIndex < _interpolatedPoints.Count)
            {
                var currentPoint = _interpolatedPoints[_currentWaypointIndex];
                UpdatePlanePosition(currentPoint);
                _timelineSlider.Value = _currentWaypointIndex;  // Sync the slider with current position

                _currentWaypointIndex++;
            }
            else
            {
                // Stop the simulation when the path is complete
                PauseSimulation();
            }
        }
        private void AddPlaneGraphic(MapPoint startingPoint)
        {
            if (_planeGraphic == null)
            {
                // Crea el gráfico del avión en el punto inicial
                _planeGraphic = new Graphic(startingPoint);

                // Puedes usar un símbolo diferente para el avión, como un símbolo de punto o cualquier otro símbolo
                var planeSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 10); // Un círculo rojo para el avión
                _planeGraphic.Symbol = planeSymbol;

                // Agregar el gráfico de avión al GraphicsOverlay
                _graphicsOverlay.Graphics.Add(_planeGraphic);
            }
        }

        // Update the plane's graphic position
        private void UpdatePlanePosition(MapPoint currentPoint)
        {
            if (_planeGraphic == null)
            {
                _planeGraphic = new Graphic(currentPoint);
                _graphicsOverlay.Graphics.Add(_planeGraphic);
            }
            else
            {
                _planeGraphic.Geometry = currentPoint;
            }
        }
        public void AddFlightPathGraphic(Graphic flightPathGraphic)
        {
            // Add the flight path graphic to the overlay
            _graphicsOverlay.Graphics.Add(flightPathGraphic);
          
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Llama al método para actualizar la simulación desde el valor del slider
            UpdateSimulationFromSlider(e.NewValue);
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
