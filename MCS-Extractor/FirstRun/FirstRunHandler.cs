using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCS_Extractor.FirstRun.Interfaces;
using MCS_Extractor.FirstRun.Postgres;

namespace MCS_Extractor.FirstRun
{
    /// <summary>
    /// Class to handle checking for a first run and responding appropriately if there is one.
    /// </summary>
    class FirstRunHandler
    {
        private IStartupCheck startup;

        private string platform;

        public FirstRunHandler()
        {
            platform = ConfigurationManager.AppSettings["DatabasePlatform"].ToLower();
            switch (platform)
            {
                case "postgres": startup = new PostgresStartupCheck();
                    break;
                default:
                    throw new Exception(String.Format("Could not find startup check for platform {0}", platform));
                    break;
            }
        }

        public bool IsFirstRun {
            get {
                bool isFirst = false;
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
                switch (platform)
                {
                    case "postgres": first = new PostgresFirstRunProcess();
                        break;
                    default: throw new Exception(String.Format("Could not find first run process for platform {0}", platform));
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
