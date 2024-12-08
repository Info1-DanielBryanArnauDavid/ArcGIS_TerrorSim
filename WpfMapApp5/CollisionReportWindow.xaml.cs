using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Class;
using System.Windows.Shapes;
using static ArcGIS_App.MapViewModel;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Security.Cryptography.X509Certificates;

namespace ArcGIS_App
{

    public partial class CollisionReportWindow : Window
    {
        private MapViewModel _mapViewModel;
        public FlightPlanListGIS listaplanes;
        public bool Solved = false;

        public CollisionReportWindow(MapViewModel mapViewModel, FlightPlanListGIS lista)
        {
            InitializeComponent();
            _mapViewModel = mapViewModel;
            listaplanes = lista;
            this.Topmost = true; // Ensure the welcome window stays on top of the MainWindow
        }

        // Method to update the progress bar
        public void UpdateProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                CollisionProgressBar.Value = progress;
            });
        }

        public void ClearCollisionData()
        {
            CollisionDataGrid.ItemsSource = null;
            CollisionDataGrid.Items.Clear();
        }

        public void FinalizeCollisionReport(List<CollisionData> rawData)
        {
            ClearCollisionData();
            var summarizedData = ProcessCollisionData(rawData);
            AddCollisionData(summarizedData);
        }

        private void CollisionProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CollisionProgressBar.Value == CollisionProgressBar.Maximum)
            {
                var rawCollisionData = CollisionDataGrid.Items.Cast<CollisionData>().ToList();

                if (rawCollisionData.Any())
                {
                    OnProgressComplete(rawCollisionData);
                }
                else
                {
                    _mapViewModel.Solved = true;
                    Solved = true;
                    MessageBox.Show("No collision data, flightplans are safe to publish.");
                }
            }
        }
        public void FixCollisionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var collisionData = CollisionDataGrid.Items.Cast<CollisionData>().ToList();
                var adjustedCallsigns = new HashSet<string>(); // Track adjusted callsigns
                var updatedFlightPlans = new List<FlightPlanGIS>(); // Track updated flight plans

                foreach (var collision in collisionData)
                {
                    // Handle the first callsign only
                    if (!adjustedCallsigns.Contains(collision.Callsign1))
                    {
                        // Parse LastWaypoint to extract the corresponding waypoint for Callsign1
                        var lastWaypointForFirstCallsign = collision.LastWaypoint.Split('-')[0];

                        // Handle the flight level adjustment or delay
                        if (TryParseFlightLevel(collision.FLcallsign1) > 200)
                        {
                            if (!AdjustFlightLevels(collision.Callsign1, lastWaypointForFirstCallsign, updatedFlightPlans))
                            {
                                DelayFlightStart(collision.Callsign1, updatedFlightPlans, 30); // Apply a delay if FL adjustment is not possible
                            }
                        }
                        else
                        {
                            DelayFlightStart(collision.Callsign1, updatedFlightPlans, 30); // Delay by 30 minutes
                        }

                        adjustedCallsigns.Add(collision.Callsign1); // Mark callsign as adjusted
                    }
                }

                // Ensure updated flight plans have valid data
                if (!updatedFlightPlans.Any())
                {
                    MessageBox.Show("No flight plans were updated. Please verify your data.", "No Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update the original listaplanes with the modified flight plans
                foreach (var updatedFlightPlan in updatedFlightPlans)
                {
                    var existingFlightPlan = listaplanes.FlightPlans.FirstOrDefault(fp => fp.Callsign == updatedFlightPlan.Callsign);
                    if (existingFlightPlan != null)
                    {
                        existingFlightPlan.CompanyName = updatedFlightPlan.CompanyName + " (Updated)";
                        existingFlightPlan.FlightLevels = updatedFlightPlan.FlightLevels;
                        existingFlightPlan.Waypoints = updatedFlightPlan.Waypoints;
                        existingFlightPlan.StartTime = updatedFlightPlan.StartTime;
                    }
                    else
                    {
                        Debug.WriteLine($"[ERROR] Flight plan for callsign {updatedFlightPlan.Callsign} not found in listaplanes.");
                    }
                }

                // Reload the updated flight plans into the map view
                _mapViewModel.LoadUpdated(listaplanes);
                MainWindow.Current.LoadUpdatedAlso(listaplanes);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while resolving collisions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DelayFlightStart(string callsign, List<FlightPlanGIS> updatedFlightPlans, int delayMinutes)
        {
            var flightPlan = listaplanes.FlightPlans.FirstOrDefault(fp => fp.Callsign == callsign);

            if (flightPlan == null)
            {
                Debug.WriteLine($"[ERROR] No flight plan found for callsign: {callsign}");
                return;
            }

            // Apply a delay to the StartTime
            flightPlan.StartTime = flightPlan.StartTime.AddMinutes(delayMinutes);

            updatedFlightPlans.Add(flightPlan);
        }

        private bool AdjustFlightLevels(string callsign, string lastWaypoint, List<FlightPlanGIS> updatedFlightPlans)
        {
            var flightPlan = listaplanes.FlightPlans.FirstOrDefault(fp => fp.Callsign == callsign);

            if (flightPlan == null)
            {
                Debug.WriteLine($"[ERROR] No flight plan found for callsign: {callsign}");
                return false;
            }

            Debug.WriteLine($"[INFO] Adjusting flight plan for callsign: {callsign}");

            // Find the index of the last waypoint
            int lastWaypointIndex = flightPlan.Waypoints.FindIndex(wp => wp.ID == lastWaypoint);
            if (lastWaypointIndex == -1)
            {
                Debug.WriteLine($"[ERROR] Last waypoint '{lastWaypoint}' not found in flight plan for callsign: {callsign}");
                // Additional logging to inspect the waypoints in the flight plan
                Debug.WriteLine("[INFO] Waypoints in flight plan:");
                foreach (var wp in flightPlan.Waypoints)
                {
                    Debug.WriteLine($"[INFO] Waypoint ID: {wp.ID}");
                }
                return false; // Return false to indicate that the waypoint was not found
            }

            // Determine which waypoints to adjust
            int beforeLastIndex = lastWaypointIndex - 1; // Waypoint before the last waypoint
            int afterLastIndex1 = lastWaypointIndex + 1; // First waypoint after the last waypoint
            int afterLastIndex2 = lastWaypointIndex + 2; // Second waypoint after the last waypoint

            // Check if we have enough waypoints to adjust
            if (beforeLastIndex < 0 || afterLastIndex1 >= flightPlan.FlightLevels.Count)
            {
                Debug.WriteLine($"[INFO] Not enough waypoints to adjust for callsign: {callsign}");
                return false; // Insufficient waypoints, apply delay instead
            }

            // Adjust the flight levels
            try
            {
                if (beforeLastIndex >= 0) // Waypoint before last
                {
                    int currentFL = int.Parse(flightPlan.FlightLevels[beforeLastIndex].Replace("FL", ""));
                    flightPlan.FlightLevels[beforeLastIndex] = $"FL{currentFL + 20}";
                    Debug.WriteLine($"[INFO] Adjusted Flight Level for Callsign {callsign} at Waypoint {beforeLastIndex}: {flightPlan.FlightLevels[beforeLastIndex]}");
                }

                // Adjust LastWaypoint
                int currentFLLast = int.Parse(flightPlan.FlightLevels[lastWaypointIndex].Replace("FL", ""));
                flightPlan.FlightLevels[lastWaypointIndex] = $"FL{currentFLLast + 20}";
                Debug.WriteLine($"[INFO] Adjusted Flight Level for Callsign {callsign} at LastWaypoint {lastWaypointIndex}: {flightPlan.FlightLevels[lastWaypointIndex]}");

                if (afterLastIndex1 < flightPlan.FlightLevels.Count) // First waypoint after last
                {
                    int currentFL1 = int.Parse(flightPlan.FlightLevels[afterLastIndex1].Replace("FL", ""));
                    flightPlan.FlightLevels[afterLastIndex1] = $"FL{currentFL1 + 20}";
                    Debug.WriteLine($"[INFO] Adjusted Flight Level for Callsign {callsign} at Waypoint {afterLastIndex1}: {flightPlan.FlightLevels[afterLastIndex1]}");
                }

                if (afterLastIndex2 < flightPlan.FlightLevels.Count) // Second waypoint after last
                {
                    int currentFL2 = int.Parse(flightPlan.FlightLevels[afterLastIndex2].Replace("FL", ""));
                    flightPlan.FlightLevels[afterLastIndex2] = $"FL{currentFL2 + 20}";
                    Debug.WriteLine($"[INFO] Adjusted Flight Level for Callsign {callsign} at Waypoint {afterLastIndex2}: {flightPlan.FlightLevels[afterLastIndex2]}");
                }

                // Add the updated flight plan to the list
                updatedFlightPlans.Add(flightPlan);
                return true;
            }
            catch (FormatException ex)
            {
                Debug.WriteLine($"[ERROR] Failed to parse flight level for callsign {callsign}: {ex.Message}");
                return false;
            }
        }


        public void OnProgressComplete(List<CollisionData> rawData)
        {
            Dispatcher.Invoke(() =>
            {
                FinalizeCollisionReport(rawData);
                FixCollisionsButton.Visibility = Visibility.Visible;
            });
        }
        public List<CollisionData> ProcessCollisionData(List<CollisionData> rawData)
        {
            var groupedData = rawData
                .GroupBy(collision =>
                    string.Compare(collision.Callsign1, collision.Callsign2) < 0
                        ? (collision.Callsign1, collision.Callsign2)
                        : (collision.Callsign2, collision.Callsign1))
                .Select(group =>
                {
                    var callsign1 = group.Key.Item1;
                    var callsign2 = group.Key.Item2;

                    var startTime = group.Min(c => c.CollisionStart);
                    var endTime = group.Max(c => c.CollisionEnd);

                    if (string.IsNullOrEmpty(endTime))
                    {
                        endTime = startTime;
                    }

                    var fl1List = group.Select(c => TryParseFlightLevel(c.FLcallsign1)).ToList();
                    var fl2List = group.Select(c => TryParseFlightLevel(c.FLcallsign2)).ToList();

                    var medianFL1 = CollisionData.CalculateMedianFL(fl1List);
                    var medianFL2 = CollisionData.CalculateMedianFL(fl2List);

                    var collisionData = new CollisionData
                    {
                        Callsign1 = callsign1,
                        Callsign2 = callsign2,
                        CollisionStart = startTime,
                        CollisionEnd = endTime,
                        FLcallsign1 = medianFL1,
                        FLcallsign2 = medianFL2,
                        LastWaypoint = group.FirstOrDefault()?.LastWaypoint // Store the first detected LastWaypoint
                    };

                    return collisionData;
                })
                .ToList();

            return groupedData;
        }

        private int TryParseFlightLevel(string flightLevel)
        {
            string cleanedString = new string(flightLevel.Where(char.IsDigit).ToArray());
            return int.TryParse(cleanedString, out int result) ? result : 0;
        }

        public void AddCollisionData(List<CollisionData> newCollisions)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var collision in newCollisions)
                {
                    CollisionDataGrid.Items.Add(collision);
                }
            });
        }
    }

}