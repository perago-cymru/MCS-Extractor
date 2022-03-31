using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;
using MCSDataImport.Interfaces;

namespace MCSDataImport
{
    /// <summary>
    /// A Type to summarise a DataMapping type definition.
    /// </summary>
    /// <typeparam name="T">The type of the database field type.</typeparam>
    public abstract class DataMappingType<T> : IDataMappingType
    {
        public DataMappingType(string csvField, string dbField, T type)
        {
            this.CSVFieldName = csvField;
            this.DatabaseFieldName = dbField;
            this.DatabaseType = type;
        }


        public string CSVFieldName { get; set; }

        public string DatabaseFieldName { get; set; }

        public virtual string TypeName() {
            return this.DatabaseType.ToString();
        }

        public T DatabaseType { get; set; }

        public abstract string GetFieldTypeName();

    }
}
