using GagSpeak;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using Newtonsoft.Json;

namespace ProjectGagSpeak.Tests.GagspeakConfiguration.Services;

public class MainConfig
{
    // This assert is now void because the prompts are static.
    [Fact]
    void GivenJson_WhenDeserializeJsonIsCalled_ThenItShouldParseForcdedStayPromptList()
    {
//        // Arrange
//        const string input = """
//                             {
//                                 "Version": 0,
//                                 "LastRunVersion": "1.2.0.0",
//                                 "LastUidLoggedIn": "",
//                                 "AcknowledgementUnderstood": false,
//                                 "ButtonUsed": false,
//                                 "AccountCreated": false,
//                                 "EnableDtrEntry": false,
//                                 "ShowPrivacyRadar": false,
//                                 "ShowActionNotifs": false,
//                                 "ShowVibeStatus": false,
//                                 "PreferThreeCharaAnonName": false,
//                                 "PreferNicknamesOverNames": false,
//                                 "ShowVisibleUsersSeparately": false,
//                                 "ShowOfflineUsersSeparately": false,
//                                 "OpenMainUiOnStartup": false,
//                                 "ShowProfiles": false,
//                                 "ProfileDelay": 1.5,
//                                 "ShowContextMenus": false,
//                                 "ChannelsGagSpeak": [
//                                     2
//                                 ],
//                                 "ChannelsPuppeteer": [],
//                                 "LiveGarblerZoneChangeWarn": false,
//                                 "NotifyForServerConnections": false,
//                                 "NotifyForOnlinePairs": false,
//                                 "NotifyLimitToNickedPairs": false,
//                                 "LogLevel": 0,
//                                 "LoggerFilters": [
//                                     0,
//                                 ],
//                                 "InfoNotification": 1,
//                                 "WarningNotification": 2,
//                                 "ErrorNotification": 3,
//                                 "Safeword": "",
//                                 "Language": "English",
//                                 "LanguageDialect": "IPA_US",
//                                 "CursedLootPanel": false,
//                                 "RemoveGagUponLockExpiration": false,
//                                 "RevertStyle": 1,
//                                 "DisableSetUponUnlock": false,
//                                 "VibratorMode": 0,
//                                 "VibeSimAudio": 0,
//                                 "IntifaceAutoConnect": false,
//                                 "IntifaceConnectionSocket": "ws://localhost:12345",
//                                 "VibeServerAutoConnect": false,
//                                 "PiShockApiKey": "",
//                                 "PiShockUsername": "",
//                                 "BlindfoldStyle": 0,
//                                 "ForceLockFirstPerson": false,
//                                 "OverlayMaxOpacity": 1.0,
//                                 "ForcedStayPromptList": {
//                                     "$type": "GagSpeak.GameInternals.Addons.TextFolderNode, ProjectGagSpeak",
//                                     "Enabled": true,
//                                     "FriendlyName": "ForcedDeclineList",
//                                     "TargetRestricted": false,
//                                     "TargetNodeName": "",
//                                     "Children": [
//                                         {
//                                         "$type": "GagSpeak.GameInternals.Addons.TextEntryNode, ProjectGagSpeak",
//                                         "Enabled": true,
//                                         "FriendlyName": "[ForcedStay] Prevent Apartment Leaving",
//                                         "TargetRestricted": true,
//                                         "TargetNodeName": "Exit",
//                                         "TargetNodeLabel": "",
//                                         "TargetNodeLabelIsRegex": false,
//                                         "SelectedOptionText": "Cancel"
//                                         }
//                                     ]
//                                 },
//                                 "MoveToChambersInEstates": false
//                             }
//                             """;

//        // Act
//        var res = JsonConvert.DeserializeObject<MainConfig>(input.ToString());

//        // Assert
//#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
//        Assert.Equal(1, MainConfig?.IndoorConfinementPromptList.Children.Count);
//#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
    }
}
