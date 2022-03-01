using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCS_Extractor.FirstRun.Interfaces
{
    public interface IStartupCheck
    {
        bool FirstRun { get; }
    }
}
