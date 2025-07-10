using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ImGuiNET;
using System.Runtime.InteropServices;
using GagSpeak.PlayerClient;
using Dalamud.Interface.Colors;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using OtterGui.Classes;
using Penumbra.GameData.Structs;
using CkCommons.Gui;
using Dalamud.Interface.Utility.Raii;

namespace GagSpeak.Utils;
public static class GsExtensions
{
    public static bool HasValidSetup(this GagspeakConfig configuration)
    {
        return configuration.AcknowledgementUnderstood;
    }

    public static bool HasValidSetup(this ServerStorage configuration)
    {
        return configuration.Authentications.Count > 0;
    }

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

    /// <summary> Retrieves the various UID text color based on the current server state. </summary>
    /// <returns> The color of the UID text in Vector4 format .</returns>
    public static Vector4 UidColor()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Connecting => ImGuiColors.DalamudYellow,
            ServerState.Reconnecting => ImGuiColors.DalamudRed,
            ServerState.Connected => ImGuiColors.ParsedPink,
            ServerState.ConnectedDataSynced => ImGuiColors.ParsedPink,
            ServerState.Disconnected => ImGuiColors.DalamudYellow,
            ServerState.Disconnecting => ImGuiColors.DalamudYellow,
            ServerState.Unauthorized => ImGuiColors.DalamudRed,
            ServerState.VersionMisMatch => ImGuiColors.DalamudRed,
            ServerState.Offline => ImGuiColors.DalamudRed,
            ServerState.NoSecretKey => ImGuiColors.DalamudYellow,
            _ => ImGuiColors.DalamudRed
        };
    }

    public static Vector4 ServerStateColor()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Connecting => ImGuiColors.DalamudYellow,
            ServerState.Reconnecting => ImGuiColors.DalamudYellow,
            ServerState.Connected => ImGuiColors.HealerGreen,
            ServerState.ConnectedDataSynced => ImGuiColors.HealerGreen,
            ServerState.Disconnected => ImGuiColors.DalamudRed,
            ServerState.Disconnecting => ImGuiColors.DalamudYellow,
            ServerState.Unauthorized => ImGuiColors.ParsedOrange,
            ServerState.VersionMisMatch => ImGuiColors.ParsedOrange,
            ServerState.Offline => ImGuiColors.DPSRed,
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

    /// <summary> Retrieves the various UID text based on the current server state. </summary>
    /// <returns> The text of the UID.</returns>
    public static string GetUidText()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Reconnecting => "Reconnecting",
            ServerState.Connecting => "Connecting",
            ServerState.Disconnected => "Disconnected",
            ServerState.Disconnecting => "Disconnecting",
            ServerState.Unauthorized => "Unauthorized",
            ServerState.VersionMisMatch => "Version mismatch",
            ServerState.Offline => "Unavailable",
            ServerState.NoSecretKey => "No Secret Key",
            ServerState.Connected => MainHub.DisplayName,
            ServerState.ConnectedDataSynced => MainHub.DisplayName,
            _ => string.Empty
        };
    }

    public static CoreIntifaceElement FromFactoryName(string factoryName)
    {
        return factoryName switch
        {
            "Lovense Ambi" => CoreIntifaceElement.Ambi,
            "Lovense Calor" => CoreIntifaceElement.Calor,
            "Lovense Diamo" => CoreIntifaceElement.Diamo,
            "Lovense Dolce" => CoreIntifaceElement.Dolce,
            "Lovense Domi" => CoreIntifaceElement.Domi,
            "Lovense Domi 2" => CoreIntifaceElement.Domi2,
            "Lovense Edge" => CoreIntifaceElement.Edge,
            "Lovense Edge 2" => CoreIntifaceElement.Edge2,
            "Lovense Exomoon" => CoreIntifaceElement.Exomoon,
            "Lovense Ferri" => CoreIntifaceElement.Ferri,
            "Lovense Flexer" => CoreIntifaceElement.Flexer,
            "Lovense Gemini" => CoreIntifaceElement.Gemini,
            "Lovense Gravity" => CoreIntifaceElement.Gravity,
            "Lovense Gush" => CoreIntifaceElement.Gush,
            "Lovense Gush 2" => CoreIntifaceElement.Gush2,
            "Lovense Hush" => CoreIntifaceElement.Hush,
            "Lovense Hush 2 (1 Inch)" => CoreIntifaceElement.Hush2,
            "Lovense Hush 2 (1.5 Inch)" => CoreIntifaceElement.Hush2,
            "Lovense Hush 2 (1.75 Inch)" => CoreIntifaceElement.Hush2,
            "Lovense Hush 2 (2.25 Inch)" => CoreIntifaceElement.Hush2,
            "Lovense Hyphy" => CoreIntifaceElement.Hyphy,
            "Lovense Lapis" => CoreIntifaceElement.Lapis,
            "Lovense Mini Sex Machine" => CoreIntifaceElement.MiniSexMachine,
            "Lovense Sex Machine" => CoreIntifaceElement.SexMachine,
            "Lovense Lush" => CoreIntifaceElement.Lush,
            "Lovense Lush 2" => CoreIntifaceElement.Lush2,
            "Lovense Lush 3" => CoreIntifaceElement.Lush3,
            "Lovense Lush 4" => CoreIntifaceElement.Lush4,
            "Lovense Max" => CoreIntifaceElement.Max,
            "Lovense Max 2" => CoreIntifaceElement.Max2,
            "Lovense Mission" => CoreIntifaceElement.Mission,
            "Lovense Mission 2" => CoreIntifaceElement.Mission2,
            "Lovense Nora" => CoreIntifaceElement.Nora,
            "Lovense Osci" => CoreIntifaceElement.Osci,
            "Lovense Osci 2" => CoreIntifaceElement.Osci2,
            "Lovense Osci 3" => CoreIntifaceElement.Osci3,
            "Lovense Ridge" => CoreIntifaceElement.Ridge,
            "Lovense Solace" => CoreIntifaceElement.Solace,
            "Lovense Solace Pro" => CoreIntifaceElement.SolacePro,
            "Lovense Tenera 2" => CoreIntifaceElement.Tenera2,
            "Lovense Vulse" => CoreIntifaceElement.Vulse,
            _ => CoreIntifaceElement.UnknownDevice // fallback case to tell us we failed.
        };
    }

    public static string ToFactoryName(this CoreIntifaceElement intifaceItem)
    {
        return intifaceItem switch
        {
            CoreIntifaceElement.Ambi => "Lovense Ambi",
            CoreIntifaceElement.Calor => "Lovense Calor",
            CoreIntifaceElement.Diamo => "Lovense Diamo",
            CoreIntifaceElement.Dolce => "Lovense Dolce",
            CoreIntifaceElement.Domi => "Lovense Domi",
            CoreIntifaceElement.Domi2 => "Lovense Domi 2",
            CoreIntifaceElement.Edge => "Lovense Edge",
            CoreIntifaceElement.Edge2 => "Lovense Edge 2",
            CoreIntifaceElement.Exomoon => "Lovense Exomoon",
            CoreIntifaceElement.Ferri => "Lovense Ferri",
            CoreIntifaceElement.Flexer => "Lovense Flexer",
            CoreIntifaceElement.Gemini => "Lovense Gemini",
            CoreIntifaceElement.Gravity => "Lovense Gravity",
            CoreIntifaceElement.Gush => "Lovense Gush",
            CoreIntifaceElement.Gush2 => "Lovense Gush 2",
            CoreIntifaceElement.Hush => "Lovense Hush",
            CoreIntifaceElement.Hush2 => "Lovense Hush 2 (1.75 Inch)",
            CoreIntifaceElement.Hyphy => "Lovense Hyphy",
            CoreIntifaceElement.Lapis => "Lovense Lapis",
            CoreIntifaceElement.MiniSexMachine => "Lovense Mini Sex Machine",
            CoreIntifaceElement.SexMachine => "Lovense Sex Machine",
            CoreIntifaceElement.Lush => "Lovense Lush",
            CoreIntifaceElement.Lush2 => "Lovense Lush 2",
            CoreIntifaceElement.Lush3 => "Lovense Lush 3",
            CoreIntifaceElement.Lush4 => "Lovense Lush 4",
            CoreIntifaceElement.Max => "Lovense Max",
            CoreIntifaceElement.Max2 => "Lovense Max 2",
            CoreIntifaceElement.Mission => "Lovense Mission",
            CoreIntifaceElement.Mission2 => "Lovense Mission 2",
            CoreIntifaceElement.Nora => "Lovense Nora",
            CoreIntifaceElement.Osci => "Lovense Osci",
            CoreIntifaceElement.Osci2 => "Lovense Osci 2",
            CoreIntifaceElement.Osci3 => "Lovense Osci 3",
            CoreIntifaceElement.Ridge => "Lovense Ridge",
            CoreIntifaceElement.Solace => "Lovense Solace",
            CoreIntifaceElement.SolacePro => "Lovense Solace Pro",
            CoreIntifaceElement.Tenera2 => "Lovense Tenera 2",
            CoreIntifaceElement.Vulse => "Lovense Vulse",
            _ => string.Empty
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

            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            // push the title, converting all color tags into the actual label.
            CkCommons.RichText.CkRichText.Text(item.Title);

            if (!item.Description.IsNullOrWhitespace())
            {
                ImGui.Separator();
                CkCommons.RichText.CkRichText.Text(350f, item.Description);
            }

            ImGui.Separator();
            CkGui.ColorText("Stacks:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Stacks.ToString());
            if (item.StackOnReapply)
            {
                ImGui.SameLine();
                CkGui.ColorText(" (inc by " + item.StacksIncOnReapply + ")", ImGuiColors.ParsedGold);
            }

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text($"{item.Days}d {item.Hours}h {item.Minutes}m {item.Seconds}");

            CkGui.ColorText("Category:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Type.ToString());

            CkGui.ColorText("Dispellable:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Dispelable ? "Yes" : "No");

            if (item.StatusOnDispell != Guid.Empty)
            {
                CkGui.ColorText("StatusOnDispell:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                var status = otherStatuses.FirstOrDefault(x => x.GUID == item.StatusOnDispell).Title ?? "Unknown";
                ImGui.Text(status);
            }

            ImGui.EndTooltip();
        }
    }
}
