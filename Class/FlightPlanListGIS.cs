using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Class
{
    public class FlightPlanListGIS
    {
        public DateTime Date { get; set; }
        public List<FlightPlanGIS> FlightPlans { get; set; }

        public FlightPlanListGIS(DateTime date)
        {
            Date = date;
            FlightPlans = new List<FlightPlanGIS>();
        }
        // Método para cargar los planes de vuelo desde el archivo
        public static List<WaypointGIS> LoadWaypointsFromFile(string filePath)
        {
            var waypoints = new List<WaypointGIS>();

            try
            {
                // Read each line from the file
                foreach (var line in File.ReadLines(filePath))
                {
                    // Skip empty lines and lines that do not match the format
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');

                    if (parts.Length == 3)
                    {
                        var id = parts[0].Trim();
                        if (double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                        {
                            waypoints.Add(new WaypointGIS(id, lat, lon));
                        }

                    }
                }
            }
            catch (Exception ex)
            {

            }

            return waypoints;
        }

        public void LoadUpdatedAlso(FlightPlanGIS planes)
        {

        }
        // Method to load flight plans from a file
        public static FlightPlanListGIS LoadFlightPlansFromFile(string filePath, List<WaypointGIS> waypoints)
        {
            var flightPlanList = new FlightPlanListGIS(new DateTime(2025, 12, 25));
            FlightPlanGIS currentFlightPlan = null;

            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');

                    if (parts.Length == 4) // First line: time, airline, callsign, aircraft
                    {
                        if (currentFlightPlan != null)
                        {
                            currentFlightPlan.TotalDuration = CalculateTotalDuration(currentFlightPlan);
                            flightPlanList.FlightPlans.Add(currentFlightPlan);
                        }

                        string timeString = parts[0].Trim();
                        string airline = parts[1].Trim();
                        string callsign = parts[2].Trim();
                        string aircraft = parts[3].Trim();

                        if (DateTime.TryParseExact(timeString, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startTime))
                        {
                            currentFlightPlan = new FlightPlanGIS(airline, startTime, callsign, aircraft);
                        }
                        else
                        {

                        }
                    }
                    else if (parts.Length == 3 && currentFlightPlan != null)
                    {
                        string waypointName = parts[0].Trim();
                        string flightLevel = parts[1].Trim();
                        string speed = parts[2].Trim();

                        var waypoint = waypoints.Find(w => w.ID == waypointName);
                        if (waypoint != null)
                        {
                            currentFlightPlan.Waypoints.Add(waypoint);
                            currentFlightPlan.FlightLevels.Add(flightLevel);
                            currentFlightPlan.Speeds.Add(speed);
                        }
                        else
                        {

                        }
                    }
                }

                if (currentFlightPlan != null)
                {
                    currentFlightPlan.TotalDuration = CalculateTotalDuration(currentFlightPlan);
                    flightPlanList.FlightPlans.Add(currentFlightPlan);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading flight plans: {ex.Message}");
            }

            return flightPlanList;
        }
        public static double CalculateTotalDuration(FlightPlanGIS flightPlan)
        {
            double totalDuration = 0.0;

            for (int i = 0; i < flightPlan.Waypoints.Count - 1; i++)
            {
                var startWaypoint = flightPlan.Waypoints[i];
                var endWaypoint = flightPlan.Waypoints[i + 1];

                // Get the distance between two waypoints
                var startPoint = new MapPoint(startWaypoint.Longitude, startWaypoint.Latitude, SpatialReferences.Wgs84);
                var endPoint = new MapPoint(endWaypoint.Longitude, endWaypoint.Latitude, SpatialReferences.Wgs84);

                GeodeticDistanceResult distanceResult = GeometryEngine.DistanceGeodetic(
                    startPoint,
                    endPoint,
                    LinearUnits.Kilometers,
                    AngularUnits.Degrees,
                    GeodeticCurveType.Geodesic
                );

                double segmentDistance = distanceResult.Distance; // distance in kilometers

                // Convert speed from knots to meters per second (1 knot = 0.514444 m/s)
                double segmentSpeed = double.Parse(flightPlan.Speeds[i]); // speed in knots
                double segmentSpeedMetersPerSecond = segmentSpeed * 0.514444;

                // Calculate the time to travel the segment (time = distance / speed)
                double segmentTimeInSeconds = (segmentDistance * 1000) / segmentSpeedMetersPerSecond; // segmentTime in seconds

                // Add the segment time to the total duration
                totalDuration += segmentTimeInSeconds;
            }

            return totalDuration;
        }
      


    }


}
