using System; // Importa el espacio de nombres para funciones básicas
using System.Collections.Generic; // Importa clases para colecciones genéricas
using System.Linq; // Importa clases para LINQ
using System.Text; // Importa clases para manipulación de texto
using System.Threading.Tasks; // Importa clases para tareas asíncronas
using Microsoft.Data.Sqlite; // Importa clases para trabajar con SQLite
using static System.Runtime.InteropServices.JavaScript.JSType; // Importa tipos de JavaScript (no utilizado en este contexto)
using System.Net; // Importa clases para operaciones de red (no utilizado en este contexto)
using System.Data; // Importa clases para trabajar con datos
using System.Reflection.PortableExecutable; // Importa clases para trabajar con archivos ejecutables portables (no utilizado en este contexto)
using System.Data.SqlClient;
namespace Class // Define el espacio de nombres de la clase
{
    public class GestionUsuarios // Clase que gestiona usuarios en una base de datos SQLite
    {
        private string connectionString = "Server=tcp:usuarios.database.windows.net,1433;Initial Catalog=UsuariosDB;Persist Security Info=False;User ID=Bryan;Password=7776Aeter;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        private SqlConnection cnx;
        public void Iniciar() // Método para iniciar la conexión a la base de datos
        {
            cnx = new SqlConnection(connectionString); // Crea una nueva conexión a la base de datos
            cnx.Open(); // Abre la conexión
        }

        public void Cerrar() // Método para cerrar la conexión a la base de datos
        {
            cnx.Close(); // Cierra la conexión
        }

        public int CrearBaseDeUsuarios() // Método para crear la tabla de usuarios si no existe
        {
            string sql = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Usuarios' AND xtype='U') " +
                      "CREATE TABLE Usuarios ( Usuario varchar(50), Contraseña varchar(50))"; // SQL para crear la tabla si no existe
            SqlCommand command = new SqlCommand(sql, cnx); // Crea un comando SQL

            try
            {
                command.ExecuteNonQuery(); // Ejecuta el comando (crea la tabla)
                return 1; // Retorna 1 si se creó correctamente
            }
            catch (Exception) // Captura cualquier excepción que ocurra
            {
                return 0; // Retorna 0 si hubo un error
            }
        }


        public int ComprovarSiElUsuarioExiste(string Usuario, string Contraseña) // Verifica si un usuario existe en la base de datos
        {
            DataTable dt = new DataTable(); // Crea una nueva tabla de datos para almacenar resultados

            string sql = "SELECT * FROM Usuarios WHERE Usuario = @Usuario"; // SQL para buscar el usuario
            SqlCommand command = new SqlCommand(sql, cnx); // Crea un comando SQL
            command.Parameters.AddWithValue("@Usuario", Usuario); // Parametrizamos para evitar inyección SQL

            var reader = command.ExecuteReader(); // Ejecuta el comando y obtiene un lector

            dt.Load(reader); // Carga los resultados en el DataTable

            if (dt.Rows.Count > 0) // Si hay filas en los resultados
            {
                return 1; // Retorna 1 si el usuario existe
            }

            return 0; // Retorna 0 si el usuario no existe
        }

        public void AñadirUsuario(string Usuario, string Contraseña) // Método para añadir un nuevo usuario a la base de datos
        {
            string sql = "INSERT INTO Usuarios (Usuario, Contraseña) VALUES (@Usuario, @Contraseña)"; // SQL para insertar un nuevo usuario
            SqlCommand command = new SqlCommand(sql, cnx); // Crea un comando SQL
            command.Parameters.AddWithValue("@Usuario", Usuario); // Parametrizamos para evitar inyección SQL
            command.Parameters.AddWithValue("@Contraseña", Contraseña); // Parametrizamos para evitar inyección SQL
            command.ExecuteNonQuery(); // Ejecuta el comando (inserta el usuario)
        }

        public int ComprovarSiElUsuarioiContraseñaExiste(string Usuario, string Contraseña) // Verifica si un usuario y contraseña coinciden en la base de datos
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM Usuarios WHERE Usuario = @Usuario AND Contraseña = @Contraseña"; // SQL para verificar las credenciales
            SqlCommand command = new SqlCommand(sql, cnx);
            command.Parameters.AddWithValue("@Usuario", Usuario); // Parametrizamos para evitar inyección SQL
            command.Parameters.AddWithValue("@Contraseña", Contraseña); // Parametrizamos para evitar inyección SQL
            var reader = command.ExecuteReader();
            dt.Load(reader);

            if (dt.Rows.Count > 0)
            {
                return 1; // Usuario y contraseña válidos
            }

            return 0; // No coinciden los datos
        }

        public void EliminarTabla() // Método para eliminar la tabla de usuarios
        {
            string sql = "DROP TABLE IF EXISTS Usuarios"; // SQL para eliminar la tabla
            SqlCommand command = new SqlCommand(sql, cnx);
            command.ExecuteNonQuery(); // Ejecuta el comando (elimina la tabla)
        }

        public string ContraseñaUsuario(string Usuario)
        {
            DataTable dt = new DataTable();
            string sql = "SELECT Contraseña FROM Usuarios WHERE Usuario = @Usuario"; // SQL para verificar las credenciales
            SqlCommand command = new SqlCommand(sql, cnx);
            command.Parameters.AddWithValue("@Usuario", Usuario); // Parametrizamos para evitar inyección SQL
            var reader = command.ExecuteReader();
            dt.Load(reader);
            if (dt.Rows.Count == 1)
            {
                string Contraseña = Convert.ToString(dt.Rows[0]["Contraseña"]);
                return Contraseña;
            }
            else { return "No existe"; }
        }
    }
}