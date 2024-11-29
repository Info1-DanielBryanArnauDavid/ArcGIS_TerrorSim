using System;
using System.Collections.Generic;
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
                        if (double.TryParse(parts[1].Trim(), out double lat) && double.TryParse(parts[2].Trim(), out double lon))
                        {
                            waypoints.Add(new WaypointGIS(id, lat, lon));
                        }
                        else
                        {
                            Console.WriteLine($"Invalid latitude or longitude values in line: {line}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Invalid format in line: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading waypoints: {ex.Message}");
            }

            return waypoints;
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
                            Console.WriteLine($"Invalid time format: {timeString}");
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
                            Console.WriteLine($"Waypoint not found: {waypointName}");
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
        private static double CalculateTotalDuration(FlightPlanGIS flightPlan)
        {
            double totalDuration = 0;

            // Iterate through each segment of the flight plan
            for (int i = 1; i < flightPlan.Waypoints.Count; i++)
            {
                var start = flightPlan.Waypoints[i - 1];
                var end = flightPlan.Waypoints[i];

                // Calculate the distance between the waypoints
                var distance = CalculateDistance(start, end);

                // Try to parse the speed and check for valid speeds (in knots)
                if (double.TryParse(flightPlan.Speeds[i - 1], out double speed) && speed > 0)
                {
                    // Convert speed from knots to meters per second
                    speed = speed * 0.514444;  // 1 knot = 0.514444 meters per second

                    // Calculate the duration for the segment based on distance (in meters) and speed (in m/s)
                    totalDuration += distance / speed;  // duration = distance / speed
                }
                else
                {
                    // Handle invalid speed (optional: set a default speed or throw an exception)
                    throw new InvalidOperationException($"Invalid speed for segment {i - 1} -> {i}: {flightPlan.Speeds[i - 1]}");
                }
            }

            return totalDuration;
        }

        private static double CalculateDistance(WaypointGIS start, WaypointGIS end)
        {
            const double EarthRadius = 6371; // in kilometers

            // Convert latitude and longitude from degrees to radians
            var dLat = ToRadians(end.Latitude - start.Latitude);
            var dLon = ToRadians(end.Longitude - start.Longitude);

            // Haversine formula to calculate the great-circle distance between two points
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(start.Latitude)) * Math.Cos(ToRadians(end.Latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // Return the distance in kilometers, then convert to meters
            return EarthRadius * c * 1000;  // Convert kilometers to meters
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }


    }


}
