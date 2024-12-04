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
using System.Windows.Shapes;

namespace ArcGIS_App
{
    /// <summary>
    /// Lógica de interacción para RecoverPassword.xaml
    /// </summary>
    public partial class RecoverPassword : Window
    {
        public RecoverPassword()
        {
            InitializeComponent();
        }
        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve input from textboxes
            string username = usernameTextBox.Text;
            string confirmUsername = confirmUsernameTextBox.Text;
            string email = emailTextBox.Text;

            // Validate input
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(confirmUsername) ||
                string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (username != confirmUsername)
            {
                MessageBox.Show("Usernames do not match. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Simulate successful recovery
            MessageBox.Show("Password recovery link has been sent to your email address.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Close the Recover Password window
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the Recover Password window
            this.Close();
        }
    }
}
