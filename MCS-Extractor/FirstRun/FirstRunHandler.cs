using System;
using System.Collections.Generic;

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCSDataImport;
using MCS_Extractor.FirstRun.Interfaces;
using MCS_Extractor.FirstRun.Postgres;
using MCS_Extractor.FirstRun.Microsoft;

namespace MCS_Extractor.FirstRun
{
    /// <summary>
    /// Class to handle checking for a first run and responding appropriately if there is one.
    /// </summary>
    class FirstRunHandler
    {
        private IStartupCheck startup;

        public DatabasePlatform Platform { get; private set; }

        public FirstRunHandler()
        {
            Platform = StorageSettings.GetInstance().DatabasePlatform;
            switch (Platform)
            {
                case DatabasePlatform.Postgres: startup = new PostgresStartupCheck();
                    break;
                case DatabasePlatform.MSSQL: startup = new MsStartupCheck();
                    break;
                default:
                    Debug.WriteLine(String.Format("Could not find startup check for platform {0}", Platform));
                    break;
            }
        }

        public bool IsFirstRun {
            get {
                bool isFirst = true;
                if (startup != null)
                {
                    isFirst = startup.FirstRun;
                }
                return isFirst;
            }  
        }

        public bool Run()
        {
            IFirstRunProcess first = null;
            bool result = false;
            if ( startup != null && startup.FirstRun )
            {
                switch (Platform)
                {
                    case DatabasePlatform.Postgres: first = new PostgresFirstRunProcess();
                        break;
                    case DatabasePlatform.MSSQL: first = new MsFirstRunProcess();
                        break;
                    default: throw new Exception(String.Format("Could not find first run process for platform {0}", Platform));
                        break;
                }
                if ( first != null)
                {
                    result = first.FirstRun();
                }
            }
            return result;
        }
    }
}
