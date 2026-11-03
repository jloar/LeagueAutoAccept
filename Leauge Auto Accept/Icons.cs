using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Leauge_Auto_Accept
{
    // Loads champion and summoner spell icons from the League client and keeps them for the rest of the session
    internal static class Icons
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        private static readonly ConcurrentDictionary<string, Task<ImageSource>> icons = new();
        private static readonly SemaphoreSlim requestLimit = new(6);

        public static Task<ImageSource> Champion(string id)
        {
            return Get($"lol-game-data/assets/v1/champion-icons/{id}.png");
        }

        public static Task<ImageSource> Spell(spellList spell)
        {
            return FromPath(spell.iconPath);
        }

        // For icon paths the client gives out, such as a rune page's keystone
        public static Task<ImageSource> FromPath(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
            {
                return Task.FromResult<ImageSource>(null);
            }

            return Get(iconPath.TrimStart('/'));
        }

        private static Task<ImageSource> Get(string path)
        {
            if (!LCU.isAuthReady)
            {
                return Task.FromResult<ImageSource>(null);
            }

            return icons.GetOrAdd(path, key => Task.Run(() => Load(key)));
        }

        private static async Task<ImageSource> Load(string path)
        {
            await requestLimit.WaitAsync();
            try
            {
                var resp = LCU.clientRequest("GET", path);
                if (resp.IsSuccessStatusCode && resp.RawBytes != null)
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(resp.RawBytes);
                    image.DecodePixelWidth = 96;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to load the icon {0}.", path);
            }
            finally
            {
                requestLimit.Release();
            }

            // Don't keep the failure, the client may just not be open yet
            icons.TryRemove(path, out _);
            return null;
        }
    }
}
