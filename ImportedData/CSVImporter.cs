using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Npgsql;
using CsvHelper;

namespace MCS_Extractor.ImportedData
{
    public class CSVImporter
    {

        private CSVSummary lastSummary = new CSVSummary();

        public bool ImportMapping(string fileName)
        {
    
            var summary = SummariseCSV(fileName);
            var loader = new MappingLoader();
            var table = loader.FindTableByHeaders(summary.Headers);
            var result = false;
            if ( table != null )
            {
                // load table
            } else
            {
                // generate Mappings.
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
                    }
                }

                this.lastSummary = result;
            }
            return result;
        }
  
    }
}
