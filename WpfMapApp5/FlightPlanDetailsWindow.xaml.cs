using Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ArcGIS_App
{
    public partial class FlightPlanDetailsWindow : Window
    {
        private List<FlightPlanGIS> _flightPlans;
        private List<FlightPlanGIS> _sortedFlightPlans;  // For storing the sorted list

        public FlightPlanDetailsWindow(List<FlightPlanGIS> flightPlans)
        {
            InitializeComponent();
            _flightPlans = flightPlans;
            _sortedFlightPlans = new List<FlightPlanGIS>(flightPlans);  // Initially, use the original list

            // Load all flight plans into the ListBox
            FlightPlansList.ItemsSource = _sortedFlightPlans.Select(fp => fp.Callsign).ToList();
        }

        private void SearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Filter the flight plans based on the search text
            string searchText = SearchBox.Text.ToLower();
            var filteredPlans = _sortedFlightPlans
                .Where(fp => fp.Callsign.ToLower().Contains(searchText))
                .Select(fp => fp.Callsign)
                .ToList();

            FlightPlansList.ItemsSource = filteredPlans;
        }

        private void FlightPlansList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // When a flight plan is selected, show the details
            string selectedCallsign = (string)FlightPlansList.SelectedItem;
            if (string.IsNullOrEmpty(selectedCallsign)) return;

            var selectedFlightPlan = _sortedFlightPlans.FirstOrDefault(fp => fp.Callsign == selectedCallsign);

            if (selectedFlightPlan != null)
            {
                string details = $"Company: {selectedFlightPlan.CompanyName}\n" +
                                 $"Callsign: {selectedFlightPlan.Callsign}\n" +
                                 $"Aircraft: {selectedFlightPlan.Aircraft}\n" +
                                 $"Start Time: {selectedFlightPlan.StartTime.ToString("HH:mm:ss")}\n\n" +
                                 "Waypoints and Flight Levels:\n";

                for (int i = 0; i < selectedFlightPlan.Waypoints.Count; i++)
                {
                    details += $"  - Waypoint: {selectedFlightPlan.Waypoints[i].ID}, " +
                               $"Flight Level: {selectedFlightPlan.FlightLevels[i]}, " +
                               $"Speed: {selectedFlightPlan.Speeds[i]}\n";
                }

                FlightPlanDetailsText.Text = details;
            }
        }

        // Hide the watermark when the TextBox gains focus
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            WatermarkLabel.Visibility = Visibility.Collapsed;
        }

        // Show the watermark when the TextBox loses focus (if it is empty)
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                WatermarkLabel.Visibility = Visibility.Visible;
            }
        }

        // Sort the flight plans by Callsign
        private void SortByName_Click(object sender, RoutedEventArgs e)
        {
            _sortedFlightPlans = _flightPlans.OrderBy(fp => fp.Callsign).ToList();
            UpdateFlightPlanList();
        }

        // Sort the flight plans by Departure (Start Time)
        private void SortByDeparture_Click(object sender, RoutedEventArgs e)
        {
            _sortedFlightPlans = _flightPlans.OrderBy(fp => fp.StartTime).ToList();
            UpdateFlightPlanList();
        }

        // Update the ListBox with the sorted flight plans
        private void UpdateFlightPlanList()
        {
            FlightPlansList.ItemsSource = _sortedFlightPlans.Select(fp => fp.Callsign).ToList();
        }
    }
}
