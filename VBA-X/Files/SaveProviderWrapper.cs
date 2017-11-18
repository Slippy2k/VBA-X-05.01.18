using EmulatorComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;
using Windows.Foundation;

namespace VBA_X
{
    class SaveProviderWrapper : ISaveProvider
    {
        private StorageManager manager;

        public SaveProviderWrapper()
        {
            this.manager = StorageManager.Current;
        }

        public IAsyncOperation<ByteWrapper> LoadSRAMAsync()
        {
            Func<Task<ByteWrapper>> helper = async () =>
            {
                SRAMData data = await this.manager.GetSRAMDataAsync();
                if(data == null)
                {
                    return null;
                }
                return new ByteWrapper(data.Data);
            };

            return helper().AsAsyncOperation();
        }

        public IAsyncAction SaveSRAMAsync(ByteWrapper bytes)
        {
            byte[] data = bytes.AsArray();
            Func<Task> helper = async () =>
            {
                await this.manager.SaveSRAMDataAsync(new SRAMData()
                {
                    Data = data
                });

            };
            return helper().AsAsyncAction();
        }

        public IAsyncAction TriggerAutosave()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Autosaving");
#endif

            Func<Task> helper = async () =>
            {
                try
                {
                    await EmulatorManager.Current.SaveAutoState();
                }catch(Exception e)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Autosave error: " + e.Message);
#endif
                }
            };
            return helper().AsAsyncAction();
        }
    }
}
