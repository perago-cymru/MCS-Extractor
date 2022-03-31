using System;
using System.Collections.Generic;

using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MCSDataImport;
using MCS_Extractor.FirstRun;
using MCS_Extractor.FirstRun.Postgres;
using MCS_Extractor.FirstRun.Microsoft;

namespace MCS_Extractor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            FirstRunHandler first = new FirstRunHandler();
            if (first.IsFirstRun)
            {
                switch (first.Platform) {
                        case DatabasePlatform.Postgres: 
                        var firstRun = new PostgresFirstRunWindow();
                    //firstRun.Owner = this;
                    firstRun.Show();
                            break;
                    case DatabasePlatform.MSSQL:
                        var msRun = new MicrosoftFirstRunWindow();
                        msRun.Show();
                        break;
                    default: var chooseRun = new SelectDatabaseWindow();
                        chooseRun.Show();
                        break;
                }
            } else
            {
                var main = new MainWindow();
                main.Show();
            }
        }
    }
}
