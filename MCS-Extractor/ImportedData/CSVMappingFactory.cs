using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCS_Extractor.ImportedData.Interfaces;
using MCS_Extractor.ImportedData.Postgres;

namespace MCS_Extractor.ImportedData
{
    public class CSVMappingFactory
    {
        public IMappingLoader Loader { get; private set; }

        public MappingCreator Creator { get; private set; }

        private string platform;

        public CSVMappingFactory()
        {
            platform = ConfigurationManager.AppSettings["DatabasePlatform"].ToLower();
            switch (platform)
            {
                case "postgres":

                    Loader = new PostgresMappingLoader();
                    Creator = new PostgresMappingCreator();
                    break;
                default:
                    throw new Exception(String.Format("Could not find startup check for platform {0}", platform));
                    break;
            }
        }

        public ICSVImporter GetCSVImporter(ref List<string> reporter)
        {
            switch ( platform )
            {
                case "postgres": return new PostgresCSVImporter(ref reporter);
                    break;
                default:
                    throw new Exception(String.Format("Could not find csv importer for platform {0}", platform));
                    break;
            }
        }

        public ITableHandler GetTableHandler(string tableName)
        {
            switch (platform)
            {
                case "postgres":
                    return new PostgresTableHandler(tableName);
                    break;
                default:
                    throw new Exception(String.Format("Could not find a table handler for platform {0}", platform));
                    break;
            }
        }

    }
}
