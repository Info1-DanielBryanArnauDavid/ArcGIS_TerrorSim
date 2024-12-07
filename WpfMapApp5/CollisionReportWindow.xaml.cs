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
                    if (!DateTime.TryParse(collision.CollisionStart, out DateTime collisionStart) ||
                        !DateTime.TryParse(collision.CollisionEnd, out DateTime collisionEnd))
                    {
                        MessageBox.Show($"Fechas inválidas para la colisión entre {collision.Callsign1} y {collision.Callsign2}.",
                                         "Error de datos", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    if (!adjustedCallsigns.Contains(collision.Callsign1))
                    {
                        AdjustFlightLevels(collision.Callsign1, collisionStart, collisionEnd, updatedFlightPlans);
                        adjustedCallsigns.Add(collision.Callsign1);
                    }

                    if (!adjustedCallsigns.Contains(collision.Callsign2))
                    {
                        AdjustFlightLevels(collision.Callsign2, collisionStart, collisionEnd, updatedFlightPlans);
                        adjustedCallsigns.Add(collision.Callsign2);
                    }
                }

                // Add "(Updated)" to the company name of the updated flight plans
                foreach (var flightPlan in updatedFlightPlans)
                {
                    flightPlan.CompanyName += " (Updated)";
                }

                // Update the original listaplanes with the modified flight plans
                foreach (var updatedFlightPlan in updatedFlightPlans)
                {
                    // Find the flight plan in the list and update it
                    var existingFlightPlan = listaplanes.FlightPlans.FirstOrDefault(fp => fp.Callsign == updatedFlightPlan.Callsign);
                    if (existingFlightPlan != null)
                    {
                        existingFlightPlan.CompanyName = updatedFlightPlan.CompanyName;
                        existingFlightPlan.FlightLevels = updatedFlightPlan.FlightLevels;
                        existingFlightPlan.Waypoints = updatedFlightPlan.Waypoints;
                        existingFlightPlan.StartTime = updatedFlightPlan.StartTime; // Assuming you're also updating start times, if necessary
                    }
                }

                // Reload the updated flight plans into the map view
                _mapViewModel.LoadUpdated(listaplanes);

                MessageBox.Show("Collisions adjusted.", "Fix Collisions", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al resolver colisiones: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void AdjustFlightLevels(string callsign, DateTime collisionStart, DateTime collisionEnd, List<FlightPlanGIS> updatedFlightPlans)
        {
            var flightPlan = listaplanes.FlightPlans.FirstOrDefault(fp => fp.Callsign == callsign);

            if (flightPlan != null)
            {
                bool updated = false;

                for (int i = 0; i < flightPlan.Waypoints.Count; i++)
                {
                    if (IsWaypointInCollisionPeriod(i, collisionStart, collisionEnd, flightPlan))
                    {
                        // Adjust the flight level for the waypoint if it's within the collision period
                        AdjustFlightLevelForWaypoint(i, flightPlan, updatedFlightPlans);
                        updated = true;
                    }
                }

                if (updated)
                {
                    updatedFlightPlans.Add(flightPlan);
                }
            }
            else
            {
                MessageBox.Show($"No se encontró un plan de vuelo para el callsign: {callsign}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdjustFlightLevelForWaypoint(int waypointIndex, FlightPlanGIS flightPlan, List<FlightPlanGIS> updatedFlightPlans)
        {
            // Calculate current flight level and add 10 for separation
            var currentFL = int.Parse(flightPlan.FlightLevels[waypointIndex].Replace("FL", ""));
            flightPlan.FlightLevels[waypointIndex] = $"FL{currentFL + 10}"; // Add 10 for separation

            // If possible, adjust two waypoints ahead as well
            if (waypointIndex + 2 < flightPlan.Waypoints.Count)
            {
                var nextFL = int.Parse(flightPlan.FlightLevels[waypointIndex + 2].Replace("FL", ""));
                flightPlan.FlightLevels[waypointIndex + 2] = $"FL{nextFL + 10}"; // Add 10 for separation
            }

            // If there is no waypoint two ahead, adjust the next one
            else if (waypointIndex + 1 < flightPlan.Waypoints.Count)
            {
                var nextFL = int.Parse(flightPlan.FlightLevels[waypointIndex + 1].Replace("FL", ""));
                flightPlan.FlightLevels[waypointIndex + 1] = $"FL{nextFL + 10}"; // Add 10 for separation
            }

            updatedFlightPlans.Add(flightPlan);
        }

        private bool IsWaypointInCollisionPeriod(int waypointIndex, DateTime collisionStart, DateTime collisionEnd, FlightPlanGIS flightPlan)
        {
            DateTime waypointTime = flightPlan.StartTime.AddMinutes(waypointIndex * 10); // assuming each waypoint is 10 minutes apart
            return waypointTime >= collisionStart && waypointTime <= collisionEnd;
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