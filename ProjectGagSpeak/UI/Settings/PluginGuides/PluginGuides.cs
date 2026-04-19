using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Interop.Helpers;
using GagSpeak.Services;
using OtterGui.Text;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Gui;

public sealed class PluginGuideProvider : IDisposable
{
    internal static Dictionary<OptionalPlugin, OptionalPluginInfo> PluginInfo = new();
    internal static ConcurrentDictionary<string, Task<IDalamudTextureWrap>> Cache = new();

    internal static HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public PluginGuideProvider()
    {
        Init();
    }

    public void Dispose()
    {
        _http.Dispose();
        foreach (var item in Cache.Values)
        {
            try
            {
                if (item.IsCompletedSuccessfully)
                    item.Result?.Dispose();
            }
            catch { }
        }
        Cache.Clear();
    }

    public OptionalPluginInfo? GetPluginInfo(OptionalPlugin plugin)
        => PluginInfo.TryGetValue(plugin, out var info) ? info : null;

    public void DrawCenterButtonRow(OptionalPlugin plugin)
    {
        if (!PluginInfo.TryGetValue(plugin, out var details))
            return;

        if (string.IsNullOrEmpty(details.DiscordUrl) || string.IsNullOrEmpty(details.GithubUrl))
            return;

        var buttonSize = new Vector2(120f * ImGuiHelpers.GlobalScale, 0);
        var repoLinkSize = CkGui.IconTextButtonSize(FAI.Copy, "Plugin Repo Link");
        var centerW = (buttonSize.X + ImUtf8.ItemSpacing.X) * 2 + repoLinkSize;

        CkGui.SetCursorXtoCenter(centerW);
        using (ImRaii.PushColor(ImGuiCol.Button, 0xFFDA8972))
            if (ImGui.Button("Discord", buttonSize))
                Util.OpenLink(details.DiscordUrl!);
        CkGui.AttachTooltip(details.DiscordTooltip, CkCol.TriStateCross.Vec4Ref());

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Button, 0xFFD5449D))
            if (ImGui.Button("GitHub", buttonSize))
                Util.OpenLink(details.GithubUrl!);
        CkGui.AttachTooltip($"View the GitHub repository for {details.Name}");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Copy, "Plugin Repo Link"))
            ImGui.SetClipboardText(details.RepoUrl);
        CkGui.AttachTooltip("Copies the plugin repository link.");
    }

    // Details about selected optional plugins.
    public void DrawOptionalPluginDetails(OptionalPlugin plugin, float width)
    {
        if (!PluginInfo.TryGetValue(plugin, out var details))
            return;

        var isCordyProject = plugin is OptionalPlugin.Sundouleia or OptionalPlugin.Loci;
        var bulletCnt = details.BulletInfo.Count + (isCordyProject ? 1 : 0);
        var headerH = ImUtf8.FrameHeightSpacing * 2 + CkGui.CalcFontTextSize("A", Fonts.Default150Percent).Y;
        var contextH = ImUtf8.TextHeightSpacing * bulletCnt;
        var imgSize = new Vector2(headerH);

        using var _ = CkRaii.FramedChildPaddedW("optional-plugin-info", width, headerH + contextH, 0, CkCol.Favorite.Uint(), CkStyle.ChildRounding());
        // Fetch the icon image from a link
        using (ImRaii.Group())
        {
            if (TryGetOnlineImage(details.IconUrl, out var wrap))
                ImGui.Image(wrap.Handle, imgSize);
            else
                ImGui.Dummy(imgSize);

            ImGui.SameLine();
            using (ImRaii.Group())
            {
                // Title + punchline
                CkGui.FontText(details.Name, Fonts.Default150Percent);
                CkGui.ColorTextFrameAligned(details.Punchline, ImGuiColors.DalamudViolet);

                var buttonSize = new Vector2(80f * ImGuiHelpers.GlobalScale, 0);

                if (!string.IsNullOrEmpty(details.DiscordUrl))
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, 0xFFDA8972))
                        if (ImGui.Button("Discord", buttonSize))
                            Util.OpenLink(details.DiscordUrl);
                    if (!string.IsNullOrEmpty(details.DiscordTooltip))
                        CkGui.AttachTooltip(details.DiscordTooltip, CkCol.TriStateCross.Vec4Ref());

                    ImGui.SameLine();
                }

                // GitHub button (optional)
                if (!string.IsNullOrEmpty(details.GithubUrl))
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, 0xFFD5449D))
                        if (ImGui.Button("GitHub", buttonSize))
                            Util.OpenLink(details.GithubUrl);
                    CkGui.AttachTooltip($"View the GitHub repository for {details.Name}");
                    ImGui.SameLine();
                }

                // Repo link copy (Handle Intiface specially here)
                if (CkGui.IconTextButton(FAI.Copy, "Plugin Repo Link"))
                    ImGui.SetClipboardText(details.RepoUrl);
                CkGui.AttachTooltip("Copies the plugin repository link.");
            }

            // Do the bullet list thing.
            foreach (var bullet in details.BulletInfo)
                ImGui.BulletText(bullet);
            if (plugin is (OptionalPlugin.Sundouleia or OptionalPlugin.Loci))
                CkGui.BulletText("For more details, view the [Cordys Projects] tab below.", ImGuiColors.DalamudViolet);
        }
    }

    /// <summary>
    ///   Get the loaded image, or start the process of loading one from a URL
    /// </summary>
    public bool TryGetOnlineImage(string url, [NotNullWhen(true)] out IDalamudTextureWrap? texture)
    {
        var task = Cache.GetOrAdd(url, LoadHttpImageAsync);
        if (!task.IsCompletedSuccessfully)
        {
            texture = null;
            return false;
        }
        // Otherwise it finished so return the result
        texture = task.Result;
        return true;
    }

    private static async Task<IDalamudTextureWrap> LoadHttpImageAsync(string url)
    {
        Svc.Logger.Information($"Loading image: {url}");
        var bytes = await _http.GetByteArrayAsync(url).ConfigureAwait(false);
        return await Svc.Texture.CreateFromImageAsync(bytes).ConfigureAwait(false);
    }

    private static void Init()
    {
        PluginInfo = new()
        {
            [OptionalPlugin.Penumbra] = new OptionalPluginInfo
            {
                IconUrl = "https://raw.githubusercontent.com/xivdev/Penumbra/master/images/icon.png",
                Name = "Penumbra",
                Author = "Ottermandias",
                Punchline = "Primary runtime loader for modding your game.",

                DiscordUrl = "https://discord.gg/kVva7DHV4r",
                DiscordTooltip = "Opens the Penumbra & Glamourer Discord--SEP----COL--DO NOT USE THIS TO REPORT GS ISSUES.--COL--",

                GithubUrl = "https://github.com/xivdev/Penumbra",
                RepoUrl = "https://raw.githubusercontent.com/Ottermandias/SeaOfStars/main/repo.json",

                BulletInfo =
                [
                    "Automatically toggle mods on or off through restraints.",
                    "Enable mods with certain settings, via ModPresets.",
                    "Perform forced automatic redraws for fast animation sync."
                ]
            },
            [OptionalPlugin.Glamourer] = new OptionalPluginInfo
            {
                IconUrl = "https://raw.githubusercontent.com/Ottermandias/Glamourer/main/images/icon.png",
                Name = "Glamourer",
                Author = "Ottermandias",
                Punchline = "Appearance editor for equipment & character customizations.",

                DiscordUrl = "https://discord.gg/kVva7DHV4r",
                DiscordTooltip = "Opens the Penumbra & Glamourer Discord--SEP----COL--DO NOT USE THIS TO REPORT GS ISSUES.--COL--",

                GithubUrl = "https://github.com/Ottermandias/Glamourer",
                RepoUrl = "https://raw.githubusercontent.com/Ottermandias/SeaOfStars/main/repo.json",

                BulletInfo =
                [
                    "Link equipment & customization updates to GagSpeak items.",
                    "'Locks' the items in place, keeping you trapped in them until freed.",
                    "A core essential to GagSpeaks immersion feeling and must have."
                ]
            },
            [OptionalPlugin.CustomizePlus] = new OptionalPluginInfo
            {
                IconUrl = "https://raw.githubusercontent.com/Aether-Tools/CustomizePlus/main/Data/icon.png",
                Name = "Customize Plus",
                Author = "RisaDev",
                Punchline = "Applies character bone manipulations during gameplay.",

                DiscordUrl = "https://discord.gg/KvGJCCnG8t",
                DiscordTooltip = "Opens the Aetherworks Discord--SEP----COL--DO NOT USE THIS TO REPORT GS ISSUES.--COL--",
                
                GithubUrl = "https://github.com/Aether-Tools/CustomizePlus",
                RepoUrl = "https://raw.githubusercontent.com/Ottermandias/SeaOfStars/main/repo.json",

                BulletInfo =
                [
                    "Mostly used to attached 'Gagged Expression' profiles with Gag items.",
                    "Profiles are 'Locked' by automatically reapply when you remove them."
                ]
            },
            [OptionalPlugin.Loci] = new OptionalPluginInfo
            {
                IconUrl = "https://raw.githubusercontent.com/CordeliaMist/Loci/main/Assets/icon_square.png",
                Name = "Loci",
                Author = "Cordelia",
                Punchline = "A powerful modern tool to create, customize, and manage status icons.",

                DiscordUrl = "https://discord.gg/QJy4zTqpMD",
                DiscordTooltip = "Opens the Sundouleia & Loci Discord--SEP----COL--DO NOT USE THIS TO REPORT GS ISSUES.--COL--",
                GithubUrl = "https://github.com/CordeliaMist/Loci",

                RepoUrl = "https://raw.githubusercontent.com/CordeliaMist/Loci/main/repo.json",

                BulletInfo =
                [
                    "Attach statuses to bondage items, applying them while active.",
                    "Applied statuses are Locked until removed, and cannot be clicked off.",
                    "Interact with GagSpeaks LociShareHub, try-on, or publish Loci Statuses!",
                    "Apply Loci Statuses & Presets to other kinksters!",
                ]
            },
            [OptionalPlugin.Lifestream] = new OptionalPluginInfo
            {
                IconUrl = "https://raw.githubusercontent.com/NightmareXIV/Lifestream/main/Lifestream/images/icon.png",
                Name = "Lifestream",
                Author = "Limiana",
                Punchline = "A plugin to speed up Aethernet travel and World changing.",

                DiscordUrl = "https://discord.gg/BeeRFKDJD3",
                DiscordTooltip = "Opens the NightmareXIV Discord Server",

                GithubUrl = "https://github.com/NightmareXIV/Lifestream",

                RepoUrl = "https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json",

                BulletInfo =
                [
                    "Used for Hardcore functionality only.",
                    "Hardcore Confinement uses Lifestream to force kinksters to locations.",
                    "During forced confined travel, the Kinkster cannot control themselves."
                ]
            },
            [OptionalPlugin.Intiface] = new OptionalPluginInfo
            {
                IconUrl = "https://raw.githubusercontent.com/intiface/intiface-central/main/assets/icons/intiface_central_icon.png",
                Name = "Intiface Central",
                Author = "qDot",
                Punchline = "Applications for accessing & controlling intimate toys",

                // No github for here
                GithubUrl = "https://github.com/intiface/intiface-central",

                RepoUrl = string.Empty,

                BulletInfo =
                [
                    "GagSpeaks Remote connects & interacts with your toys via Intiface.",
                    "Patterns can control individual motors and multiple devices",
                    "Alarms in GagSpeak can link to your toys",
                ]
            },
            [OptionalPlugin.Sundouleia] = new OptionalPluginInfo
            {
                IconUrl = "https://raw.githubusercontent.com/Sundouleia/repo/main/Images/icon.png",
                Name = "Sundouleia",
                Author = "Cordelia",
                Punchline = "Fresh approach to DataSync through a new lens, free of constraint.",

                DiscordUrl = "https://discord.gg/QJy4zTqpMD",
                DiscordTooltip = "Opens the Sundouleia & Loci Discord--SEP----COL--DO NOT USE THIS TO REPORT GS ISSUES.--COL--",
                GithubUrl = "https://github.com/Sundouleia",

                RepoUrl = "https://raw.githubusercontent.com/Sundouleia/repo/main/sundouleia.json",

                BulletInfo =
                [
                    "Near-Instant updates from interactions, as fast as 250ms",
                    "Minimal redraws to prevent constant blipping",
                    "Designed with optimizations to enhance GS Immersion in mind.",
                    "Fully supports Loci integration, syncing player & pet statuses!",
                ]
            },
        };
    }
}



