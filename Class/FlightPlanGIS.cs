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
        public DateTime StartTime { get; set; }  // Start time is DateTime now
        public List<WaypointGIS> Waypoints { get; set; }
        public List<string> FlightLevels { get; set; }
        public List<string> Speeds { get; set; }

        // Constructor
        public FlightPlanGIS(string companyName, DateTime startTime)
        {
            CompanyName = companyName;
            StartTime = startTime;
            Waypoints = new List<WaypointGIS>();
            FlightLevels = new List<string>();
            Speeds = new List<string>();
        }
    }



}
