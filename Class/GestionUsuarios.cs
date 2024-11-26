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

namespace Class // Define el espacio de nombres de la clase
{
    public class GestionUsuarios // Clase que gestiona usuarios en una base de datos SQLite
    {
        private SqliteConnection cnx; // Conexión a la base de datos SQLite

        public void Iniciar() // Método para iniciar la conexión a la base de datos
        {
            string dataSource = "Data Source=practica.db"; // Especifica la fuente de datos (archivo de base de datos)
            cnx = new SqliteConnection(dataSource); // Crea una nueva conexión a la base de datos
            cnx.Open(); // Abre la conexión
        }

        public void Cerrar() // Método para cerrar la conexión a la base de datos
        {
            cnx.Close(); // Cierra la conexión
        }

        public int CrearBaseDeUsuarios() // Método para crear la tabla de usuarios si no existe
        {
            string sql = "CREATE TABLE IF NOT EXISTS Usuarios ( Usuario varchar, Contraseña varchar)"; // SQL para crear la tabla
            SqliteCommand command = new SqliteCommand(sql, cnx); // Crea un comando SQL

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
            CrearBaseDeUsuarios(); // Asegura que la tabla exista antes de consultar
            DataTable dt = new DataTable(); // Crea una nueva tabla de datos para almacenar resultados

            string sql = "SELECT * FROM Usuarios WHERE Usuario = '" + Usuario + "'"; // SQL para buscar el usuario
            SqliteCommand command = new SqliteCommand(sql, cnx); // Crea un comando SQL
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
            string sql = "INSERT INTO Usuarios VALUES ('" + Usuario + "', '" + Contraseña + "')"; // SQL para insertar un nuevo usuario
            SqliteCommand command = new SqliteCommand(sql, cnx); // Crea un comando SQL
            command.ExecuteNonQuery(); // Ejecuta el comando (inserta el usuario)
        }

        public int ComprovarSiElUsuarioiContraseñaExiste(string Usuario, string Contraseña) // Verifica si un usuario y contraseña coinciden en la base de datos
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM Usuarios WHERE Usuario = '" + Usuario + "' AND Contraseña = '" + Contraseña + "'";
            SqliteCommand command = new SqliteCommand(sql, cnx);
            var reader = command.ExecuteReader();
            dt.Load(reader);

            if (dt.Rows.Count > 0)
            {
                return 1;
            }

            return 0;
        }

        public void EliminarTabla() // Método para eliminar la tabla de usuarios
        {
            string sql = "DROP TABLE Usuarios"; // SQL para eliminar la tabla
            SqliteCommand command = new SqliteCommand(sql, cnx);
            command.ExecuteNonQuery(); // Ejecuta el comando (elimina la tabla)
        }
    }
}