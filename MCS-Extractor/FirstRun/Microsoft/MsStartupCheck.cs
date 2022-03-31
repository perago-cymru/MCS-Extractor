using System;
using System.Collections.Generic;

using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCSDataImport;
using MCS_Extractor.FirstRun.Interfaces;

namespace MCS_Extractor.FirstRun.Microsoft
{
    class MsStartupCheck : IStartupCheck
    {
        public bool FirstRun { get; private set; }
        
        public MsStartupCheck()
        {
            var exists = false;
            try
            {
                var conn = new SqlConnection(StorageSettings.GetInstance().ConnectionString);
                var command = new SqlCommand("Select count(id) FROM csv_table_mappings;", conn);
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
            }
            catch (Exception ef )
            {
                FirstRun = false;
            }
           
            FirstRun = !exists;

        }
    }
}
