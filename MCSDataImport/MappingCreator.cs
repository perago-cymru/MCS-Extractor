using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace MCSDataImport
{

    public abstract class MappingCreator
    {



        protected TableSummary summary = new TableSummary();



        public void CreateSummary(string tableName, string idField, string startField, string closeField, string[] identifierFields)
        {
            summary = new TableSummary()
            {
                TableName = tableName,
                IdField = idField,
                StartField = startField,
                CloseField = closeField,
                UserIdentifierFields = identifierFields
            };
        }

        public abstract void AddMapping(string csvName, DBType type);

        public abstract void SaveMappings(string tableName);

        public static string ClearSpacing(string name, string replacement = "_")
        {
            name =name.ToLower();
            name = Regex.Replace(name, @"\s+", " ");
            name = Regex.Replace(name, " ", replacement);
            return name;
        }

        


    }

}
