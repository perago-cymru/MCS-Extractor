using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCS_Extractor.ImportedData.Interfaces;

namespace MCS_Extractor.ImportedData.Microsoft
{
    public class MicrosoftDataMappingType : IDataMappingType
    {
        private DBType type;

        private Dictionary<DBType, string> FieldTypeNames = new Dictionary<DBType, string>
        {
            { DBType.Boolean, "bit" },
            { DBType.Int, "int" },
            { DBType.Double, "float" },
            { DBType.Long, "real" },
            { DBType.Numeric, "numeric" },
            { DBType.String, "nvarchar(255)" },
            { DBType.Text, "ntext" },
            { DBType.Date, "datetime" }
        };

        private Dictionary<DBType, Type> FieldTypes = new Dictionary<DBType, Type>
        {
            { DBType.Boolean, typeof(bool) },
            { DBType.Int, typeof(int) },
            { DBType.Double, typeof(double) },
            { DBType.Long, typeof(long) },
            { DBType.Numeric, typeof(int) },
            { DBType.String, typeof(string) },
            { DBType.Text, typeof(string) },
            { DBType.Date, typeof(DateTime) }
        };

        public MicrosoftDataMappingType(string csvField, string dbField, DBType type)
        {
            this.CSVFieldName = csvField;
            this.DatabaseFieldName = dbField;
            this.type = type;
        }

        public string CSVFieldName { get; set; }
        public string DatabaseFieldName { get; set; }

        public string GetFieldTypeName()
        {
            return FieldTypeNames[this.type];
        }

        public virtual string TypeName()
        {
            return this.type.ToString();
        }

        public object Map(string val)
        {
            Type t = FieldTypes[this.type];
            if (( val.Trim() == "" ) && ( IsNullableType(t))) {
                return DBNull.Value;
            }
            try
            {
                return Convert.ChangeType(val, t);
            } 
            catch (Exception ef)
            {
                Exception newEx = new Exception("Could not convert '" + val.ToString() + "' for "+CSVFieldName+" to " + t.Name, ef);
                throw newEx;
            }
        }

        private static bool IsNullableType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.DateTime:
                    return true;
                default:
                    return false;
            }
        }
    }
}
