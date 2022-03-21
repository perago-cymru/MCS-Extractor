using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCS_Extractor.FirstRun.Interfaces;

namespace MCS_Extractor.FirstRun.Microsoft
{
    class MsStartupCheck : IStartupCheck
    {
        public bool FirstRun { get; private set; }
        
        public MsStartupCheck()
        {
            var conn = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]);
            var command = new SqlCommand("Select count(id) FROM csv_table_mappings;", conn);
                var exists = false;
            try
            {
                conn.Open();
                exists = 0 <= (int)command.ExecuteScalar();
            }
            catch (Exception ef)
            {
                Debug.WriteLine("Exception checking start-up: " + ef.Message);
                Debug.WriteLine("Query: " + command.CommandText);
                Debug.Write(ef.StackTrace);
            }
            FirstRun = !exists;

        }
    }
}
