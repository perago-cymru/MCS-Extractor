using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCS_Extractor.ImportedData
{
    public class DataMappingType
    {
        public DataMappingType(string csvField, string dbField, PGType type)
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

        public PGType PostgresType { get; set; }

    }
}
