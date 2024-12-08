using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using _Excel = Microsoft.Office.Interop.Excel;
using System.Globalization;

namespace ExcelReader
{
    public class LeeExcel
    {
        string path="";
        _Application excel = new _Excel.Application();
        Workbook wb;
        Worksheet ws;
        public LeeExcel(string path, int Sheet) 
        {
            this.path = path;
            wb = excel.Workbooks.Open(path);
            ws = wb.Worksheets[Sheet];
        }

        public string ReadWaypoints(int i)
        {
            StreamWriter w = new StreamWriter(@"C:\Users\05agu\Desktop\Uni\3r Quatri\Info 2\EuropeWaypoints.txt.txt", false);
            string id;
            string lat;
            string lon;
            string Cell;
            while ((Cell= ws.Cells[i, 3].Value)!=null)
            {
                //Guardar id
                id = ws.Cells[i,3].Value;
                bool continuar = false;
                while (!continuar)
                {
                    if(id.Length < 5)
                    {
                        id += "_";
                    }
                    else
                    {
                        continuar = true;
                    }
                }

                //Latitud
                lat= ws.Cells[i, 4].Value;
                
                int latDegrees = int.Parse(lat.Substring(1, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int latMinutes = int.Parse(lat.Substring(3, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int latSeconds = int.Parse(lat.Substring(5, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                double latitude = (latDegrees + latMinutes / 60.0 + latSeconds / 3600.0);
                if (lat[0] == 'S')
                    latitude = -1 * latitude;
                latitude = Math.Round(latitude, 3);
                string latitudetxt=latitude.ToString("F3",CultureInfo.InvariantCulture);

                //Latitud
                lon = ws.Cells[i, 5].Value;

                int lonDegrees = int.Parse(lon.Substring(1, 3), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int lonMinutes = int.Parse(lon.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                int lonSeconds = int.Parse(lon.Substring(6, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
                double longitude = (lonDegrees + lonMinutes / 60.0 + lonSeconds / 3600.0);
                if (lon[0] == 'W')
                    longitude = -1 * longitude;
                longitude = Math.Round(longitude, 3);
                string longitudetxt = longitude.ToString("F3", CultureInfo.InvariantCulture);



                w.WriteLine(id+","+latitudetxt+","+longitudetxt);
                Console.WriteLine(id + "," + latitudetxt + "," + longitudetxt);


                i++;
            }
            Console.ReadLine();
            w.Close();
            return "a";
        }
        public List<List<string>> readCountries()
        {
            List<string> countryInfo = new List<string>();
            List<List<string>> countries = new List<List<string>>();

            string Cell;
            int i = 2;
            while ((Cell = ws.Cells[i, 3].Value) != null)
            {
                countryInfo = new List<string>();
                countryInfo.Add(ws.Cells[i,2].Value);
                countryInfo.Add(ws.Cells[i, 1].Value);
                countryInfo.Add(ws.Cells[i, 6].Value);
                countries.Add(countryInfo);
                i++;
            }
            
            return countries;
        }

    }
}
