using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;

namespace VBA_X
{
    public class UWPServiceProvider :
        IVBAXServiceProvider
    {
        private CoreDispatcher dispatcher;
        private IStorageService storageService;
        private IMessageService messageService;

        public UWPServiceProvider(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }


        public IMessageService MessageService
        {
            get
            {
                if (this.messageService == null)
                {
                    this.messageService = new UWPMessageService(this.dispatcher);
                }
                return this.messageService;
            }
        }

        public IStorageService StorageService
        {
            get
            {
                if (this.storageService == null)
                {
                    this.storageService = new UWPStorageService();
                }
                return this.storageService;
            }
        }
    }
}
