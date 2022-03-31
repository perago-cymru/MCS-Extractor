using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSDataImport.Interfaces
{
    public interface IDataMappingType
    {

        string CSVFieldName { get; set; }

        string DatabaseFieldName { get; set; }

        string TypeName();

        string GetFieldTypeName();

    }
}
