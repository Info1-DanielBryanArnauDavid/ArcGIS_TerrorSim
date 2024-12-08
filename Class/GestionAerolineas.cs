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
using System.IO.Packaging;
namespace Class // Define el espacio de nombres de la clase
{
    public class GestionAerolineas // Clase que gestiona usuarios en una base de datos SQLite
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

        public int CrearBaseAerolineas() // Método para crear la tabla de aerolíneas si no existe
        {
            string sql = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Aerolineas' AND xtype='U') " +
                      "CREATE TABLE Aerolineas ( Nombre varchar(100), Telefono varchar(100), Email varchar(100), Identificador varchar(100))"; // SQL para crear la tabla si no existe
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
        public string[] GetContactInfo(string aerolinea)
        {
            DataTable dt = new DataTable();
            // Define the SQL query with a parameter placeholder
            string sql = "SELECT Telefono, Email FROM Aerolineas WHERE Nombre = @Nombre";
            SqlCommand command = new SqlCommand(sql, cnx);
            command.Parameters.AddWithValue("@Nombre", aerolinea); // Parametrizamos para evitar inyección SQL
            var reader = command.ExecuteReader();
            dt.Load(reader);
            if (dt.Rows.Count == 1)
            {
                string Telefono = Convert.ToString(dt.Rows[0]["Telefono"]);
                string Email = Convert.ToString(dt.Rows[0]["Email"]);
                string[] Values = { Telefono, Email };
                return Values;
            }
            else
            {
                return ["No Data","No Data"];
            }
        }
        public List<List<string>> GetDataBaseValues()
        {
            DataTable dt = new DataTable();
            string sql = "SELECT * FROM Aerolineas";
            SqlCommand command = new SqlCommand(sql,cnx);
            var reader = command.ExecuteReader();
            dt.Load(reader);
            List<List<string>> datos = new List<List<string>>();
            if(dt.Rows.Count >= 1)
            {
                List<string> datosLinea = new List<string>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {                    
                    datosLinea.Add(Convert.ToString(dt.Rows[i]["Nombre"]));
                    datosLinea.Add(Convert.ToString(dt.Rows[i]["Telefono"]));
                    datosLinea.Add(Convert.ToString(dt.Rows[i]["Email"]));
                    datosLinea.Add(Convert.ToString(dt.Rows[i]["Identificador"]));
                    datos.Add(datosLinea);
                }
                return datos;
            }
            else
            {
                return null;
            }
        }
        public int AddAerolinea(string nombre, string numero, string email, string id)
        {
            string sql = "INSERT INTO Aerolineas VALUES ('"+nombre+ "','"+numero+ "','"+email+ "','"+id+"')";
            SqlCommand command = new SqlCommand(sql, cnx);

            // Adding parameters to avoid SQL injection
            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@Telefono", numero);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Identificador", id);

            try
            {
                command.ExecuteNonQuery(); // Executes the command, inserting the new record
                return 1; // Return 1 if the insertion is successful
            }
            catch (Exception)
            {
                return 0; // Return 0 if there was an error during insertion
            }

        }
        public int DeleteAerolinea(string nombre)
        {
            string sql = "DELETE FROM Aerolineas WHERE Nombre='" + nombre + "'";
            SqlCommand cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@Nombre", nombre);

            try
            {
                // Execute the command to delete the record
                int rowsAffected = cmd.ExecuteNonQuery();

                // Check if any rows were affected (deleted)
                if (rowsAffected > 0)
                {
                    return 1; // Return 1 if at least one record was deleted
                }
                else
                {
                    return 0; // Return 0 if no records were deleted (e.g., if the airline doesn't exist)
                }
            }
            catch (Exception)
            {
                return 0; // Return 0 in case of an error (e.g., if there's a connection issue)
            }
        }
    }
    
}
