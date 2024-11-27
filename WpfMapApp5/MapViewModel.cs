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

        private System.Windows.Point? _startPoint; // Nullable Point for WPF

        public bool IsPlaying => _isPlaying;
        private List<WaypointGIS> _waypoints;

        public MapViewModel(SceneView sceneView, Label timeLabel, Slider timelineSlider, List<WaypointGIS> waypoints)
        {
            _sceneView = sceneView;
            _timeLabel = timeLabel;
            _timelineSlider = timelineSlider;
            _waypoints = waypoints;
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
            double textSize = sizeFactor * 20; // Multiply by 1000 to scale the size to visible range

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
        public void UpdateWaypoints(List<WaypointGIS> waypoints)
        {
            _waypoints = waypoints;

            _graphicsOverlay.Graphics.Clear();

            foreach (var waypoint in _waypoints)
            {
                // Create the base point and top point for the vertical line
                MapPoint basePoint = new MapPoint(waypoint.Longitude, waypoint.Latitude, 0, SpatialReferences.Wgs84);
                MapPoint topPoint = new MapPoint(waypoint.Longitude, waypoint.Latitude, 10000, SpatialReferences.Wgs84); // 10000 meters high

                // Create a line geometry
                Polyline verticalLine = new Polyline(new List<MapPoint> { basePoint, topPoint });

                // Create a graphic for the vertical line with light gray color
                Graphic lineGraphic = new Graphic(verticalLine);
                SimpleLineSymbol lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.LightGray, 2); // Light gray line
                lineGraphic.Symbol = lineSymbol;

                // Add the line graphic to the overlay
                _graphicsOverlay.Graphics.Add(lineGraphic);

                // Create a text symbol for the label with a cleaner font style
                TextSymbol labelSymbol = new TextSymbol
                {
                    Text = waypoint.ID,
                    Color = Color.Black,  // Set label color to black
                    Size = 14,  // Initial size, will be adjusted on zoom
                    HorizontalAlignment = Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                    FontFamily = "Roboto",  // Use a cleaner font (Arial)
                    FontStyle = Esri.ArcGISRuntime.Symbology.FontStyle.Normal, // Regular style
                };

                // Create a graphic for the label
                Graphic labelGraphic = new Graphic(topPoint, labelSymbol);

                // Add the label graphic to the overlay
                _graphicsOverlay.Graphics.Add(labelGraphic);
            }

            // Adjust the viewpoint to include the waypoints
            if (_waypoints.Count > 0)
            {
                var waypointPoints = _waypoints.Select(w => new MapPoint(w.Longitude, w.Latitude, SpatialReferences.Wgs84));
                var envelope = new Envelope(waypointPoints.Min(p => p.X), waypointPoints.Min(p => p.Y),
                                            waypointPoints.Max(p => p.X), waypointPoints.Max(p => p.Y),
                                            SpatialReferences.Wgs84);

                var expandedEnvelope = new Envelope(
                                envelope.XMin - envelope.Width * 0.25,
                                envelope.YMin - envelope.Height * 0.25,
                                envelope.XMax + envelope.Width * 0.25,
                                envelope.YMax + envelope.Height * 0.25,
                                envelope.SpatialReference
                            );

                _sceneView.SetViewpointAsync(new Viewpoint(expandedEnvelope), TimeSpan.FromSeconds(1));
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

            // Optionally adjust the viewpoint to include the new flight path
            var polylineExtent = flightPathGraphic.Geometry.Extent;
            _sceneView.SetViewpoint(new Viewpoint(polylineExtent.GetCenter(), 5000000)); // Adjust scale
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Llama al método para actualizar la simulación desde el valor del slider
            UpdateSimulationFromSlider(e.NewValue);
        }


        // Mueve el avión a lo largo del camino con cada tick del temporizador

        // Actualiza la posición del avión en el gráfico


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
