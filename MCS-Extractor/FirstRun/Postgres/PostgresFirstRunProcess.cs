using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MCSDataImport;
using MCS_Extractor.FirstRun.Interfaces;

namespace MCS_Extractor.FirstRun.Postgres
{
    public class PostgresFirstRunProcess : IFirstRunProcess
    {

        public bool FirstRun()
        {
            var settings = StorageSettings.GetInstance();
            var dataCreator = new PostgresDatabaseCreation(settings.ConnectionString);
            var result = dataCreator.RunSQLFileFromPath(CSVFileHandler.GetInstallFolder() + "\\sql\\postgres\\database.sql");
            if (0 < dataCreator.Log.Count)
            {
                foreach (var l in dataCreator.Log)
                {
                    Debug.WriteLine(l);
                }
            }
            if (result)
            {
                result = PostgresOdbcCreation.CreateODBC(String.Format("{0};Database={1}", settings.ConnectionString, settings.DatabaseName));
            }
            return result;
        }

    }
}
