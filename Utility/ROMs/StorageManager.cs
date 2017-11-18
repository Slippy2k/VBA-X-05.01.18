using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Utility
{
    internal struct ZipData
    {
        public byte[] Bytes;
        public String ROMExtension;
    }

    public sealed class ROMEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private StorageFile file;
        private ROMDatabaseEntry dbEntry;
        private String snapshotImage;
        private bool startedLoadingImage = false;
        private IList<CheatData> cheatCache;
        private double displayWidth = 300.0f;

        public ROMDatabaseEntry DatabaseEntry
        {
            get
            {
                return this.dbEntry;
            }
            internal set
            {
                if (this.dbEntry != value)
                {
                    this.dbEntry = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public StorageFile File
        {
            get
            {
                return this.file;
            }
            set
            {
                if(this.file != value)
                {
                    this.file = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public String Name
        {
            get
            {
                return File.DisplayName;
            }
        }

        public String SnapshotImage
        {
            get
            {
                if(this.snapshotImage == null && !this.startedLoadingImage)
                {
                    this.startedLoadingImage = true;
                    StorageManager.Current.LoadSnapshotImage(this);
                }
                if(this.snapshotImage == null)
                {
                    return this.file.FileType;
                }
                return snapshotImage;
            }

            internal set
            {
                if(this.snapshotImage != value)
                {
                    snapshotImage = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public double EntryDisplayWidth
        {
            get
            {
                return this.displayWidth;
            }
            set
            {
                if(this.displayWidth != value)
                {
                    this.displayWidth = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public IList<CheatData> CheatCache
        {
            get
            {
                return cheatCache;
            }

            set
            {
                if(cheatCache != value)
                {
                    cheatCache = value;
                    //this.NotifyPropertyChanged();
                }
            }
        }

        internal void ResetImageLoading()
        {
            this.startedLoadingImage = false;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public delegate void FolderCallback(IStorageFolder folder);

    public sealed class StorageManager : INotifyPropertyChanged
    {
        #region Singleton
        private static StorageManager singleton;

        public static StorageManager Current
        {
            get
            {
                if (singleton == null)
                {
                    throw new InvalidOperationException("StorageManager must be initialized first.");
                }
                return singleton;
            }
        }

        public static IAsyncOperation<StorageManager> InitializeAsync(IVBAXServiceProvider serviceProvider, ISaveInfo saveInfo)
        {
            Func<Task<StorageManager>> helper = async () =>
            {
                if (singleton != null)
                {
                    throw new InvalidOperationException("StorageManager has been initialized already.");
                }
                singleton = new StorageManager(serviceProvider, saveInfo);
                await singleton.Initialize();

                return singleton;
            };

            return helper().AsAsyncOperation();
        }
        #endregion

        private const string CHEAT_FILE_EXTENSION = ".cht";
        private const String DEFAULT_ROM_SUBDIR = "roms";
        private const String SCREENSHOT_NAME_TEMPLATE = "{0} - {1}";
        private const int AUTOSAVE_SLOT = 9;

        public static int AutosaveSlot
        {
            get { return AUTOSAVE_SLOT; }
        }

        private IVBAXServiceProvider services;
        private ISaveInfo info;
        private StorageFolder romDirectory;
        private ObservableCollection<ROMEntry> romList;
        private string[] romFileExtensions;
        private ResourceLoader resources;
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public event FolderCallback ROMDirectoryChanged;

        private Dictionary<String, BitmapImage> snapshotMap;

        private ROMEntry currentROM;

        public ROMEntry CurrentROM
        {
            get
            {
                return this.currentROM;
            }
        }

        public StorageFolder ROMDirectory
        {
            get { return romDirectory; }
            private set
            {
                romDirectory = value;
                this.NotifyPropertyChanged();
                if (this.ROMDirectoryChanged != null)
                {
                    this.ROMDirectoryChanged(this.romDirectory);
                }
            }
        }

        public IReadOnlyList<ROMEntry> ROMList
        {
            get { return this.romList; }
        }

        private StorageManager(IVBAXServiceProvider serviceProvider, ISaveInfo saveInfo)
        {
            this.services = serviceProvider;
            this.info = saveInfo;
            this.snapshotMap = new Dictionary<string, BitmapImage>();
            this.romList = new ObservableCollection<ROMEntry>();
            this.resources = new ResourceLoader();
        }

        private async Task<StorageFolder> GetDefaultROMDir()
        {
            var dir = ApplicationData.Current.LocalFolder;

            StorageFolder romDir = await dir.CreateFolderAsync(DEFAULT_ROM_SUBDIR, CreationCollisionOption.OpenIfExists);
            return romDir;
        }

        private async Task Initialize()
        {
            // initialize storage service
            String fileExtensions = resources.GetString("folderPickerFilters");
            this.romFileExtensions = this.services.StorageService.ROMFileExtensions = fileExtensions.Split(';');

            // restore folders from access list
            await GetUserROMDirectory();

            await RefreshROMListAsync();
        }

        private async Task GetUserROMDirectory()
        {
            var accessListEntries = StorageApplicationPermissions.FutureAccessList.Entries;
            if (accessListEntries.Count == 0)
            {
                // no entries -> use local storage
                ROMDirectory = await this.GetDefaultROMDir();
            }
            else
            {
                var entry = accessListEntries[0];
                try
                {
                    ROMDirectory = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(entry.Token);
                }
                catch (IOException ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(ex.Message);                
#endif
                    await services.MessageService.ShowMessage(this.resources.GetString("storageROMDirAccessDenied"), this.resources.GetString("errorCaption"));
                    StorageApplicationPermissions.FutureAccessList.Remove(entry.Token);
                    ROMDirectory = await this.GetDefaultROMDir();
                }
            }
        }

        internal void LoadSnapshotImage(ROMEntry entry)
        {
#pragma warning disable CS4014
            this.services.MessageService.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
#pragma warning restore CS4014
            {
                var path = this.replaceExtension(entry.Name, ".png");
                var file = await this.romDirectory.TryGetItemAsync(path);
                if(file == null)
                {
                    // try again later maybe
                    entry.ResetImageLoading();
                    return;
                }

                var fs = await ((IStorageFile) file).OpenAsync(FileAccessMode.Read);
                var img = new BitmapImage();
                img.SetSource(fs);

                this.snapshotMap[path] = img;

                entry.SnapshotImage = path;
            });
        }

        public BitmapImage GetSnapshotImage(String key)
        {
            BitmapImage image = null;
            this.snapshotMap.TryGetValue(key, out image);
            return image;
        }

        public IAsyncOperation<IStorageFile> GetAssetFileAsync(String name)
        {
            var uri = new Uri("ms-appx:///Assets/" + name);

            Func<Task<IStorageFile>> helper = async () =>
            {
                return await StorageFile.GetFileFromApplicationUriAsync(uri);
            };

            return helper().AsAsyncOperation<IStorageFile>();
        }

        public IAsyncAction PickROMDirectoryAsync()
        {
            Func<Task> helper = async () =>
            {
                StorageFolder romDirectory = await services.StorageService.GetFolderAsync();
                if (romDirectory != null)
                {
                    //int result = await services.MessageService.ShowOptionDialog(
                    //    String.Format(resources.GetString("storageSubdirConfirmation"), DEFAULT_ROM_SUBDIR),
                    //    resources.GetString("storageSubdirConfirmationYes"),
                    //    resources.GetString("storageSubdirConfirmationNo"));

                    //if (result == 0)
                    //{
                    //    romDirectory = await romDirectory.CreateFolderAsync(DEFAULT_ROM_SUBDIR, CreationCollisionOption.OpenIfExists);
                    //}

                    this.ROMDirectory = romDirectory;
                    StorageApplicationPermissions.FutureAccessList.Clear();
                    StorageApplicationPermissions.FutureAccessList.Add(romDirectory);
                    await RefreshROMListAsync();
                }
            };
            return helper().AsAsyncAction();
        }

        public IAsyncAction ResetROMDirectoryAsync()
        {
            Func<Task> helper = async () =>
            {
                this.ROMDirectory = await GetDefaultROMDir();
                await RefreshROMListAsync();
            };
            return helper().AsAsyncAction();
        }

        public IAsyncAction RefreshROMListAsync()
        {
            Func<Task> helper = async () =>
            {
                this.romList.Clear();

                var fileQuery = this.romDirectory.CreateFileQueryWithOptions(
                    new Windows.Storage.Search.QueryOptions(
                        Windows.Storage.Search.CommonFileQuery.DefaultQuery,
                        this.romFileExtensions
                    )
                );

                IReadOnlyList<StorageFile> files = await fileQuery.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    var entry = new ROMEntry();
                    entry.File = file;
                    entry.DatabaseEntry = ROMDatabase.Current.GetOrCreateEntry(entry.Name);

                    this.romList.Add(entry);
                }

                this.NotifyPropertyChanged("ROMList");
            };
            return helper().AsAsyncAction();
        }

        public IAsyncOperation<ROMData> LoadROMAsync(ROMEntry entry)
        {
            IStorageFile file = entry.File;
            String fileExtension = this.GetFileExtension(file).ToLower();
            Func<Task<ROMData>> helper = async () =>
            {
                ROMData romData = new ROMData();
                if (fileExtension == "zip")
                {
                    var zipData = await this.GetBytesFromZipFileAsync(file, this.romFileExtensions);
                    romData.data = zipData.Bytes;
                    romData.fileExtension = zipData.ROMExtension;
                }
                else
                {
                    romData.data = await this.GetBytesFromFileAsync(file);
                    romData.fileExtension = this.GetFileExtension(file);
                }
                
                romData.file = file;
                romData.size = (uint)romData.data.Length;

                if(this.currentROM != null)
                {
                    this.currentROM.CheatCache = null;
                }
                this.currentROM = entry;

                return romData;
            };

            return helper().AsAsyncOperation<ROMData>();
        }

        private async Task<ZipData> GetBytesFromZipFileAsync(IStorageFile file, string[] targetExtensions)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("READ ZIP: " + file.Name);
#endif
            if(this.GetFileExtension(file).ToLower() != "zip")
            {
                throw new InvalidOperationException("The given file is not a zip file.");
            }

            using (System.IO.Stream stream = await file.OpenStreamForReadAsync())
            {
                ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
                foreach(var entry in archive.Entries)
                {
                    string foundExtension = null;
                    foreach (var extension in targetExtensions)
                    {
                        if(entry.Name.EndsWith(extension, StringComparison.CurrentCultureIgnoreCase))
                        {
                            int dotIndex = extension.LastIndexOf('.');
                            if(dotIndex < 0)
                            {
                                dotIndex = -1;
                            }
                            foundExtension = extension.Substring(dotIndex + 1);
                            break;
                        }
                    }

                    if(foundExtension != null)
                    {
                        using (Stream fileStream = entry.Open())
                        {
                            byte[] bytes = new byte[entry.Length];
                            int offset = 0;
                            int read = 0;
                            while((read = await fileStream.ReadAsync(bytes, offset, (int) entry.Length - offset)) > 0)
                            {
                                offset += read;
                            }
                            if(offset != entry.Length)
                            {
                                throw new IOException("Can't read ROM file from archive.");
                            }
                            return new ZipData()
                            {
                                Bytes = bytes,
                                ROMExtension = foundExtension
                            };
                        }
                    }
                }
            }

            throw new IOException("The given archive does not contain any valid ROMs.");
        }

        private async Task<byte[]> GetBytesFromFileAsync(IStorageFile file)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("READ: " + file.Name);
#endif
            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                using (var inputStream = stream.GetInputStreamAt(0L))
                {
                    BasicProperties properties = await file.GetBasicPropertiesAsync();
                    Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer((uint)properties.Size);
                    IBuffer data = await inputStream.ReadAsync(buffer, (uint)properties.Size, InputStreamOptions.None);
                    DataReader reader = DataReader.FromBuffer(data);
                    byte[] bytes = new byte[data.Length];
                    reader.ReadBytes(bytes);
                    reader.DetachBuffer();
                    return bytes;
                }
            }        
        }

        private async Task WriteBytesToFileAsync(IStorageFile file, byte[] bytes)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("WRITE: " + file.Name);
#endif
            IStorageFile tmpFile = await this.GetTmpFile();
#if DEBUG
            System.Diagnostics.Debug.WriteLine("TEMP FILE: " + tmpFile.Name);
#endif

            using (IRandomAccessStream stream = await tmpFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0L))
                {
                    using (DataWriter writer = new DataWriter(outputStream))
                    {
                        writer.WriteBytes(bytes);
                        await writer.StoreAsync();
                        writer.DetachStream();
                        await outputStream.FlushAsync();
                        await tmpFile.MoveAndReplaceAsync(file);
                    }
                }                    
            }

            //// allows all file handles to be released immediately
            //// instead of a delay to the next garbage collection
            //writer = null;
            //outputStream = null;
            //stream = null;
            //tmpFile = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

        }

        public String GetSnapshotPath(String name)
        {
            //var test = File.Exists(this.romDirectory.Path + "\\" + this.replaceExtension(name, ".png"));
            return this.romDirectory.Path + "\\" + this.replaceExtension(name, ".png");
        }

        private String replaceExtension(String path, String extension)
        {
            int index = path.LastIndexOf('.');
            if(index < 0)
            {
                return path + extension;
            }
            return path.Substring(0, index) + extension;
        }

        public IAsyncAction SaveSnapshotAsync(SnapshotData data)
        {
            byte[] pixeldata = data.data;
            int pitch = data.pitch;

            Func<Task> helper = async () =>
            {
                if(this.currentROM == null)
                {
                    return;
                }

                int pixelWidth = pitch / 4;
                int pixelHeight = (int)pixeldata.Length / pitch;

                IStorageFile file = await this.GetFileUsingExtension(".png", true);
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore,
                        (uint) pixelWidth, (uint)pixelHeight, 96.0, 96.0, pixeldata);
                    await encoder.FlushAsync();
                }
                await this.RefreshROMListAsync();
            };

            return helper().AsAsyncAction();
        }

        public IAsyncAction SaveScreenshotAsync(SnapshotData data)
        {
            byte[] pixeldata = data.data;
            int pitch = data.pitch;

            Func<Task> helper = async () =>
            {
                if (this.currentROM == null)
                {
                    return;
                }

                int pixelWidth = pitch / 4;
                int pixelHeight = (int)pixeldata.Length / pitch;
                
                //IStorageFile file = await this.GetFileUsingExtension(".png", true);

                var lib = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                StorageFolder saveFolder = lib.SaveFolder;
                saveFolder = await saveFolder.CreateFolderAsync(Package.Current.DisplayName, CreationCollisionOption.OpenIfExists);
                String filename = String.Format(
                    SCREENSHOT_NAME_TEMPLATE, 
                    (this.currentROM.File as StorageFile).DisplayName, 
                    DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss")
                    ) + ".png";
                StorageFile file = await saveFolder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore,
                        (uint)pixelWidth, (uint)pixelHeight, 96.0, 96.0, pixeldata);
                    await encoder.FlushAsync();
                }
            };

            return helper().AsAsyncAction();
        }

        public IAsyncOperation<IList<CheatData>> LoadCheatDataAsync()
        {
            Func<Task<IList<CheatData>>> asyncHelper = async () =>
            {
                IList<CheatData> results = null;
                if (this.currentROM == null)
                {
                    return new List<CheatData>();
                }

                if(this.currentROM.CheatCache != null)
                {
                    return this.currentROM.CheatCache;
                }
                else
                {
                    results = new List<CheatData>();
                    this.currentROM.CheatCache = results;
                }
                
                IStorageFile file = await this.GetFileUsingExtension(CHEAT_FILE_EXTENSION);
                if(file == null)
                {
                    return results;
                }

                String data = await this.ReadFileToStringAsync(file);
                String[] lines = data.Split('\n');
                int i = 0;
                CheatData cheat = null;
                foreach (var line in lines)
                {
                    if(line.Trim() == string.Empty)
                    {
                        continue;
                    }
                    if(i % 3 == 0)
                    {
                        cheat = new CheatData();
                        cheat.Description = line;
                    }else if(i % 3 == 1)
                    {
                        cheat.CheatCode = line;
                    }else if(i % 3 == 2)
                    {
                        int enable = 1;
                        int.TryParse(line, out enable);
                        cheat.Enabled = (enable == 1);

                        results.Add(cheat);
                    }
                    i++;
                }

                return results;
            };
            Func<Task<IList<CheatData>>> exceptionHelper = async () =>
            {
                try
                {
                    return await asyncHelper();
                }catch(IOException)
                {
                    return null;
                }
            };
            return exceptionHelper().AsAsyncOperation();
        }

        public IAsyncAction StoreCheatDataAsync(IList<CheatData> cheats)
        {
            Func<Task> helper = async () =>
            {
                if(this.currentROM == null)
                {
                    return;
                }

                this.currentROM.CheatCache = cheats;

                // save to file
                StringBuilder cheatBuilder = new StringBuilder();
                foreach (var cheat in cheats)
                {
                    cheatBuilder.Append(cheat.Description);
                    cheatBuilder.Append('\n');
                    cheatBuilder.Append(cheat.CheatCode);
                    cheatBuilder.Append('\n');
                    cheatBuilder.Append(cheat.Enabled ? '1' : '0');
                    cheatBuilder.Append('\n');
                }
                IStorageFile file = await this.GetFileUsingExtension(CHEAT_FILE_EXTENSION, true);
                await this.StoreStringInFileAsync(file, cheatBuilder.ToString());
            };

            return helper().AsAsyncAction();
        }

        public IAsyncAction CopyDemoROM(string path)
        {
            try
            {
                return this.copyDemoROM(path).AsAsyncAction();
            }catch(Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e.Message);
#endif
            }
#pragma warning disable CS1998
            Func<Task> def = async () =>
#pragma warning restore CS1998
            {

            };
            return def().AsAsyncAction();
        }

        private async Task copyDemoROM(string path)
        {
            IStorageFile file = await this.GetAssetFileAsync(path);
            if(file != null)
            {
                await file.CopyAsync(this.romDirectory);
                await this.RefreshROMListAsync();
            }
        }

        private async Task<StorageFile> GetTmpFile()
        {
            Random r = new Random();
            int tmpNumber = r.Next(100000, 10000000);
            String tmpName = tmpNumber + ".tmp";
            return await this.romDirectory.CreateFileAsync(tmpName, CreationCollisionOption.OpenIfExists);
        }

        private async Task<IStorageFile> GetFileUsingExtension(String extension, bool create = false)
        {
            var rom = this.currentROM;
            String filename = this.replaceExtension(rom.File.Name, extension);
            IStorageFile file;
            if (create)
            {
                file = await this.romDirectory.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
            }
            else
            {
                file = await this.romDirectory.GetFileAsync(filename);
            }

            return file;
        }

        public IAsyncOperation<SRAMData> GetSRAMDataAsync()
        {
            if (this.currentROM == null)
            {
                throw new InvalidOperationException("No ROM loaded.");
            }
            Func<Task<SRAMData>> helper = async () =>
            {
                IStorageFile file = null;
                try
                {
                    file = await this.GetFileUsingExtension(this.info.SRAMExtension).AsAsyncOperation(); 
                    return new SRAMData() { data = await this.GetBytesFromFileAsync(file) };
                }
                catch(FileNotFoundException)
                {
                    return null;
                }
                catch(UnauthorizedAccessException)
                {
                    if (file != null)
                    {
                        await this.services.MessageService.ShowMessage(
                            String.Format(this.resources.GetString("storageAccessDenied"), file.Name),
                            this.resources.GetString("storageAccessDeniedCaption"));
                    }
                    else
                    {
                        await this.services.MessageService.ShowMessage(
                            this.resources.GetString("storageSRAMAccessUnknownReason"),
                            this.resources.GetString("storageAccessDeniedCaption"));
                    }

                    throw;
                }
            };
            return helper().AsAsyncOperation();
        }

        public IAsyncAction SaveSRAMDataAsync(SRAMData data)
        {
            if (this.currentROM == null)
            {
                throw new InvalidOperationException("No ROM loaded.");
            }
            Func<Task> helper = async () =>
            {
                var file = await this.GetFileUsingExtension(this.info.SRAMExtension, true).AsAsyncOperation();
                await this.WriteBytesToFileAsync(file, data.data);
            };
            return helper().AsAsyncAction();
        }

        public IAsyncOperation<SaveStateData> GetStateDataAsync(int slot)
        {
            if (this.currentROM == null)
            {
                throw new InvalidOperationException("No ROM loaded.");
            }
            Func<Task<SaveStateData>> helper = async () =>
            {
                IStorageFile file = null;
                try
                {
                    file = await this.GetFileUsingExtension(String.Format(this.info.SaveStateExtension, slot)).AsAsyncOperation();
                    return new SaveStateData()
                    {
                        data = await this.GetBytesFromFileAsync(file),
                        slot = slot
                    };
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
                catch (UnauthorizedAccessException)
                {
                    if (file != null)
                    {
                        await this.services.MessageService.ShowMessage(
                            String.Format(this.resources.GetString("storageAccessDenied"), file.Name),
                            this.resources.GetString("storageAccessDeniedCaption"));
                    }
                    else
                    {
                        await this.services.MessageService.ShowMessage(
                            this.resources.GetString("storageStateAccessUnknownReason"),
                            this.resources.GetString("storageAccessDeniedCaption"));
                    }

                    throw;
                }
            };
            return helper().AsAsyncOperation();
        }

        private async Task saveStateDataAsync(SaveStateData data)
        {
            if (this.currentROM == null)
            {
                throw new InvalidOperationException("No ROM loaded.");
            }
            String extension = String.Format(this.info.SaveStateExtension, data.slot);
            var file = await this.GetFileUsingExtension(extension, true);
            await this.WriteBytesToFileAsync(file, data.data);
        }

        public IAsyncAction SaveStateDataAsync(SaveStateData data)
        {
            return this.saveStateDataAsync(data).AsAsyncAction();
        }

        public String GetFileExtension(IStorageFile file)
        {
            String filename = file.Name;

            int index = filename.LastIndexOf('.');
            if (index == -1)
            {
                return null;
            }

            return filename.Substring(index + 1, filename.Length - index - 1);
        }

        public IAsyncOperation<String> ReadFileToStringAsync(IStorageFile file)
        {
            Func<Task<String>> helper = async () =>
            {
                IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
                DataReader reader = new DataReader(stream);
                uint bytesRead = await reader.LoadAsync((uint)stream.Size);
                return reader.ReadString(bytesRead);
            };

            return helper().AsAsyncOperation<String>();
        }

        public IAsyncAction StoreStringInFileAsync(IStorageFile file, String str)
        {
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(str.ToCharArray());

            return this.WriteBytesToFileAsync(file, bytes).AsAsyncAction();

//            Func<Task> helper = async () =>
//            {

//#if DEBUG
//                System.Diagnostics.Debug.WriteLine("WRITE: " + file.Name);
//#endif
//                IStorageFile tmpFile = await this.GetTmpFile();
//#if DEBUG
//                System.Diagnostics.Debug.WriteLine("TEMP FILE: " + tmpFile.Name);
//#endif

//                using (IRandomAccessStream stream = await tmpFile.OpenAsync(FileAccessMode.ReadWrite))
//                {
//                    using (IOutputStream outputStream = stream.GetOutputStreamAt(0L))
//                    {
//                        using (DataWriter writer = new DataWriter(outputStream))
//                        {
//                            writer.WriteString(str);
//                            await writer.StoreAsync();
//                            writer.DetachStream();
//                            await outputStream.FlushAsync();
//                            await tmpFile.MoveAndReplaceAsync(file);
//                        }
//                    }
//                }



//                //IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
//                //DataReader reader = new DataReader(stream);
//                //uint bytesRead = await reader.LoadAsync((uint)stream.Size);
//                //return reader.ReadString(bytesRead);
//            };

//            return helper().AsAsyncAction();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
