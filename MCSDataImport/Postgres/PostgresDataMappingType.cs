using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace MCSDataImport.Postgres
{
    public class PostgresDataMappingType : DataMappingType<NpgsqlDbType>
    {

        public PostgresDataMappingType(string csvField, string dbField, NpgsqlDbType type) : base(csvField, dbField, type)
        {
            
        }

        public override string GetFieldTypeName()
        {
            string result = this.TypeName();
            if (DatabaseType == NpgsqlDbType.Varchar)
            {
                result += "(255)";
            }
            else if (DatabaseType == NpgsqlDbType.Double)
            {
                result = "Numeric";
            }
            return result;
        }

    }
}
