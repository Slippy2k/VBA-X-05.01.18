using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorComponent;
using Windows.Foundation;
using Windows.Storage;
using Utility;

namespace VBA_X
{
    public class ROMData : IROMData
    {
        private byte[] data;
        private IStorageFile file;
        private string fileExtension;
        private uint size;

        public byte[] Data
        {
            get
            {
                return this.data;
            }
        }

        public IStorageFile File
        {
            get
            {
                return this.file;
            }
        }

        public string FileExtension
        {
            get
            {
                return this.fileExtension;
            }
        }

        public uint Size
        {
            get
            {
                return this.size;
            }
        }

        private ROMData() { }

        internal static ROMData FromUtility(Utility.ROMData data)
        {
            return new ROMData()
            {
                size = data.Size,
                file = data.File,
                fileExtension = data.FileExtension,
                data = data.Data
            };
        }
    }
}
