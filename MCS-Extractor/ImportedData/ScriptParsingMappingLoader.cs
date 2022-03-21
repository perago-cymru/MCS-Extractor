using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCS_Extractor.ImportedData.Interfaces;

namespace MCS_Extractor.ImportedData
{
    public abstract class ScriptParsingMappingLoader : IMappingLoader
    {
        public abstract string FindTableByHeaders(List<string> csvHeaders);

        public abstract List<IDataMappingType> GetMappings(string tableName);

        public abstract void SaveMappings(TableSummary summary, List<IDataMappingType> mappings);

        public abstract bool TableExists(string tableName);

        protected string ParseMappingFile(string relativePath, TableSummary summary)
        {
            var reader = new StreamReader(CSVFileHandler.GetInstallFolder() + relativePath);
            var statement = reader.ReadToEnd();
            reader.Close();

            statement = statement.Replace("{$table}", summary.TableName);
            statement = statement.Replace("{$start_date}", summary.StartField);
            statement = statement.Replace("{$end_date}", summary.CloseField);
            statement = statement.Replace("{$id}", summary.IdField);
            statement = statement.Replace("{$identifier}", summary.UserIdentifier);
            return statement;
        }
       
    }
}
