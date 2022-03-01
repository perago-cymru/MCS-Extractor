using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCS_Extractor.FirstRun.Interfaces
{
    /// <summary>
    /// Interface for initial process to create databases etc if they do not already exist.
    /// </summary>
    interface IFirstRunProcess
    {
        bool FirstRun();
    }
}
