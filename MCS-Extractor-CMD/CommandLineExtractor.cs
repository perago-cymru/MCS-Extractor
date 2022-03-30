using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCSDataImport;

namespace MCS_Extractor_CMD
{
    class CommandLineExtractor
    {
        static void Main(string[] args)
        {
            Console.WriteLine("MCS Extractor command line tool.");
            var conf = StorageSettings.GetInstance();
            if ( conf.DatabasePlatform == DatabasePlatform.unset )
            {
                Console.WriteLine("Could not read a configuration platform, have you run the MCS Extractor?");
                Environment.Exit(1);
            }
            Console.WriteLine("Platform: {0}", conf.DatabasePlatform);
            Console.WriteLine("ConnectionString: {0}", conf.ConnectionString);

            var importer = new CSVImporter(conf.DatabasePlatform, conf.ConnectionString, conf.DatabaseName);
            var csvHandler = new CSVFileHandler(conf.StoragePath, conf.DatabasePlatform, conf.ConnectionString, conf.DatabaseName);
            var files = csvHandler.GetFileList();

            if (0 < files.Count)
            {
                for (int fn = 0; fn < files.Count; fn++)
                {
                    if (importer.ImportMapping(files[fn]))
                    {

                        Console.Write("Imported: "+files[fn]);
                        
                    }
                    else
                    {
                        Console.WriteLine("Could not import " + files[fn] + " as no mapping is recognised, please run MCS Extractor");

                    }
                }
            }
            else
            {

                Console.WriteLine( "No new files to import." );


            }
            Environment.Exit(0);
        }
    }
}
