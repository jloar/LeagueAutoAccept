using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leauge_Auto_Accept
{
    public class itemList
    {
        public string name { get; set; }
        public string id { get; set; }
        public bool free { get; set; }
    }

    public class spellList
    {
        public string name { get; set; }
        public string id { get; set; }
        public string iconPath { get; set; }
        public List<string> gameModes { get; set; } = new();
    }

    public class runePage
    {
        public string name { get; set; }
        public string id { get; set; }
        public string keystoneName { get; set; }
        public string keystoneIconPath { get; set; }
        public string primaryStyleName { get; set; }
        public string secondaryStyleName { get; set; }

        // The Mobalytics desktop app saves its pages into the client with this prefix
        public bool fromMobalytics => name.StartsWith("Moba:");
    }

    internal class Data
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        public static List<itemList> champsSorted = new List<itemList>();
        public static List<runePage> runesList = new List<runePage>();
        public static List<spellList> spellsSorted = new List<spellList>();

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

        public static bool loadRunesList()
        {
            // Rune pages change whenever the player edits them, so they're always fetched fresh
            Log.Info("Loading rune pages from service");
            var runesResp = LCU.clientRequestUntilSuccess<LCUTypes.LolPerksPagesV1[]>("GET", "lol-perks/v1/pages");
            if (!runesResp.IsSuccessStatusCode || runesResp.Data == null)
            {
                Log.Warn("Failed to load runes list. statusCode={0} responseStatus={1}", runesResp.StatusCode, runesResp.ResponseStatus);
                return false;
            }

            runesList = runesResp.Data
                .Where(x => x.IsValid == true)
                .Select(x => new runePage()
                {
                    name = x.Name,
                    id = x.Id.ToStringInvariant(),
                    keystoneName = x.PageKeystone?.Name,
                    keystoneIconPath = x.PageKeystone?.IconPath,
                    primaryStyleName = x.PrimaryStyleName,
                    secondaryStyleName = x.SecondaryStyleName
                })
                .ToList();
            return true;
        }

        // Makes a rune page the active one in the client, outside of champ select too
        public static bool setCurrentRunePage(string id)
        {
            var resp = LCU.clientRequest("PUT", "lol-perks/v1/currentpage", id);
            if (!resp.IsSuccessStatusCode)
            {
                Log.Warn("Failed to set the current rune page. statusCode={0}", resp.StatusCode);
            }
            return resp.IsSuccessStatusCode;
        }

        public static bool loadSpellsList()
        {
            Log.Debug("spellsSorted.Count={0}", spellsSorted?.Count);
            if (!spellsSorted.Any())
            {
                var spellsResp = LCU.clientRequest<JsonArray>("GET", "lol-game-data/assets/v1/summoner-spells.json");
                if (!spellsResp.IsSuccessStatusCode || spellsResp.Data == null)
                {
                    Log.Warn("Failed to load spells list. statusCode={0} responseStatus={1}", spellsResp.StatusCode, spellsResp.ResponseStatus);
                    return false;
                }

                // Add to list with names and available gamemodes
                var spellsTmp = new List<spellList>(20);
                foreach (var spell in spellsResp.Data)
                {
                    if (spell == null)
                        continue;

                    var gameModes = spell["gameModes"]?.Deserialize<List<string>>() ?? new List<string>();
                    string name = (string)spell["name"] ?? "";

                    // Skip spells with no available gamemodes, and Riot's unused placeholders
                    if (gameModes.Count == 0 || name.Length == 0 || name.StartsWith("Placeholder"))
                        continue;

                    spellsTmp.Add(new spellList()
                    {
                        name = name,
                        id = spell["id"].ToString(),
                        iconPath = (string)spell["iconPath"],
                        gameModes = gameModes
                    });
                }

                // Remove spells from the list for gamemodes where the user has no choice
                foreach (var spell in spellsTmp.ToList())
                {
                    foreach (var mode in spell.gameModes.ToList())
                    {
                        int count = 0;

                        foreach (var otherSpell in spellsTmp)
                        {
                            if (otherSpell.gameModes.Contains(mode))
                                count++;
                        }
                        if (count <= 2)
                            spell.gameModes.Remove(mode);
                    }
                    if (spell.gameModes.Count == 0)
                        spellsTmp.Remove(spell);
                }
                spellsSorted = spellsTmp;
            }
            return true;
        }
    }
}
