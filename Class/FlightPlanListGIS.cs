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

            // Assume some reasonable climb and descent rates (in meters per second)
            const double climbRate = 5.0;  // meters per second (climbing)
            const double descentRate = 3.0;  // meters per second (descending)

            // Iterate through each segment of the flight plan
            for (int i = 1; i < flightPlan.Waypoints.Count; i++)
            {
                var start = flightPlan.Waypoints[i - 1];
                var end = flightPlan.Waypoints[i];

                // Calculate the horizontal distance between the waypoints
                var distance = CalculateDistance(start, end);

                // Try to parse the speed for the segment and check for valid speeds (in knots)
                if (double.TryParse(flightPlan.Speeds[i - 1], out double speed) && speed > 0)
                {
                    // Convert speed from knots to meters per second
                    speed = speed * 0.514444;  // 1 knot = 0.514444 meters per second

                    // Calculate the duration for the horizontal travel (in seconds)
                    double horizontalDuration = distance / speed;

                    // Now calculate the climb/descent time
                    double altitudeChange = Math.Abs(ParseAltitude(flightPlan.FlightLevels[i]) - ParseAltitude(flightPlan.FlightLevels[i - 1]));

                    double verticalDuration = 0;

                    if (altitudeChange > 0)
                    {
                        // If the plane is climbing
                        double climbTime = altitudeChange / climbRate;
                        verticalDuration += climbTime;
                    }
                    else if (altitudeChange < 0)
                    {
                        // If the plane is descending
                        double descentTime = altitudeChange / descentRate;
                        verticalDuration += descentTime;
                    }

                    // Add the time spent on the horizontal and vertical segments
                    double segmentDuration = horizontalDuration + verticalDuration;
                    totalDuration += segmentDuration;

                    // Log segment info for debugging
                    Console.WriteLine($"Segment {i}: Start({start.Latitude}, {start.Longitude}) -> End({end.Latitude}, {end.Longitude}), " +
                        $"Distance: {distance} meters, Speed: {speed} m/s, Horizontal Duration: {horizontalDuration}s, " +
                        $"Altitude Change: {altitudeChange}m, Vertical Duration: {verticalDuration}s, Total Duration: {segmentDuration}s");
                }
                else
                {
                    // Handle invalid speed (optional: set a default speed or throw an exception)
                    throw new InvalidOperationException($"Invalid speed for segment {i - 1} -> {i}: {flightPlan.Speeds[i - 1]}");
                }
            }

            // Return the total duration in seconds
            return totalDuration;
        }

        private static double ParseAltitude(string altitudeStr)
        {
            // Check if the altitude string has 'FL' (flight level), 'm' (meters), or 'AGL' (above ground level)
            if (altitudeStr.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
            {
                // Flight level (e.g., FL120) - convert from hundreds of feet to meters
                string levelStr = altitudeStr.Substring(2); // Remove "FL"
                if (int.TryParse(levelStr, out int flightLevel))
                {
                    // Convert FL (Flight Level) to meters (1 FL = 100 feet)
                    return (flightLevel * 100) * 0.3048; // Convert FL to meters (1 FL = 100 feet -> meters)
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
                    return altitudeMeters; // Return altitude in meters
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
                    return altitudeAGL; // Return AGL altitude (above ground level)
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

        private static double CalculateDistance(WaypointGIS start, WaypointGIS end)
        {
            const double R = 6371000; // Radius of the Earth in meters
            double lat1 = start.Latitude * Math.PI / 180; // Convert latitude to radians
            double lon1 = start.Longitude * Math.PI / 180; // Convert longitude to radians
            double lat2 = end.Latitude * Math.PI / 180;
            double lon2 = end.Longitude * Math.PI / 180;

            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = R * c; // Distance in meters
            return distance;
        }



    }


}
