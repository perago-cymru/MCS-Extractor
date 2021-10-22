using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Configuration;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MCS_Extractor.FirstRun;
using MCS_Extractor.ImportedData;

namespace MCS_Extractor
{
    /// <summary>
    /// Interaction logic for FirstRunWindow.xaml
    /// </summary>
    public partial class FirstRunWindow : Window
    {
        public FirstRunWindow()
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
                var start = new StartupCheck();
                var result = !start.FirstRun;
                if (!result)
                {

                    var dataCreator = new DatabaseCreation(ConfigurationManager.AppSettings["ConnectionString"]);
                    result = dataCreator.RunSQLFileFromPath(CSVFileHandler.GetInstallFolder() + "\\sql\\database.sql");
                    if (0 < dataCreator.Log.Count)
                    {
                        foreach (var l in dataCreator.Log)
                        {
                            Debug.WriteLine(l);
                        }
                    }
                    if (result)
                    {
                        result = OdbcCreation.CreateODBC(String.Format("{0};Database={1}", ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["DatabaseName"]));
                    }
                    CheckForDataDirectory();
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
                        MessageBox.Show("There was a problem configuring the database, please check that PostgreSQL is enabled and your credentials are correct.");
                        CreateDatabaseButton.IsEnabled = true;
                    });
                }
            });
        }

        private void CheckForDataDirectory()
        {
            var datadir = System.IO.Path.Combine(CSVFileHandler.GetInstallFolder(), ConfigurationManager.AppSettings["DataDirectory"]);
            if ( !Directory.Exists(datadir))
            {
                Directory.CreateDirectory(datadir);
            }
        }
    }
}
