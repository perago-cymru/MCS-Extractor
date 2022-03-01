using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Npgsql;

namespace MCS_Extractor.ImportedData
{
    class CSVFileHandler
    {


        public List<string> GetFileList()
        {
            string path = Path.Combine(GetInstallFolder(), ConfigurationManager.AppSettings["DataDirectory"]);
            List<string> unprocessed = new List<string>();
            var mapFactory = new CSVMappingFactory();
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
