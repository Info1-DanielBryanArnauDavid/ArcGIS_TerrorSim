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

namespace ArcGIS_App
{
    public partial class CollisionReportWindow : Window
    {
        public CollisionReportWindow()
        {
            InitializeComponent();
            this.Topmost = true; // Ensure the welcome window stays on top of the MainWindow
        }

        // Method to update the progress bar
        public void UpdateProgress(double progress)
        {
            // Use Dispatcher to ensure updates are done on the UI thread
            Dispatcher.Invoke(() =>
            {
                CollisionProgressBar.Value = progress;
            });
        }

        public void ClearCollisionData()
        {
            // Clear the current items from the DataGrid
            CollisionDataGrid.ItemsSource = null;
            CollisionDataGrid.Items.Clear();
        }

        public void FinalizeCollisionReport(List<CollisionData> rawData)
        {
            // Clear the current DataGrid content
            ClearCollisionData();

            // Process raw collision data into summarized format
            var summarizedData = ProcessCollisionData(rawData);

            // Replace the current DataGrid content with the summarized data
            AddCollisionData(summarizedData);
        }


        private void CollisionProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Check if the progress bar has reached its maximum value
            if (CollisionProgressBar.Value == CollisionProgressBar.Maximum)
            {
                // Retrieve the raw data from the DataGrid (accessing the Items collection directly)
                var rawCollisionData = CollisionDataGrid.Items.Cast<CollisionData>().ToList();

                if (rawCollisionData != null && rawCollisionData.Any())
                {
                    // Trigger the progress complete logic
                    OnProgressComplete(rawCollisionData);
                }
                else
                {
                    MessageBox.Show("No collision data available to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        // Example of tying progress completion to finalizing the collision report
        public void OnProgressComplete(List<CollisionData> rawData)
        {
            Dispatcher.Invoke(() =>
            {
                // Process data and update DataGrid
                FinalizeCollisionReport(rawData);

                // Optionally, reset the progress bar to 0 (if needed)
                CollisionProgressBar.Value = 0;
            });
        }
        public List<CollisionData> ProcessCollisionData(List<CollisionData> rawData)
        {
            // Group by unique callsign pairs
            var groupedData = rawData
                .GroupBy(collision =>
                    string.Compare(collision.Callsign1, collision.Callsign2) < 0
                        ? (collision.Callsign1, collision.Callsign2)
                        : (collision.Callsign2, collision.Callsign1)) // Ensure unique pairing
                .Select(group =>
                {
                    var callsign1 = group.Key.Item1;
                    var callsign2 = group.Key.Item2;

                    // Extract the start time (earliest) and end time (latest)
                    var startTime = group.Min(c => c.CollisionStart); // Earliest time in the group
                    var endTime = group.Max(c => c.CollisionEnd); // Latest time in the group

                    // Ensure CollisionEnd is not the same as CollisionStart
                    if (string.IsNullOrEmpty(endTime))
                    {
                        // Set CollisionEnd to a reasonable default if no end time exists
                        endTime = startTime;
                    }

                    // Extract flight levels from the group
                    var fl1List = group.Select(c => int.Parse(c.FLcallsign1.Replace("FL", ""))).ToList();
                    var fl2List = group.Select(c => int.Parse(c.FLcallsign2.Replace("FL", ""))).ToList();

                    // Calculate median flight levels
                    var medianFL1 = CollisionData.CalculateMedianFL(fl1List);
                    var medianFL2 = CollisionData.CalculateMedianFL(fl2List);

                    // Create summarized CollisionData object
                    return new CollisionData
                    {
                        Callsign1 = callsign1,
                        Callsign2 = callsign2,
                        CollisionStart = startTime,  // Earliest collision time
                        CollisionEnd = endTime,     // Latest collision time
                        FLcallsign1 = medianFL1,
                        FLcallsign2 = medianFL2
                    };
                })
                .ToList();

            return groupedData;
        }


        public void AddCollisionData(List<CollisionData> newCollisions)
        {
            // Add new collision data to the DataGrid
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