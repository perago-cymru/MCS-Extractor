using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using MCSDataImport.Interfaces;

namespace MCSDataImport.Postgres
{
    public class PostgresMappingLoader : ScriptParsingMappingLoader
    {

        private const string mappingTable = "csv_table_mappings";

        private const string fileTable = "loaded_files";

        private NpgsqlConnection connection;

        private Dictionary<String, Func<string, string, PostgresDataMappingType>> typeLookup = new Dictionary<string, Func<string, string, PostgresDataMappingType>> {
            {  NpgsqlDbType.Boolean.ToString(), (a, b) => new PostgresDataMapping<bool>(a, b, NpgsqlDbType.Boolean) },
            {  NpgsqlDbType.Varchar.ToString(), (a, b) => new PostgresDataMapping<string>(a, b, NpgsqlDbType.Varchar) },
            { NpgsqlDbType.Integer.ToString(), (a, b) => new PostgresDataMapping<int>(a, b, NpgsqlDbType.Integer) },
            {  NpgsqlDbType.Bigint.ToString(), (a, b) => new PostgresDataMapping<long>(a, b, NpgsqlDbType.Bigint) },
            {  NpgsqlDbType.Text.ToString(), (a, b) => new PostgresDataMapping<string>(a, b, NpgsqlDbType.Text) },
            { NpgsqlDbType.Double.ToString(), (a, b) => new PostgresDataMapping<decimal>(a, b, NpgsqlDbType.Numeric) },
            { NpgsqlDbType.Numeric.ToString(), (a, b) => new PostgresDataMapping<decimal>(a, b, NpgsqlDbType.Numeric) },
            { NpgsqlDbType.Date.ToString(), (a, b) => new PostgresDataMapping<DateTime>(a, b,NpgsqlDbType.Date) }
        };


        public PostgresMappingLoader()
        {
            connection = PostgresCSVImporter.GetConnection();
        }

        public override List<IDataMappingType> GetMappings(string tableName)
        {
            connection.Open();
            var cmd = new NpgsqlCommand("SELECT * FROM " + mappingTable + " WHERE table_name = @tn", connection);
            cmd.Parameters.AddWithValue("tn", tableName);
            var response = cmd.ExecuteReader();
            var result = new List<PostgresDataMappingType>();
            if (response.HasRows )
            {
                int csvNameOrd = response.GetOrdinal("csv_name");
                int dbNameOrd = response.GetOrdinal("db_name");
                int typeNameOrd = response.GetOrdinal("type_name");
                while ( response.Read() )
                {
                    result.Add(typeLookup[response.GetString(typeNameOrd)](response.GetString(csvNameOrd), response.GetString(dbNameOrd)));
                }
            }
            connection.Close();
            return result.Cast<IDataMappingType>().ToList();
        }

        public override void SaveMappings(TableSummary summary, List<IDataMappingType> mappings)
        {
            var mapList = mappings.Select(x => x as PostgresDataMappingType).ToList<PostgresDataMappingType>();
            if (!TableExists(summary.TableName))
            {
                connection.Open();
                var tran = connection.BeginTransaction();
                CreateTable(summary, mapList, false);
                RunTemplateScript(summary, false);
                InsertMappings(summary.TableName, mapList, false);
                tran.Commit();
                connection.Close();
            }

        }

        public override string FindTableByHeaders(List<string> csvHeaders)
        {

            var query = new StringBuilder("SELECT matches.table_name, count(distinct matches.id) as match_rows, count(distinct equivalent.id) as total_rows ");
            query.Append(" FROM csv_table_mappings matches  LEFT JOIN csv_table_mappings equivalent on matches.table_name = equivalent.table_name ");
            query.Append("WHERE matches.csv_name IN ( @p0");
          //  query.Append(csvHeaders[0]);
          //  ( @p0");
        for ( int i = 1; i< csvHeaders.Count; i++)
        {
            query.AppendFormat(", @p{0}", i);
        } 
          /*  for (int i = 1; i < csvHeaders.Count; i++)
            {
                query.Append("', '");
                query.Append( csvHeaders[i]);
            }*/
            query.Append(" ) GROUP BY matches.table_name");
            connection.Open();
            var cmd = new NpgsqlCommand(query.ToString(), connection);
            for (int i = 0; i < csvHeaders.Count; i++)
            {
                cmd.Parameters.AddWithValue(String.Format("@p{0}", i), csvHeaders[i]);
            }
            var read = cmd.ExecuteReader();
            string result = "";
            while (read.Read())
            {
                if ( Convert.ToInt32(read["match_rows"]) == Convert.ToInt32(read["total_rows"]))
                {
                    result = (string) read["table_name"];
                }
            }
            connection.Close();
            return result; 
        }

        public override bool TableExists(string tableName)
        {
            connection.Open();
            var cmd = new NpgsqlCommand("SELECT count(id) FROM " + mappingTable + " WHERE table_name = @tb", connection);
            cmd.Parameters.AddWithValue("tb", tableName);
            int result = Convert.ToInt32( cmd.ExecuteScalar());
            connection.Close();
            return 0 < result;
        }

        private void CreateTable(TableSummary summary, List<PostgresDataMappingType> mappings, bool openConnection = true)
        {
            var table = new StringBuilder("CREATE TABLE ");
            table.AppendFormat("{0} ( ", summary.TableName);
            table.AppendLine("id SERIAL PRIMARY KEY");
         
            foreach ( var map in mappings )
            {
                table.AppendFormat(", {0} {1}", map.DatabaseFieldName, map.GetFieldTypeName());
            
            }
            if (  1 < summary.UserIdentifierFields.Length )
            {
                table.AppendFormat(", {0} VARCHAR(255)", summary.UserIdentifier);
            }
            table.Append(");");
            if (openConnection)
            {

                connection.Open();
            }
            var cmd = new NpgsqlCommand(table.ToString(), connection);
            cmd.ExecuteNonQuery();
            if (openConnection)
            {
                connection.Close();
            }
        }

        private void RunTemplateScript(TableSummary summary, bool openConnection = true)
        {
            var statement = ParseMappingFile("\\sql\\postgres\\template.sql", summary);
            var command = new NpgsqlCommand(statement, connection);
            if (openConnection)
            {
                connection.Open();
            }
            command.ExecuteNonQuery();
            if (openConnection)
            {
                connection.Close();
            }
            SaveSummary(summary, openConnection);
        }

        private void SaveSummary(TableSummary summary, bool openConnection = true)
        {
            var command = new NpgsqlCommand("INSERT INTO csv_index_fields ( table_name, index_field, start_field, close_field, unique_identifier ) VALUES ( @tb, @idx, @start, @close, @unique )", connection);
            if (openConnection)
            {
                connection.Open();
            }
            command.Parameters.AddWithValue("tb", summary.TableName);
            command.Parameters.AddWithValue("idx", summary.IdField);
            command.Parameters.AddWithValue("start", summary.StartField);
            command.Parameters.AddWithValue("close", summary.CloseField);
            command.Parameters.AddWithValue("unique", summary.UserIdentifier);
            command.ExecuteNonQuery();
            if (openConnection)
            {
                connection.Close();
            }

        }
  
        private void InsertMappings(string tableName, List<PostgresDataMappingType> mappings, bool openConnection = true)
        {
            if (openConnection)
            {
                connection.Open();
            }
            var cmd = new NpgsqlCommand("INSERT INTO " + mappingTable + " (table_name, csv_name, db_name, type_name) VALUES ( @tn, @csv, @db, @tp )", connection);
            foreach(PostgresDataMappingType mp in mappings )
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("tn", tableName);
                cmd.Parameters.AddWithValue("csv", mp.CSVFieldName);
                cmd.Parameters.AddWithValue("db", mp.DatabaseFieldName);
                cmd.Parameters.AddWithValue("tp", mp.TypeName());
                cmd.ExecuteNonQuery();

            }
            if (openConnection)
            {
                connection.Close();
            }
        }



    }
}
