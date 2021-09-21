using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Npgsql;

namespace MCS_Extractor.ImportedData
{
    class CSVFileHandler
    {


        public List<string> GetFileList()
        {
            string path = Path.Combine(GetInstallFolder(), ConfigurationManager.AppSettings["DataDirectory"]);
            List<string> unprocessed = new List<string>();
            foreach (string file in Directory.EnumerateFiles(path))
            {
                if (!FileLoaded(file))
                {
                    unprocessed.Add(file);
                }
            }
            return unprocessed;
        }

        public static string GetInstallFolder()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        }

        private bool FileLoaded(string filename)
        {
            var conn = CSVImporter.GetConnection(); 
            conn.Open();
            var cmd = new NpgsqlCommand("SELECT 0 < count(id) FROM loaded_files WHERE filename = @name ", conn);
            cmd.Parameters.AddWithValue("name", filename);
            bool result = (bool)cmd.ExecuteScalar();
            conn.Close();
            return result;
        }
    }
}
