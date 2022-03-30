using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private DatabasePlatform databasePlatform;

        private string connectionString;

        private string storagePath;

        private string databaseName;

        private static StorageSettings settings;

        public StorageSettings()
        {
            databaseName = Properties.MCSDataImport.Default.DatabaseName;
            connectionString = Properties.MCSDataImport.Default.ConnectionString;
            storagePath = Properties.MCSDataImport.Default.StoragePath;
            switch (Properties.MCSDataImport.Default.DatabasePlatform)
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
                    Properties.MCSDataImport.Default.DatabasePlatform = platformString;
                    Properties.MCSDataImport.Default.Save();
                }
            }           
        }

        public string DatabaseName {
            get => databaseName;
            set {
                if (value != databaseName)
                {
                    databaseName = value;
                    Properties.MCSDataImport.Default.DatabaseName = databaseName;
                    Properties.MCSDataImport.Default.Save();
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
                    Properties.MCSDataImport.Default.ConnectionString = connectionString;
                    Properties.MCSDataImport.Default.Save();
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
                    Properties.MCSDataImport.Default.StoragePath = storagePath;
                    Properties.MCSDataImport.Default.Save();
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
        

    }
}
