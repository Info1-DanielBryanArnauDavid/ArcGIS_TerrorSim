using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Class
{
    public class CollisionDetection
    {
        // Method to detect collisions and return a formatted report
        public static string DetectCollisions(FlightPlanListGIS flightPlanList, List<WaypointGIS> waypoints, double minimumSeparationDistance)
        {
            StringBuilder collisionReport = new StringBuilder();

            // Check if flightPlanList or waypoints are null or empty
            if (flightPlanList == null || flightPlanList.FlightPlans.Count == 0)
            {
                return "No flight plans available for collision detection.";
            }

            if (waypoints == null || waypoints.Count == 0)
            {
                return "No waypoints available for collision detection.";
            }

            // Loop through each pair of flight plans in the flight plan list
            for (int i = 0; i < flightPlanList.FlightPlans.Count; i++)
            {
                var flightPlan1 = flightPlanList.FlightPlans[i];

                for (int j = i + 1; j < flightPlanList.FlightPlans.Count; j++)
                {
                    var flightPlan2 = flightPlanList.FlightPlans[j];

                    // For each pair of planes, compare their waypoints and flight levels
                    List<Tuple<string, string, DateTime, DateTime>> collisions = DetectCollisionsBetweenPlanes(flightPlan1, flightPlan2, waypoints, minimumSeparationDistance);

                    // Format the output for each detected collision
                    foreach (var collision in collisions)
                    {
                        string collisionInfo = $"{flightPlan1.Callsign} - {flightPlan2.Callsign} - " +
                                               $"{collision.Item3:HH:mm:ss} - {collision.Item4:HH:mm:ss} - " +
                                               $"{collision.Item1} - {collision.Item2}";

                        collisionReport.AppendLine(collisionInfo);
                    }
                }
            }

            return collisionReport.Length > 0 ? collisionReport.ToString() : "No collisions detected.";
        }
        public static List<Tuple<string, string, DateTime, DateTime>> DetectCollisionsBetweenPlanes(FlightPlanGIS flightPlan1, FlightPlanGIS flightPlan2, List<WaypointGIS> waypoints, double minimumSeparationDistance)
        {
            List<Tuple<string, string, DateTime, DateTime>> collisions = new List<Tuple<string, string, DateTime, DateTime>>();

            // Check if the flight plans have enough waypoints
            if (flightPlan1.Waypoints.Count == 0 || flightPlan2.Waypoints.Count == 0)
            {
                return collisions;
            }

            // Simulate time steps and check for collisions
            DateTime simulationStartTime = flightPlan1.StartTime;
            DateTime simulationEndTime = flightPlan2.StartTime.AddSeconds(flightPlan2.TotalDuration);  // You can adjust this end time based on the longest flight duration.

            // Assuming we simulate every second for simplicity
            for (DateTime currentTime = simulationStartTime; currentTime <= simulationEndTime; currentTime = currentTime.AddSeconds(1))
            {
                var position1 = GetPlanePositionAtTime(flightPlan1, currentTime);
                var position2 = GetPlanePositionAtTime(flightPlan2, currentTime);

                // Calculate the distance between the planes at this time step
                double distanceBetweenPlanes = CalculateDistanceBetweenPlanes(position1, position2);

                if (distanceBetweenPlanes <= minimumSeparationDistance)
                {
                    // Record collision start and end times
                    collisions.Add(new Tuple<string, string, DateTime, DateTime>(
                        flightPlan1.FlightLevels.Last(),
                        flightPlan2.FlightLevels.Last(),
                        currentTime,  // Collision start time
                        currentTime   // Collision end time (for simplicity, we're considering immediate collision)
                    ));
                }
            }

            return collisions;
        }
        private static MapPoint GetPlanePositionAtTime(FlightPlanGIS flightPlan, DateTime currentTime)
        {
            // Calculate the time elapsed since the plane started
            double elapsedTimeInSeconds = (currentTime - flightPlan.StartTime).TotalSeconds;

            // Find which waypoint the plane is at based on the time and speed
            for (int i = 1; i < flightPlan.Waypoints.Count; i++)
            {
                // Calculate distance between the two waypoints
                double segmentDuration = CalculateTimeToTravel(flightPlan, i);

                // If elapsed time exceeds segment duration, the plane is between waypoints
                if (elapsedTimeInSeconds <= segmentDuration)
                {
                    var startWaypoint = flightPlan.Waypoints[i - 1];
                    var endWaypoint = flightPlan.Waypoints[i];

                    // Calculate position based on distance and speed
                    double distance = elapsedTimeInSeconds * GetSpeedInMetersPerSecond(flightPlan.Speeds[i - 1]);

                    // Calculate the interpolated position between waypoints
                    return InterpolatePosition(startWaypoint, endWaypoint, distance);
                }

                elapsedTimeInSeconds -= segmentDuration;
            }

            return flightPlan.Waypoints.Last().Location;  // Return last waypoint if we exceed the total duration
        }

        private static double CalculateTimeToTravel(FlightPlanGIS flightPlan, int segmentIndex)
        {
            var startWaypoint = flightPlan.Waypoints[segmentIndex - 1];
            var endWaypoint = flightPlan.Waypoints[segmentIndex];
            double distance = CalculateDistanceBetweenWaypoints(startWaypoint, endWaypoint);
            double speed = GetSpeedInMetersPerSecond(flightPlan.Speeds[segmentIndex - 1]);
            return distance / speed;  // Time = distance / speed
        }

        private static double GetSpeedInMetersPerSecond(string speedInKnots)
        {
            if (double.TryParse(speedInKnots, out double speed))
            {
                return speed * 0.514444;  // Convert knots to meters per second
            }

            return 0; // Default value if speed is invalid
        }

        private static MapPoint InterpolatePosition(WaypointGIS startWaypoint, WaypointGIS endWaypoint, double distance)
        {
            double totalDistance = CalculateDistanceBetweenWaypoints(startWaypoint, endWaypoint);
            double fraction = distance / totalDistance;

            double newX = startWaypoint.Location.X + fraction * (endWaypoint.Location.X - startWaypoint.Location.X);
            double newY = startWaypoint.Location.Y + fraction * (endWaypoint.Location.Y - startWaypoint.Location.Y);
            double newZ = startWaypoint.Location.Z + fraction * (endWaypoint.Location.Z - startWaypoint.Location.Z);

            return new MapPoint(newX, newY, newZ);
        }
        private static double CalculateDistanceBetweenPlanes(MapPoint position1, MapPoint position2)
        {
            const double R = 6371000; // Radius of Earth in meters

            double lat1 = position1.Y * Math.PI / 180;
            double lon1 = position1.X * Math.PI / 180;
            double lat2 = position2.Y * Math.PI / 180;
            double lon2 = position2.X * Math.PI / 180;

            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Return the distance in meters
        }
        // Method to calculate the distance between two waypoints (in meters)
        private static double CalculateDistanceBetweenWaypoints(WaypointGIS waypoint1, WaypointGIS waypoint2)
        {
            const double R = 6371000; // Earth radius in meters

            // Validate the waypoints
            if (waypoint1 == null || waypoint2 == null)
            {
                return double.MaxValue; // Return a very large number to indicate invalid waypoints
            }

            double lat1 = waypoint1.Latitude * Math.PI / 180; // Convert to radians
            double lon1 = waypoint1.Longitude * Math.PI / 180;
            double lat2 = waypoint2.Latitude * Math.PI / 180;
            double lon2 = waypoint2.Longitude * Math.PI / 180;

            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Return the distance in meters
        }


    }

}
