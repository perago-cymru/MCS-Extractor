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
using MCS_Extractor.ImportedData;

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
            importer = new CSVImporter();
            csvHandler = new CSVFileHandler();
            InitializeComponent();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportButton.IsEnabled = false;
            ImportLabel.Content = "Importing...";
            ImportList.Items.Clear();
             ImportFiles();
            ImportLabel.Content = "Import complete, " + ImportList.Items.Count + " files imported.";
            ImportButton.IsEnabled = true;

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {

            string tableName = TableName.Text;
            if (0 < tableName.Length)
            {
                if (ValidateSummaryFields())
                {
                    tableName = MappingCreator.ClearSpacing(tableName);

                    MappingLoader ml = new MappingLoader();
                    if (!ml.TableExists(tableName))
                    {
                        var creator = new MappingCreator();
                        var identifiers = new List<string>(IdentifierFields.Items.Count);
                        foreach (var item in IdentifierFields.Items)
                        {
                            identifiers.Add(MappingCreator.ClearSpacing(item.ToString()));
                        }
                        creator.CreateSummary(tableName,
                            MappingCreator.ClearSpacing(StartField.SelectedValue.ToString()),
                            MappingCreator.ClearSpacing(EndField.SelectedValue.ToString()), identifiers.ToArray());

                        for (int i = 0; i < MappingGrid.Items.Count; i++)
                        {
                            MappingType row = (MappingType)MappingGrid.Items[i];
                            creator.AddMapping(row.RowName, row.DataType);
                        }

                        creator.SaveMappings(tableName);
                        MappingContainer.Visibility = Visibility.Collapsed;
                        ImportList.Visibility = Visibility.Visible;
                        ImportFiles();

                    }
                    else
                    {
                        MessageBox.Show("Table already exists", String.Format("The table '{0}' already exists- please try a new name.", tableName));
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

        private async void ImportFiles()
        {
            await Task.Run(() =>
            {
                var files = csvHandler.GetFileList();
                try
                {
                    ProcessFiles(files);
                }
                catch (Npgsql.PostgresException ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Import error: " + ex.Message + Environment.NewLine + "Location: " + ex.Where);
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
                        this.Dispatcher.Invoke(() =>
                        {
                            ShowMappingForm(files[fn]);
                        });
                        break;

                    }
                }
            }
            else
            {
                ImportLabel.Content = "No new files to import.";

            }

        }

        private void ShowMappingForm(string fileName)
        {
            ImportLabel.Content = "Could not find mapping for " + fileName;
            ImportList.Visibility = Visibility.Collapsed;
            MappingContainer.Visibility = Visibility.Visible;
            var csMapping = importer.SummariseCSV(fileName);
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
                    if (0 < csMapping.Values.Count)
                    {
                        m.Example2 = csMapping.Values[1][i];
                    }
                    if (0 < csMapping.Values.Count)
                    {
                        m.Example3 = csMapping.Values[2][i];
                    }
                    //   MappingGrid.Columns[0].csMapping.Headers[i], Binding = new Binding(String.Format("[{0}]", i)) });
                    MappingGrid.Items.Add(m);
                    StartField.Items.Add(m.RowName);
                    EndField.Items.Add(m.RowName);
                    FieldNames.Items.Add(m.RowName);
                }
            }
        }
        
    }


    public class MappingType
    {
            public MappingType()
            {
                Example1 = "";
                Example2 = "";
                Example3 = "";
            }

        public string RowName { get; set; }

        public PGType DataType { get; set; }

        public string Example1 { get; set; }

        public string Example2 { get; set; }

        public string Example3 { get; set; }
    }
}
