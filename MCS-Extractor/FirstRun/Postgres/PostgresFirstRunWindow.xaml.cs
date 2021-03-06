using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using MCSDataImport;
using MCS_Extractor;

namespace MCS_Extractor.FirstRun.Postgres
{
    /// <summary>
    /// Interaction logic for FirstRunWindow.xaml
    /// </summary>
    public partial class PostgresFirstRunWindow : Window
    {
        public PostgresFirstRunWindow()
        {
            InitializeComponent();
        }

        private async void CreateDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            CreateDatabaseButton.IsEnabled = false;
            var username = PGUsername.Text;
            var password = PGPassword.Text;
            await Task.Run(() =>
            {

                var config = new ConfigurationChanged();
                config.SetDatabaseCredentials(username, password);
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
                        MessageBox.Show("There was a problem configuring the database, please check that the database is enabled and your credentials are correct.");
                        CreateDatabaseButton.IsEnabled = true;
                    });
                }
            });
        }

        private void CheckForDataDirectory()
        {
            var datadir = System.IO.Path.Combine(CSVFileHandler.GetInstallFolder(), StorageSettings.GetInstance().StoragePath);
            if ( !Directory.Exists(datadir))
            {
                Directory.CreateDirectory(datadir);
            }
        }
    }
}
