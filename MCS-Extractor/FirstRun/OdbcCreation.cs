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
            var split = connectionString.Split(';');
            var build = new StringBuilder();
            var comma = "";
            var propertyValues = new List<String>();
            foreach ( string s in split )
            {
                if (!String.IsNullOrEmpty(s))
                {
                    var parts = s.Split('=');
                    if ( parts[0].ToLower() == "host" )
                    {
                        parts[0] = "Server";
                    }
                    build.AppendFormat("{0}\"{1}={2}\"", comma, parts[0], parts[1]);
                    propertyValues.Add(String.Format("{0}={1}", parts[0], parts[1]));
                    comma = ", ";
                }
            }
           // var psCommand = String.Format("Add-OdbcDsn -Name \"MCS-Extractor-Install\" -DriverName \"PostgreSQL Unicode\" -DsnType \"user\" -SetPropertyValue @({0})", build.ToString());
            var powershell = new Command("Add-Odbcdsn");
            powershell.Parameters.Add("Name", "MCS Extractor");
            powershell.Parameters.Add("DriverName", "PostgreSQL Unicode");
            powershell.Parameters.Add("DsnType", "User");
            powershell.Parameters.Add("SetPropertyValue", propertyValues);

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
                Debug.WriteLine("In command: " + powershell.CommandText);
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
