using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCSDataImport;

namespace MCS_Extractor.FirstRun
{
    public class ConfigurationChanged
    {
        private StorageSettings settings;

        public ConfigurationChanged()
        {
             settings = StorageSettings.GetInstance();
        }

        public void SetDatabaseCredentials(string username, string password)
        {
            
            //  Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var connectionString = settings.ConnectionString;// ConfigurationManager.AppSettings["ConnectionString"];
            var components = connectionString.Split(';');
            for ( int i =0; i<components.Length; i++)
            {
                if ( components[i].StartsWith("username"))
                {
                    components[i] = String.Format("username={0}", username);
                }
                if (components[i].StartsWith("password"))
                {
                    components[i] = String.Format("password={0}", password);
                }
            }
            connectionString = String.Join(";", components);
            settings.ConnectionString= connectionString;
        }

        public void SetConnectionString(string newConnectionString)
        {
            settings.ConnectionString = newConnectionString;
        }

        public void SetDatabasePlatform(string newPlatform)
        {
            var platform = DatabasePlatform.unset;
            switch (newPlatform)
            {
                case "mssql": platform = DatabasePlatform.MSSQL;
                    break;
                case "postgres": platform = DatabasePlatform.Postgres;
                    break;
                default: throw new Exception(String.Format("\"{0}\" is not a valid database platform.", newPlatform));
            }
 
            settings.DatabasePlatform= platform;
        }


    }
}
