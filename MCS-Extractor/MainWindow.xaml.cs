using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MCSDataImport;
using MCS_Extractor.FirstRun;

namespace MCS_Extractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CSVImporter importer;

        private CSVFileHandler csvHandler;


        public MainWindow()
        {
            var settings = StorageSettings.GetInstance();
            var platform = settings.DatabasePlatform;
            var connectionString = settings.ConnectionString;
            var dbName = settings.DatabaseName;
            importer = new CSVImporter(platform, connectionString, dbName );
            csvHandler = new CSVFileHandler(settings.StoragePath, platform, connectionString, dbName);
          
            InitializeComponent();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {

            ImportList.Items.Clear();
            ImportFiles();


        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            StorageSettings settings = StorageSettings.GetInstance();
            string tableName = TableName.Text;
            CSVMappingFactory factory = new CSVMappingFactory(settings.DatabasePlatform, settings.ConnectionString, settings.DatabaseName);
            if (0 < tableName.Length)
            {
                if (ValidateSummaryFields())
                {
                    tableName = MappingCreator.ClearSpacing(tableName);
                    var ml = factory.Loader;
                    if (!ml.TableExists(tableName))
                    {
                        var creator = factory.Creator;
                        var identifiers = new List<string>(IdentifierFields.Items.Count);
                        foreach (var item in IdentifierFields.Items)
                        {
                            identifiers.Add(MappingCreator.ClearSpacing(item.ToString()));
                        }
                        creator.CreateSummary(tableName,
                            MappingCreator.ClearSpacing(IdField.SelectedValue.ToString()),
                            MappingCreator.ClearSpacing(StartField.SelectedValue.ToString()),
                            MappingCreator.ClearSpacing(EndField.SelectedValue.ToString()), identifiers.ToArray());

                        for (int i = 0; i < MappingGrid.Items.Count; i++)
                        {
                            MappingType row = (MappingType)MappingGrid.Items[i];
                            creator.AddMapping(row.RowName, row.DataType);
                        }
                        try
                        {
                            creator.SaveMappings(tableName);
                            MappingContainer.Visibility = Visibility.Collapsed;
                            ImportList.Visibility = Visibility.Visible;
                            ImportFiles();
                        }
                        catch ( Exception ef )
                        {
                            MessageBox.Show("Error creating mappings: " + ef.Message + " - please check all your fields are of the right type.", "Error creating mappings");
                        }
                    }
                    else
                    {
                        MessageBox.Show(String.Format("The table '{0}' already exists- please try a new name.", tableName), "Table already exists");
                    }
                } else
                {
                    MessageBox.Show("Please select indexing properties.");
                }
            }
            else
            {
                MessageBox.Show("Please enter a table name.", "Empty table name");
            }

        }

        private bool ValidateSummaryFields()
        {
            var result = true;
            if (StartField.SelectedIndex == 0)
            {
                StartField.BorderBrush = new SolidColorBrush(Colors.Red);
                result = false;
            }

            if (EndField.SelectedIndex == 0)
            {
                EndField.BorderBrush = new SolidColorBrush(Colors.Red);
                result = false;
            }

            if (IdField.SelectedIndex == 0)
            {
                IdField.BorderBrush = new SolidColorBrush(Colors.Red);
                result = false;
            }

            if (IdentifierFields.Items.Count == 0)
            {
                IdentifierFields.BorderBrush = new SolidColorBrush(Colors.Red);
                result = false;
            }
            return result;
        }

        private void FieldNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < e.AddedItems.Count)
            {
                var selected = FieldNames.SelectedItem;
                FieldNames.Items.RemoveAt(FieldNames.SelectedIndex);
                IdentifierFields.Items.Add(selected);
            }
        }

        private void IdentifierFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < e.AddedItems.Count)
            {
                var selected = IdentifierFields.SelectedItem;
                IdentifierFields.Items.RemoveAt(IdentifierFields.SelectedIndex);
                FieldNames.Items.Add(selected);
            }
        }

        private void DataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox box = sender as ComboBox;
            if (MappingGrid.CurrentCell.IsValid)
            {
                MappingType row = (MappingType)MappingGrid.CurrentCell.Item;
                DBType newVal = (DBType)Enum.Parse(typeof(DBType), box.SelectedItem.ToString());
                if (newVal != row.DataType)
                {
                    row.DataType = newVal;
                }
            }
            
        }

        private async void ImportFiles()
        {
            await Task.Run(() =>
            {

                try
                {
                    var files = csvHandler.GetFileList();
                    this.Dispatcher.Invoke(() =>
                    {
                        ImportButton.IsEnabled = false;
                        ImportLabel.Content = "Importing...";
                    });
                    ProcessFiles(files);

                }
                catch (Npgsql.PostgresException ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Import error: " + ex.Message + Environment.NewLine + "Location: " + ex.Where);
                    });
                }
                catch ( Exception ef )
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Import error: " + ef.Message + Environment.NewLine + "(Check your mapping field types)");
                    });
                }
                finally
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        ImportLabel.Content = "Import complete, " + ImportList.Items.Count + " files imported.";
                        ImportButton.IsEnabled = true;
                    });
                }
            });
        }

        private void ProcessFiles(List<string> files)
        {
            if (0 < files.Count)
            {
                for (int fn = 0; fn < files.Count; fn++)
                {
                    if (importer.ImportMapping(files[fn]))
                    {
                        // ImportLabel.Content = "Successfully imported " + files[fn];
                        this.Dispatcher.Invoke(() =>
                        {
                            ImportList.Items.Add(files[fn]);
                        });
                    }
                    else
                    {
                        var csMapping = importer.SummariseCSV(files[fn]);
                        if (!csMapping.Empty)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                ShowMappingForm(csMapping);
                            });
                            break;
                        }
                        else {
                            this.Dispatcher.Invoke(() =>
                            {
                                ImportList.Items.Add(files[fn]);
                            });
                        };
                    

                    }
                }
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    ImportLabel.Content = "No new files to import.";
                });

            }

        }

        private void ShowMappingForm(CSVSummary csMapping)
        {
            ImportLabel.Content = "Could not find mapping for " + csMapping.FileName;
            ImportList.Visibility = Visibility.Collapsed;
            MappingContainer.Visibility = Visibility.Visible;
           
            MappingGrid.Items.Clear();
            FieldNames.Items.Clear();
            IdentifierFields.Items.Clear();
            foreach ( var cb in new [] {  StartField, EndField, IdField })
            {
                for ( var i=cb.Items.Count-1; 0 < i; i-- )
                {
                    cb.Items.RemoveAt(i);
                }
                cb.SelectedIndex = 0;
               // ((ComboBoxItem)cb.Items[0]).IsSelected = true;
            }   
            
            if (!csMapping.Empty)
            {
                var typeList = csMapping.EstimateTypes();

                var ls = new List<MappingType>();
                for (int i = 0; i < csMapping.Headers.Count; i++)
                {
                    MappingType m = new MappingType();
                    m.RowName = csMapping.Headers[i];
                    m.DataType = typeList[i];
                    if (0 < csMapping.Values.Count)
                    {
                        m.Example1 = csMapping.Values[0][i];
                    }
                    if (1 < csMapping.Values.Count)
                    {
                        m.Example2 = csMapping.Values[1][i];
                    }
                    if (2 < csMapping.Values.Count)
                    {
                        m.Example3 = csMapping.Values[2][i];
                    }
                    //   MappingGrid.Columns[0].csMapping.Headers[i], Binding = new Binding(String.Format("[{0}]", i)) });
                    MappingGrid.Items.Add(m);
                    StartField.Items.Add(m.RowName);
                    EndField.Items.Add(m.RowName);
                    IdField.Items.Add(m.RowName);
                    FieldNames.Items.Add(m.RowName);
                }
            }
            else
            {

            }
        }
    }

    /**
     * Wrapping type for the UI to display row data.
     **/
    public class MappingType
    {
            public MappingType()
            {
                Example1 = "";
                Example2 = "";
                Example3 = "";
            }

        public string RowName { get; set; }

        public DBType DataType { get; set; }

        public string Example1 { get; set; }

        public string Example2 { get; set; }

        public string Example3 { get; set; }
    }
}
