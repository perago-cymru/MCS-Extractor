using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCS_Extractor.ImportedData
{
    public enum DBType
    {
        Boolean,
        String,
        Int,
        Long,
        Text,
        Double,
        Numeric,
        Date
    }

    public class CSVSummary
    {
        public string FileName { get; set; }

        public List<String> Headers { get; set; }

        public List<List<String>> Values { get; set; }

        public List<DBType> Types { get; set; }

        public bool Empty { get; set; }

        public CSVSummary()
        {
            this.Headers = new List<string>();
            this.Values = new List<List<string>>();
            this.Types = new List<DBType>();
            this.Empty = true;
        }

        public List<DBType> EstimateTypes()
        {
            var intMatch = new Regex(@"^\-?[1-9][0-9]{0,9}$");
            var boolMatch = new Regex("^true|false$");
            var doubleMatch = new Regex(@"^-?[0-9]+\.[0-9]+$");
            var dateMatch = new Regex(@"[0-9]{2}\/[0-9]{2}\/[0-9]{4} ");
            var dateMatch2 = new Regex(@"[0-9]{1,2} [A-Za-z]{3} [0-9]{4}");
            var results = new List<DBType>();
            if ( 0 < this.Values.Count )
            {

                for ( int j =0; j < Values[0].Count; j++ )
                {
                    var possibilities = new HashSet<DBType>( new[] { DBType.Boolean, DBType.Date, DBType.Double, DBType.Int, DBType.String, DBType.Text });
                var empty = true;
                    for (int i = 0; i < Values.Count; i++)
                    {
                        var val = Values[i][j];
                        if (0 < val.Length)
                        {
                            empty = false;
                            if (possibilities.Contains(DBType.Int) && !intMatch.IsMatch(val))
                            {
                                possibilities.Remove(DBType.Int);
                            }
                            if (possibilities.Contains(DBType.Boolean) && !boolMatch.IsMatch(val))
                            {
                                possibilities.Remove(DBType.Boolean);
                            }
                            if (possibilities.Contains(DBType.Double) && !doubleMatch.IsMatch(val))
                            {
                                possibilities.Remove(DBType.Double);
                            }
                            if (possibilities.Contains(DBType.Date) && !dateMatch.IsMatch(val) && !dateMatch2.IsMatch(val))
                            {
                                possibilities.Remove(DBType.Date);
                            }
                            if (255 < val.Length)
                            {
                                possibilities.Remove(DBType.String);
                            }
                        }

                    }
                    if (empty)
                    {
                        results.Add(DBType.String);
                    }
                    else
                    {
                        results.Add(ChooseType(possibilities));
                    }

                }



            }

            return results;
        }

        private DBType ChooseType(HashSet<DBType> chooseFrom)
        {
            var result = DBType.String;
            if (chooseFrom.Count == 1)
            {
                result = chooseFrom.First();
            }
            else if ( 1 < chooseFrom.Count )
            {
                if (chooseFrom.Contains(DBType.Double))
                {
                    result=DBType.Double;
                }
                if (chooseFrom.Contains(DBType.Int))
                {
                    result = DBType.Int;
                }
                if (chooseFrom.Contains(DBType.Date))
                {
                    result = DBType.Date;
                }
                if (chooseFrom.Contains(DBType.Boolean))
                {
                    result = DBType.Boolean;
                }
            }
            return result;
        }

    }
}
