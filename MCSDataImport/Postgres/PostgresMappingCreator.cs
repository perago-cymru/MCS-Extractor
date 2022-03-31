using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;
using MCSDataImport.Interfaces;

namespace MCSDataImport.Postgres
{
    class PostgresMappingCreator: MappingCreator
    {
        private List<DataMappingType<NpgsqlDbType>> mappings = new List<DataMappingType<NpgsqlDbType>>();

        private Dictionary<DBType, NpgsqlDbType> dbTypes = new Dictionary<DBType, NpgsqlDbType>
        {
            {DBType.Boolean, NpgsqlDbType.Boolean },
            {DBType.String, NpgsqlDbType.Varchar },
            {DBType.Date, NpgsqlDbType.Date },
            {DBType.Int, NpgsqlDbType.Integer },
            {DBType.Long, NpgsqlDbType.Bigint },
            {DBType.Text, NpgsqlDbType.Text },
            {DBType.Double, NpgsqlDbType.Double },
            {DBType.Numeric, NpgsqlDbType.Numeric }
        };

        public override void AddMapping(string csvName, DBType type)
        {
            string dbName = ClearSpacing(csvName);

            PostgresDataMappingType existent = (PostgresDataMappingType) mappings.Find(x => x.CSVFieldName == csvName || x.DatabaseFieldName == dbName);

            if (existent != null)
            {
                throw new Exception("A second field named " + csvName + " is being added!");
            }
            var newMapping = new PostgresDataMappingType(csvName, dbName, dbTypes[type]);
            mappings.Add(newMapping);

        }

        public override void SaveMappings(string tableName)
        {
            var loader = new PostgresMappingLoader();
            if (summary.TableName != tableName)
            {
                throw new Exception("Cannot save mappings without a table summary");
            }
            loader.SaveMappings(summary, mappings.ToList<IDataMappingType>());
        }

    }
}
