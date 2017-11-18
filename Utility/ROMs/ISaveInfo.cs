using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public interface ISaveInfo
    {
        String SRAMExtension { get; }
        String SaveStateExtension { get; }
    }
}
