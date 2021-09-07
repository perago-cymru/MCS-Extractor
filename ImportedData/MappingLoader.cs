﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace MCS_Extractor.ImportedData
{
    public class MappingLoader
    {

        private const string mappingTable = "csv_table_mappings";

        private const string fileTable = "loaded_files";

        private NpgsqlConnection connection;

        private Dictionary<String, Func<string, string, DataMappingType>> typeLookup = new Dictionary<string, Func<string, string, DataMappingType>> {
            {  NpgsqlDbType.Boolean.ToString(), (a, b) => new DataMapping<bool>(a, b, NpgsqlDbType.Boolean) },
            {  NpgsqlDbType.Varchar.ToString(), (a, b) => new DataMapping<string>(a, b, NpgsqlDbType.Varchar) },
            { NpgsqlDbType.Integer.ToString(), (a, b) => new DataMapping<int>(a, b, NpgsqlDbType.Integer) },
            {  NpgsqlDbType.Bigint.ToString(), (a, b) => new DataMapping<long>(a, b, NpgsqlDbType.Bigint) },
            {  NpgsqlDbType.Text.ToString(), (a, b) => new DataMapping<string>(a, b, NpgsqlDbType.Text) },
            { NpgsqlDbType.Double.ToString(), (a, b) => new DataMapping<decimal>(a, b, NpgsqlDbType.Numeric) },
            { NpgsqlDbType.Numeric.ToString(), (a, b) => new DataMapping<decimal>(a, b, NpgsqlDbType.Numeric) },
            { NpgsqlDbType.Date.ToString(), (a, b) => new DataMapping<DateTime>(a, b,NpgsqlDbType.Date) }
        };


        public MappingLoader()
        {
            connection = new NpgsqlConnection(ConfigurationManager.AppSettings["ConnectionString"]);
        }

        public List<DataMappingType> GetMappings(string tableName)
        {
            connection.Open();
            var cmd = new NpgsqlCommand("SELECT * FROM " + mappingTable + " WHERE table_name = @tn", connection);
            cmd.Parameters.AddWithValue("tn", tableName);
            var response = cmd.ExecuteReader();
            var result = new List<DataMappingType>();
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
            return result;
        }

        public void SaveMappings(string tableName, List<DataMappingType> mappings)
        {
            if (!TableExists(tableName))
            {
                CreateTable(tableName, mappings);
                InsertMappings(tableName, mappings);
            }

        }

        public string FindTableByHeaders(List<string> csvHeaders)
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

        public bool TableExists(string tableName)
        {
            connection.Open();
            var cmd = new NpgsqlCommand("SELECT count(id) FROM " + mappingTable + " WHERE table_name = @tb", connection);
            cmd.Parameters.AddWithValue("tb", tableName);
            int result = Convert.ToInt32( cmd.ExecuteScalar());
            connection.Close();
            return 0 < result;
        }

        private void CreateTable(string tableName, List<DataMappingType> mappings)
        {
            var table = new StringBuilder("CREATE TABLE ");
            table.AppendFormat("{0} ( ", tableName);
            table.AppendLine("id SERIAL PRIMARY KEY");
            foreach ( var map in mappings )
            {
                table.AppendFormat(", {0} {1}", map.DatabaseFieldName, map.GetFieldTypeName());
            
            }
            table.Append(");");
            connection.Open();
            var cmd = new NpgsqlCommand(table.ToString(), connection);
            cmd.ExecuteNonQuery();
            connection.Close();
        }
  
        private void InsertMappings(string tableName, List<DataMappingType> mappings )
        {
            connection.Open();
            var cmd = new NpgsqlCommand("INSERT INTO " + mappingTable + " (table_name, csv_name, db_name, type_name) VALUES ( @tn, @csv, @db, @tp )", connection);
            foreach( DataMappingType mp in mappings )
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("tn", tableName);
                cmd.Parameters.AddWithValue("csv", mp.CSVFieldName);
                cmd.Parameters.AddWithValue("db", mp.DatabaseFieldName);
                cmd.Parameters.AddWithValue("tp", mp.TypeName());
                cmd.ExecuteNonQuery();

            }

            connection.Close();
        }



    }
}
