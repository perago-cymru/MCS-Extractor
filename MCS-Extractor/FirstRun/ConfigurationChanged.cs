using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCS_Extractor.FirstRun
{
    public class ConfigurationChanged
    {
        public ConfigurationChanged()
        {
           
        }

        public void SetDatabaseCredentials(string username, string password)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
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
            UpdateAppSettings("ConnectionString", connectionString);
        }

        public void SetConnectionString(string newConnect)
        {
            UpdateAppSettings("ConnectionString", newConnect);
        }

        public void SetDatabasePlatform(string newPlatform)
        {
            var viablePlatforms = new string[] { "mssql", "postgres" };
            if ( !viablePlatforms.Contains(newPlatform) )
            {
                throw new Exception(String.Format("\"{0}\" is not a valid database platform.", newPlatform));
            }
            UpdateAppSettings("DatabasePlatform", newPlatform);
        }

        private void UpdateAppSettings(string theKey, string theValue)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (ConfigurationManager.AppSettings.AllKeys.Contains(theKey))
            {
                configuration.AppSettings.Settings[theKey].Value = theValue;
            }

            configuration.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection("appSettings");
        }

    }
}
