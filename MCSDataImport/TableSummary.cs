using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSDataImport
{
    public class TableSummary
    {

        public int Id { get; set; }

        public string TableName { get; set; }

        public string StartField { get; set; }

        public string CloseField { get; set; }

        public string IdField { get; set; }

        public string[] UserIdentifierFields { get; set; }

        public string UserIdentifier
        {
            get
            {
                return String.Join("_", UserIdentifierFields.OrderBy(o => o).ToArray());
            }
        }
    }
}
