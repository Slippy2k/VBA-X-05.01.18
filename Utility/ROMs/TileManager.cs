using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.StartScreen;

namespace Utility
{
    public sealed class TileManager
    {
        private static TileManager instance = null;

        public static TileManager Current
        {
            get
            {
                if(instance == null)
                {
                    instance = new TileManager();
                }
                return instance;
            }
        }

        private TileManager()
        {

        }

        public bool HasTilePinned(ROMEntry entry)
        {
            return false;
        }

        public IAsyncOperation<bool> PinTile(ROMEntry entry)
        {
            Func<Task<bool>> helper = async () =>
            {
                var snapshotPath = StorageManager.Current.GetSnapshotPath(entry.File.Name);

                SecondaryTile tile = new SecondaryTile(/*entry.Name, "123", arguments, new Uri("ms-appx:///Assets/defaultSnapshotGBA.png"), TileSize.Square150x150 | TileSize.Wide310x150 | TileSize.Square70x70*/);
                tile.TileId = generateTileId(entry);
                tile.Arguments = generateTileArguments(entry);
                tile.DisplayName = generateDisplayName(entry);
                tile.RoamingEnabled = false;
                tile.TileOptions = TileOptions.ShowNameOnLogo | TileOptions.ShowNameOnWideLogo;
                tile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/defaultSnapshotGBA.png");

                return await tile.RequestCreateAsync();
            };
            return helper().AsAsyncOperation();
        }

        public bool HasPinnedTile(ROMEntry entry)
        {
            return false;
        }
        
        public IAsyncAction UnpinTile(ROMEntry entry)
        {
            Func<Task> helper = async () =>
            {

            };
            return helper().AsAsyncAction();
        }

        private static string generateDisplayName(ROMEntry entry)
        {
            return entry.Name;
        }

        private static string generateTileArguments(ROMEntry entry)
        {
            return "launch-rom:" + entry.Name;
        }

        private static string generateTileId(ROMEntry entry)
        {
            string name = entry.Name;
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if((c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c >= '0' && c <= '9') ||
                   (c == '.'))
                {
                    sb.Append(c);
                }else if(c == ' ')
                {
                    sb.Append('_');
                }
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Tile ID: " + sb.ToString());
#endif
            return sb.ToString();
        }
    }
}
