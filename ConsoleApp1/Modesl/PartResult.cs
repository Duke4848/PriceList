using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Modesl
{
    public enum PartResult
    {
        Failure = 1,
        Success = 2,
        PreviousLineJoinSuccess = 4,
        NoPreviousLineJoinFailure = 8
    }
}
