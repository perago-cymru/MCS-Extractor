using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCSDataImport
{
    public enum DatabasePlatform
    {
        unset,
        MSSQL,
        Postgres
    }

    public class StorageSettings
    {
        private const string configFolderName = "MCS Extractor";
        private const string configFileName = "StorageSettings.config";

        private DatabasePlatform databasePlatform;

        private string connectionString;

        private string storagePath;

        private string databaseName;

        private static StorageSettings settings;

        private Configuration config;


        public StorageSettings()
        {
            config = LoadConfigFile();
            databaseName = config.AppSettings.Settings["DatabaseName"].Value;
            connectionString = config.AppSettings.Settings["ConnectionString"].Value;
            storagePath = config.AppSettings.Settings["StoragePath"].Value;
            switch (config.AppSettings.Settings["DatabasePlatform"].Value)
            {
                case "mssql": databasePlatform = DatabasePlatform.MSSQL;
                    break;
                case "postgres": databasePlatform = DatabasePlatform.Postgres;
                    break;
                default: databasePlatform = DatabasePlatform.unset;
                    break;
            }
        }

        public DatabasePlatform DatabasePlatform { get => databasePlatform;
            set
            {
                if (value != databasePlatform)
                {
                    databasePlatform = value;
                    var platformString = "";
                    switch (databasePlatform)
                    {
                        case DatabasePlatform.MSSQL: platformString = "mssql";
                            break;
                        case DatabasePlatform.Postgres: platformString = "postgres";
                            break;
                    }
                    config.AppSettings.Settings["DatabasePlatform"].Value = platformString;
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
        }

        public string DatabaseName {
            get => databaseName;
            set {
                if (value != databaseName)
                {
                    databaseName = value;
                    config.AppSettings.Settings["DatabaseName"].Value = databaseName;
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
        }

        public string ConnectionString {
            get => connectionString;
            set
            {
                if (value != connectionString)
                {
                    connectionString = value;
                    config.AppSettings.Settings["ConnectionString"].Value = connectionString;
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
        }

        public string StoragePath {
            get => storagePath;
            set
            {
                if (value != storagePath)
                {
                    storagePath = value;
                    config.AppSettings.Settings["StoragePath"].Value = storagePath;
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
        }

        public static StorageSettings GetInstance()
        {
            if (settings == null)
            {
                settings = new StorageSettings();
            }
            return settings;
        }

        private Configuration LoadConfigFile()
        {
            ExeConfigurationFileMap configFilemap = new ExeConfigurationFileMap();
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + configFolderName;
            if (!Directory.Exists(folderPath))
            {
                CreateConfigFile(folderPath);
            }
            configFilemap.ExeConfigFilename = folderPath + "\\" + configFileName;
            Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(configFilemap, ConfigurationUserLevel.None);
            return cfg;
        }

        private void CreateConfigFile(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            new XDocument(
                    new XElement("configuration", new XElement("appSettings",
                        new XElement[]{
                             new XElement("add", new XAttribute( "key", "DatabasePlatform" ), new XAttribute("value", "" ) ),
                            new XElement("add", new XAttribute("key", "DatabaseName" ), new XAttribute("value", "mcs-extractor" ) ),
                            new XElement("add", new XAttribute( "key", "ConnectionString" ), new XAttribute("value", "Host=localhost;Port=5432;username=;password=;")),
                            new XElement("add", new XAttribute( "key", "StoragePath" ), new XAttribute( "value", "Downloaded" ))

                        }))
                ).Save(folderPath + "\\" + configFileName);
        }

   
        

    }
}
