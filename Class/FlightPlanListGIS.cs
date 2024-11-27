using System;
using System.Collections.Generic;
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
        public static List<FlightPlanGIS> LoadFlightPlansFromFile(string filePath, List<WaypointGIS> waypoints)
        {
            var flightPlans = new List<FlightPlanGIS>();
            FlightPlanGIS currentFlightPlan = null;

            try
            {
                // Read each line from the flight plan file
                foreach (var line in File.ReadLines(filePath))
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');

                    if (parts.Length == 2) // First two columns: time and company
                    {
                        string timeString = parts[0].Trim();
                        DateTime startTime;
                        if (DateTime.TryParseExact(timeString, "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out startTime))
                        {
                            string companyName = parts[1].Trim();

                            // Add the previous flight plan to the list if it exists
                            if (currentFlightPlan != null)
                            {
                                flightPlans.Add(currentFlightPlan);
                            }

                            // Start a new flight plan with the company name and start time
                            currentFlightPlan = new FlightPlanGIS(companyName, startTime);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid time format: {timeString}");
                        }
                    }
                    else if (parts.Length == 3 && currentFlightPlan != null) // Waypoint, FlightLevel, Speed
                    {
                        string waypointName = parts[0].Trim();
                        string flightLevel = parts[1].Trim();
                        string speed = parts[2].Trim();

                        // Find the waypoint by its ID
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

                // Add the last flight plan to the list
                if (currentFlightPlan != null)
                {
                    flightPlans.Add(currentFlightPlan);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading flight plans: {ex.Message}");
            }

            return flightPlans;
        }

        public List<(double Latitude, double Longitude, double Height, string Speed)> CreatePath(List<WaypointGIS> waypoints,List<double> heights,List<string> speeds)
        {
            // This method assumes that the heights and speeds lists have the same number of elements as the waypoints list.
            if (waypoints.Count != heights.Count || waypoints.Count != speeds.Count)
                throw new ArgumentException("The number of waypoints, heights, and speeds must match.");

            List<(double Latitude, double Longitude, double Height, string Speed)> path = new List<(double, double, double, string)>();

            for (int i = 0; i < waypoints.Count; i++)
            {
                path.Add((waypoints[i].Latitude, waypoints[i].Longitude, heights[i], speeds[i]));
            }

            return path;
        }
        


    }


}
