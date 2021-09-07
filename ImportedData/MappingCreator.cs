using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace MCS_Extractor.ImportedData
{

    public class MappingCreator
    {

        private List<DataMappingType> mappings = new List<DataMappingType>();

        private Dictionary<PGType, NpgsqlDbType> dbTypes = new Dictionary<PGType, NpgsqlDbType>
        {
            {PGType.Boolean, NpgsqlDbType.Boolean },
            {PGType.String, NpgsqlDbType.Varchar },
            {PGType.Date, NpgsqlDbType.Date },
            {PGType.Int, NpgsqlDbType.Integer },
            {PGType.Long, NpgsqlDbType.Bigint },
            {PGType.Text, NpgsqlDbType.Text },
            {PGType.Double, NpgsqlDbType.Double },
            {PGType.Numeric, NpgsqlDbType.Numeric }
        };

        public void AddMapping( string csvName, PGType type )
        {
            string dbName = ClearSpacing(csvName);

            DataMappingType existent = mappings.Find(x => x.CSVFieldName == csvName || x.DatabaseFieldName == dbName);

            if ( existent != null )
            {
                throw new Exception("A second field named " + csvName + " is being added!");
            }
            DataMappingType newMapping = new DataMappingType(csvName, dbName, dbTypes[type]);
            mappings.Add(newMapping);

        }

        public void SaveMappings(string tableName)
        {
            var loader = new MappingLoader();
            loader.SaveMappings(tableName, mappings);
        }

        public static string ClearSpacing(string name, string replacement = "_")
        {
            name =name.ToLower();
            name = Regex.Replace(name, @"\s+", " ");
            name = Regex.Replace(name, " ", replacement);
            return name;
        }

        


    }

}
