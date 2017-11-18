using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Utility
{
    public sealed class ROMData
    {
        internal byte[] data;
        internal IStorageFile file;
        internal string fileExtension;
        internal uint size;

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
    }

    public sealed class SRAMData
    {
        internal byte[] data;

        public byte[] Data
        {
            get
            {
                return this.data;
            }
            set
            {
                this.data = value;
            }
        }

        public SRAMData()
        { }
    }

    public sealed class SaveStateData
    {
        internal byte[] data;
        internal int slot;

        public byte[] Data
        {
            get
            {
                return this.data;
            }
            set
            {
                this.data = value;
            }
        }

        public int Slot
        {
            get { return this.slot; }
            set { this.slot = value; }
        }

        public SaveStateData()
        {  }
    }

    public sealed class SnapshotData
    {
        internal byte[] data;
        internal int pitch;

        public byte[] Data
        {
            get
            {
                return this.data;
            }
            set
            {
                this.data = value;
            }
        }

        public int Pitch
        {
            get { return this.pitch; }
            set { this.pitch = value; }
        }

        public SnapshotData()
        { }
    }
}
