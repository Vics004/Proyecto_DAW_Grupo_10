using Microsoft.Data.SqlClient;

namespace Proyecto_DAW_Grupo_10.Servicios
{
    public class correo
    {
        private IConfiguration _configuration;

        // Con esta variable podemos acceder a las 
        // configuraciones del archivo appsetting.json
        public correo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void enviar(string destinatario, string asunto, string cuerpo)
        {
            try
            {
                string connetionString = _configuration.GetSection("ConnectionStrings").GetSection("TicketsDbConnection").Value;
                string sqlQuery = "exec msdb.dbo.sp_send_dbmail " +
                                    "@profile_name = 'SQLMail_AYUDIN', " +
                                    "@recipients = @par_destinatarios, " +
                                    "@subject = @par_asunto, " +
                                    "@body = @par_mensaje ";

                using (SqlConnection connection = new SqlConnection(connetionString))
                {
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        command.Parameters.AddWithValue("@par_destinatarios", destinatario);
                        command.Parameters.AddWithValue("@par_asunto", asunto);
                        command.Parameters.AddWithValue("@par_mensaje", cuerpo);

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
