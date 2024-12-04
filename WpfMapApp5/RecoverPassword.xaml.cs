using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
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
using Class;

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

            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587;
            string senderEmail = "TerrorSim11@gmail.com"; // Correo del remitente autorizado
            string senderPassword = "ihez zurg ldlr ztzl"; // Contraseña de aplicación de Gmail

            GestionUsuarios MisUsuarios = new GestionUsuarios();
            MisUsuarios.Iniciar();
            string Password = MisUsuarios.ContraseñaUsuario(username);

            if (Password != "No existe")
            {
                try
                {
                    MailMessage mail = new MailMessage
                    {
                        From = new MailAddress(senderEmail), // Usa el correo del remitente autorizado
                        Subject = "Password Recovery",
                        Body = $"Hi {username},\n\n" +
                               $"Your password is: {Password}\n\n" +
                               "If you did not request this, please secure your account.\n\n" +
                               "Best regards,\nYour Support Team",
                        IsBodyHtml = false
                    };
                    mail.To.Add(email); // Correo del destinatario

                    using (SmtpClient smtp = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }

                    MessageBox.Show("Password recovery link has been sent to your email address.",
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending email: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    MisUsuarios.Cerrar();
                }
            }
            else
            {
                MessageBox.Show("No existe el usuario mencionado");
                MisUsuarios.Cerrar();
            }
        }
            private void CancelButton_Click(object sender, RoutedEventArgs e)
            {
               // Close the Recover Password window
               this.Close();
            }
    }
}
