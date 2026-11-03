using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using RestSharp;

namespace Leauge_Auto_Accept
{
    internal class Updater
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        public const string releasesUrl = "https://github.com/sweetriverfish/LeagueAutoAccept/releases/latest";

        public static string appVersion;

        // Set once a newer release is found, the window shows it as a banner
        public static string latestTag;

        public static void initialize()
        {
            Version appVersionTmp = Assembly.GetExecutingAssembly().GetName().Version;
            appVersion = appVersionTmp.Major + "." + appVersionTmp.Minor;

            if (!Debugger.IsAttached && !Settings.disableUpdateCheck)
            {
                Task.Run(versionCheck);
            }
        }

        private static void versionCheck()
        {
            var releaseResp = webRequest("https://api.github.com/repos/sweetriverfish/LeagueAutoAccept/releases/latest");
            if (releaseResp == null || releaseResp.IsSuccessStatusCode == false)
            {
                Log.Warn("Failed to check for an update.");
                return;
            }

            try
            {
                var tag = (string)JsonNode.Parse(releaseResp.Content)["tag_name"];

                // Only an actual newer release counts, this build can be ahead of the latest release
                if (Version.TryParse(tag.TrimStart('v'), out Version latest) && latest > Version.Parse(appVersion))
                {
                    latestTag = tag;
                }
            }
            catch (Exception ex)
            {
                // In case github changes the json format or something idk
                Log.Warn(ex, "Failed to read the latest release.");
            }
        }

        public static RestResponse webRequest(string url)
        {
            RestResponse resp = null;

            try
            {
                using (var client = new RestClient(configureDefaultHeaders: headers => {
                    headers.Add("User-Agent", "League_Auto_Accept/" + appVersion);
                }))
                {

                    var request = new RestRequest(url)
                    {
                        Timeout = TimeSpan.FromMilliseconds(5000)
                    };

                    // Get the response
                    resp = client.ExecuteGet(request);
                    resp.ThrowIfError();
                    return resp;
                }
            }
            catch
            {
                return resp;
            }
        }

    }

}
