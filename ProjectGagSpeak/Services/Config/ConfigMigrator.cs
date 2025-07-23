using CkCommons.GarblerCore;

namespace GagSpeak.Services.Configs;

/// <summary> Migrates all configs before v1.2 to v1.3+ format. </summary>
public static class ConfigMigrator
{
    public static JObject MigrateMainConfig(JObject mainConfig, ConfigFileProvider fileNames)
    {
        Svc.Logger.Warning("Outdated MainConfig detected, migrating to new format!");

        // Create a new JObject for the config section
        var config = new JObject();

        // Map fields from the old format to the new config object
        config["LastRunVersion"] = mainConfig["LastRunVersion"];
        config["LastUidLoggedIn"] = mainConfig["LastUidLoggedIn"];

        // open the server config file and count how many auth objects there are inside of it. If any are present, set acknoledge to true.
        if (File.Exists(fileNames.ServerConfig))
        {
            var json = File.ReadAllText(fileNames.ServerConfig);
            var serverConfig = JObject.Parse(json);
            var authObjects = serverConfig["ServerStorage"]!["Authentications"];
            if (authObjects != null && authObjects.HasValues)
                config["AcknowledgementUnderstood"] = true;
            else
                config["AcknowledgementUnderstood"] = false;
        }
        else
        {
            config["AcknowledgementUnderstood"] = false;
        }

        config["AcknowledgementUnderstood"] = mainConfig["AcknowledgementUnderstood"];
        config["ButtonUsed"] = mainConfig["ButtonUsed"];
        config["EnableDtrEntry"] = mainConfig["EnableDtrEntry"];
        config["ShowPrivacyRadar"] = mainConfig["ShowPrivacyRadar"];
        config["ShowActionNotifs"] = mainConfig["ShowActionNotifs"];
        config["ShowVibeStatus"] = mainConfig["ShowVibeStatus"];
        config["PreferThreeCharaAnonName"] = mainConfig["PreferThreeCharaAnonName"];
        config["PreferNicknamesOverNames"] = mainConfig["PreferNicknamesOverNames"];
        config["ShowVisibleUsersSeparately"] = mainConfig["ShowVisibleUsersSeparately"];
        config["ShowOfflineUsersSeparately"] = mainConfig["ShowOfflineUsersSeparately"];
        config["OpenMainUiOnStartup"] = mainConfig["OpenMainUiOnStartup"];
        config["ShowProfiles"] = mainConfig["ShowProfiles"];
        config["ProfileDelay"] = mainConfig["ProfileDelay"];
        config["ShowContextMenus"] = mainConfig["ShowContextMenus"];

        // Adjust the PuppeteerChannelsBitfield mapping
        // In your conversion process:
        if (mainConfig["ChannelsPuppeteer"] != null)
        {
            // Get the list of channels
            var channelsPuppeteer = mainConfig["ChannelsPuppeteer"]?.ToObject<List<int>>() ?? new List<int>();

            // Convert the channels into a bitfield
            var puppeteerChannelsBitfield = ConvertToBitfield(channelsPuppeteer);

            // Set the bitfield in the config
            config["PuppeteerChannelsBitfield"] = puppeteerChannelsBitfield;
        }
        else
        {
            // If the old format doesn't have the PuppeteerChannelsBitfield, set it to 0
            config["PuppeteerChannelsBitfield"] = 0;
        }

        config["LiveGarblerZoneChangeWarn"] = mainConfig["LiveGarblerZoneChangeWarn"];
        config["NotifyForServerConnections"] = mainConfig["NotifyForServerConnections"];
        config["NotifyForOnlinePairs"] = mainConfig["NotifyForOnlinePairs"];
        config["NotifyLimitToNickedPairs"] = mainConfig["NotifyLimitToNickedPairs"];
        config["InfoNotification"] = mainConfig["InfoNotification"];
        config["WarningNotification"] = mainConfig["WarningNotification"];
        config["ErrorNotification"] = mainConfig["ErrorNotification"];
        config["Safeword"] = mainConfig["Safeword"];
        config["Language"] = mainConfig["Language"];
        // make sure to convert this from the string back to the enum value.
        var dialectString = mainConfig["LanguageDialect"]?.ToString() ?? throw new Exception("LanguageDialect is missing or null");
        config["LanguageDialect"] = JToken.FromObject(dialectString.ToDialect());



        config["CursedLootPanel"] = mainConfig["CursedDungeonLoot"];
        config["RemoveRestrictionOnTimerExpire"] = mainConfig["RemoveGagUponLockExpiration"];
        config["VibratorMode"] = mainConfig["VibratorMode"];
        config["VibeSimAudio"] = mainConfig["VibeSimAudio"];
        config["IntifaceAutoConnect"] = mainConfig["IntifaceAutoConnect"];
        config["IntifaceConnectionSocket"] = mainConfig["IntifaceConnectionSocket"];
        config["PiShockApiKey"] = mainConfig["PiShockApiKey"];
        config["PiShockUsername"] = mainConfig["PiShockUsername"];
        config["BlindfoldStyle"] = mainConfig["BlindfoldStyle"];
        config["ForceLockFirstPerson"] = mainConfig["ForceLockFirstPerson"];
        config["OverlayMaxOpacity"] = mainConfig["OverlayMaxOpacity"];

        // Handle ForcedStayPromptList conversion (keeping it intact)
        config["ForcedStayPromptList"] = mainConfig["ForcedStayPromptList"];

        config["MoveToChambersInEstates"] = mainConfig["MoveToChambersInEstates"];

        // Add the config object to the new format
        var newFormat = new JObject()
        {
            ["Version"] = mainConfig["Version"],
            ["Config"] = JObject.FromObject(config),
            ["LogLevel"] = mainConfig["LogLevel"],
            ["LoggerFilters"] = mainConfig["LoggerFilters"],
        };

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(ConfigFileProvider.GagSpeakDirectory, "config-testing.json.bak*"))
        {
            // Send it to the shadow realm.
            var fileName = Path.GetFileName(file);
            File.Delete(file);
        }

        return newFormat;
    }

    public static JObject MigrateGagRestrictionsConfig(JObject oldConfig, ConfigFileProvider fileNames, string oldPath)
    {
        Svc.Logger.Warning("Outdated GagRestrictionConfig detected, migrating to new format!");

        // Create the new GagRestrictions object
        var gagRestrictions = new JObject();

        // Extract the old GagEquipData from the old config
        var oldGagEquipData = (JObject)oldConfig["GagStorage"]!["GagEquipData"]!;

        // Iterate over the old GagEquipData keys and migrate each entry
        foreach (var gagItem in oldGagEquipData)
        {
            // continue if the key is "None"
            if (gagItem.Key == "None")
                continue;

            var gagName = gagItem.Key;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            var oldGagData = (JObject)gagItem.Value;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            // Create the new structure for the current gag
            var newGagData = new JObject();

            // Map "IsEnabled" to new structure
            newGagData["IsEnabled"] = oldGagData!["IsEnabled"];
            // Build the "Glamour" object and map the old fields
            newGagData["Glamour"] = new JObject()
            {
                ["Slot"] = oldGagData["Slot"],
                ["CustomItemId"] = oldGagData["CustomItemId"],
                ["Stains"] = oldGagData["GameStain"],
            };
            // Build the "Mod" object (with default values)
            newGagData["Mod"] = new JObject();
            // Build the Moodle object with default values. Assume MoodleStatus.
            newGagData["Moodle"] = new JObject()
            {
                ["Type"] = "Status",
                ["Id"] = "00000000-0000-0000-0000-000000000000"
            };

            newGagData["Traits"] = "None";
            newGagData["HeadgearState"] = "null";
            newGagData["VisorState"] = "null";
            newGagData["ProfileGuid"] = oldGagData["CustomizeGuid"];
            newGagData["ProfilePriority"] = oldGagData["CustomizePriority"];
            newGagData["DoRedraw"] = "false";

            // Add the new gag data to the GagRestrictions
            gagRestrictions[gagName] = newGagData;
        }

        // Add the GagRestrictions to the new config
        var newConfig = new JObject()
        {
            ["Version"] = 0,
            ["GagRestrictions"] = gagRestrictions,
        };

        // remove the backups of old versions.
        var oldFormatBackupDir = Path.Combine(fileNames.CurrentPlayerDirectory, "OldFormatBackups");
        if (!Directory.Exists(oldFormatBackupDir))
            Directory.CreateDirectory(oldFormatBackupDir);

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(fileNames.CurrentPlayerDirectory, "gag-storage.json*"))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(oldFormatBackupDir, fileName);

            // Overwrite by deleting first
            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Move(file, destPath);
        }

        return newConfig;
    }

    public static JObject MigrateWardrobeConfig(JObject oldConfig, ConfigFileProvider fileNames, string oldPath)
    {
        Svc.Logger.Warning("Outdated RestraintConfig detected, migrating to new format!");

        var restraintSets = new JArray();
        var oldRestraints = oldConfig["WardrobeStorage"]!["RestraintSets"]!;

        foreach (var restraint in oldRestraints)
        {
            var slots = new JObject();
            foreach (JProperty property in restraint["DrawData"]!)
            {
                var slot = (JObject)property.Value;
                string name = (string)slot["Slot"]!;
                var newslot = new JObject()
                {
                    ["Type"] = "Basic",
                    ["ApplyFlags"] = 33,
                    ["Glamour"] = new JObject()
                    {
                        ["Slot"] = name,
                        ["CustomItemId"] = slot["CustomItemId"],
                        ["Stains"] = slot["GameStain"]
                    }
                };
                slots.Add(new JProperty(name, newslot));
            }
            // There seems to be an issue where the serialized data from old-GS could have written overflow values in here
            // So to prevent issues, double checking the data here.
            UInt16 glasses = 0;
            if (!UInt16.TryParse((string)restraint["BonusDrawData"]![0]!["BonusDrawData"]!["CustomItemId"]!, out glasses))
            {
                glasses = 0;
            }
            // Note: While individual moodles can be saved, presets cannot because it now requires storing the associated moodles.
            var moodles = new JArray();
            foreach (JValue moodle in restraint["AssociatedMoodles"]!)
            {

                var newmoodle = new JObject()
                {
                    ["Type"] = "Status",
                    ["Id"] = moodle
                };
                moodles.Add(newmoodle);
            }
            var newrestraint = new JObject
            {
                ["Identifier"] = (string)restraint["RestraintId"]!,
                ["Label"] = (string)restraint["Name"]!,
                ["Description"] = (string)restraint["Description"]!,
                ["ThumbnailPath"] = "",
                ["DoRedraw"] = false,
                ["RestraintSlots"] = slots,
                ["RestraintLayers"] = new JArray(),
                ["Glasses"] = new JObject()
                {
                    ["Slot"] = "Glasses",
                    ["CustomItemId"] = glasses
                },
                ["MetaStates"] = new JObject()
                {
                    ["Headgear"] = restraint["ForceHeadgear"],
                    ["Visor"] = restraint["ForceVisor"],
                    ["Weapon"] = "null"
                },
                ["BaseMods"] = new JArray(),
                ["BaseMoodles"] = moodles,
                ["BaseTraits"] = "None",
                ["BaseArousal"] = "None",
            }
        ;
            restraintSets.Add(newrestraint);
        }

        // remove the backups of old versions.
        var oldFormatBackupDir = Path.Combine(fileNames.CurrentPlayerDirectory, "OldFormatBackups");
        if (!Directory.Exists(oldFormatBackupDir))
            Directory.CreateDirectory(oldFormatBackupDir);

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(fileNames.CurrentPlayerDirectory, "wardrobe.json*"))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(oldFormatBackupDir, fileName);

            // Overwrite by deleting first
            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Move(file, destPath);

        }
        var newFormat = new JObject()
        {
            ["Version"] = 0,
            ["RestraintSets"] = restraintSets
        };
        return newFormat;
    }

    /// <summary> "Migrates" the few external values. Actual cursed items must be reset. </summary>
    public static JObject MigrateCursedLootConfig(JObject oldConfig, ConfigFileProvider fileNames, string oldPath)
    {
        Svc.Logger.Warning("Outdated CursedLootConfig detected, migrating to new format!");

        // Only Feasible to convert gags, too much work to create new restriction items from given items.
        var items = oldConfig["CursedLootStorage"]!["CursedItems"]!;
        var newItems = new JArray();
        foreach (var item in items)
        {
            if ((bool)item["IsGag"]!)
            {
                var newobj = new JObject() {
                        { "Identifier", (string)item["LootId"]! },
                        { "Label", (string)item["Name"]!},
                        { "InPool", (bool)item["InPool"]! },
                        { "CanOverride", (bool)item["CanOverride"]!},
                        { "Precedence", (string)item["OverridePrecedence"]!},
                        { "RestrictionRef", (string)item["GagType"]! },
                    };
                newItems.Add(newobj);
            }
        }

        var newFormat = new JObject()
        {
            ["Version"] = oldConfig["Version"],
            ["CursedItems"] = newItems,
            ["LockRangeUpper"] = oldConfig["CursedLootStorage"]!["LockRangeUpper"],
            ["LockChance"] = oldConfig["CursedLootStorage"]!["LockChance"],
        };
        // remove the backups of old versions.
        var oldFormatBackupDir = Path.Combine(fileNames.CurrentPlayerDirectory, "OldFormatBackups");
        if (!Directory.Exists(oldFormatBackupDir))
            Directory.CreateDirectory(oldFormatBackupDir);

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(fileNames.CurrentPlayerDirectory, "cursedloot.json*"))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(oldFormatBackupDir, fileName);

            // Overwrite by deleting first
            if (File.Exists(destPath))
                File.Delete(destPath); 
            
            File.Move(file, destPath);
        }

        return newFormat;
    }

    public static JObject MigratePatternConfig(JObject oldConfig, ConfigFileProvider fileNames)
    {
        Svc.Logger.Warning("Outdated PatternConfig detected, migrating to new format!");

        var oldPatternArray = oldConfig["PatternStorage"]!["Patterns"];
        // for each of the pattern objects in the old array, construct a new one to add.
        var newPatternArray = new JArray();

        // if the old pattern array is null, return an empty array.
        if (oldPatternArray is null)
        {
            Svc.Logger.Error("Old pattern array is null, returning empty array.");
            return new JObject()
            {
                ["Version"] = 0,
                ["Patterns"] = new JArray()
            };
        }

        foreach (var oldPattern in oldPatternArray)
        {
            var newPattern = new JObject()
            {
                ["Identifier"] = oldPattern["UniqueIdentifier"],
                ["Label"] = oldPattern["Name"],
                ["Description"] = oldPattern["Description"],
                ["Duration"] = oldPattern["Duration"],
                ["StartPoint"] = oldPattern["StartPoint"],
                ["PlaybackDuration"] = oldPattern["PlaybackDuration"],
                ["ShouldLoop"] = oldPattern["ShouldLoop"],
                ["PatternByteData"] = oldPattern["PatternByteData"],
            };
            newPatternArray.Add(newPattern);

        }

        var newFormat = new JObject()
        {
            ["Version"] = 0,
            ["Patterns"] = newPatternArray
        };

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(ConfigFileProvider.GagSpeakDirectory, "patterns.json.bak*"))
        {
            // Send it to the shadow realm.
            var fileName = Path.GetFileName(file);
            File.Delete(file);
        }

        return newFormat;
    }

    public static JObject MigrateAlarmsConfig(JObject oldConfig, ConfigFileProvider fileNames)
    {
        Svc.Logger.Warning("Outdated AlarmsConfig detected, migrating to new format!");

        var oldAlarmArray = oldConfig["AlarmStorage"]!["Alarms"];
        // for each of the pattern objects in the old array, construct a new one to add.
        var newAlarmArray = new JArray();

        // if the old pattern array is null, return an empty array.
        if (oldAlarmArray is null)
        {
            Svc.Logger.Error("Old alarm array is null, returning empty array.");
            return new JObject()
            {
                ["Version"] = 0,
                ["Alarms"] = new JArray()
            };
        }

        foreach (var oldAlarm in oldAlarmArray)
        {
            var newAlarm = new JObject()
            {
                ["Identifier"] = oldAlarm["Identifier"],
                ["Enabled"] = oldAlarm["Enabled"],
                ["Label"] = oldAlarm["Name"],
                ["SetTimeUTC"] = oldAlarm["SetTimeUTC"],
                ["PatternToPlay"] = oldAlarm["PatternToPlay"],
                ["PatternStartPoint"] = oldAlarm["PatternStartPoint"],
                ["PatternDuration"] = oldAlarm["PatternDuration"],
                ["RepeatFrequency"] = oldAlarm["RepeatFrequency"]
            };
            newAlarmArray.Add(newAlarm);
        }

        var newFormat = new JObject()
        {
            ["Version"] = 0,
            ["Alarms"] = newAlarmArray
        };

        Svc.Logger.Information("New JOBject:" + newFormat.ToString(Formatting.Indented));

        // remove the backups of old versions.
        var oldFormatBackupDir = Path.Combine(fileNames.CurrentPlayerDirectory, "OldFormatBackups");
        if (!Directory.Exists(oldFormatBackupDir))
            Directory.CreateDirectory(oldFormatBackupDir);

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(fileNames.CurrentPlayerDirectory, "alarms.json*"))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(oldFormatBackupDir, fileName);

            // Overwrite by deleting first
            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Move(file, destPath);
        }

        return newFormat;
    }

    public static JObject MigrateTriggersConfig(JObject oldConfig, ConfigFileProvider fileNames, string oldPath)
    {
        Svc.Logger.Warning("Outdated TriggersConfig detected, migrating to new format!");

        var newFormat = new JObject()
        {
            ["Version"] = 0,
            ["Triggers"] = oldConfig["TriggerStorage"]!["Triggers"]
        };

        foreach (var trigger in newFormat["Triggers"]!)
        {
            // Ensure the trigger object has a "Name" field before renaming
            if (trigger["Name"] is JToken nameToken)
            {
                // Rename "Name" to "Label" by directly setting the "Label" property
                trigger["Label"] = nameToken;

                // Remove the "Name" field (directly from the JObject)
                ((JObject)trigger).Remove("Name");
            }

            // replace "ExecutionType" with "ActionType"
            if (trigger["ExecutionType"] is JToken executionTypeToken)
            {
                trigger["ActionType"] = executionTypeToken;
                ((JObject)trigger).Remove("ExecutionType");
            }

            // replace "ExecutableAction" with "InvokableAction"
            if (trigger["ExecutableAction"] is JToken executableActionToken)
            {
                trigger["InvokableAction"] = executableActionToken;
                ((JObject)trigger).Remove("ExecutableAction");
            }

            // This is going to probably have trouble with certain trigger types.
        }

        // remove the backups of old versions.
        var oldFormatBackupDir = Path.Combine(fileNames.CurrentPlayerDirectory, "OldFormatBackups");
        if (!Directory.Exists(oldFormatBackupDir))
            Directory.CreateDirectory(oldFormatBackupDir);

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(fileNames.CurrentPlayerDirectory, "triggers.json*"))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(oldFormatBackupDir, fileName);

            // Overwrite by deleting first
            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Move(file, destPath);
        }

        return newFormat;
    }

    public static JObject MigrateServerConfig(JObject serverConfig, ConfigFileProvider fileNames)
    {
        Svc.Logger.Warning("Outdated ServerConfig detected, migrating to new format!");
        // we should search the directory to see if the servertags.json file exists, and if so remove it.
        var serverTagsPath = Path.Combine(ConfigFileProvider.AssemblyLocation, "servertags.json");
        if (File.Exists(serverTagsPath))
            File.Delete(serverTagsPath);

        // nothing else needed that i can see.
        return serverConfig;
    }

    public static JObject MigrateNicknamesConfig(JObject nicknamesConfig)
    {
        Svc.Logger.Warning("Outdated NicknamesConfig detected, migrating to new format!");

        // Ensure that the "ServerNicknames" object exists
        if (nicknamesConfig["ServerNicknames"] is not JObject)
            nicknamesConfig["ServerNicknames"] = new JObject();

        // Move "UidServerComments" into "Nicknames"
        if (nicknamesConfig["ServerNicknames"]!["UidServerComments"] is JObject oldNicknames)
        {
            nicknamesConfig["ServerNicknames"]!["Nicknames"] = new JObject(oldNicknames);
            nicknamesConfig.Remove("UidServerComments");
        }
        else
        {
            nicknamesConfig["ServerNicknames"]!["Nicknames"] = new JObject(); // Ensure it's always a JObject
        }

        return nicknamesConfig;
    }

    private static JObject? MigrateAction(JObject oldaction)
    {
        var action_type = (int)oldaction["ExecutionType"]!;
        switch (action_type)
        {
            // Text output actions
            case 0:
                return new JObject()
                {
                    ["ActionType"] = 0,
                    ["OutputCommand"] = oldaction["OutputCommand"]
                };
            // Gag actions
            case 1:
                return new JObject()
                {
                    ["ActionType"] = 1,
                    ["LayerIdx"] = -1,
                    ["NewState"] = oldaction["NewState"],
                    ["GagType"] = oldaction["GagType"],
                    ["Padlock"] = 0,
                    ["LowerBound"] = "00:00:00",
                    ["UpperBound"] = "00:00:00"
                };

            // Case for restraint types
            case 2:
                return new JObject()
                {
                    ["ActionType"] = 3,
                    ["NewState"] = oldaction["NewState"],
                    ["RestrictionId"] = oldaction["OutputIdentifier"]
                };

            // Moodleitems
            case 3:
                return new JObject()
                {
                    ["ActionType"] = 4,
                    ["MoodleItem"] = new JObject()
                    {
                        ["Id"] = oldaction["Identifier"]!
                    }
                    ["IsValid"] = true
                };
                // Other options may be in here, but don't have the data to confirm.
        }
        return null;
    }
    public static JObject MigratePuppeteerAliasConfig(JObject oldConfig, ConfigFileProvider fileNames, string oldPath)
    {
        Svc.Logger.Warning("Outdated PuppeteerAliasConfig detected, migrating to new format!");

        var globalStorage = new JArray();
        foreach (JObject aliasitem in oldConfig["GlobalAliasList"]!)
        {
            var actions = new JArray();
            foreach (JProperty actionprop in aliasitem["Executions"]!)
            {
                Svc.Logger.Debug($"{actionprop})");
                JObject oldaction = (JObject)actionprop.Value;
                var action = MigrateAction(oldaction);
                if (action is not null)
                {
                    actions.Add(action);
                }
            }
            var newalias = new JObject()
            {
                ["Identifier"] = aliasitem["AliasIdentifier"]!,
                ["Enabled"] = aliasitem["Enabled"]!,
                ["Label"] = aliasitem["Name"],
                ["InputCommand"] = aliasitem["InputCommand"],
                ["Actions"] = actions
            };
            globalStorage.Add(newalias);
        }
        var pairStorage = new JObject();
        foreach (JProperty pair in oldConfig["AliasStorage"]!)
        {
            var name = pair.Name;
            JObject value = (JObject)pair.Value;
            JArray aliases = new JArray();
            foreach (JObject pairalias in value["AliasList"]!)
            {
                JArray actions = new JArray();
                foreach (JProperty actionprop in pairalias["Executions"]!)
                {
                    var actionobj = (JObject)actionprop.Value;
                    var action = MigrateAction(actionobj);
                    if (action is not null)
                    {
                        actions.Add(action);
                    }
                }
                var newalias = new JObject()
                {
                    ["Identifier"] = pairalias["AliasIdentifier"]!,
                    ["Enabled"] = pairalias["Enabled"]!,
                    ["Label"] = pairalias["Name"],
                    ["InputCommand"] = pairalias["InputCommand"],
                    ["Actions"] = actions
                };
                aliases.Add(newalias);
            }
            var newpair = new JObject()
            {
                ["StoredNameWorld"] = "",
                ["Storage"] = aliases
            };
            pairStorage.Add(new JProperty(name, newpair));
        }
        var newFormat = new JObject()
        {
            ["Version"] = 0,
            ["GlobalStorage"] = globalStorage,
            ["PairStorage"] = pairStorage
        };

        // remove the backups of old versions.
        var oldFormatBackupDir = Path.Combine(fileNames.CurrentPlayerDirectory, "OldFormatBackups");
        if (!Directory.Exists(oldFormatBackupDir))
            Directory.CreateDirectory(oldFormatBackupDir);

        // move all old files into the backup folder.
        foreach (var file in Directory.GetFiles(fileNames.CurrentPlayerDirectory, "alias-lists.json*"))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(oldFormatBackupDir, fileName);

            // Overwrite by deleting first
            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Move(file, destPath);
        }

        return newFormat;

    }
    public static int ConvertToBitfield(List<int> channelsPuppeteer)
    {
        var bitfield = 0;
        foreach (var channel in channelsPuppeteer)
            if (channel >= 0 && channel < 32)  // Assuming channel numbers are between 0 and 31
                bitfield |= (1 << channel);  // Set the corresponding bit
        return bitfield;
    }


}
