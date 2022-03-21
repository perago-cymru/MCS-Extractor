using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCS_Extractor.ImportedData.Interfaces;

namespace MCS_Extractor.ImportedData.Microsoft
{
    public class MicrosoftMappingCreator : MappingCreator
    {
        private List<MicrosoftDataMappingType> mappings = new List<MicrosoftDataMappingType>();

        public override void AddMapping(string csvName, DBType type)
        {
            string dbName = ClearSpacing(csvName);

            MicrosoftDataMappingType existent = mappings.Find(x => x.CSVFieldName == csvName || x.DatabaseFieldName == dbName);

            if (existent != null)
            {
                throw new Exception("A second field named " + csvName + " is being added!");
            }
            var newMapping = new MicrosoftDataMappingType(csvName, dbName, type);
            mappings.Add(newMapping);

        }

        public override void SaveMappings(string tableName)
        {
            var loader = new MicrosoftMappingLoader();
            if (summary.TableName != tableName)
            {
                throw new Exception("Cannot save mappings without a table summary");
            }
            loader.SaveMappings(summary, mappings.ToList<IDataMappingType>());
        }
    }
}
