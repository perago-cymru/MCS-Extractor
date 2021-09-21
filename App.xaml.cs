using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MCS_Extractor.FirstRun;

namespace MCS_Extractor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            StartupCheck check = new StartupCheck();
            if (check.FirstRun)
            {
                var firstRun = new FirstRunWindow();
                //firstRun.Owner = this;
                firstRun.Show();
            } else
            {
                var main = new MainWindow();
                main.Show();
            }
        }
    }
}
