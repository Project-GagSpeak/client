using Dalamud.Plugin.Services;
using static FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement.Delegates;
using System.ComponentModel;
using System;
using System.Globalization;

namespace GagSpeak.Achievements;

// construct a small private class within the manager for us to store the data we want to send over our IPC.

public class AchievementSaveData
{
    // easter egg progress.
    public Dictionary<string, bool> EasterEggIcons { get; set; } = new Dictionary<string, bool>() { { "Orders", false }, { "Gags", false }, { "Wardrobe", false }, { "Puppeteer", false }, { "Toybox", false }, { "Migrations", false } };
    // World Tour Progress. MAPPING: (129 = Limsa Lominsa, 132 = Gridania, 130 = Ul'dah, 418 = Ishgard, 628 = Kugane, 819 = The Crystarium, 820 = Eulmore, 962 = Old Sharlayan, 963 = Raz-At-Han, 1185 = Tuliyollal, 1186 = Solution 9)
    public Dictionary<ushort, bool> VisitedWorldTour { get; set; } = new Dictionary<ushort, bool>() { { 129, false }, { 132, false }, { 130, false }, { 418, false }, { 628, false }, { 819, false }, { 820, false }, { 962, false }, { 963, false }, { 1185, false }, { 1186, false } };
    public Dictionary<int, AchievementBase> Achievements { get; private set; }
    public AchievementSaveData()
    {
        Achievements = new Dictionary<int, AchievementBase>();
    }

    public void LoadFromLightAchievements(List<LightAchievement> lightAchievements)
    {
        foreach (var lightAchievement in lightAchievements)
        {
            if (lightAchievement.Type is AchievementType.Progress && Achievements[lightAchievement.AchievementId] is ProgressAchievement progressAchievement)
            {
                progressAchievement.IsCompleted = lightAchievement.IsCompleted;
                progressAchievement.Progress = lightAchievement.Progress;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.Conditional && Achievements[lightAchievement.AchievementId] is ConditionalAchievement conditionalAchievement)
            {
                conditionalAchievement.IsCompleted = lightAchievement.IsCompleted;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.Threshold && Achievements[lightAchievement.AchievementId] is ThresholdAchievement thresholdAchievement)
            {
                thresholdAchievement.IsCompleted = lightAchievement.IsCompleted;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.Duration && Achievements[lightAchievement.AchievementId] is DurationAchievement durationAchievement)
            {
                durationAchievement.IsCompleted = lightAchievement.IsCompleted;
                durationAchievement.ActiveItems = lightAchievement.ActiveItems;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.ConditionalThreshold && Achievements[lightAchievement.AchievementId] is ConditionalThresholdAchievement conditionalThresholdAchievement)
            {
                conditionalThresholdAchievement.IsCompleted = lightAchievement.IsCompleted;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.ConditionalProgress && Achievements[lightAchievement.AchievementId] is ConditionalProgressAchievement conditionalProgressAchievement)
            {
                conditionalProgressAchievement.IsCompleted = lightAchievement.IsCompleted;
                conditionalProgressAchievement.Progress = lightAchievement.Progress;
                conditionalProgressAchievement.ConditionalTaskBegun = lightAchievement.ConditionalTaskBegun;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.TimedProgress && Achievements[lightAchievement.AchievementId] is TimedProgressAchievement timedProgressAchievement)
            {
                timedProgressAchievement.IsCompleted = lightAchievement.IsCompleted;
                timedProgressAchievement.ProgressTimestamps = lightAchievement.RecordedDateTimes;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.TimeLimitConditional && Achievements[lightAchievement.AchievementId] is TimeLimitConditionalAchievement timeLimited)
            {
                timeLimited.IsCompleted = lightAchievement.IsCompleted;
                timeLimited.StartPoint = lightAchievement.StartTime;
                continue; // skip to next achievement
            }

            if (lightAchievement.Type is AchievementType.RequiredTimeConditional && Achievements[lightAchievement.AchievementId] is TimeRequiredConditionalAchievement timeRequired)
            {
                timeRequired.IsCompleted = lightAchievement.IsCompleted;
                timeRequired.StartPoint = lightAchievement.StartTime;
                continue; // skip to next achievement
            }
        }
    }

    public void AddProgress(AchievementModuleKind module, AchievementInfo info, int goal, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new ProgressAchievement(module, info, goal, onCompleted, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddConditional(AchievementModuleKind module, AchievementInfo info, Func<bool> cond, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new ConditionalAchievement(module, info, cond, onCompleted, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddThreshold(AchievementModuleKind module, AchievementInfo info, int goal, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new ThresholdAchievement(module, info, goal, onCompleted, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddDuration(AchievementModuleKind module, AchievementInfo info, TimeSpan duration, DurationTimeUnit timeUnit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new DurationAchievement(module, info, duration, onCompleted, timeUnit, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddRequiredTimeConditional(AchievementModuleKind module, AchievementInfo info, TimeSpan duration, Func<bool> cond, DurationTimeUnit timeUnit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new TimeRequiredConditionalAchievement(module, info, duration, cond, onCompleted, timeUnit, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddTimeLimitedConditional(AchievementModuleKind module, AchievementInfo info, TimeSpan dur, Func<bool> cond, DurationTimeUnit timeUnit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new TimeLimitConditionalAchievement(module, info, dur, cond, onCompleted, timeUnit, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddConditionalProgress(AchievementModuleKind module, AchievementInfo info, int goal, Func<bool> cond, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool reqBeginAndFinish = true, bool isSecret = false)
    {
        var achievement = new ConditionalProgressAchievement(module, info, goal, cond, onCompleted, reqBeginAndFinish, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddConditionalThreshold(AchievementModuleKind module, AchievementInfo info, int goal, Func<bool> cond, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new ConditionalThresholdAchievement(module, info, goal, cond, onCompleted, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public void AddTimedProgress(AchievementModuleKind module, AchievementInfo info, int goal, TimeSpan timeLimit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
    {
        var achievement = new TimedProgressAchievement(module, info, goal, timeLimit, onCompleted, prefix, suffix, isSecret);
        Achievements.Add(info.Id, achievement);
    }

    public LightSaveDataDto ToLightSaveDataDto()
    {
        var dto = new LightSaveDataDto
        {
            LightAchievementData = new List<LightAchievement>(),
            EasterEggIcons = this.EasterEggIcons,
            VisitedWorldTour = this.VisitedWorldTour
        };

        foreach (var achievementItem in Achievements.Values)
        {
            var lightAchievement = new LightAchievement
            {
                Type = achievementItem.GetAchievementType(),
                AchievementId = achievementItem.AchievementId,
                IsCompleted = achievementItem.IsCompleted,
                Progress = GetProgress(achievementItem) ?? 0,
                ConditionalTaskBegun = achievementItem is ConditionalProgressAchievement conditionalProgressAchievement ? conditionalProgressAchievement.ConditionalTaskBegun : false,
                StartTime = GetStartTime(achievementItem) ?? DateTime.MinValue,
                RecordedDateTimes = achievementItem is TimedProgressAchievement timedProgressAchievement ? (timedProgressAchievement.ProgressTimestamps ?? new List<DateTime>()) : new List<DateTime>(),
                ActiveItems = achievementItem is DurationAchievement durationAchievement ? durationAchievement.ActiveItems : new List<TrackedItem>()
            };

            dto.LightAchievementData.Add(lightAchievement);
        }
        return dto;
    }

    public void LoadFromLightSaveDataDto(LightSaveDataDto dto)
    {
        try
        {
            // Update Easter Egg Icons
            EasterEggIcons = new Dictionary<string, bool>(dto.EasterEggIcons);
            VisitedWorldTour = new Dictionary<ushort, bool>(dto.VisitedWorldTour);
            LoadFromLightAchievements(dto.LightAchievementData);
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogError(e, "Failed to load achievement data from save data.");
        }
    }

    private int? GetProgress(AchievementBase achievement)
    {
        if (achievement is ConditionalProgressAchievement conditionalProgressAchievement)
            return conditionalProgressAchievement.Progress;
        if (achievement is ProgressAchievement progressAchievement)
            return progressAchievement.Progress;
        return null;
    }

    private DateTime? GetStartTime(AchievementBase achievement)
    {
        if (achievement is TimeLimitConditionalAchievement timeLimited)
            return timeLimited.StartPoint;
        if (achievement is TimeRequiredConditionalAchievement timeRequired)
            return timeRequired.StartPoint;
        return null;
    }
}
