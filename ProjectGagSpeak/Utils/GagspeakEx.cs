using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using Penumbra.GameData.Structs;
using System.Runtime.InteropServices;

namespace GagSpeak.Utils;
public static class GagspeakEx
{
    /// <summary>
    ///     A reliable Player.Interactable, that also waits on the loading screen to finish. <para />
    ///     Useful when waiting on player loading for UI manipulation and interactions.
    /// </summary>
    public static async Task WaitForPlayerLoading()
    {
        while (!await Svc.Framework.RunOnFrameworkThread(IsPlayerFullyLoaded).ConfigureAwait(false))
        {
            await Task.Delay(100).ConfigureAwait(false);
        }
    }

    public static bool IsPlayerFullyLoaded()
        => PlayerData.Interactable && HcTaskUtils.IsScreenReady();

    public static bool HasValidSetup(this GagspeakConfig configuration)
        => configuration.AcknowledgementUnderstood;
    public static bool HasValidSetup(this AccountStorage configuration)
        => configuration.Profiles.Count > 0;

    public static string AsAnonKinkster(this string kinksterUid, bool isLegacy = true)
        => $"Kinkster-{kinksterUid[^(isLegacy ? 4 : 3)..]}";

    /// <summary> Linearly interpolates between two values based on a factor t. </summary>
    /// <remarks> Think, “What number is 35% between 56 and 132?" </remarks>
    /// <param name="a"> lower bound value </param>
    /// <param name="b"> upper bound value </param>
    /// <param name="t"> should be in the range [a, b] </param>
    /// <returns> the interpolated value between a and b </returns>
    public static float Lerp(float a, float b, float t) 
        => a + (b - a) * t;

    public static float EaseInExpo(float t) 
        => t <= 0f ? 0f : MathF.Pow(2f, 10f * (t - 1f));

    public static float EaseOutExpo(float t)
        => t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);

    public static float EaseInOutSine(float t)
        => (1f - MathF.Cos(t * MathF.PI)) * 0.5f;



    public static string ExtractText(this SeString seStr, bool onlyFirst = false)
    {
        StringBuilder sb = new();
        foreach (var x in seStr.Payloads)
        {
            if (x is TextPayload tp)
            {
                sb.Append(tp.Text);
                if (onlyFirst) break;
            }
            if (x.Type == PayloadType.Unknown && x.Encode().SequenceEqual<byte>([0x02, 0x1d, 0x01, 0x03]))
            {
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }

    public unsafe static string Read(this Span<byte> bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0)
            {
                fixed (byte* ptr = bytes)
                {
                    return Marshal.PtrToStringUTF8((nint)ptr, i);
                }
            }
        }
        fixed (byte* ptr = bytes)
        {
            return Marshal.PtrToStringUTF8((nint)ptr, bytes.Length);
        }
    }

    public static Vector4 ToColor(this Precedence precedence)
        => precedence switch
        {
            Precedence.VeryLow => ImGuiColors.ParsedPurple,
            Precedence.Low => ImGuiColors.TankBlue,
            Precedence.Default => ImGuiColors.ParsedGold.Darken(.15f),
            Precedence.High => ImGuiColors.DalamudOrange,
            Precedence.VeryHigh => ImGuiColors.DalamudRed,
            Precedence.Highest => ImGuiColors.DPSRed,
            _ => ImGuiColors.DalamudGrey,
        };

    public static Vector4 ServerStateColor()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Connecting => ImGuiColors.DalamudYellow,
            ServerState.Reconnecting => ImGuiColors.DalamudYellow,
            ServerState.Connected => ImGuiColors.HealerGreen,
            ServerState.ConnectedDataSynced => ImGuiColors.HealerGreen,
            ServerState.Disconnected => CkCol.TriStateCross.Vec4Ref(),
            ServerState.Disconnecting => ImGuiColors.DalamudYellow,
            ServerState.Unauthorized => ImGuiColors.ParsedOrange,
            ServerState.VersionMisMatch => ImGuiColors.ParsedOrange,
            ServerState.Offline => CkCol.TriStateCross.Vec4Ref(),
            ServerState.NoSecretKey => ImGuiColors.ParsedOrange,
            _ => ImGuiColors.ParsedOrange
        };
    }

    public static FAI ServerStateIcon(ServerState state)
    {
        return state switch
        {
            ServerState.Connecting => FAI.SatelliteDish,
            ServerState.Reconnecting => FAI.SatelliteDish,
            ServerState.Connected => FAI.Link,
            ServerState.ConnectedDataSynced => FAI.Link,
            ServerState.Disconnected => FAI.Unlink,
            ServerState.Disconnecting => FAI.SatelliteDish,
            ServerState.Unauthorized => FAI.Shield,
            ServerState.VersionMisMatch => FAI.Unlink,
            ServerState.Offline => FAI.Signal,
            ServerState.NoSecretKey => FAI.Key,
            _ => FAI.ExclamationTriangle
        };
    }

    public static string GetCenterStateText()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Reconnecting => "Reconnecting",
            ServerState.Connecting => "Connecting",
            ServerState.Disconnected => "Disconnected",
            ServerState.Disconnecting => "Disconnecting",
            ServerState.Unauthorized => "Unauthorized",
            ServerState.VersionMisMatch => "Version Version",
            ServerState.Offline => "Offline",
            ServerState.NoSecretKey => "No Secret Key",
            ServerState.Connected => "Connected",
            ServerState.ConnectedDataSynced => "Connected",
            _ => "UNK-STATE"
        };
    }

    public static string GetButtonStateText()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Reconnecting        => "Connecting",
            ServerState.Connecting          => "Connecting",
            ServerState.Disconnected        => "Offline",
            ServerState.Disconnecting       => "Aborting",
            ServerState.Unauthorized        => "Bad Auth",
            ServerState.VersionMisMatch     => "Missmatch",
            ServerState.Offline             => "Offline",
            ServerState.NoSecretKey         => "No Key",
            ServerState.Connected           => "Connected",
            ServerState.ConnectedDataSynced => "Connected",
            _ => "UNK-STATE"
        };
    }

    public static string ToName(this LimitedActionEffectType type)
    {
        return type switch
        {
            LimitedActionEffectType.Nothing => "Anything",
            LimitedActionEffectType.Miss => "Action Missed",
            LimitedActionEffectType.Damage => "Damage Related",
            LimitedActionEffectType.Heal => "Heal Related",
            LimitedActionEffectType.BlockedDamage => "Damage Blocked",
            LimitedActionEffectType.ParriedDamage => "Damage Parried",
            LimitedActionEffectType.Attract1 => "Rescue Used",
            LimitedActionEffectType.Knockback => "Pushed Back",
            _ => "UNK"
        };
    }

    public static JObject Serialize(this Moodle moodle)
    {
        var type = moodle is MoodlePreset ? MoodleType.Preset : MoodleType.Status;

        var json = new JObject
        {
            ["Type"] = type.ToString(),
            ["Id"] = moodle.Id.ToString(),
        };

        if (moodle is MoodlePreset moodlePreset)
        {
            json["StatusIds"] = new JArray(moodlePreset.StatusIds.Select(x => x.ToString()));
        }

        return json;
    }

    public static Moodle LoadMoodle(JToken? token)
    {
        if (token is not JObject jsonObject)
            throw new ArgumentException("Invalid JObjectToken!");

        var type = Enum.TryParse<MoodleType>(jsonObject["Type"]?.Value<string>(), out var moodleType) ? moodleType : MoodleType.Status;
        Guid id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
        IEnumerable<Guid> statusIds = jsonObject["StatusIds"]?.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>();
        return type switch
        {
            MoodleType.Preset => new MoodlePreset(id, statusIds),
            _ => new Moodle(id)
        };
    }

    public static StainIds ParseCompactStainIds(JToken? jToken)
    {
        if (jToken is not JObject stainJson)
            return StainIds.None;

        var result = StainIds.None;
        var gameStainString = (stainJson["Stains"]?.Value<string>() ?? "0,0").Split(',');
        return gameStainString.Length == 2
               && int.TryParse(gameStainString[0], out int stain1)
               && int.TryParse(gameStainString[1], out int stain2)
            ? new StainIds((StainId)stain1, (StainId)stain2)
            : StainIds.None;
    }

    public static void DrawMoodleStatusTooltip(MoodlesStatusInfo item, IEnumerable<MoodlesStatusInfo> otherStatuses)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(350f, 0f), new Vector2(350f, float.MaxValue));
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c =ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            // push the title, converting all color tags into the actual label.
            CkCommons.RichText.CkRichText.Text(item.Title, cloneId: 100);

            if (!item.Description.IsNullOrWhitespace())
            {
                ImGui.Separator();
                CkCommons.RichText.CkRichText.Text(350f, item.Description);
            }

            // This has... heavily changed... lol.
            ImGui.Separator();
            CkGui.ColorText("Stacks:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Stacks.ToString());
            //if (item.StackOnReapply)
            //{
            //    ImGui.SameLine();
            //    CkGui.ColorText(" (inc by " + item.StacksIncOnReapply + ")", ImGuiColors.ParsedGold);
            //}

            //CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            //ImGui.SameLine();
            //ImGui.Text($"{item.Days}d {item.Hours}h {item.Minutes}m {item.Seconds}");

            //CkGui.ColorText("Category:", ImGuiColors.ParsedGold);
            //ImGui.SameLine();
            //ImGui.Text(item.Type.ToString());

            //CkGui.ColorText("Dispellable:", ImGuiColors.ParsedGold);
            //ImGui.SameLine();
            //ImGui.Text(item.Dispelable ? "Yes" : "No");

            //if (item.StatusOnDispell != Guid.Empty)
            //{
            //    CkGui.ColorText("StatusOnDispell:", ImGuiColors.ParsedGold);
            //    ImGui.SameLine();
            //    var status = otherStatuses.FirstOrDefault(x => x.GUID == item.StatusOnDispell).Title ?? "Unknown";
            //    ImGui.Text(status);
            //}

            ImGui.EndTooltip();
        }
    }
}
