using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MCS_Extractor.FirstRun;
using MCS_Extractor.ImportedData;

namespace MCS_Extractor.FirstRun.Microsoft
{
    /// <summary>
    /// The first run window for Microsoft SQL Server deployments.
    /// </summary>
    public partial class MicrosoftFirstRunWindow : Window
    {
        public MicrosoftFirstRunWindow()
        {
            InitializeComponent();
        }

        private async void ConnectDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectDatabaseButton.IsEnabled = false;
            var connectionString = ConnectionString.Text;
            if (0 < connectionString.Trim().Length)
            {
                await Task.Run(() =>
                {

                    var config = new ConfigurationChanged();
                    config.SetConnectionString(connectionString);
                    var start = new FirstRunHandler();
                    var result = !start.IsFirstRun;
                    if (!result)
                    {

                        CheckForDataDirectory();
                        result = start.Run();

                    }
                    if (result)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            var mw = new MainWindow();
                            mw.Show();
                            this.Hide();
                        });
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("There was a problem connecting to the database, please check that the database is enabled and your credentials are correct.");
                            ConnectDatabaseButton.IsEnabled = true;
                        });
                    }
                });
            } else
            {
                ConnectDatabaseButton.IsEnabled = true;
            }

        }

        private void CheckForDataDirectory()
        {
            var datadir = System.IO.Path.Combine(CSVFileHandler.GetInstallFolder(), ConfigurationManager.AppSettings["DataDirectory"]);
            if (!Directory.Exists(datadir))
            {
                Directory.CreateDirectory(datadir);
            }
        }
    }


}
