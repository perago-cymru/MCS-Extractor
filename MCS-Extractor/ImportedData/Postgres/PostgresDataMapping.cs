using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace MCS_Extractor.ImportedData.Postgres
{
    public class PostgresDataMapping<T> : PostgresDataMappingType
    {
        public PostgresDataMapping(string csvField, string dbField, NpgsqlDbType type) : base(csvField, dbField, type)
        {
        }

        public T Value { get; set; }

        public T Map( string val )
        {
            return (T)Convert.ChangeType(val, typeof(T));
        }  

        public override string TypeName()
        {
            return typeof(T).Name;
        }

       



    }
}
