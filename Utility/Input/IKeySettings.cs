using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Utility
{
    public interface IKeySettings
    {

        VirtualKey LeftBinding
        {
            get;
        }

        VirtualKey RightBinding
        {
            get;
        }

        VirtualKey UpBinding
        {
            get;
        }

        VirtualKey DownBinding
        {
            get;
        }

        VirtualKey ABinding
        {
            get;
        }

        VirtualKey BBinding
        {
            get;
        }

        VirtualKey LBinding
        {
            get;
        }

        VirtualKey RBinding
        {
            get;
        }

        VirtualKey StartBinding
        {
            get;
        }

        VirtualKey SelectBinding
        {
            get;
        }

        VirtualKey TurboBinding
        {
            get;
        }
    }
}
