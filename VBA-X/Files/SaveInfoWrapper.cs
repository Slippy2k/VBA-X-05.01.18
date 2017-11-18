using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VBA_X
{
    class SaveInfoWrapper : Utility.ISaveInfo
    {
        EmulatorComponent.ISaveInfo saveInfo;

        public SaveInfoWrapper(EmulatorComponent.ISaveInfo saveInfo)
        {
            this.saveInfo = saveInfo;
        }

        public string SaveStateExtension
        {
            get
            {
                return this.saveInfo.SaveStateExtension;
            }
        }

        public string SRAMExtension
        {
            get
            {
                return this.saveInfo.SRAMExtension;
            }
        }
    }
}
