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
                    .UseLicense("runtimelite,1000,rud4181779618,none,KGE60RFLTHLK4P7EJ117")
                    .UseApiKey("AAPTxy8BH1VEsoebNVZXo8HurCi114pfGLP0iELTnYVZjS-uxnIX0WlAXSNpuphVxu2cWDIySIgv7GXBrrOTo0iTQ-LT2O8z-qXfYEZ5sDtn8OHJRE-waw9KrCJDnsxwdYvtUo2OnxQ3kiFUo9gn-eWCvSoR-flhQYABsRHKivlVQGiNczspWnZW_jMQ-mD0uGRQjw_pVuvZdhhLQ7DvRoNsJ4gAnfnzoNYfud9WcSm-qcE.AT1_sYuiSXs7")
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