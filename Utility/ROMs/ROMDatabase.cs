using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Utility
{
    public sealed class ROMDatabaseEntry : INotifyPropertyChanged
    {
        private ApplicationDataCompositeValue data;
        private ApplicationDataContainer container;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string Name
        {
            get
            {
                return (string)this.data["Name"];
            }
            internal set
            {
                if(value != this.Name)
                {
                    this.data["Name"] = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public long LastPlayed
        {
            get
            {
                return long.Parse(this.data["LastPlayed"].ToString());
            }
            set
            {
                if(value != this.LastPlayed)
                {
                    this.data["LastPlayed"] = value.ToString();
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int SaveSlot
        {
            get
            {
                return int.Parse(this.data["saveSlot"].ToString());
            }
            set
            {
                if (value != this.SaveSlot)
                {
                    this.data["saveSlot"] = value.ToString();
                    this.NotifyPropertyChanged();
                }
            }
        }


        internal ROMDatabaseEntry(ApplicationDataContainer container, ApplicationDataCompositeValue data, string name = "")
        {
            Initialize(container, data, name);
        }

        private void Initialize(ApplicationDataContainer container, ApplicationDataCompositeValue data, string name = "")
        {
            this.container = container;

            this.PropertyChanged += ROMDatabaseEntry_PropertyChanged;

            this.data = data;

            int version = -1;
            bool restoring = data.ContainsKey("version");
            if (restoring)
            {
                version = (int)data["version"];
            }

            if (version < 0)
            {
                data["version"] = version = 0;

                // load version 0 defaults;
                this.data["Name"] = name;
                this.data["LastPlayed"] = "1337";
            }

            if (version < 1)
            {
                // load version 1 defaults
                data["version"] = 1;

                data["saveSlot"] = 0;
            }

            this.storeData();
        }

        private void ROMDatabaseEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            storeData();
        }

        private void storeData()
        {
            this.container.Values[this.Name] = this.data;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class ROMDatabase
    {
        #region Singleton

        private static ROMDatabase singleton;

        public static ROMDatabase Current
        {
            get
            {
                if (singleton == null)
                {
                    //throw new InvalidOperationException("ROMDatabase not initialized.");
                    Initialize();
                }
                return singleton;
            }
        }

        public static ROMDatabase Initialize()
        {
            if (singleton != null)
            {
                throw new InvalidOperationException("ROMDatabase has been initialized already.");
            }

            return singleton = new ROMDatabase();
        }

        #endregion

        private ApplicationDataContainer container;

        public ROMDatabaseEntry GetOrCreateEntry(string romName)
        {
            object tmp = null;
            if (!this.container.Values.TryGetValue(romName, out tmp))
            {
                ApplicationDataCompositeValue newValue = new ApplicationDataCompositeValue();
                this.container.Values.Add(romName, newValue);
                ROMDatabaseEntry entry = new ROMDatabaseEntry(this.container, newValue, romName);
                return entry;
            }
            else
            {
                ROMDatabaseEntry entry = new ROMDatabaseEntry(this.container, (ApplicationDataCompositeValue)tmp);
                return entry;
            }
        }

        private ROMDatabase()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            bool restoring = localSettings.Containers.ContainsKey("ROMDatabase");
            this.container = localSettings.CreateContainer(
                "ROMDatabase",
                ApplicationDataCreateDisposition.Always
                );

        }
    }
}
