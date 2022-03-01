using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Npgsql;
using NpgsqlTypes;
using CsvHelper;

namespace MCS_Extractor.ImportedData
{
    public class CSVImporter
    {

        protected CSVSummary lastSummary = new CSVSummary();

        public List<string> report = new List<string>();

        public bool ImportMapping(string fileName)
        {

            var summary = SummariseCSV(fileName);
            var result = false;
            if (!summary.Empty)
            {
                var factory = new CSVMappingFactory();
                var loader = factory.Loader;
                var table = loader.FindTableByHeaders(summary.Headers);

                if (!String.IsNullOrEmpty(table))
                {
                    factory.GetCSVImporter(ref report).ImportToTable(fileName, table);
                    result = true;
                }
            }
            return result;
        }

        public CSVSummary SummariseCSV(string fileName)
        {
            var result = this.lastSummary;
            if (fileName != this.lastSummary.FileName)
            {
                if ( 0 < result.Headers.Count )
                {
                    result = new CSVSummary();
                }

                result.FileName = fileName;
             //   Path.Combine(CSVFileHandler.GetInstallFolder(), ConfigurationManager.AppSettings["DataDirectory"]);
                using (var reader = new StreamReader(fileName))
                {
                    try
                    {
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            csv.Read();
                            csv.ReadHeader();
                            result.Headers.AddRange(csv.HeaderRecord);
                            int count = 0;
                            while (csv.Read() && count < 50)
                            {
                                var ls = new List<string>();
                                foreach (string head in csv.HeaderRecord)
                                {
                                    ls.Add(csv.GetField(head));

                                }
                                result.Values.Add(ls);
                            }
                            result.Empty = false;
                        }
                    } catch ( Exception ef )
                    {
                        Debug.WriteLine("Exception loading " + fileName + ": " + ef.Message);
                        Debug.Write(ef.StackTrace);
                        result.Empty = true;
                    }
                }

                this.lastSummary = result;
            }
            return result;
        }

 

    

        
    
    }
}
