using Class;
using System.Windows;

namespace ArcGIS_App
{
    public partial class Registro : Window
    {
        private GestionUsuarios MisUsuarios = new GestionUsuarios();

        public Registro()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string username = textBox1.Text;
            string password = textBox2.Password;
            string confirmPassword = textBox3.Password;

            if (password != confirmPassword)
            {
                label4.Content = "Passwords \ndon't match";
                return;
            }

            // Ensure both username and password are passed
            if (MisUsuarios.ComprovarSiElUsuarioExiste(username, password) == 1)
            {
                label4.Content = "Username \ntaken";
                return;
            }

            if (username.Length > 20 || password.Length > 20)
            {
                label4.Content = "Username or \nPassword too \nlong";
                return;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                label4.Content = "Empty username/\npassword is not valid";
                return;
            }

            if (username.Contains(" "))
            {
                label4.Content = "No spaces \nin username";
                return;
            }

            if (password.Contains(" "))
            {
                label4.Content = "No spaces \nin password";
                return;
            }

            // Register the user if they do not exist
            if (MisUsuarios.ComprovarSiElUsuarioExiste(username, password) == 0)
            {
                MisUsuarios.AñadirUsuario(username, password);
                MessageBox.Show("User correctly registered");
                Close();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void SetGestionUsuarios(GestionUsuarios misUsuarios)
        {
            this.MisUsuarios = misUsuarios;
        }
    }
}