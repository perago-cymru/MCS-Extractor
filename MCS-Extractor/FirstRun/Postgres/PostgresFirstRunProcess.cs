using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using MCS_Extractor.ImportedData;
using MCS_Extractor.FirstRun.Interfaces;

namespace MCS_Extractor.FirstRun.Postgres
{
    public class PostgresFirstRunProcess : IFirstRunProcess
    {

        public bool FirstRun()
        {
            var dataCreator = new PostgresDatabaseCreation(ConfigurationManager.AppSettings["ConnectionString"]);
            var result = dataCreator.RunSQLFileFromPath(CSVFileHandler.GetInstallFolder() + "\\sql\\database.sql");
            if (0 < dataCreator.Log.Count)
            {
                foreach (var l in dataCreator.Log)
                {
                    Debug.WriteLine(l);
                }
            }
            if (result)
            {
                result = PostgresOdbcCreation.CreateODBC(String.Format("{0};Database={1}", ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["DatabaseName"]));
            }
            return result;
        }

    }
}
