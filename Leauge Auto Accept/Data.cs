using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Leauge_Auto_Accept
{
    public class itemList
    {
        public string name { get; set; }
        public string id { get; set; }
        public bool free { get; set; }
    }

    internal class Data
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        public static List<itemList> champsSorted = new List<itemList>();

        public static long currentSummonerId = 0;
        public static string currentChatId = "";

        public static bool loadSummonerId()
        {
            Log.Debug("currentSummonerId={0}", currentSummonerId);
            if (currentSummonerId == 0)
            {
                Log.Info("Loading summonerId from service");
                var currentSummoner = LCU.clientRequestUntilSuccess<LCUTypes.LolSummonerV1CurrentSummoner>("GET", "lol-summoner/v1/current-summoner");
                if (!currentSummoner.IsSuccessStatusCode || currentSummoner.Data == null)
                {
                    Log.Warn("Failed to load summoner id. statusCode={0} responseStatus={1}", currentSummoner.StatusCode, currentSummoner.ResponseStatus);
                    return false;
                }
                currentSummonerId = currentSummoner.Data.SummonerId;
            }
            return true;
        }

        public static bool loadPlayerChatId()
        {
            var chatProfileResp = LCU.clientRequest<LCUTypes.LolChatMeV1>("GET", "lol-chat/v1/me");
            if (!chatProfileResp.IsSuccessStatusCode || chatProfileResp.Data == null)
            {
                Log.Warn("Failed to load player chat id. statusCode={0} responseStatus={1}", chatProfileResp.StatusCode, chatProfileResp.ResponseStatus);
                return false;
            }
            currentChatId = chatProfileResp.Data.Id;
            currentSummonerId = chatProfileResp.Data.SummonerId;
            return true;
        }

        public static bool loadChampionsList()
        {

            Log.Debug("champsSorted.Count={0}", champsSorted?.Count);
            if (!champsSorted.Any())
            {
                Log.Info("Loading available champions list from service");
                if (!loadSummonerId())
                {
                    return false;
                }

                List<itemList> champs = new List<itemList>();

                var ownedChampsResp = LCU.clientRequestUntilSuccess<LCUTypes.LolChampionsInventoriesChampionsMinimalV1[]>("GET", $"lol-champions/v1/inventories/{currentSummonerId}/champions-minimal");
                if (!ownedChampsResp.IsSuccessStatusCode || ownedChampsResp.Data == null)
                {
                    Log.Warn("Failed to load champions list. statusCode={0} responseStatus={1}", ownedChampsResp.StatusCode, ownedChampsResp.ResponseStatus);
                    return false;
                }

                champs = ownedChampsResp.Data
                    // Riot also lists mode-specific copies of champions with ids of 60001 and up (e.g. Jade_Annie)
                    .Where(x => x.Name != "None" && x.Id < 10000)
                    .Select(x => {
                        bool isAvailable = x.Ownership.Owned || x.Ownership.XboxGPReward || x.FreeToPlay;
                        return new itemList() { name = x.Name, id = x.Id.ToString(), free = isAvailable };
                    })
                    .OrderBy(x => x.name)
                    .ToList();

                champsSorted = champs;
            }

            return true;
        }
    }
}
