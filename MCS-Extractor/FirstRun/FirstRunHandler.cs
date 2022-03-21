using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public string Platform { get; private set; }

        public FirstRunHandler()
        {
            Platform = ConfigurationManager.AppSettings["DatabasePlatform"].ToLower();
            switch (Platform)
            {
                case "postgres": startup = new PostgresStartupCheck();
                    break;
                case "mssql": startup = new MsStartupCheck();
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
                    case "postgres": first = new PostgresFirstRunProcess();
                        break;
                    case "mssql": first = new MsFirstRunProcess();
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
