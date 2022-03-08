using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCS_Extractor.ImportedData.Interfaces
{
    public interface IMappingLoader
    {
        List<IDataMappingType> GetMappings(string tableName);

        void SaveMappings(TableSummary summary, List<IDataMappingType> mappings);

        string FindTableByHeaders(List<string> csvHeaders);

        bool TableExists(string tableName);

    }
}
