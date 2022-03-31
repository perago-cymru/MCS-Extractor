using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSDataImport.Interfaces
{
    public interface ICSVImporter
    {
        void ImportToTable(string filename, string tableName);

        bool HasBeenRead(string filename);
    }
}
