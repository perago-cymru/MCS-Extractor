using System;
using System.Collections.Generic;
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
using MCS_Extractor.FirstRun.Microsoft;
using MCS_Extractor.FirstRun.Postgres;

namespace MCS_Extractor
{
    /// <summary>
    /// Interaction logic for SelectDatabase.xaml
    /// </summary>
    public partial class SelectDatabaseWindow : Window
    {
        public SelectDatabaseWindow()
        {
            InitializeComponent();
        }

        private void SetDatabase_Click(object sender, RoutedEventArgs e)
        {
            var selected = (ComboBoxItem)DatabasePlatform.SelectedItem;
            if ( selected != null )
            {
                var platformName = selected.Name;
                if (0 < platformName.Length)
                {
                    var config = new ConfigurationChanged();
                    config.SetDatabasePlatform(platformName);
                    switch (platformName)
                    {
                        case "postgres":
                            var pgRun = new PostgresFirstRunWindow();
                            pgRun.Show();
                            this.Hide();
                            break;
                        case "mssql":
                            var msRun = new MicrosoftFirstRunWindow();
                            msRun.Show();
                            this.Hide();
                            break;
                    }
                }
            }
        }
    }
}
