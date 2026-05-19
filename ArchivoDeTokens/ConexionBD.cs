using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ArchivoDeTokens
{
    internal class ConexionBD
    {
        private static string cadenaConexion = ConfigurationManager.ConnectionStrings["ConexionBD"].ConnectionString;

        public static SqlConnection ObtenerConexion()
        {
            SqlConnection conexion = new SqlConnection(cadenaConexion);
            return conexion;
        }
    }
}
