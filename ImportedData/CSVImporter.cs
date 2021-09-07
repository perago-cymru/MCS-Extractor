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

        public bool ImportMapping(string fileName)
        {
    
            var summary = SummariseCSV(fileName);
            var loader = new MappingLoader();
            var table = loader.FindTableByHeaders(summary.Headers);
            var result = false;
            if ( !String.IsNullOrEmpty(table))
            {
                ImportToTable(fileName, table);
            } else
            {
                // generate Mappings.
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
                    }
                }

                this.lastSummary = result;
            }
            return result;
        }

        private void ImportToTable(string filename, string tableName)
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
                        var conn = new NpgsqlConnection(ConfigurationManager.AppSettings["ConnectionString"]);
                        conn.Open();
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
                        conn.Close();
                    
                    } else
                    {
                        Debug.WriteLine("Cannot read from " + filename);
                    }
                }
            }

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
    
        private NpgsqlBinaryImporter ImportItem(NpgsqlBinaryImporter import, DataMappingType m, string fieldvalue)
        {
            if (fieldvalue == null)
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
