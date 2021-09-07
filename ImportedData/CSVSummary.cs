using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCS_Extractor.ImportedData
{
    public enum PGType
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

        public List<PGType> Types { get; set; }

        public CSVSummary()
        {
            this.Headers = new List<string>();
            this.Values = new List<List<string>>();
            this.Types = new List<PGType>();
        }

        public List<PGType> EstimateTypes()
        {
            var intMatch = new Regex(@"^\-?[1-9][0-9]{0,9}$");
            var boolMatch = new Regex("^true|false$");
            var doubleMatch = new Regex(@"^-?[0-9]+\.[0-9]+$");
            var dateMatch = new Regex(@"[0-9]{2}\/[0-9]{2}\/[0-9]{4} ");
            var dateMatch2 = new Regex(@"[0-9]{1,2} [A-Za-z]{3} [0-9]{4}");
            var results = new List<PGType>();
            if ( 0 < this.Values.Count )
            {

                for ( int j =0; j < Values[0].Count; j++ )
                {
                    var possibilities = new HashSet<PGType>( new[] { PGType.Boolean, PGType.Date, PGType.Double, PGType.Int, PGType.String, PGType.Text });
                var empty = true;
                    for (int i = 0; i < Values.Count; i++)
                    {
                        var val = Values[i][j];
                        if (0 < val.Length)
                        {
                            empty = false;
                            if (possibilities.Contains(PGType.Int) && !intMatch.IsMatch(val))
                            {
                                possibilities.Remove(PGType.Int);
                            }
                            if (possibilities.Contains(PGType.Boolean) && !boolMatch.IsMatch(val))
                            {
                                possibilities.Remove(PGType.Boolean);
                            }
                            if (possibilities.Contains(PGType.Double) && !doubleMatch.IsMatch(val))
                            {
                                possibilities.Remove(PGType.Double);
                            }
                            if (possibilities.Contains(PGType.Date) && !dateMatch.IsMatch(val) && !dateMatch2.IsMatch(val))
                            {
                                possibilities.Remove(PGType.Date);
                            }
                            if (255 < val.Length)
                            {
                                possibilities.Remove(PGType.String);
                            }
                        }

                    }
                    if (empty)
                    {
                        results.Add(PGType.String);
                    }
                    else
                    {
                        results.Add(ChooseType(possibilities));
                    }

                }



            }

            return results;
        }

        private PGType ChooseType(HashSet<PGType> chooseFrom)
        {
            var result = PGType.String;
            if (chooseFrom.Count == 1)
            {
                result = chooseFrom.First();
            }
            else if ( 1 < chooseFrom.Count )
            {
                if (chooseFrom.Contains(PGType.Double))
                {
                    result=PGType.Double;
                }
                if (chooseFrom.Contains(PGType.Int))
                {
                    result = PGType.Int;
                }
                if (chooseFrom.Contains(PGType.Date))
                {
                    result = PGType.Date;
                }
                if (chooseFrom.Contains(PGType.Boolean))
                {
                    result = PGType.Boolean;
                }
            }
            return result;
        }

    }
}
