using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;

namespace Class
{
    public class WaypointGIS
    {
        public string ID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public MapPoint Location { get; private set; }

        public WaypointGIS(string id, double lat, double lon)
        {
            ID = id;
            Latitude = lat;
            Longitude = lon;
            Location = new MapPoint(lon, lat, SpatialReferences.Wgs84);
        }
    }
}
