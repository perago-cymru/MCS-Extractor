using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCSDataImport.Interfaces;
using MCSDataImport.Postgres;
using MCSDataImport.Microsoft;

namespace MCSDataImport
{
    public class CSVMappingFactory
    {
        public IMappingLoader Loader { get; private set; }

        public MappingCreator Creator { get; private set; }

        private DatabasePlatform Platform;

        private string ConnectionString;

        private string DatabaseName;

        public CSVMappingFactory(DatabasePlatform platform, string connectionString, string databaseName = null)
        {
            this.ConnectionString = connectionString;
            this.DatabaseName = databaseName;
            this.Platform = platform;
           // platform = ConfigurationManager.AppSettings["DatabasePlatform"].ToLower();
            switch (platform)
            {
                case DatabasePlatform.Postgres:

                    Loader = new PostgresMappingLoader();
                    Creator = new PostgresMappingCreator();
                    break;
                case DatabasePlatform.MSSQL:
                    Loader = new MicrosoftMappingLoader();
                    Creator = new MicrosoftMappingCreator();
                    break;
                default:
                    throw new Exception(String.Format("Could not find startup check for platform {0}", platform));
                    break;
            }
        }

        public ICSVImporter GetCSVImporter(ref List<string> reporter)
        {
            switch ( Platform )
            {
                case DatabasePlatform.Postgres:
                    return new PostgresCSVImporter(ref reporter, ConnectionString, DatabaseName);
                    break;
                case DatabasePlatform.MSSQL:
                    return new MicrosoftCSVImporter(ref reporter, ConnectionString);
                    break;
                default:
                    throw new Exception(String.Format("Could not find csv importer for platform {0}", Platform));
                    break;
            }
        }


        



    }
}
