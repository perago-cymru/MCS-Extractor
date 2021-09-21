using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace MCS_Extractor.FirstRun
{
    class DatabaseCreation
    {
        private string connectionString;

        private NpgsqlConnection connection;

        public List<string> Log { get; set; }

        public DatabaseCreation(string connectionString)
        {
            this.connectionString = connectionString;
            Log = new List<string>();
        }

        public bool RunSQLFileFromPath(string path)
        {
            var result = false;
            Log.Add("Create database from " + path);
            var dbName = ConfigurationManager.AppSettings["DatabaseName"];
            var create = String.Format("CREATE DATABASE {0} WITH OWNER = postgres ENCODING = 'UTF8';", dbName);

            var reader = new StreamReader(path);

            var statement = reader.ReadToEnd();
            reader.Close();
            
            try
            {
                var connection = new NpgsqlConnection(connectionString);
                var command = new NpgsqlCommand(create, connection);
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                connection = new NpgsqlConnection(connectionString + "database="+dbName);
                command = new NpgsqlCommand(statement, connection);
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                result = true;
            }
            catch (Exception e)
            {
                Log.Add("SQL Exception: " + e.Message);
                Log.Add("Statement: " + statement);
                Log.Add(e.StackTrace);
            }
            return result;
        }

    }
}
