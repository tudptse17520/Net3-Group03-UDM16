using Microsoft.Data.SqlClient;

namespace CaroServer.Data
{
    public static class DbConnectionFactory
    {
        private const string ConnectionString =
            @"Server=.\SQLEXPRESS;Database=CaroDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}