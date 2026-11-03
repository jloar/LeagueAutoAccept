using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Leauge_Auto_Accept
{
    // Loads champion portraits from the League client and keeps them for the rest of the session
    internal static class ChampionIcons
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        private static readonly ConcurrentDictionary<string, Task<ImageSource>> icons = new();
        private static readonly SemaphoreSlim requestLimit = new(6);

        public static Task<ImageSource> Get(string id)
        {
            if (!LCU.isAuthReady)
            {
                return Task.FromResult<ImageSource>(null);
            }

            return icons.GetOrAdd(id, key => Task.Run(() => Load(key)));
        }

        private static async Task<ImageSource> Load(string id)
        {
            await requestLimit.WaitAsync();
            try
            {
                var resp = LCU.clientRequest("GET", $"lol-game-data/assets/v1/champion-icons/{id}.png");
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
                Log.Warn(ex, "Failed to load the portrait for champion {0}.", id);
            }
            finally
            {
                requestLimit.Release();
            }

            // Don't keep the failure, the client may just not be open yet
            icons.TryRemove(id, out _);
            return null;
        }
    }
}
