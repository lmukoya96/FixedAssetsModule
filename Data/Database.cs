using Microsoft.Data.SqlClient;

namespace TestModule.Data
{
    public class Database
    {
        private readonly string _server;
        private readonly string _database;
        public string connectionString;

        public Database(string server, string database)
        {
            _server = server;
            _database = database;

            connectionString = $"Server={_server};Database={_database};Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";
        }

        public static Database DB_Connection()
        {
            const string server = "PC-L4M3CK\\MYMSSQLSERVER";
            const string database = "TestModule";

            return new Database(server, database);
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
