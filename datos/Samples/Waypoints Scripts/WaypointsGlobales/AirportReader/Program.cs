using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirportReader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] european_countries = {"SPAIN"};

            StreamReader r = new StreamReader(@"C:\Users\05agu\Desktop\Uni\3r Quatri\Info 2\GlobalAirportDatabase.txt");
            StreamWriter w = new StreamWriter(@"C:\Users\05agu\Desktop\Uni\3r Quatri\Info 2\Waypoint\TxtFileDir\ESPAirports.txt");
            StreamWriter w2 = new StreamWriter(@"C:\Users\05agu\Desktop\Uni\3r Quatri\Info 2\Waypoint\TxtFileDir\ESPAirportsCoordinates.txt", false);
            string linea;
            while((linea = r.ReadLine()) != null)
            {
                
                string[] trozos = linea.Split(':');
                if (Convert.ToDouble(trozos[14]) != 0 && Convert.ToDouble(trozos[15]) != 0 && european_countries.Contains(trozos[4]))
                {
                    w.WriteLine($"{trozos[0]},{trozos[13]}");
                    w2.WriteLine($"{trozos[0]},{trozos[14]},{trozos[15]}");
                }
                    
            }
            w.Close();
            r.Close();
            w2.Close();

        }
    }
}
