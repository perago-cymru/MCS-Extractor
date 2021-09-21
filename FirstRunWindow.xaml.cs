﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private void CreateDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var username = PGUsername.Text;
            var password = PGPassword.Text;
            var config = new ConfigurationChanged();
            config.SetDatabaseCredentials(username, password);
            var dataCreator = new DatabaseCreation(ConfigurationManager.AppSettings["ConnectionString"]);
            var result = dataCreator.RunSQLFileFromPath(CSVFileHandler.GetInstallFolder() + "\\sql\\database.sql");
            if (0 < dataCreator.Log.Count)
            {
                foreach (var l in dataCreator.Log)
                {
                    Debug.WriteLine(l);
                }
            }
            if (result)
            {
                OdbcCreation.CreateODBC(String.Format("{0};Database={1}", ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["DatabaseName"]));
            }
            if ( result )
            {
                var mw = new MainWindow();
                mw.Show();
                this.Hide();
            }
        }
    }
}