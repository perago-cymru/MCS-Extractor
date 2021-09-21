using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace MCS_Extractor.ImportedData
{
    public class TableHandler
    {

        private string tableName;

        private NpgsqlConnection connection;

        public TableHandler(string tableName)
        {
            this.tableName = tableName;
            connection = CSVImporter.GetConnection();
        }

        public bool TableExists()
        {
            var cmd = new NpgsqlCommand(String.Format("SELECT EXISTS( SELECT FROM information_schema.tables WHERE  table_name = '{0}'); ", tableName), connection);
            bool result = (bool)cmd.ExecuteScalar();
            return result;
        }

        public bool CreateTable(string tableTitle, List<DataMappingType> mappings)
        {
            if ( mappings.Count == 0 )
            {
                throw new Exception("Attempt to create a table from an empty mapping list");
            }
            var result = false;
            var statement = new StringBuilder();
            statement.AppendFormat("CREATE TABLE IF NOT EXISTS `public.{0}` (");
            statement.AppendLine("id serial,");
            statement.AppendLine("created_at timestamp NOT NULL DEFAULT now()");
            foreach ( DataMappingType tp in mappings )
            {
                statement = AppendMapping(statement, tp);
            }
            statement.AppendLine(", ");
            statement.AppendLine("PRIMARY KEY (id)");
            statement.AppendLine(");");
            try
            {
                Debug.WriteLine("Create table: " + statement.ToString());
                var cmd = new NpgsqlCommand(statement.ToString(), connection);
                cmd.ExecuteNonQuery();

                cmd = new NpgsqlCommand("INSERT INTO table_metadata (user_facing_title, table_name) VALUES ( @title, @tname )", connection);
                cmd.Parameters.AddWithValue("@title", tableTitle);
                cmd.Parameters.AddWithValue("@tname", tableName);
                cmd.ExecuteNonQuery();
                result = true;
            } catch ( Exception e )
            {
                Debug.WriteLine("Failed to create table: " + e.Message);
                Debug.Write(e.StackTrace);
            }

            return result;
        }

        private StringBuilder AppendMapping(StringBuilder create, DataMappingType mapping)
        {
            create.AppendLine(", ");
            create.Append(mapping.DatabaseFieldName);
            switch (mapping.TypeName())
            {
                case "boolean":
                    create.Append(" boolean");
                    break;

                case "string":
                    create.Append(" character varying(255) ");
                    break;

                case "int":
                    create.Append(" integer");
                    break;
                case "long":
                    create.Append(" bigint");
                    break;
                case "text":
                    create.Append(" text");
                    break;
                case "double":
                    create.Append(" double precision");
                    break;
                case "numeric":
                    create.Append(" numeric");
                    break;
                case "date":
                    create.Append(" timestamp");
                    break;
                default: throw new Exception("Unknown typename: " + mapping.TypeName());
            }
            return create;
        }
    }
}
