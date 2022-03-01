using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCS_Extractor.ImportedData.Interfaces
{
    public interface ITableHandler
    {
        bool TableExists();

        bool CreateTable(string tableTitle, List<IDataMappingType> mappings);
    }
}
