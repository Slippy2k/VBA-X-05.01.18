using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Utility
{
    public interface IStorageService
    {
        String[] ROMFileExtensions { get; set; }
        IAsyncOperation<StorageFolder> GetFolderAsync();
    }
}
