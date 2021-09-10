using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace MCS_Extractor.ImportedData
{
    public class TableSummary
    {

        public int Id { get; set; }

        public string TableName { get; set; }

        public string StartField { get; set; }

        public string CloseField { get; set; }

        public string[] UserIdentifierFields { get; set; }

        public string UserIdentifier {
            get
            {
                return String.Join("_", UserIdentifierFields);
            }
        }

        public static TableSummary LoadFromDatabase(NpgsqlConnection connection, string tableName, bool openConnection = true )
        {
            var command = new NpgsqlCommand("SELECT * FROM csv_index_fields WHERE table_name= @tb", connection);
            command.Parameters.AddWithValue("tb", tableName);
            if (openConnection)
            {
                connection.Open();
            }
            try
            {
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    var result = new TableSummary()
                    {
                        Id = (int)reader["id"],
                        TableName = tableName,
                        StartField = (string)reader["start_field"],
                        CloseField = (string)reader["close_field"],
                        UserIdentifierFields = ((string)reader["unique_identifier"]).Split('_')
                    };

                    return result;
                }
                else
                {
                    throw new Exception("Could not find a summary for " + tableName);
                }
            }
            finally {
                if (openConnection)
                {
                    connection.Close();
                }
            }
           
        }
    }
}
