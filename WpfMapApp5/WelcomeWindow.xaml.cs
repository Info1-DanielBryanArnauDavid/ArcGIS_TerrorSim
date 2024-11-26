using System;
using System.Windows;

namespace ArcGIS_App
{
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
        }

        // Override OnActivated to make sure it's always on top of MainWindow
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.Topmost = true; // Ensure the welcome window stays on top of the MainWindow
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Close the welcome window
        }

        // Make the window draggable when clicking and dragging
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove(); // Allows dragging the window
            }
        }
    }
}
