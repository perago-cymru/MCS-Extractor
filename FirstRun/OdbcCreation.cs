using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace MCS_Extractor.FirstRun
{
    class OdbcCreation
    {
        public static bool CreateODBC(string connectionString)
        {
            Debug.WriteLine("Create Postgres ODBC connection");
            var result = false;
            var psCommand = String.Format("Add-OdbcDsn -Name \"MCS-Extractor-Install\" -DriverName \"PostgreSQL Unicode\" -DsnType \"user\" -SetPropertyValue \"{0}\"", connectionString);
            var powershell = new Command("Add-Odbcdsn");
            powershell.Parameters.Add("Name", "MCS-Extractor-Install");
            powershell.Parameters.Add("DriverName", "PostgreSQL Unicode");
            powershell.Parameters.Add("DsnType", "user");
            powershell.Parameters.Add("SetPropertyValue", connectionString);

            var runSpace = RunspaceFactory.CreateRunspace();
            try
            {
            
                runSpace.Open();
                var pipeline = runSpace.CreatePipeline();
                pipeline.Commands.Add(powershell);
                pipeline.Invoke();
                result = true;
                
            }
            catch (Exception e)
            {
                Debug.WriteLine("Powershell Exception: " + e.Message);
                Debug.WriteLine("In command: " + psCommand.ToString());
                Debug.Write(e.StackTrace);
            } 
            finally
            {
                runSpace.Close();
            }
            return result;
        }

    }
}
