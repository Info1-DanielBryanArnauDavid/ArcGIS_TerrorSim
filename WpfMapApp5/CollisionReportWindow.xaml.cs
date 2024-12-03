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
namespace ArcGIS_App
{
    /// <summary>
    /// Lógica de interacción para CollisionReportWindow.xaml
    /// </summary>
    public partial class CollisionReportWindow : Window
    {
        public CollisionReportWindow()
        {
            InitializeComponent();
        }

        // Method to update the progress bar
        public void UpdateProgress(double progress)
        {
            CollisionProgressBar.Value = progress;
        }


    }
    public class CollisionData
    {
        public string Callsign1 { get; set; }
        public string Callsign2 { get; set; }
        public string CollisionStart { get; set; }
        public string CollisionEnd { get; set; }
        public string FLcallsign1 { get; set; }
        public string FLcallsign2 { get; set; }
        }
      
    }
