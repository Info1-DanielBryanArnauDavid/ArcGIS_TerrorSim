using Class;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ArcGIS_App
{
    public partial class Bonjour : Window
    {
        private GestionUsuarios MisUsuarios = new GestionUsuarios();

        public Bonjour()
        {
            InitializeComponent();
            StyleForm();
            InitializeLogic();
        }

        private void StyleForm()
        {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.NoResize;
            this.Background = Brushes.White;

            textBox1.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            textBox2.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

            button1.Background = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            button1.Foreground = Brushes.White;

            button2.Background = Brushes.White;
            button2.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            button2.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            button2.BorderThickness = new Thickness(1);
        }

        private void InitializeLogic()
        {
            MisUsuarios.Iniciar(); // Initialize user management
            MisUsuarios.CrearBaseDeUsuarios(); // Create user database
        }

         private void button1_Click(object sender, RoutedEventArgs e)
         {
            recoverPasswordLabel.Visibility = Visibility.Visible;
            if (MisUsuarios.ComprovarSiElUsuarioiContraseñaExiste(textBox1.Text, textBox2.Password) == 1)
            {
                MisUsuarios.Cerrar();
                MainWindow mainWindow = new MainWindow();
                mainWindow.Closed += (s, args) =>
                {
                    // When MainWindow closes, we want to ensure we shut down the application.
                    Application.Current.Shutdown(); // Ensure application exits
                };

                mainWindow.Show(); // Show the main window
                this.Close(); // Close the Bonjour window
            }
            else
            {
                MessageBox.Show("Incorrect Username/Password");
            }
        }

        private void RecoverPasswordLabel_Click(object sender, RoutedEventArgs e)
        {
            RecoverPassword recover = new RecoverPassword();
            recover.ShowDialog();
        }
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Registro registro = new Registro();
            registro.SetGestionUsuarios(MisUsuarios); // Pass user management to registration form
            registro.ShowDialog(); // Show registration dialog
        }

        private void checkBox1_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (checkBox1.IsChecked == true)
            {
                // Mostrar contraseña
                textBoxVisible.Text = textBox2.Password; // Sincronizar texto
                textBoxVisible.Visibility = Visibility.Visible;
                textBox2.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Ocultar contraseña
                textBox2.Password = textBoxVisible.Text; // Sincronizar contraseña
                textBoxVisible.Visibility = Visibility.Collapsed;
                textBox2.Visibility = Visibility.Visible;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Closed += (s, args) =>
                {
                    // Optional: Do something when the main window is closed
                    // For example, you might want to re-enable the current window
                };
                mainWindow.Show(); // Show the main window
                this.Hide(); // Hide instead of closing to prevent potential app shutdown
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening MainWindow: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}