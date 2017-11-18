using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace VBA_X
{
    public class UWPStorageService
        : IStorageService
    {
        public string[] ROMFileExtensions
        {
            get;  set;
        }

        public IAsyncOperation<StorageFolder> GetFolderAsync()
        {
            FolderPicker picker = new FolderPicker();

            foreach (String extension in this.ROMFileExtensions)
            {
                picker.FileTypeFilter.Add(extension);
            }
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

            return picker.PickSingleFolderAsync();
        }
    }
}
