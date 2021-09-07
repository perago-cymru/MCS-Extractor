using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace MCS_Extractor.ImportedData
{
    public class DataMapping<T> : DataMappingType
    {
        public DataMapping(string csvField, string dbField, PGType type) : base(csvField, dbField, type)
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
