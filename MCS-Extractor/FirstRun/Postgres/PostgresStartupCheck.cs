using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Diagnostics;
using MCS_Extractor.FirstRun.Interfaces;

namespace MCS_Extractor.FirstRun.Postgres
{
    public class PostgresStartupCheck : IStartupCheck
    {
        public bool FirstRun { get; private set; }

        public PostgresStartupCheck()
        {
            var conn = new NpgsqlConnection(ConfigurationManager.AppSettings["ConnectionString"]);
            var query = String.Format("SELECT count(datname) = 1 FROM pg_catalog.pg_database WHERE lower(datname) = lower('{0}')", ConfigurationManager.AppSettings["DatabaseName"]);
            var command = new NpgsqlCommand(query, conn);
            var exists = false;
            try
            {
                conn.Open();
                exists = (bool)command.ExecuteScalar();
            }
            catch ( Exception ef )
            {
                Debug.WriteLine("Exception checking start-up: " + ef.Message);
                Debug.WriteLine("Query: " + query);
                Debug.Write(ef.StackTrace);
            }
            FirstRun = !exists;

        }
    }
}
