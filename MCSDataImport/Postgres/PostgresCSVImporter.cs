using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Npgsql;
using NpgsqlTypes;
using CsvHelper;
using MCSDataImport.Interfaces;

namespace MCSDataImport.Postgres
{
    public class PostgresCSVImporter : ICSVImporter
    {
        public List<string> Reporter { get; set; }

        private static string ConnectionString;

        private static string DatabaseName;

        public PostgresCSVImporter(ref List<string> reporter, string connectionString, string dbName)
        {
            //ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["DatabaseName"]);
            this.Reporter = reporter;
            SetConnectionProperties(connectionString, DatabaseName);
        }

        public void ImportToTable(string filename, string tableName)
        {
            if (!HasBeenRead(filename))
            {
                var loader = new PostgresMappingLoader();
                var mappingSet = loader.GetMappings(tableName).Cast<PostgresDataMappingType>().ToList();

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
                            try
                            {
                                using (var importer = conn.BeginBinaryImport(GetPgSQLCopyStatement(tableName, mappingSet)))
                                {
                                    while (csv.Read())
                                    {
                                        importer.StartRow();
                                        foreach (var map in mappingSet)
                                        {
                                            string field = csv.GetField(map.CSVFieldName);
                                            ImportItem(importer, map, field);
                                        }

                                    }
                                    importer.Complete();
                                }
                                var command = new NpgsqlCommand("INSERT INTO loaded_files ( loaded, filename ) VALUES ( @loaded, @filename )", conn);
                                command.Parameters.AddWithValue("loaded", DateTime.UtcNow);
                                command.Parameters.AddWithValue("filename", filename);
                                command.ExecuteNonQuery();

                            }
                            catch (PostgresException ex)
                            {
                                Debug.WriteLine("Exception: " + ex.Message);
                                Debug.Write("Location: " + ex.Where);
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

        public bool HasBeenRead(string filename)
        {
            var conn = GetConnection();
            var command = new NpgsqlCommand("SELECT count(id) FROM loaded_files WHERE filename = @filename", conn);
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

        private Dictionary<string, PostgresDataMappingType> GetMappingDictionary(string tableName)
        {
            var loader = new PostgresMappingLoader();
            var mappingSet = loader.GetMappings(tableName).Cast<PostgresDataMappingType>();
            return mappingSet.ToDictionary(x => x.CSVFieldName);
        }

        private string GetPgSQLCopyStatement(string tableName, List<PostgresDataMappingType> mappings)
        {
            StringBuilder b = new StringBuilder("COPY ");
            b.Append(tableName);
            b.Append(" (");
            var first = true;
            foreach (var mpt in mappings)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    b.Append(", ");
                }
                b.Append(mpt.DatabaseFieldName);

            }
            b.Append(") FROM STDIN (FORMAT BINARY) ");
            return b.ToString();
        }

        private void SetUniqueField(string tableName, NpgsqlConnection connection)
        {
            var summary = LoadTableSummary(connection, tableName);
            if (1 < summary.UserIdentifierFields.Length)
            {
                var query = new StringBuilder(String.Format("UPDATE {0} areq SET {1} =  ", tableName, summary.UserIdentifier));
                var first = true;
                foreach (string field in summary.UserIdentifierFields)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        query.Append(" || '-' || ");
                    }
                    query.AppendFormat("b.{0}", field);
                }
                query.AppendFormat(" FROM {0} b WHERE areq.id = b.id AND areq.{1} IS NULL", tableName, summary.UserIdentifier);
                var command = new NpgsqlCommand(query.ToString(), connection);
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        private NpgsqlBinaryImporter ImportItem(NpgsqlBinaryImporter import, PostgresDataMappingType m, string fieldvalue)
        {
            if (String.IsNullOrEmpty(fieldvalue))
            {
                import.WriteNull();
            }
            else
            {
                switch (m.DatabaseType)
                {
                    case NpgsqlDbType.Boolean:
                        var mb = m as PostgresDataMapping<bool>;
                        import.Write(mb.Map(fieldvalue), m.DatabaseType);
                        break;
                    case NpgsqlDbType.Integer:
                        var mi = m as PostgresDataMapping<int>;
                        import.Write(mi.Map(fieldvalue), m.DatabaseType);
                        break;
                    case NpgsqlDbType.Bigint:
                        var ml = m as PostgresDataMapping<long>;
                        import.Write(ml.Map(fieldvalue), m.DatabaseType);
                        break;
                    case NpgsqlDbType.Double:
                        var md = m as PostgresDataMapping<double>;
                        import.Write(md.Map(fieldvalue), m.DatabaseType);
                        break;
                    case NpgsqlDbType.Numeric:
                        var mn = m as PostgresDataMapping<decimal>;
                        import.Write(mn.Map(fieldvalue), m.DatabaseType);
                        break;
                    case NpgsqlDbType.Date:
                        var mdt = m as PostgresDataMapping<DateTime>;
                        import.Write(mdt.Map(fieldvalue), m.DatabaseType);
                        break;
                    default:
                        import.Write(fieldvalue, m.DatabaseType);
                        break;
                }
            }
            return import;
        }

        public static void SetConnectionProperties(string connectionString, string dbName)
        {
            ConnectionString = connectionString;
            DatabaseName = dbName;
        }

        public static NpgsqlConnection GetConnection()
        {
            if ( String.IsNullOrEmpty(ConnectionString) || String.IsNullOrEmpty(DatabaseName))
            {
                throw new Exception("Cannot create connection with unset ConnectionString or DatabaseName");
            }
            var connectionString = String.Format("{0}Database={1}", ConnectionString, DatabaseName);
            return new NpgsqlConnection(connectionString);
        }

        public static TableSummary LoadTableSummary(NpgsqlConnection connection, string tableName, bool openConnection = true)
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
    }
}
