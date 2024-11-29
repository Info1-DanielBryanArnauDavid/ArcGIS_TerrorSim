using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Class
{
    public class FlightPlanGIS
    {
        public string CompanyName { get; set; }
        public string Callsign {  get; set; }
        public string Aircraft { get; set; }
        public DateTime StartTime { get; set; }  // Start time is DateTime now
        public List<WaypointGIS> Waypoints { get; set; }
        public List<string> FlightLevels { get; set; }
        public List<string> Speeds { get; set; }
        public double TotalDuration { get; set; }

        // Constructor
        public FlightPlanGIS(string companyName, DateTime startTime, string callsign, string aircraft)
        {
            CompanyName = companyName;
            StartTime = startTime;
            Waypoints = new List<WaypointGIS>();
            FlightLevels = new List<string>();
            Speeds = new List<string>();
            Callsign = callsign;
            Aircraft = aircraft;
        }
    }



}
