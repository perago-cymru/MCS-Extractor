using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCS_Extractor.ImportedData.Interfaces;

namespace MCS_Extractor.ImportedData.Microsoft
{
    public class MicrosoftMappingLoader : ScriptParsingMappingLoader
    {
        private const string mappingTable = "csv_table_mappings";

        private const string fileTable = "loaded_files";

        private SqlConnection connection;

        private Dictionary<string, Func<string, string, MicrosoftDataMappingType>> typeLookup = new Dictionary<string, Func<string, string, MicrosoftDataMappingType>> {
            {  DBType.Boolean.ToString(), (a, b) => new MicrosoftDataMappingType(a, b, DBType.Boolean) },
            {  DBType.String.ToString(), (a, b) => new MicrosoftDataMappingType(a, b, DBType.String) },
            { DBType.Int.ToString(), (a, b) => new MicrosoftDataMappingType(a, b,  DBType.Int) },
            {  DBType.Text.ToString(), (a, b) => new MicrosoftDataMappingType(a, b, DBType.Text) },
            { DBType.Double.ToString(), (a, b) => new MicrosoftDataMappingType(a, b, DBType.Double) },
            { DBType.Numeric.ToString(), (a, b) => new MicrosoftDataMappingType(a, b, DBType.Numeric) },
            { DBType.Date.ToString(), (a, b) => new MicrosoftDataMappingType(a, b, DBType.Date) }
        };

        public MicrosoftMappingLoader()
        {
            connection = MicrosoftCSVImporter.GetConnection();
        }

        public override bool TableExists(string tableName)
        {
            connection.Open();
            var cmd = new SqlCommand("SELECT count(id) FROM csv_table_mappings WHERE table_name = @tb", connection);
            cmd.Parameters.AddWithValue("tb", tableName);
            int result = Convert.ToInt32(cmd.ExecuteScalar());
            connection.Close();
            return 0 < result;
        }

        public override string FindTableByHeaders(List<string> csvHeaders)
        {
            var query = new StringBuilder("SELECT matches.table_name, count(distinct matches.id) as match_rows, count(distinct equivalent.id) as total_rows ");
            query.Append(" FROM csv_table_mappings matches  LEFT JOIN csv_table_mappings equivalent on matches.table_name = equivalent.table_name ");
            query.Append("WHERE matches.csv_name IN ( @p0");

            for (int i = 1; i < csvHeaders.Count; i++)
            {
                query.AppendFormat(", @p{0}", i);
            }
            query.Append(" ) GROUP BY matches.table_name");
            connection.Open();
            var cmd = new SqlCommand(query.ToString(), connection);
            for (int i = 0; i < csvHeaders.Count; i++)
            {
                cmd.Parameters.AddWithValue(String.Format("@p{0}", i), csvHeaders[i]);
            }
            var read = cmd.ExecuteReader();
            string result = "";
            while (read.Read())
            {
                if (Convert.ToInt32(read["match_rows"]) == Convert.ToInt32(read["total_rows"]))
                {
                    result = (string)read["table_name"];
                }
            }
            connection.Close();
            return result;
        }

        public override List<IDataMappingType> GetMappings(string tableName)
        {
            connection.Open();
            var cmd = new SqlCommand("SELECT * FROM csv_table_mappings WHERE table_name = @tn", connection);
            cmd.Parameters.AddWithValue("tn", tableName);
            var response = cmd.ExecuteReader();
            var result = new List<MicrosoftDataMappingType>();
            if (response.HasRows)
            {
                int csvNameOrd = response.GetOrdinal("csv_name");
                int dbNameOrd = response.GetOrdinal("db_name");
                int typeNameOrd = response.GetOrdinal("type_name");
                while (response.Read())
                {
                    var typeName = response.GetString(typeNameOrd);
                    result.Add(typeLookup[typeName](response.GetString(csvNameOrd), response.GetString(dbNameOrd)));
                }
            }
            connection.Close();
            return result.Cast<IDataMappingType>().ToList();
        }

        public override void SaveMappings(TableSummary summary, List<IDataMappingType> mappings)
        {
            var mapList = mappings.Select(x => x as MicrosoftDataMappingType).ToList<MicrosoftDataMappingType>();
            if (!TableExists(summary.TableName))
            {

                    connection.Open();
                    var tran = connection.BeginTransaction();
                try
                {
                    CreateTable(summary, mapList, tran);
                    RunTemplateScript(summary, tran);

                    InsertMappings(summary.TableName, mapList, tran);
                    tran.Commit();
                }
                catch (Exception ef)
                {
                    Console.WriteLine("Failed to create mapping: " + ef.Message);
                    tran.Rollback();
                    throw ef;
                }
                finally
                {
                    connection.Close();
                }
            }
        }




        private void CreateTable(TableSummary summary, List<MicrosoftDataMappingType> mappings, SqlTransaction tran)
            {
                var table = new StringBuilder("CREATE TABLE ");
                table.AppendFormat("{0} ( ", summary.TableName);
                table.AppendLine("id INT NOT NULL IDENTITY(1,1) PRIMARY KEY");

                foreach (var map in mappings)
                {
                    table.AppendFormat(", {0} {1}", map.DatabaseFieldName, map.GetFieldTypeName());

                }
                if (1 < summary.UserIdentifierFields.Length)
                {
                    table.AppendFormat(", {0} VARCHAR(255)", summary.UserIdentifier);
                }
                table.Append(");");

                var cmd = new SqlCommand(table.ToString(), connection, tran);
                cmd.ExecuteNonQuery();

            }

        private void InsertMappings(string tableName, List<MicrosoftDataMappingType> mappings, SqlTransaction tran)
        {

            var cmd = new SqlCommand("INSERT INTO csv_table_mappings (table_name, csv_name, db_name, type_name) VALUES ( @tn, @csv, @db, @tp )", connection, tran);
            foreach (MicrosoftDataMappingType mp in mappings)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("tn", tableName);
                cmd.Parameters.AddWithValue("csv", mp.CSVFieldName);
                cmd.Parameters.AddWithValue("db", mp.DatabaseFieldName);
                cmd.Parameters.AddWithValue("tp", mp.TypeName());
                cmd.ExecuteNonQuery();

            }
        }

        private void SaveSummary(TableSummary summary, SqlTransaction tran)
        {
            var command = new SqlCommand("INSERT INTO csv_index_fields ( table_name, index_field, start_field, close_field, unique_identifier ) VALUES ( @tb, @idx, @start, @close, @unique )", connection, tran);

            command.Parameters.AddWithValue("tb", summary.TableName);
            command.Parameters.AddWithValue("idx", summary.IdField);
            command.Parameters.AddWithValue("start", summary.StartField);
            command.Parameters.AddWithValue("close", summary.CloseField);
            command.Parameters.AddWithValue("unique", summary.UserIdentifier);
            command.ExecuteNonQuery();


        }

        private void RunTemplateScript(TableSummary summary, SqlTransaction tran)
        {
            var statement = ParseMappingFile("\\sql\\mssql\\template.sql", summary);
            // GO is a server studio thing that CLR calls don't recognise.
            foreach (string subsection in statement.Split(new String[] { "\r\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {

                var command = new SqlCommand(subsection, connection, tran);

                command.ExecuteNonQuery();
            }
            SaveSummary(summary, tran);
            
        }



    }
}
