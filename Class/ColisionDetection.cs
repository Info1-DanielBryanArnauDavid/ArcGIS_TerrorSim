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

                    // Only add the first and last collision time for each collision pair
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


        public static List<Tuple<string, string, DateTime, DateTime>> DetectCollisionsBetweenPlanes(
      FlightPlanGIS flightPlan1, FlightPlanGIS flightPlan2, List<WaypointGIS> waypoints, double minimumSeparationDistance)
        {
            List<Tuple<string, string, DateTime, DateTime>> collisions = new List<Tuple<string, string, DateTime, DateTime>>();

            if (flightPlan1.Waypoints.Count == 0 || flightPlan2.Waypoints.Count == 0)
            {
                return collisions;
            }

            DateTime simulationStartTime = flightPlan1.StartTime;
            DateTime simulationEndTime = flightPlan2.StartTime.AddSeconds(flightPlan2.TotalDuration);

            DateTime? collisionStartTime = null;
            DateTime? collisionEndTime = null;

            for (DateTime currentTime = simulationStartTime; currentTime <= simulationEndTime; currentTime = currentTime.AddSeconds(1))
            {
                // Only check if planes are flying
                if (IsPlaneFlying(flightPlan1, currentTime) && IsPlaneFlying(flightPlan2, currentTime))
                {
                    var position1 = GetPlanePositionAtTime(flightPlan1, currentTime);
                    var position2 = GetPlanePositionAtTime(flightPlan2, currentTime);

                    // Calculate the lateral distance between planes (ignoring altitude)
                    double distanceBetweenPlanes = CalculateLateralDistance(position1, position2);

                    if (distanceBetweenPlanes <= minimumSeparationDistance)
                    {
                        if (!collisionStartTime.HasValue)
                        {
                            // First collision detected, set start time
                            collisionStartTime = currentTime;
                        }

                        // Keep updating the end time
                        collisionEndTime = currentTime;
                    }
                    else
                    {
                        if (collisionStartTime.HasValue && collisionEndTime.HasValue)
                        {
                            // Record the collision if it ended
                            collisions.Add(new Tuple<string, string, DateTime, DateTime>(
                                flightPlan1.FlightLevels.Last(),
                                flightPlan2.FlightLevels.Last(),
                                collisionStartTime.Value,
                                collisionEndTime.Value
                            ));

                            // Reset collision times for the next possible collision
                            collisionStartTime = null;
                            collisionEndTime = null;
                        }
                    }
                }
            }

            // If there is an ongoing collision at the end of the simulation, add it
            if (collisionStartTime.HasValue && collisionEndTime.HasValue)
            {
                collisions.Add(new Tuple<string, string, DateTime, DateTime>(
                    flightPlan1.FlightLevels.Last(),
                    flightPlan2.FlightLevels.Last(),
                    collisionStartTime.Value,
                    collisionEndTime.Value
                ));
            }

            return collisions;
        }

        private static bool IsPlaneFlying(FlightPlanGIS flightPlan, DateTime currentTime)
        {
            // Check if the plane is flying based on whether it has reached its final waypoint
            double elapsedTimeInSeconds = (currentTime - flightPlan.StartTime).TotalSeconds;
            return elapsedTimeInSeconds < flightPlan.TotalDuration; // Plane is flying if the time is before the final waypoint
        }

        private static double CalculateLateralDistance(MapPoint position1, MapPoint position2)
        {
            const double R = 6371000; // Earth's radius in meters

            // Calculate the difference in latitudes and longitudes (ignore altitude)
            double deltaLat = position2.Y - position1.Y;
            double deltaLon = position2.X - position1.X;

            // Use Haversine formula for lateral distance calculation
            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(position1.Y) * Math.Cos(position2.Y) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;  // Return the lateral distance in meters
        }


        // Get the plane's position at a specific time (based on its speed and waypoints)
        private static MapPoint GetPlanePositionAtTime(FlightPlanGIS flightPlan, DateTime currentTime)
        {
            double elapsedTimeInSeconds = (currentTime - flightPlan.StartTime).TotalSeconds;

            // Find which waypoint the plane is at based on the elapsed time
            for (int i = 1; i < flightPlan.Waypoints.Count; i++)
            {
                double segmentDuration = CalculateTimeToTravel(flightPlan, i);

                if (elapsedTimeInSeconds <= segmentDuration)
                {
                    var startWaypoint = flightPlan.Waypoints[i - 1];
                    var endWaypoint = flightPlan.Waypoints[i];

                    double distance = elapsedTimeInSeconds * GetSpeedInMetersPerSecond(flightPlan.Speeds[i - 1]);

                    return InterpolatePosition(startWaypoint, endWaypoint, distance);
                }

                elapsedTimeInSeconds -= segmentDuration;
            }

            return flightPlan.Waypoints.Last().Location;  // Return last waypoint if we exceed the total duration
        }
        // Method to calculate the time to travel between two waypoints based on the plane's speed
        private static double CalculateTimeToTravel(FlightPlanGIS flightPlan, int segmentIndex)
        {
            var startWaypoint = flightPlan.Waypoints[segmentIndex - 1];
            var endWaypoint = flightPlan.Waypoints[segmentIndex];

            // Calculate the distance between the two waypoints (in meters)
            double distance = CalculateDistanceBetweenWaypoints(startWaypoint, endWaypoint);

            // Get the speed of the plane at the given segment (in meters per second)
            double speed = GetSpeedInMetersPerSecond(flightPlan.Speeds[segmentIndex - 1]);

            // Time = Distance / Speed
            return distance / speed;  // The result is in seconds
        }


        // Method to interpolate the position between two waypoints at a specific distance
        private static MapPoint InterpolatePosition(WaypointGIS startWaypoint, WaypointGIS endWaypoint, double distance)
        {
            double totalDistance = CalculateDistanceBetweenWaypoints(startWaypoint, endWaypoint);
            double fraction = distance / totalDistance;

            double newX = startWaypoint.Location.X + fraction * (endWaypoint.Location.X - startWaypoint.Location.X);
            double newY = startWaypoint.Location.Y + fraction * (endWaypoint.Location.Y - startWaypoint.Location.Y);
            double newZ = startWaypoint.Location.Z + fraction * (endWaypoint.Location.Z - startWaypoint.Location.Z);

            return new MapPoint(newX, newY, newZ);
        }

        // Method to calculate the distance between two waypoints (in meters)
        private static double CalculateDistanceBetweenWaypoints(WaypointGIS waypoint1, WaypointGIS waypoint2)
        {
            const double R = 6371000; // Earth's radius in meters

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
        private static double ParseAltitude(string altitudeStr)
        {
            // If altitude is given as FL (Flight Level)
            if (altitudeStr.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
            {
                string levelStr = altitudeStr.Substring(2); // Remove "FL"
                if (int.TryParse(levelStr, out int flightLevel))
                {
                    return flightLevel * 100 * 0.3048; // Convert FL to meters (1 FL = 100 feet)
                }
            }
            // If altitude is in meters (e.g., 1200.0m)
            else if (altitudeStr.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                string metersStr = altitudeStr.Substring(0, altitudeStr.Length - 1); // Remove "m"
                if (double.TryParse(metersStr, out double altitudeMeters))
                {
                    return altitudeMeters;  // Return altitude in meters
                }
            }
            // Handle other formats if needed
            return 0; // Default value if the format is unknown
        }


        // Method to calculate the speed of the plane in meters per second
        private static double GetSpeedInMetersPerSecond(string speedInKnots)
        {
            if (double.TryParse(speedInKnots, out double speed))
            {
                return speed * 0.514444;  // Convert knots to meters per second
            }

            return 0; // Default value if speed is invalid
        }
    }


}
