using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Npgsql;

namespace MCSDataImport
{
    public class CSVFileHandler
    {
        private string DataDirectory;

        private DatabasePlatform Platform;

        private string ConnectionString;

        private string DatabaseName;

        public CSVFileHandler(string dataDirectory, DatabasePlatform dbPlatform, string connectionString, string databaseName =  null)
        {
            //ConfigurationManager.AppSettings["DataDirectory"]
            this.DataDirectory = dataDirectory;
            this.Platform = dbPlatform;
            this.ConnectionString = connectionString;
            this.DatabaseName = databaseName;
        }

        public List<string> GetFileList()
        {
            string path = Path.Combine(GetInstallFolder(), this.DataDirectory);
            List<string> unprocessed = new List<string>();
            var mapFactory = new CSVMappingFactory(Platform, ConnectionString, DatabaseName);
            var reports = new List<string>();
            var importer = mapFactory.GetCSVImporter(ref reports);
            foreach (string file in Directory.EnumerateFiles(path))
            {
                if (!importer.HasBeenRead(file))
                {
                    unprocessed.Add(file);
                }
            }
            return unprocessed;
        }

        public static string GetInstallFolder()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        }

   
    }
}
