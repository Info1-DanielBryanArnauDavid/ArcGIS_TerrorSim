using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Class
{
    public class WaypointGIS
    {
        public string ID { get; set; }      // Waypoint identifier
        public double Latitude { get; set; } // Latitude in degrees
        public double Longitude { get; set; } // Longitude in degrees

        public WaypointGIS(string id, double latitude, double longitude)
        {
            ID = id;
            Latitude = latitude;
            Longitude = longitude;
        }
    }

}
