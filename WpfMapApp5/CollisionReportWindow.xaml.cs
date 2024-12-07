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

namespace ArcGIS_App
{

    public partial class CollisionReportWindow : Window
    {
        private MapViewModel _mapViewModel;
        public FlightPlanListGIS listaplanes;

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
                    MessageBox.Show("No collision data available to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void FixCollisionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var collisionData = CollisionDataGrid.Items.Cast<CollisionData>().ToList();
                var adjustedCallsigns = new HashSet<string>();
                var updatedFlightPlans = new List<FlightPlanGIS>();

                foreach (var collision in collisionData)
                {
                    // Split the LastWaypoint string by the dash to separate both callsigns' waypoints
                    var lastWaypointForFirstCallsign = collision.LastWaypoint.Split('-')[0];

                    // Check if we have already adjusted this callsign (first callsign only)
                    if (!adjustedCallsigns.Contains(collision.Callsign1))
                    {
                        AdjustFlightLevels(collision.Callsign1, lastWaypointForFirstCallsign, updatedFlightPlans);
                        adjustedCallsigns.Add(collision.Callsign1);
                    }

                    // For the second callsign, we don't need to adjust the flight plan (skip it)
                }

                // Ensure updated flight plans have valid data
                if (!updatedFlightPlans.Any())
                {
                    MessageBox.Show("No flight plans were updated. Please verify your data.", "No Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update the original listaplanes with the modified flight plans.
                foreach (var updatedFlightPlan in updatedFlightPlans)
                {
                    var existingFlightPlan = listaplanes.FlightPlans.FirstOrDefault(fp => fp.Callsign == updatedFlightPlan.Callsign);
                    if (existingFlightPlan != null)
                    {
                        existingFlightPlan.CompanyName = updatedFlightPlan.CompanyName + " (Updated)";
                        existingFlightPlan.FlightLevels = updatedFlightPlan.FlightLevels;
                        existingFlightPlan.Waypoints = updatedFlightPlan.Waypoints;
                        existingFlightPlan.StartTime = updatedFlightPlan.StartTime;

                        Debug.WriteLine($"[INFO] Updated flight plan for callsign: {updatedFlightPlan.Callsign}");
                    }
                    else
                    {
                        Debug.WriteLine($"[ERROR] Flight plan for callsign {updatedFlightPlan.Callsign} not found in listaplanes.");
                    }
                }

                // Reload the updated flight plans into the map view.
                _mapViewModel.LoadUpdated(listaplanes);
                MainWindow.Current.LoadUpdatedAlso(listaplanes);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while resolving collisions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AdjustFlightLevels(string callsign, string lastWaypoint, List<FlightPlanGIS> updatedFlightPlans)
        {
            // Find the flight plan for the given callsign
            var flightPlan = listaplanes.FlightPlans.FirstOrDefault(fp => fp.Callsign == callsign);

            if (flightPlan == null)
            {
                Debug.WriteLine($"[ERROR] No flight plan found for callsign: {callsign}");
                return;
            }

            Debug.WriteLine($"[INFO] Adjusting flight plan for callsign: {callsign}");

            bool updated = false;

            // Check if the last waypoint is valid and ensure it is not the last waypoint in the list
            int startIndex = flightPlan.Waypoints.FindIndex(wp => wp.ID == lastWaypoint);
            if (startIndex == -1 || startIndex == flightPlan.Waypoints.Count - 1)
            {
                Debug.WriteLine($"[ERROR] Last waypoint '{lastWaypoint}' not found or already at the end.");
                return;
            }

            // If the number of waypoints is greater than 8, adjust flight levels for the last waypoint and the next 3 waypoints
            int waypointsToAdjust = flightPlan.Waypoints.Count > 8 ? 4 : 0; // Adjust 4 waypoints if more than 8 waypoints exist

            // If fewer than 8 waypoints, just apply a 20-minute delay
            if (waypointsToAdjust == 0)
            {
                Debug.WriteLine($"[INFO] Flight plan has less than 8 waypoints. Applying 20-minute delay.");
                flightPlan.StartTime = flightPlan.StartTime.AddMinutes(20); // Delay by 20 minutes
                updated = true;
            }
            else
            {
                // Adjust the flight levels for the last waypoint and the next 3 waypoints (up to 4 waypoints)
                int endIndex = Math.Min(startIndex + waypointsToAdjust, flightPlan.Waypoints.Count); // Ensure we don't exceed the number of waypoints

                for (int i = startIndex; i < endIndex; i++)
                {
                    // Check if the flight level is a cruising level (e.g., FL320, FL340)
                    string currentFL = flightPlan.FlightLevels[i];
                    if (IsCruiseLevel(currentFL))
                    {
                        // Add 10 to the current flight level for separation
                        try
                        {
                            int currentFLValue = int.Parse(currentFL.Replace("FL", ""));
                            flightPlan.FlightLevels[i] = $"FL{currentFLValue + 10}"; // Add 10 for separation
                            Debug.WriteLine($"[INFO] Adjusted Flight Level at Waypoint {i}: {flightPlan.FlightLevels[i]}");
                            updated = true;
                        }
                        catch (FormatException)
                        {
                            Debug.WriteLine($"[ERROR] Failed to parse flight level: {currentFL}");
                        }
                    }
                }
            }

            // If the flight plan was updated, add it to the list of updated flight plans
            if (updated)
            {
                Debug.WriteLine($"[INFO] Flight plan for callsign: {callsign} was updated.");
                updatedFlightPlans.Add(flightPlan);
            }
            else
            {
                Debug.WriteLine($"[INFO] No updates were made to the flight plan for callsign: {callsign}");
            }
        }

        // Helper function to check if a flight level is a cruising level (e.g., FL320, FL340)
        private bool IsCruiseLevel(string flightLevel)
        {
            if (string.IsNullOrEmpty(flightLevel)) return false;

            // Check if the flight level starts with "FL" and then contains a value above FL290 (cruise level)
            if (flightLevel.StartsWith("FL"))
            {
                if (int.TryParse(flightLevel.Replace("FL", ""), out int flValue))
                {
                    return flValue >= 290; // Consider anything above FL290 as a cruise level
                }
            }

            return false;
        }



        public void OnProgressComplete(List<CollisionData> rawData)
        {
            Dispatcher.Invoke(() =>
            {
                FinalizeCollisionReport(rawData);
                CollisionProgressBar.Value = 0;
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
            // Clean the string to remove non-numeric characters (e.g., "FL", "m", etc.)
            string cleanedString = new string(flightLevel.Where(char.IsDigit).ToArray());

            // Try to parse the cleaned string
            if (int.TryParse(cleanedString, out int result))
            {
                return result;
            }
            else
            {
                return 0; // Or some other fallback value
            }
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