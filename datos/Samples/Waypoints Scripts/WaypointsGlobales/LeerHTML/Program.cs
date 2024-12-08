using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using System.Globalization;
using ExcelReader;
using System.Web;
using System.Text.Encodings.Web;
using System.Net;

namespace HTMLReader
{
    public class LeeHTML
    {
        public static string saveLatitude(string lat)
        {
            string latString;
            if (lat.Length > 7) {
                if (lat.Contains(' ') && lat.Contains('.') && !lat.Contains('-') && lat.Split(' ').Length == 2)
                {
                    string[] latParts = lat.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int latDegrees = int.Parse(latParts[0]);
                    double latMinutes = double.Parse(latParts[1].TrimEnd('S', 'N'));

                    double latitude = latDegrees + (latMinutes / 60.0);
                    if (lat.EndsWith("S"))
                        latitude = -latitude; // Negative for South
                    latitude = Math.Round(latitude, 3);
                    latString = latitude.ToString("F3", CultureInfo.InvariantCulture);

                }
                else if (lat.Contains('-'))
                {
                    string[] latParts = lat.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    int latDegrees = int.Parse(latParts[0]);
                    int latMinutes = int.Parse(latParts[1]);
                    double latSeconds = double.Parse(latParts[2].TrimEnd('N', 'S'));

                    double latitude = latDegrees + (latMinutes / 60.0) + (latSeconds / 3600.0);
                    if (lat.EndsWith("S"))
                        latitude = -latitude; // Negative for South
                    latitude = Math.Round(latitude, 3);
                    latString = latitude.ToString("F3", CultureInfo.InvariantCulture);
                }
                else
                {
                    string decodedLat = WebUtility.HtmlDecode(lat);
                    string[] latTrozos = decodedLat.Split(new[] { "°", "'", "\"", ((char)(176)).ToString() }, StringSplitOptions.RemoveEmptyEntries);
                    int latDegrees = int.Parse(latTrozos[0]);
                    int latMinutes = int.Parse(latTrozos[1]);
                    latTrozos[2] = latTrozos[2].Replace(" N", "").Replace(" S", "");
                    double latSeconds = double.Parse(latTrozos[2]);

                    double latitude = latDegrees + (latMinutes / 60.0) + (latSeconds / 3600.0);
                    if (lat.EndsWith("S"))
                        latitude = -latitude; // Negative for South
                    latitude = Math.Round(latitude, 3);
                    latString = latitude.ToString("F3", CultureInfo.InvariantCulture);
                }
            }
            else
            {
                int latDegrees = int.Parse(lat.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int latMinutes = int.Parse(lat.Substring(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int latSeconds = int.Parse(lat.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                double latitude = (latDegrees + latMinutes / 60.0 + latSeconds / 3600.0);
                if (lat[6] == 'S')
                    latitude = -1 * latitude;
                latitude = Math.Round(latitude, 3);
                string latitudetxt = latitude.ToString("F3", CultureInfo.InvariantCulture);
                latString = latitude.ToString("F3", CultureInfo.InvariantCulture);
            }
            return latString;
        }
        public static string saveLongitude(string lon)
        {
            string lonString;
            if (lon.Length > 8)
            {
                if (lon.Contains(' ') && lon.Contains('.') && !lon.Contains('-') && lon.Split(' ').Length == 2)
                {
                    string[] lonParts = lon.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int lonDegrees = int.Parse(lonParts[0]);
                    double lonMinutes = double.Parse(lonParts[1].TrimEnd('E', 'W'));

                    double longitude = lonDegrees + (lonMinutes / 60.0);
                    if (lon.EndsWith("W"))
                        longitude = -longitude; // Negative for West
                    longitude = Math.Round(longitude, 3);

                    lonString = longitude.ToString("F3", CultureInfo.InvariantCulture);
                }
                else if (lon.Contains("-"))
                {
                    string[] lonParts = lon.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    int lonDegrees = int.Parse(lonParts[0]);
                    int lonMinutes = int.Parse(lonParts[1]);
                    double lonSeconds = double.Parse(lonParts[2].TrimEnd('E', 'W'));

                    double longitude = lonDegrees + (lonMinutes / 60.0) + (lonSeconds / 3600.0);
                    if (lon.EndsWith("W"))
                        longitude = -longitude; // Negative for West
                    longitude = Math.Round(longitude, 3);

                    // Output for verification
                    lonString = longitude.ToString("F3", CultureInfo.InvariantCulture);
                }
                else
                {
                    string decodedLon = WebUtility.HtmlDecode(lon);
                    string[] lonTrozos = decodedLon.Split(new[] { "°", "'", "\"" }, StringSplitOptions.RemoveEmptyEntries);
                    lonTrozos[0] = lonTrozos[0].Replace(":", "");
                    int lonDegrees = int.Parse(lonTrozos[0]);
                    int lonMinutes = int.Parse(lonTrozos[1]);
                    lonTrozos[2] = lonTrozos[2].Replace(" E", "").Replace(" W", "");
                    double lonSeconds = double.Parse(lonTrozos[2]);

                    double longitude = lonDegrees + (lonMinutes / 60.0) + (lonSeconds / 3600.0);
                    if (lon.EndsWith("W"))
                        longitude = -longitude; // Negative for West
                    longitude = Math.Round(longitude, 3);

                    lonString = longitude.ToString("F3", CultureInfo.InvariantCulture);
                }
            }
            else
            {
                int lonDegrees = int.Parse(lon.Substring(0, 3), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int lonMinutes = int.Parse(lon.Substring(3, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int lonSeconds = int.Parse(lon.Substring(5, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                double longitude = (lonDegrees + lonMinutes / 60.0 + lonSeconds / 3600.0);
                if (lon[7] == 'W')
                    longitude = -1 * longitude;
                longitude = Math.Round(longitude, 3);
                lonString = longitude.ToString("F3", CultureInfo.InvariantCulture);
            }
            return lonString;
        }
        static string[] SplitCoordinate(string coordinate)
        {
            return coordinate.Split(new[] { "º ", "' ", "\" " }, StringSplitOptions.RemoveEmptyEntries);
        }
        public static void Main(string[] args)
        {

            LeeExcel a = new LeeExcel(@"C:\Users\05agu\Desktop\Uni\3r Quatri\Info 2\CountriesInfoExcel.xlsx",1);
            StreamWriter w = new StreamWriter(@"C:\Users\05agu\Desktop\Uni\3r Quatri\Info 2\Waypoint\TxtFileDir\WorldWaypoints.txt", false);
            StreamWriter w2 = new StreamWriter(@"C:\Users\05agu\Desktop\Uni\3r Quatri\Info 2\Waypoint\TxtFileDir\ClassifiedWorldWaypoints.txt", false);
            List<List<string>> countries = a.readCountries();
            Console.ReadKey();
            List<string> paises = new List<string> { "uk" };
            LeeHTML b = new LeeHTML();
            HtmlDocument doc = new HtmlDocument();
            for (int i = 0; i < countries.Count; i++)
            {
                string pais = countries[i][0];
                Console.WriteLine(countries[i][1]);
                string enlace = "http://opennav.com/waypoint/" + pais;

                doc = new HtmlWeb().Load(enlace);
                //Leer el archivo HTMl

                // Load the HTML file                    
                var table = doc.DocumentNode.SelectSingleNode("//table[@class='datagrid fullwidth']");
                var rows = table.SelectNodes(".//tr");

                if (rows == null)
                {
                    Console.WriteLine("No rows found in the table.");
                    return;
                }

                List<string> waypoints = new List<string>();
                for (int j = 2; j < rows.Count; j++)
                {
                    var row = rows[j];
                    var cells = row.SelectNodes(".//td");
                    if (cells != null && cells.Count >= 5)
                    {
                        string Ident = cells[0].InnerText.Trim();
                        string lat = cells[2].InnerText.Trim();
                        string lon = cells[4].InnerText.Trim();
                        string[] exceptions = { "POVOX", "AMIR", "ARUSI", "GIKTU", "PIANO", "AJENT", "APRIL", "AUGUR", "BRAVO", "CANDY", "CHALI", "COSMO", "DECOY", "FETUS", "JAMMY", "MARCH", "MAYOR", "NEPAS", "NOVAS", "OCTAN", "SEPIA", "SUMER" };
                        if (exceptions.Contains(Ident))
                        {
                            continue;
                        }
                        else
                        {
                            if (Ident == "DENIM")
                            {
                                lon = "1152335E";
                            }
                            else if(Ident == "HELIX")
                            {
                                lon = "1164745E";
                            }
                            else if (Ident == "ADBON")
                            {
                                lat = "000000N";
                            }
                            else if (Ident == "ARTOP")
                            {
                                lat = "005241S";
                            }
                            else if (Ident == "BAYUS")
                            {
                                lat = "005405N";
                                lon = "1751248W";
                            }
                            else if (Ident == "BISOX")
                            {
                                lat = "001918S";
                                lon = "1492930W";
                            }
                            string latString = saveLatitude(lat);
                            string lonString = saveLongitude(lon);
                            w.WriteLine($"{Ident},{latString},{lonString}");
                            Console.WriteLine($"{Ident},{latString},{lonString}");
                            w2.WriteLine($"{countries[i][2]},{countries[i][1]},{Ident},{latString},{lonString}");
                            /*
                            if (lat.Length > 7)
                            {
                                if (lat.Contains(' ') && lat.Contains('.') && !lat.Contains('-') && lat.Split(' ').Length == 2)
                                {
                                    // Format: 20 24.58S
                                    string[] latParts = lat.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    int latDegrees = int.Parse(latParts[0]);
                                    double latMinutes = double.Parse(latParts[1].TrimEnd('S', 'N'));

                                    double latitude = latDegrees + (latMinutes / 60.0);
                                    if (lat.EndsWith("S"))
                                        latitude = -latitude; // Negative for South
                                    latitude = Math.Round(latitude, 3);

                                    // Longitude: 047 54.52W
                                    string[] lonParts = lon.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    int lonDegrees = int.Parse(lonParts[0]);
                                    double lonMinutes = double.Parse(lonParts[1].TrimEnd('E', 'W'));

                                    double longitude = lonDegrees + (lonMinutes / 60.0);
                                    if (lon.EndsWith("W"))
                                        longitude = -longitude; // Negative for West
                                    longitude = Math.Round(longitude, 3);

                                    string latString = latitude.ToString("F3", CultureInfo.InvariantCulture);
                                    string lonString = longitude.ToString("F3", CultureInfo.InvariantCulture);
                                    w.WriteLine($"{Ident},{latString},{lonString}");
                                    Console.WriteLine($"{Ident},{latString},{lonString}");
                                    w2.WriteLine($"{countries[i][2]},{countries[i][1]},{Ident},{latString},{lonString}");
                                }
                                else if (lat.Contains('-'))
                                {
                                    // Split by '-' for this format: 47-22-59.27N
                                    string[] latParts = lat.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                                    int latDegrees = int.Parse(latParts[0]);
                                    int latMinutes = int.Parse(latParts[1]);
                                    double latSeconds = double.Parse(latParts[2].TrimEnd('N', 'S'));

                                    double latitude = latDegrees + (latMinutes / 60.0) + (latSeconds / 3600.0);
                                    if (lat.EndsWith("S"))
                                        latitude = -latitude; // Negative for South
                                    latitude = Math.Round(latitude, 3);

                                    // Longitude (similar process for 'E' or 'W')
                                    string[] lonParts = lon.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                                    int lonDegrees = int.Parse(lonParts[0]);
                                    int lonMinutes = int.Parse(lonParts[1]);
                                    double lonSeconds = double.Parse(lonParts[2].TrimEnd('E', 'W'));

                                    double longitude = lonDegrees + (lonMinutes / 60.0) + (lonSeconds / 3600.0);
                                    if (lon.EndsWith("W"))
                                        longitude = -longitude; // Negative for West
                                    longitude = Math.Round(longitude, 3);

                                    // Output for verification
                                    string latString = latitude.ToString("F3", CultureInfo.InvariantCulture);
                                    string lonString = longitude.ToString("F3", CultureInfo.InvariantCulture);
                                    w.WriteLine($"{Ident},{latString},{lonString}");
                                    Console.WriteLine($"{Ident},{latString},{lonString}");
                                    w2.WriteLine($"{countries[i][2]},{countries[i][1]},{Ident},{latString},{lonString}");
                                }
                                else
                                {
                                    // Existing cases for '08° 29' 06.00" S' format
                                    string decodedLat = WebUtility.HtmlDecode(lat);
                                    string[] latTrozos = decodedLat.Split(new[] { "°", "'", "\"", ((char) (176)).ToString() }, StringSplitOptions.RemoveEmptyEntries);
                                    // Process as before...
                                    
                                    
                                    string[] latTrozos = lat.Split(new[] { "º ", "' ", "\" " }, StringSplitOptions.RemoveEmptyEntries);
                                    int latDegrees = int.Parse(latTrozos[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
                                    int latMinutes = int.Parse(latTrozos[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                                    double latSeconds = double.Parse(latTrozos[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                                    double latitude = (latDegrees + latMinutes / 60.0 + latSeconds / 3600.0);
                                    if (lat.EndsWith("S"))
                                        latitude = -1 * latitude;
                                    latitude = Math.Round(latitude, 3);
                                    string latitudetxt = latitude.ToString("F3", CultureInfo.InvariantCulture);

                                    //Latitud
                                    string[] lonTrozos = lon.Split(new[] { "º ", "' ", "\" " }, StringSplitOptions.RemoveEmptyEntries);
                                    int lonDegrees = int.Parse(lonTrozos[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
                                    int lonMinutes = int.Parse(lonTrozos[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                                    double lonSeconds = double.Parse(lonTrozos[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                                    double longitude = (lonDegrees + lonMinutes / 60.0 + lonSeconds / 3600.0);
                                    if (lon.EndsWith("W"))
                                        longitude = -1 * longitude;
                                    longitude = Math.Round(longitude, 3);

                                    string longitudetxt = longitude.ToString("F3", CultureInfo.InvariantCulture);
                                    
                                    int latDegrees = int.Parse(latTrozos[0]);
                                    int latMinutes = int.Parse(latTrozos[1]);
                                    latTrozos[2] = latTrozos[2].Replace(" N", "").Replace(" S", "");
                                    double latSeconds = double.Parse(latTrozos[2]);

                                    double latitude = latDegrees + (latMinutes / 60.0) + (latSeconds / 3600.0);
                                    if (lat.EndsWith("S"))
                                        latitude = -latitude; // Negative for South
                                    latitude = Math.Round(latitude, 3);

                                    // Split Longitude
                                    string decodedLon = WebUtility.HtmlDecode(lon);
                                    string[] lonTrozos = decodedLon.Split(new[] { "°", "'", "\""}, StringSplitOptions.RemoveEmptyEntries);
                                    int lonDegrees = int.Parse(lonTrozos[0]);
                                    int lonMinutes = int.Parse(lonTrozos[1]);
                                    lonTrozos[2] = lonTrozos[2].Replace(" E", "").Replace(" W", "");
                                    double lonSeconds = double.Parse(lonTrozos[2]);

                                    double longitude = lonDegrees + (lonMinutes / 60.0) + (lonSeconds / 3600.0);
                                    if (lon.EndsWith("W"))
                                        longitude = -longitude; // Negative for West
                                    longitude = Math.Round(longitude, 3);

                                    string latString = latitude.ToString("F3", CultureInfo.InvariantCulture);
                                    string lonString = longitude.ToString("F3", CultureInfo.InvariantCulture);
                                    w.WriteLine($"{Ident},{latString},{lonString}");
                                    Console.WriteLine($"{Ident},{latString},{lonString}");
                                    w2.WriteLine($"{countries[i][2]},{countries[i][1]},{Ident},{latString},{lonString}");
                                }
                            }
                            else
                            {
                                int latDegrees = int.Parse(lat.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                                int latMinutes = int.Parse(lat.Substring(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                                int latSeconds = int.Parse(lat.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                                double latitude = (latDegrees + latMinutes / 60.0 + latSeconds / 3600.0);
                                if (lat[6] == 'S')
                                    latitude = -1 * latitude;
                                latitude = Math.Round(latitude, 3);
                                string latitudetxt = latitude.ToString("F3", CultureInfo.InvariantCulture);

                                //Latitud

                                int lonDegrees = int.Parse(lon.Substring(0, 3), NumberStyles.Integer, CultureInfo.InvariantCulture);
                                int lonMinutes = int.Parse(lon.Substring(3, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                                int lonSeconds = int.Parse(lon.Substring(5, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                                double longitude = (lonDegrees + lonMinutes / 60.0 + lonSeconds / 3600.0);
                                if (lon[7] == 'W')
                                    longitude = -1 * longitude;
                                longitude = Math.Round(longitude, 3);
                                string longitudetxt = longitude.ToString("F3", CultureInfo.InvariantCulture);
                                w.WriteLine($"{Ident},{latitudetxt},{longitudetxt}");
                                Console.WriteLine($"{Ident},{latitude},{longitude}");
                                w2.WriteLine($"{countries[i][2]},{countries[i][1]},{Ident},{latitudetxt},{longitudetxt}");
                            }
                            */
                        }
                        
                    }
                }
                
            }

            Console.ReadLine();

        }
    }
}

