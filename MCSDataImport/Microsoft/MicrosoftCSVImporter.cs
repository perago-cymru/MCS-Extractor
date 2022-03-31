using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using MCSDataImport.Interfaces;

namespace MCSDataImport.Microsoft
{
    public class MicrosoftCSVImporter : ICSVImporter
    {
        public List<string> Reporter { get; set; }

        public static string ConnectionString { get; set; }

        public MicrosoftCSVImporter(ref List<string> reporter, string connectionString)
        {
            this.Reporter = reporter;
            ConnectionString = connectionString;
        }

        public bool HasBeenRead(string filename)
        {
            var conn = GetConnection();
            var command = new SqlCommand("SELECT count(id) FROM loaded_files WHERE filename = @filename", conn);
            command.Parameters.AddWithValue("filename", filename);
            conn.Open();
            bool result = 0 < Convert.ToInt32(command.ExecuteScalar());
            conn.Close();
            if (result)
            {
                Reporter.Add(String.Format("File {0} has been imported previously.", filename));
            }
            return result;

        }

        public void ImportToTable(string filename, string tableName)
        {
            if (!HasBeenRead(filename))
            {
                var loader = new MicrosoftMappingLoader();
                var mappingSet = loader.GetMappings(tableName).Cast<MicrosoftDataMappingType>().ToList();

                using (var reader = new StreamReader(filename))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        if (csv.Read())
                        {
                            csv.ReadHeader();
                            var header = csv.HeaderRecord;
                            var conn = GetConnection();
                            conn.Open();
                            var tran = conn.BeginTransaction();
                            var command = new SqlCommand(GetInsertQuery(tableName, mappingSet), conn, tran);
                            int rowNumber = 0;
                            try
                            {
                                while (csv.Read())
                                {

                                    foreach (var map in mappingSet)
                                    {
                                        string field = csv.GetField(map.CSVFieldName);
                                        if (rowNumber == 0)
                                        {
                                            command.Parameters.AddWithValue(map.DatabaseFieldName, map.Map(field));
                                        }
                                        else
                                        {
                                            command.Parameters[map.DatabaseFieldName].Value = map.Map(field);
                                        }
                                    }
                                    command.ExecuteNonQuery();
                                    rowNumber++;
                                }

                                command.CommandText = "INSERT INTO loaded_files ( loaded, filename ) VALUES ( @loaded, @filename )";
                                command.Parameters.AddWithValue("loaded", DateTime.UtcNow);
                                command.Parameters.AddWithValue("filename", filename);
                                command.ExecuteNonQuery();
                                tran.Commit();
                            }
                            catch (SqlException ex)
                            {
                                Debug.WriteLine("Exception: " + ex.Message);
                                Debug.Write("Query: " + command.CommandText);
                                tran.Rollback();
                                if (ex.Number == 8152) // truncation exception.
                                {
                                    foreach (SqlParameter param in command.Parameters)
                                    {
                                        if (param.DbType == System.Data.DbType.String)
                                        {
                                            if (255 < param.Value.ToString().Length)
                                            {
                                                Debug.WriteLine("The problem field is: " + param.ParameterName);
                                                throw new Exception("Field " + param.ParameterName + " in row "+rowNumber+" (first field '" + command.Parameters[0].Value.ToString() + "') would be truncated.", ex);
                                            }

                                        }
                                    }
                                }

                                    throw ex;
                                
                            }
                            finally
                            {
                                conn.Close();
                            }
                            SetUniqueField(tableName, conn);

                        }
                        else
                        {
                            Debug.WriteLine("Cannot read from " + filename);
                        }

                    }
                }

            }
        }

 

        public static SqlConnection GetConnection()
        {
            // var connectionString = ConfigurationManag[er.AppSettings["ConnectionString"];
            if (String.IsNullOrEmpty(ConnectionString))
            {
                ConnectionString = StorageSettings.GetInstance().ConnectionString;
            }
            if (String.IsNullOrEmpty(ConnectionString))
            {
                throw new Exception("Attempt to create connection without having established a connectionstring");
            }
            return new SqlConnection(ConnectionString);
        }

        public static TableSummary LoadTableSummary(SqlConnection connection, string tableName, bool openConnection = true)
        {
            var command = new SqlCommand("SELECT * FROM csv_index_fields WHERE table_name= @tb", connection);
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
                        IdField = (string)reader["index_field"],
                        UserIdentifierFields = ((string)reader["unique_identifier"]).Split('_')
                    };

                    return result;
                }
                else
                {
                    throw new Exception("Could not find a summary for " + tableName);
                }
            }
            finally
            {
                if (openConnection)
                {
                    connection.Close();
                }
            }

        }

        private string GetInsertQuery(string tableName, List<MicrosoftDataMappingType>mappings) 
    {
            StringBuilder build = new StringBuilder("INSERT INTO ");
            build.Append(tableName);
            build.Append(" (");
            StringBuilder vals = new StringBuilder();
            bool first = true;
            foreach ( MicrosoftDataMappingType mp in mappings )
            {
                if (!first ) {
                    build.Append(", ");
                    vals.Append(", ");
                } else {
                    first = false;
                }

                build.Append(mp.DatabaseFieldName);
                vals.AppendFormat("@{0}", mp.DatabaseFieldName);
            }
            build.AppendFormat(") VALUES ( {0} )", vals.ToString());
            return build.ToString();
    }

        private void SetUniqueField(string tableName, SqlConnection connection)
        {
            var summary = LoadTableSummary(connection, tableName);
            if (1 < summary.UserIdentifierFields.Length)
            {
                var query = new StringBuilder(String.Format("UPDATE {0} SET {1} =  ", tableName, summary.UserIdentifier));
                var first = true;
                foreach (string field in summary.UserIdentifierFields)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        query.Append(" + '-' + ");
                    }
                    query.AppendFormat("b.{0}", field);
                }
                query.AppendFormat(" FROM {0} b WHERE id = b.id AND {1} IS NULL", tableName, summary.UserIdentifier);
                var command = new SqlCommand(query.ToString(), connection);
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}
