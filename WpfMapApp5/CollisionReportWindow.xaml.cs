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
using static ArcGIS_App.MapViewModel;
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
            this.Topmost = true; // Ensure the welcome window stays on top of the MainWindow
        }

        // Method to update the progress bar
        // Method to update the progress bar
        public void UpdateProgress(double progress)
        {
            // Use Dispatcher to ensure updates are done on the UI thread
            Dispatcher.Invoke(() =>
            {
                CollisionProgressBar.Value = progress;
            });
        }
        public void ClearCollisionData()
        {
            // Clear the current items from the DataGrid
            CollisionDataGrid.ItemsSource = null;
            CollisionDataGrid.Items.Clear();
        }

        public void AddCollisionData(List<CollisionData> newCollisions)
        {
            // Add new collision data to the DataGrid
            Dispatcher.Invoke(() =>
            {
                foreach (var collision in newCollisions)
                {
                    CollisionDataGrid.Items.Add(collision);
                }
            });
        }




        //metodo de añadir a la datagridview

    }

}
