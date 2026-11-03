﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Leauge_Auto_Accept
{
    internal class Settings
    {
        public static string[] currentChamp = { "Unselected", "0" };
        public static string[] currentBackupChamp = { "Unselected", "0" };
        public static string[] secondaryChamp = { "Unselected", "0" };
        public static string[] secondaryBackupChamp = { "Unselected", "0" };
        public static string[] currentBan = { "Unselected", "0" };
        public static string[] currentBackupBan = { "Unselected", "0" };
        public static string[] secondaryBan = { "Unselected", "0" };
        public static string[] secondaryBackupBan = { "Unselected", "0" };
        public static bool bravery = false;
        public static bool banCrowdFavourite = false;
        public static string[] crowdFavouraiteChamp1 = { "Unselected", "0" };
        public static string[] crowdFavouraiteChamp2 = { "Unselected", "0" };
        public static string[] crowdFavouraiteChamp3 = { "Unselected", "0" };
        public static string[] crowdFavouraiteChamp4 = { "Unselected", "0" };
        public static string[] crowdFavouraiteChamp5 = { "Unselected", "0" };
        public static bool chatMessagesEnabled = false;
        public static List<string> chatMessages = new List<string>();
        public static bool saveSettings = false;
        public static bool preloadData = false;
        public static bool instaLock = false;
        public static bool instaBan = false;
        public static bool disableUpdateCheck = false;
        public static bool autoPickOrderTrade = false;
        public static bool instantHover = false;
        public static bool shouldAutoAcceptbeOn = false;
        public static bool autoRestartQueue = false;
        public static bool cancelQueueAfterDodge = false;

        public static int pickStartHoverDelay = 10000;
        public static int pickStartlockDelay = 999999999;
        public static int pickEndlockDelay = 1000;
        public static int banStartHoverDelay = 1500;
        public static int banStartlockDelay = 999999999;
        public static int banEndlockDelay = 1000;
        public static int queueMaxTime = 300000;
        public static int chatMessagesDelay = 100;

        public static void settingsModify(int item)
        {
            switch (item)
            {
                case 0:
                    if (saveSettings && preloadData)
                    {
                        preloadData = !preloadData;
                    }
                    if (saveSettings && disableUpdateCheck)
                    {
                        disableUpdateCheck = !disableUpdateCheck;
                    }
                    saveSettings = !saveSettings;
                    break;
                case 1:
                    if (!preloadData && !saveSettings)
                    {
                        saveSettings = !saveSettings;
                    }
                    preloadData = !preloadData;
                    break;
                case 2:
                    instaLock = !instaLock;
                    break;
                case 3:
                    instaBan = !instaBan;
                    break;
                case 4:
                    if (!disableUpdateCheck && !saveSettings)
                    {
                        saveSettings = !saveSettings;
                    }
                    disableUpdateCheck = !disableUpdateCheck;
                    break;
                case 5:
                    autoPickOrderTrade = !autoPickOrderTrade;
                    break;
                case 6:
                    instantHover = !instantHover;
                    break;
                case 7:
                    autoRestartQueue = !autoRestartQueue;
                    break;
                case 8:
                    cancelQueueAfterDodge = !cancelQueueAfterDodge;
                    break;
            }

            if (saveSettings)
            {
                settingsSave();
            }
            else if (item == 0)
            {
                deleteSettings();
            }
        }

        public static void setDelay(int item, int value)
        {
            value = Math.Clamp(value, 0, 999999999);

            switch (item)
            {
                case 0:
                    pickStartHoverDelay = value;
                    break;
                case 1:
                    pickStartlockDelay = value;
                    break;
                case 2:
                    pickEndlockDelay = value;
                    break;
                case 3:
                    banStartHoverDelay = value;
                    break;
                case 4:
                    banStartlockDelay = value;
                    break;
                case 5:
                    banEndlockDelay = value;
                    break;
                case 6:
                    queueMaxTime = value;
                    break;
                case 7:
                    chatMessagesDelay = value;
                    break;
            }

            if (saveSettings)
            {
                settingsSave();
            }
        }

        public static void saveSelectedChamp(string[] slot, string name, string id)
        {
            slot[0] = name;
            slot[1] = id;

            if (saveSettings)
            {
                settingsSave();
            }
        }

        public static void updateChatMessage(int index, string message)
        {
            if (chatMessages.Count > index)
            {
                chatMessages[index] = message;
            }
            else
            {
                chatMessages.Add(message);
            }
            updateChatMessagesToggle();
            if (saveSettings)
            {
                settingsSave();
            }
        }

        public static void deleteChatMessage(int index)
        {
            if (chatMessages.Count > index)
            {
                chatMessages.RemoveAt(index);
            }
            updateChatMessagesToggle();

            if (saveSettings)
            {
                settingsSave();
            }
        }

        private static void updateChatMessagesToggle()
        {
            if (chatMessages.Count > 0)
            {
                chatMessagesEnabled = true;
            }
            else
            {
                chatMessagesEnabled = false;
            }
        }

        private static string encodeMessagesIntoBase64()
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(string.Join('|', chatMessages));
            string base64String = Convert.ToBase64String(byteArray);

            return base64String;
        }

        private static void decodeMessagesFromBase64(string messages)
        {
            if (messages == "") { return; }
            byte[] byteArray = Convert.FromBase64String(messages);
            string joinedString = Encoding.UTF8.GetString(byteArray);
            chatMessages = new List<string>(joinedString.Split('|'));
        }

        public static void settingsSave()
        {
            string config =
                "champName:" + currentChamp[0] +
                ",champId:" + currentChamp[1] +
                ",champBackupName:" + currentBackupChamp[0] +
                ",champBackupId:" + currentBackupChamp[1] +
                ",secondaryChampName:" + secondaryChamp[0] + 
                ",secondaryChampId:" + secondaryChamp[1] +
                ",secondaryBackupChampName:" + secondaryBackupChamp[0] +
                ",secondaryBackupChampId:" + secondaryBackupChamp[1] +
                ",arenaBravery:" + bravery +
                ",banCrowdFavourite:" + banCrowdFavourite +
                ",arenaCrowdFavourite1Name:" + crowdFavouraiteChamp1[0] +
                ",arenaCrowdFavourite1ChampId:" + crowdFavouraiteChamp1[1] +
                ",arenaCrowdFavourite2Name:" + crowdFavouraiteChamp2[0] +
                ",arenaCrowdFavourite2ChampId:" + crowdFavouraiteChamp2[1] +
                ",arenaCrowdFavourite3Name:" + crowdFavouraiteChamp3[0] +
                ",arenaCrowdFavourite3ChampId:" + crowdFavouraiteChamp3[1] +
                ",arenaCrowdFavourite4Name:" + crowdFavouraiteChamp4[0] +
                ",arenaCrowdFavourite4ChampId:" + crowdFavouraiteChamp4[1] +
                ",arenaCrowdFavourite5Name:" + crowdFavouraiteChamp5[0] +
                ",arenaCrowdFavourite5ChampId:" + crowdFavouraiteChamp5[1] +
                ",banName:" + currentBan[0] +
                ",banId:" + currentBan[1] +
                ",backupBanName:" + currentBackupBan[0] +
                ",backupBanId:" + currentBackupBan[1] +
                ",secondaryBanName:" + secondaryBan[0] +
                ",secondaryBanId:" + secondaryBan[1] +
                ",secondaryBackupBanName:" + secondaryBackupBan[0] +
                ",secondaryBackupBanId:" + secondaryBackupBan[1] +
                ",autoAcceptOn:" + shouldAutoAcceptbeOn +
                ",preloadData:" + preloadData +
                ",instaLock:" + instaLock +
                ",instaBan:" + instaBan +
                ",pickStartHoverDelay:" + pickStartHoverDelay +
                ",pickStartlockDelay:" + pickStartlockDelay +
                ",pickEndlockDelay:" + pickEndlockDelay +
                ",banStartHoverDelay:" + banStartHoverDelay +
                ",banStartlockDelay:" + banStartlockDelay +
                ",banEndlockDelay:" + banEndlockDelay +
                ",queueMaxTime:" + queueMaxTime +
                ",chatMessagesDelay:" + chatMessagesDelay +
                ",autoPickOrderTrade:" + autoPickOrderTrade +
                ",instantHover:" + instantHover +
                ",autoRestartQueue:" + autoRestartQueue +
                ",cancelQueueAfterDodge:" + cancelQueueAfterDodge +
                ",disableUpdateCheck:" + disableUpdateCheck +
                ",chatMessages:" + encodeMessagesIntoBase64();

            string dirParameter = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Leauge Auto Accept Config.txt";
            using (StreamWriter m_WriterParameter = new StreamWriter(dirParameter, false))
            {
                m_WriterParameter.Write(config);
            }
        }

        public static void toggleBraverySetting()
        {
            bravery = !bravery;
            if (saveSettings)
            {
                settingsSave();
            }
        }

        public static void toggleBanCrowdFavouriteSetting()
        {
            banCrowdFavourite = !banCrowdFavourite;
            if (saveSettings)
            {
                settingsSave();
            }
        }

        public static void toggleAutoAcceptSetting()
        {
            shouldAutoAcceptbeOn = !shouldAutoAcceptbeOn;
            MainLogic.isAutoAcceptOn = shouldAutoAcceptbeOn && LCU.isLeagueOpen;
            if (saveSettings)
            {
                settingsSave();
            }
        }

        public static void deleteSettings()
        {
            string dirParameter = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Leauge Auto Accept Config.txt";
            File.Delete(dirParameter);
        }

        public static void loadSettings()
        {
            string dirParameter = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Leauge Auto Accept Config.txt";
            if (File.Exists(dirParameter))
            {
                string text = File.ReadAllText(dirParameter);
                string[] commas = text.Split(',');
                foreach (var comma in commas)
                {
                    string[] columns = comma.Split(':');
                    switch (columns[0])
                    {
                        case "champName":
                            currentChamp[0] = columns[1];
                            break;
                        case "champId":
                            currentChamp[1] = columns[1];
                            break;
                        case "champBackupName":
                            currentBackupChamp[0] = columns[1];
                            break;
                        case "champBackupId":
                            currentBackupChamp[1] = columns[1];
                            break;
                        case "secondaryChampName":
                            secondaryChamp[0] = columns[1];
                            break;   
                        case "secondaryChampId":
                            secondaryChamp[1] = columns[1];
                            break;
                        case "secondaryBackupChampName":
                            secondaryBackupChamp[0] = columns[1];
                            break;
                        case "secondaryBackupChampId":
                            secondaryBackupChamp[1] = columns[1];
                            break;
                        case "arenaBravery":
                            bravery = Boolean.Parse(columns[1]);
                            break;
                        case "banCrowdFavourite":
                            banCrowdFavourite = Boolean.Parse(columns[1]);
                            break;
                        case "arenaCrowdFavourite1Name":
                            crowdFavouraiteChamp1[0] = columns[1];
                            break;
                        case "arenaCrowdFavourite1ChampId":
                            crowdFavouraiteChamp1[1] = columns[1];
                            break;
                        case "arenaCrowdFavourite2Name":
                            crowdFavouraiteChamp2[0] = columns[1];
                            break;
                        case "arenaCrowdFavourite2ChampId":
                            crowdFavouraiteChamp2[1] = columns[1];
                            break;
                        case "arenaCrowdFavourite3Name":
                            crowdFavouraiteChamp3[0] = columns[1];
                            break;
                        case "arenaCrowdFavourite3ChampId":
                            crowdFavouraiteChamp3[1] = columns[1];
                            break;
                        case "arenaCrowdFavourite4Name":
                            crowdFavouraiteChamp4[0] = columns[1];
                            break;
                        case "arenaCrowdFavourite4ChampId":
                            crowdFavouraiteChamp4[1] = columns[1];
                            break;
                        case "arenaCrowdFavourite5Name":
                            crowdFavouraiteChamp5[0] = columns[1];
                            break;
                        case "arenaCrowdFavourite5ChampId":
                            crowdFavouraiteChamp5[1] = columns[1];
                            break;
                        case "banName":
                            currentBan[0] = columns[1];
                            break;
                        case "banId":
                            currentBan[1] = columns[1];
                            break;
                        case "backupBanName":
                            currentBackupBan[0] = columns[1];
                            break;
                        case "backupBanId":
                            currentBackupBan[1] = columns[1];
                            break;
                        case "secondaryBanName":
                            secondaryBan[0] = columns[1];
                            break;
                        case "secondaryBanId":
                            secondaryBan[1] = columns[1];
                            break;
                        case "secondaryBackupBanName":
                            secondaryBackupBan[0] = columns[1];
                            break;
                        case "secondaryBackupBanId":
                            secondaryBackupBan[1] = columns[1];
                            break;
                        case "pickStartHoverDelay":
                            pickStartHoverDelay = Int32.Parse(columns[1]);
                            break;
                        case "pickStartlockDelay":
                            pickStartlockDelay = Int32.Parse(columns[1]);
                            break;
                        case "pickEndlockDelay":
                            pickEndlockDelay = Int32.Parse(columns[1]);
                            break;
                        case "banStartHoverDelay":
                            banStartHoverDelay = Int32.Parse(columns[1]);
                            break;
                        case "banStartlockDelay":
                            banStartlockDelay = Int32.Parse(columns[1]);
                            break;
                        case "banEndlockDelay":
                            banEndlockDelay = Int32.Parse(columns[1]);
                            break;
                        case "queueMaxTime":
                            queueMaxTime = Int32.Parse(columns[1]);
                            break;
                        case "chatMessagesDelay":
                            chatMessagesDelay = Int32.Parse(columns[1]);
                            break;
                        case "autoAcceptOn":
                            shouldAutoAcceptbeOn = Boolean.Parse(columns[1]);
                            break;
                        case "preloadData":
                            preloadData = Boolean.Parse(columns[1]);
                            break;
                        case "instaLock":
                            instaLock = Boolean.Parse(columns[1]);
                            break;
                        case "instaBan":
                            instaBan = Boolean.Parse(columns[1]);
                            break;
                        case "disableUpdateCheck":
                            disableUpdateCheck = Boolean.Parse(columns[1]);
                            break;
                        case "autoPickOrderTrade":
                            autoPickOrderTrade = Boolean.Parse(columns[1]);
                            break;
                        case "instantHover":
                            instantHover = Boolean.Parse(columns[1]);
                            break;
                        case "autoRestartQueue":
                            autoRestartQueue = Boolean.Parse(columns[1]);
                            break;
                        case "cancelQueueAfterDodge":
                            cancelQueueAfterDodge = Boolean.Parse(columns[1]);
                            break;
                        case "chatMessages":
                            decodeMessagesFromBase64(columns[1]);
                            updateChatMessagesToggle();
                            break;
                    }
                    saveSettings = true;
                }
            }
        }
    }
}
