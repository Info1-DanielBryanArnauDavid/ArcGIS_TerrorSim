using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Class; // Assuming the "Class" project is referenced

namespace ArcGISAppTest
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            // Define file paths for waypoints and flight plans
            string waypointFilePath = "C:\\Users\\bolty\\Desktop\\waypoints.txt"; // Path to the waypoint data file
            string flightPlanFilePath = "C:\\Users\\bolty\\Desktop\\flight_plans.txt"; // Path to the flight plan data file

            // Load waypoints from file
            List<WaypointGIS> waypoints = FlightPlanListGIS.LoadWaypointsFromFile(waypointFilePath);

            // Check if waypoints were loaded
            if (waypoints.Count > 0)
            {
                Console.WriteLine("Waypoints loaded successfully:");
                foreach (var waypoint in waypoints)
                {
                    Console.WriteLine($"Waypoint ID: {waypoint.ID}, Lat: {waypoint.Latitude}, Lon: {waypoint.Longitude}");
                }
            }
            else
            {
                Console.WriteLine("No waypoints were loaded.");
            }

            // Load flight plans from file using the loaded waypoints
            List<FlightPlanGIS> flightPlans = FlightPlanListGIS.LoadFlightPlansFromFile(flightPlanFilePath, waypoints);

            // Check if flight plans were loaded
            if (flightPlans.Count > 0)
            {
                Console.WriteLine("\nFlight Plans loaded successfully:");
                foreach (var flightPlan in flightPlans)
                {
                    Console.WriteLine($"\nFlight Plan for Company: {flightPlan.CompanyName}, Start Time: {flightPlan.StartTime}");
                    Console.WriteLine("Waypoints:");
                    for (int i = 0; i < flightPlan.Waypoints.Count; i++)
                    {
                        var wp = flightPlan.Waypoints[i];
                        string flightLevel = flightPlan.FlightLevels[i];
                        string speed = flightPlan.Speeds[i];
                        Console.WriteLine($"  - {wp.ID} (Lat: {wp.Latitude}, Lon: {wp.Longitude}), FL: {flightLevel}, Speed: {speed} KT");
                    }
                }
            }
            else
            {
                Console.WriteLine("No flight plans were loaded.");
            }

            // Wait for user input to close the program
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}


