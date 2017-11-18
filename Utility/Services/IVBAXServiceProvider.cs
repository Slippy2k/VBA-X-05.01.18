using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;

namespace Utility
{
    public interface IVBAXServiceProvider
    {
        IMessageService MessageService { get; }
        IStorageService StorageService{ get; }
    }
}
