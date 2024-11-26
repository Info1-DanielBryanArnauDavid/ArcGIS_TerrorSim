using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology; // For SimpleLineSymbol
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls; // Make sure to include this for SceneView
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows; // For System.Windows.Point
using System.Windows.Input;
using System.Drawing; // For System.Drawing.Color

namespace ArcGIS_App
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private Scene _scene; // Change from Map to Scene
        private SceneView _sceneView; // Reference to the SceneView control
        private GraphicsOverlay _graphicsOverlay; // Graphics overlay for waypoints


        private System.Windows.Point? _startPoint; // Nullable Point for WPF

        public MapViewModel(SceneView sceneView)
        {
            _sceneView = sceneView;
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

            _sceneView.MouseDown += SceneView_MouseDown;
            _sceneView.MouseUp += SceneView_MouseUp;
            _sceneView.MouseMove += SceneView_MouseMove;

        }


        private void AddTerrainLayer()
        {
            var terrainLayer = new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
            _scene.BaseSurface.ElevationSources.Add(terrainLayer);
        }

        private void AddPathWithElevationAndGreatCircle()
        {
            // Define waypoints with latitude, longitude, and height (elevation)
            List<(double Longitude, double Latitude, double Height)> waypoints = new List<(double, double, double)>
    {
        (-3.7038, 40.4168, 650), // Madrid: 650 meters
        (-1.0, 41.0, 9000),       // Example intermediate point: 900 meters
        (1.0, 41.2, 10200),       // Example intermediate point: 1200 meters
        (2.1734, 41.3851, 5)     // Barcelona: Sea level (5 meters)
    };

            // List to hold the interpolated 3D MapPoints
            List<MapPoint> interpolatedPoints = new List<MapPoint>();

            // Interpolate between each pair of waypoints
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                var startPoint = waypoints[i];
                var endPoint = waypoints[i + 1];

                // Get great-circle points between this pair of waypoints
                var segmentPoints = CalculateGreatCircleWithElevation(
                    new MapPoint(startPoint.Longitude, startPoint.Latitude, startPoint.Height, SpatialReferences.Wgs84),
                    new MapPoint(endPoint.Longitude, endPoint.Latitude, endPoint.Height, SpatialReferences.Wgs84),
                    50 // Number of interpolated points between each pair
                );

                interpolatedPoints.AddRange(segmentPoints);
            }

            // Create a polyline for the 3D path
            var path = new Polyline(interpolatedPoints);

            // Create a graphic for the path with a line symbol
            var pathSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.FromArgb(255, 0, 0, 255), 3); // Blue line
            var pathGraphic = new Graphic(path, pathSymbol);

            // Add the graphic to the graphics overlay
            _graphicsOverlay.Graphics.Add(pathGraphic);

            // Adjust the viewpoint to center on the path and show its vertical extent
            // Set the viewpoint to include the vertical extent of the polyline
            var polylineExtent = path.Extent;
            _sceneView.SetViewpoint(new Viewpoint(polylineExtent.GetCenter(), 5000000)); // Adjust scale

        }


        private List<MapPoint> CalculateGreatCircleWithElevation(MapPoint startPoint, MapPoint endPoint, int numPoints)
        {
            List<MapPoint> points = new List<MapPoint>();

            // Convert latitude and longitude to radians
            double lat1 = startPoint.Y * Math.PI / 180;
            double lon1 = startPoint.X * Math.PI / 180;
            double lat2 = endPoint.Y * Math.PI / 180;
            double lon2 = endPoint.X * Math.PI / 180;

            double h1 = startPoint.Z; // Elevation of start point
            double h2 = endPoint.Z;   // Elevation of end point

            for (int i = 0; i <= numPoints; i++)
            {
                double fraction = (double)i / numPoints;

                // Great-circle interpolation
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

                // Interpolate elevation
                double newHeight = h1 + fraction * (h2 - h1);

                // Add the interpolated point
                points.Add(new MapPoint(newLon, newLat, newHeight, SpatialReferences.Wgs84));
            }

            return points;
        }



        private void SceneView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _startPoint = e.GetPosition(_sceneView); // Uses System.Windows.Point
                Mouse.Capture(_sceneView);
            }
        }

        private void SceneView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _startPoint = null;
                Mouse.Capture(null);
            }
        }

        private void SceneView_MouseMove(object sender, MouseEventArgs e)
        {
            if (_startPoint.HasValue && e.MiddleButton == MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(_sceneView); // Uses System.Windows.Point
                var deltaX = currentPoint.X - _startPoint.Value.X;
                var deltaY = currentPoint.Y - _startPoint.Value.Y;

                // Pan the scene based on mouse movement

                // Update start point for next move
                _startPoint = currentPoint;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}