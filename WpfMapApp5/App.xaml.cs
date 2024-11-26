using System;
using System.Windows;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Security;

namespace ArcGIS_App
{
    public partial class App : Application
    {
       public App()
        {
            // Set shutdown mode to OnLastWindowClose
            this.ShutdownMode = ShutdownMode.OnLastWindowClose;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Initialize ArcGIS Maps SDK
                ArcGISRuntimeEnvironment.Initialize(config => config
                    //.UseLicense() descomentar y añadir la licencia
                    //.UseApiKey() descomentar y añadir la API y eso quehay en el .txt
                    .ConfigureAuthentication(auth => auth.UseDefaultChallengeHandler())
                );

                // Show Bonjour window
                Bonjour bonjourWindow = new Bonjour();
                bonjourWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}