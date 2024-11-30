using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Class;
using OxyPlot.Axes;

namespace ArcGIS_App
{
    public partial class FlightPlanDetailsWindow : Window
    {
        private List<FlightPlanGIS> _flightPlans;
        private MainWindow _mainWindow;
        private FlightPlanGIS _currentlyTrackedFlightPlan; // Keep track of the currently tracked flight plan

        public FlightPlanDetailsWindow(List<FlightPlanGIS> flightPlans, MainWindow mainWindow)
        {
            InitializeComponent();
            _flightPlans = flightPlans;
            _mainWindow = mainWindow;

            // Load all flight plans into the ListBox
            FlightPlansList.ItemsSource = _flightPlans.Select(fp => fp.Callsign).ToList();
            this.Topmost = true; // Ensure the welcome window stays on top of the MainWindow
        }

        private void FlightPlansList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string selectedCallsign = (string)FlightPlansList.SelectedItem;
            if (string.IsNullOrEmpty(selectedCallsign)) return;

            var selectedFlightPlan = _flightPlans.FirstOrDefault(fp => fp.Callsign == selectedCallsign);

            if (selectedFlightPlan != null)
            {
                // Show flight details info and vertical profile
                ShowFlightInfo(selectedFlightPlan);
                ShowVerticalProfile(selectedFlightPlan);
            }
        }

        private void ShowFlightInfo(FlightPlanGIS selectedFlightPlan)
        {
            FlightPlanDetailsText.Visibility = Visibility.Visible;
            FlightPlanDetailsText.Text = $"Company: {selectedFlightPlan.CompanyName}\n" +
                                         $"Callsign: {selectedFlightPlan.Callsign}\n" +
                                         $"Aircraft: {selectedFlightPlan.Aircraft}\n" +
                                         $"Start Time: {selectedFlightPlan.StartTime.ToString("HH:mm:ss")}\n\n" +
                                         "Waypoints and Flight Levels:\n";

            foreach (var waypoint in selectedFlightPlan.Waypoints)
            {
                FlightPlanDetailsText.Text += $"  - Waypoint: {waypoint.ID}, " +
                                              $"Flight Level: {selectedFlightPlan.FlightLevels[selectedFlightPlan.Waypoints.IndexOf(waypoint)]}, " +
                                              $"Speed: {selectedFlightPlan.Speeds[selectedFlightPlan.Waypoints.IndexOf(waypoint)]}KT\n";
            }

            // Enable GoTo button and make it visible
            GoToButton.Visibility = Visibility.Visible;
        }

        private void ShowVerticalProfile(FlightPlanGIS selectedFlightPlan)
        {
            // Prepare the plot model for vertical profile
            var plotModel = new PlotModel { Title = "Flight Profile" };

            // Prepare the data series for the profile
            var series = new LineSeries
            {
                Title = "Flight Levels",
                Color = OxyColors.RoyalBlue,
                StrokeThickness = 1.5,
                MarkerType = MarkerType.Circle,
                MarkerSize = 5
            };

            // Get the flight levels and waypoints
            var flightLevels = selectedFlightPlan.FlightLevels;
            var waypoints = selectedFlightPlan.Waypoints;

            // Filter unique flight levels (only the defined FLs, removing duplicates)
            var uniqueFlightLevels = flightLevels
                .Where(f => f.StartsWith("FL") || f.EndsWith("m")) // Only FL or meters
                .Distinct()
                .OrderBy(f => f) // Sort FLs in ascending order
                .ToList();

            // Define the vertical axis with only the specific flight levels present in the flight plan
            var verticalAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Flight Level (FL)",
                MajorStep = 5000, // Display every 5000 feet (adjust as needed)
                Minimum = 0,
                Maximum = 50000, // Ensure the max height is FL500 (50000 feet)
                LabelFormatter = value =>  // Format the vertical axis labels as FLXX
                {
                    var matchingFL = uniqueFlightLevels.FirstOrDefault(f =>
                        int.TryParse(f.Substring(2), out int level) && level * 100 == value);
                    return matchingFL ?? string.Empty;  // Only return FL values that match the defined flight levels
                }
            };

            // Add vertical axis to the plot model
            plotModel.Axes.Add(verticalAxis);

            // Horizontal Axis - Display waypoint names
            var horizontalAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Waypoints",
                Angle = 45, // Rotate the axis labels by 45 degrees for better fit
                IsTickCentered = true
            };

            // Add waypoint names to the horizontal axis
            foreach (var waypoint in waypoints)
            {
                horizontalAxis.Labels.Add(waypoint.ID);  // Using waypoint ID as the label
            }

            // Add horizontal axis to the plot model
            plotModel.Axes.Add(horizontalAxis);

            // Add data points for each waypoint to the line series
            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                var flightLevel = flightLevels[i];

                double verticalPosition = 0;

                if (flightLevel.StartsWith("FL"))
                {
                    // Extract the flight level value (e.g., FL340 -> 34000 feet)
                    int level = int.Parse(flightLevel.Substring(2));
                    verticalPosition = level * 100; // Convert FL to feet (e.g., FL350 -> 35000ft)
                }
                else if (flightLevel.EndsWith("m"))
                {
                    // Convert meters to feet for consistency
                    verticalPosition = double.Parse(flightLevel.Replace("m", ""));
                }

                // Add the point to the line series (only at waypoints with defined FL)
                if (verticalPosition > 0)
                {
                    series.Points.Add(new DataPoint(i, verticalPosition));

                    // Add a vertical dotted line at each waypoint (using a line series)
                    var verticalLine = new LineSeries
                    {
                        StrokeThickness = 1,
                        Color = OxyColors.Gray,
                        MarkerType = MarkerType.None,
                    };

                    verticalLine.Points.Add(new DataPoint(i, 0)); // Start at the bottom (0)
                    verticalLine.Points.Add(new DataPoint(i, verticalPosition)); // Go to the FL value
                    plotModel.Series.Add(verticalLine);  // Add the vertical line for each waypoint

                    // Draw horizontal lines connecting the flight levels to the vertical axis
                    var horizontalLine = new LineSeries
                    {
                        StrokeThickness = 1,
                        Color = OxyColors.Gray,
                        MarkerType = MarkerType.None
                    };

                    horizontalLine.Points.Add(new DataPoint(0, verticalPosition)); // Start at the first waypoint (horizontal axis)
                    horizontalLine.Points.Add(new DataPoint(i, verticalPosition)); // Connect to the current waypoint
                    plotModel.Series.Add(horizontalLine); // Add the horizontal line
                }
            }

            // Add the series to the plot model
            plotModel.Series.Add(series);

            // Assign the plot model to the PlotView (VerticalProfilePlot)
            VerticalProfilePlot.Model = plotModel;
        }




        private void GoToButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedCallsign = (string)FlightPlansList.SelectedItem;
            if (string.IsNullOrEmpty(selectedCallsign)) return;

            var selectedFlightPlan = _flightPlans.FirstOrDefault(fp => fp.Callsign == selectedCallsign);

            if (selectedFlightPlan != null)
            {
                if (_currentlyTrackedFlightPlan == selectedFlightPlan)
                {
                    // Stop tracking
                    _mainWindow.StartTrackingPlane(null); // Pass null to stop tracking
                    GoToButton.Content = "Go To"; // Change button text back to "Go To"
                    _currentlyTrackedFlightPlan = null; // Reset the tracked flight plan
                }
                else
                {
                    // Start tracking the new flight plan
                    _mainWindow.StartTrackingPlane(selectedFlightPlan);
                    GoToButton.Content = "Stop Tracking"; // Change button text to "Stop Tracking"
                    _currentlyTrackedFlightPlan = selectedFlightPlan;
                }
            }
        }

        // Handle GotFocus event for SearchBox (hide watermark)
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            WatermarkLabel.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                WatermarkLabel.Visibility = Visibility.Visible;
            }
        }


        // Sort Flight Plans by Callsign (Name)
        private void SortByName_Click(object sender, RoutedEventArgs e)
        {
            _flightPlans = _flightPlans.OrderBy(fp => fp.Callsign).ToList();
            FlightPlansList.ItemsSource = _flightPlans.Select(fp => fp.Callsign).ToList();
        }

        // Sort Flight Plans by Departure (StartTime)
        private void SortByDeparture_Click(object sender, RoutedEventArgs e)
        {
            _flightPlans = _flightPlans.OrderBy(fp => fp.StartTime).ToList();
            FlightPlansList.ItemsSource = _flightPlans.Select(fp => fp.Callsign).ToList();
        }
        private void SearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            var filteredPlans = _flightPlans
                .Where(fp => fp.Callsign.ToLower().Contains(searchText))
                .Select(fp => fp.Callsign)
                .ToList();

            FlightPlansList.ItemsSource = filteredPlans;
        }
      
    }
}


      