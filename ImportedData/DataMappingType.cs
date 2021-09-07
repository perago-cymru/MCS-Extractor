using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace MCS_Extractor.ImportedData
{
    public class DataMappingType
    {
        public DataMappingType(string csvField, string dbField, NpgsqlDbType type)
        {
            this.CSVFieldName = csvField;
            this.DatabaseFieldName = dbField;
            this.PostgresType = type;
        }


        public string CSVFieldName { get; set; }

        public string DatabaseFieldName { get; set; }

        public virtual string TypeName() {
            return this.PostgresType.ToString();
        }

        public NpgsqlDbType PostgresType { get; set; }

        public string GetFieldTypeName()
        {
            string result = this.TypeName();
            if ( PostgresType == NpgsqlDbType.Varchar )
            {
                result += "(255)";
            } else if ( PostgresType == NpgsqlDbType.Double)
            {
                result = "Numeric";
            }
            return result;
        }

    }
}
