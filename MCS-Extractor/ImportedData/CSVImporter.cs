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

namespace MCS_Extractor.ImportedData
{
    public class CSVImporter
    {

        private CSVSummary lastSummary = new CSVSummary();

        public List<string> report = new List<string>();

        public bool ImportMapping(string fileName)
        {

            var summary = SummariseCSV(fileName);
            var result = false;
            if (!summary.Empty)
            {
                var loader = new MappingLoader();
                var table = loader.FindTableByHeaders(summary.Headers);

                if (!String.IsNullOrEmpty(table))
                {
                    ImportToTable(fileName, table);
                    result = true;
                }
            }
            return result;
        }

        public CSVSummary SummariseCSV(string fileName)
        {
            var result = this.lastSummary;
            if (fileName != this.lastSummary.FileName)
            {
                if ( 0 < result.Headers.Count )
                {
                    result = new CSVSummary();
                }

                result.FileName = fileName;
             //   Path.Combine(CSVFileHandler.GetInstallFolder(), ConfigurationManager.AppSettings["DataDirectory"]);
                using (var reader = new StreamReader(fileName))
                {
                    try
                    {
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            csv.Read();
                            csv.ReadHeader();
                            result.Headers.AddRange(csv.HeaderRecord);
                            int count = 0;
                            while (csv.Read() && count < 50)
                            {
                                var ls = new List<string>();
                                foreach (string head in csv.HeaderRecord)
                                {
                                    ls.Add(csv.GetField(head));

                                }
                                result.Values.Add(ls);
                            }
                            result.Empty = false;
                        }
                    } catch ( Exception ef )
                    {
                        Debug.WriteLine("Exception loading " + fileName + ": " + ef.Message);
                        Debug.Write(ef.StackTrace);
                        result.Empty = true;
                    }
                }

                this.lastSummary = result;
            }
            return result;
        }

        public static NpgsqlConnection GetConnection()
        {
            var connectionString = String.Format("{0}Database={1}", ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["DatabaseName"]);
            return new NpgsqlConnection(connectionString);
        }

        private void ImportToTable(string filename, string tableName)
        {
            if (!HasBeenRead(filename))
            {
                var loader = new MappingLoader();
                var mappingSet = loader.GetMappings(tableName);

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

        private bool HasBeenRead(string filename)
        {
            var conn = GetConnection();
            var command = new NpgsqlCommand("SELECT count(id) FROM loaded_files WHERE filename = @filename", conn);
            command.Parameters.AddWithValue("filename", filename);
            conn.Open();
            bool result = 0 < Convert.ToInt32(command.ExecuteScalar());
            conn.Close();
            if ( result )
            {
                report.Add(String.Format("File {0} has been imported previously.", filename));
            }
            return result;

        }

        private Dictionary<string, DataMappingType> GetMappingDictionary(string tableName)
        {
            var loader = new MappingLoader();
            var mappingSet = loader.GetMappings(tableName);
            return mappingSet.ToDictionary(x => x.CSVFieldName);
        }

        private string GetPgSQLCopyStatement(string tableName, List<DataMappingType> mappings)
        {
            StringBuilder b = new StringBuilder("COPY ");
            b.Append(tableName);
            b.Append(" (");
            var first = true;
            foreach ( var mpt in mappings )
            {
                if ( first)
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
            var summary = TableSummary.LoadFromDatabase(connection, tableName);
            if ( 1 < summary.UserIdentifierFields.Length )
            {
                var query = new StringBuilder(String.Format("UPDATE {0} areq SET {1} =  ", tableName, summary.UserIdentifier));
                var first = true;
                foreach ( string field in summary.UserIdentifierFields )
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
    
        private NpgsqlBinaryImporter ImportItem(NpgsqlBinaryImporter import, DataMappingType m, string fieldvalue)
        {
            if (String.IsNullOrEmpty(fieldvalue))
            { 
                import.WriteNull();
            }
            else
            {
                switch (m.PostgresType)
                {
                    case NpgsqlDbType.Boolean:
                        var mb = m as DataMapping<bool>;
                        import.Write(mb.Map(fieldvalue), m.PostgresType);
                        break;
                    case NpgsqlDbType.Integer:
                        var mi = m as DataMapping<int>;
                        import.Write(mi.Map(fieldvalue), m.PostgresType);
                        break;
                    case NpgsqlDbType.Bigint:
                        var ml = m as DataMapping<long>;
                        import.Write(ml.Map(fieldvalue), m.PostgresType);
                        break;
                    case NpgsqlDbType.Double:
                        var md = m as DataMapping<double>;
                        import.Write(md.Map(fieldvalue), m.PostgresType);
                        break;
                    case NpgsqlDbType.Numeric:
                        var mn = m as DataMapping<decimal>;
                        import.Write(mn.Map(fieldvalue), m.PostgresType);
                        break;
                    case NpgsqlDbType.Date:
                        var mdt = m as DataMapping<DateTime>;
                        import.Write(mdt.Map(fieldvalue), m.PostgresType);
                        break;
                    default:
                        import.Write(fieldvalue, m.PostgresType);
                        break;
                }
            }
            return import;
        }

        
    
    }
}
