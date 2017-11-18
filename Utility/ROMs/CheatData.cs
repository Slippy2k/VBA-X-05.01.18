using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public sealed class CheatData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private string code;
        private string description;
        private bool enabled;

        public String CheatCode
        {
            get
            {
                return this.code;
            }
            set
            {
                if(value != code)
                {
                    this.code = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public String Description
        {
            get
            {
                return this.description;
            }
            set
            {
                if (value != description)
                {
                    this.description = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool Enabled
        {
            get
            {
                return this.enabled;
            }
            set
            {
                if (value != enabled)
                {
                    this.enabled = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
