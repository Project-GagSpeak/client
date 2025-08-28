using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using GagSpeak.State;
using GagSpeak.State.Caches;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Files.ShaderStructs;
using System.Reflection;

namespace GagSpeak.PlayerControl;

/// <summary>
///     TaskManager for Hardcore operations, that is capable of automating multiple sub tasks 
///     in sequence, while completely provending various forms of player movement.
/// </summary>
/// <remarks> 
///     It is possible to retain multiple instances if this is assumed with 
///     static CkCommon integration, but the questionable hardcore methods might make it best 
///     to keep seperate.
/// </remarks>
public partial class HcTaskManager : IDisposable
{
    private readonly ILogger<HcTaskManager> _logger;
    private readonly PlayerControlCache _cache;

    /// <summary> 
    ///     The list of hardcore task operations managed by the HcTaskManager.
    /// </summary>
    private static List<HardcoreTaskBase> _taskOperations = new List<HardcoreTaskBase>();

    public HcTaskManager(ILogger<HcTaskManager> logger, PlayerControlCache cache)
    {
        _logger = logger;
        _cache = cache;

        Svc.Framework.Update += ProcessTask;
        _logger.LogInformation("Hardcore Task Manager Initialized.");
    }

    /// <summary> # of tasks observed by the HcTaskManager. </summary>
    public static int ObservedTasks {get; private set; } = 0;

    /// <summary> The total number of currently queued Tasks. </summary>
    public static int QueuedTasks => _taskOperations.Count;

    /// <summary> If the Hardcore Task Manager is currently busy performing tasks. </summary>
    public static bool IsBusy => QueuedTasks > 0;
    public static long ElapsedTime => IsBusy ? _taskOperations[0].ElapsedTime : 0;
    public void Dispose()
    {
        _cache.SetActiveTaskControl(HcTaskControl.None);
        Svc.Framework.Update -= ProcessTask;
        _logger.LogInformation("Hardcore Task Manager Disposed.");
        GC.SuppressFinalize(this);
    }

    public void AbortIfActive(string taskName)
    {
        if (_taskOperations.Count is 0)
            return;

        if (taskName.Equals(_taskOperations[0].Name, StringComparison.OrdinalIgnoreCase))
            AbortCurrentTask();
    }
    public void RemoveIfPresent(string taskName)
    {
        if (_taskOperations.Count is 0)
            return;

        // if the task with the same name exists in the queue, remove it.
        if (_taskOperations[0].Name.Equals(taskName, StringComparison.OrdinalIgnoreCase))
            AbortCurrentTask();
        else
            _taskOperations.RemoveAll(t => taskName.Equals(t.Name, StringComparison.OrdinalIgnoreCase));
    }

    public void AbortTasks()
    {
        _taskOperations.Clear();
        _cache.SetActiveTaskControl(HcTaskControl.None);
    }

    public void AbortCurrentTask()
    {
        if (_taskOperations.Count > 0)
        {
            _logger.LogDebug($"Aborting Task: {_taskOperations[0].Name}", LoggerType.HardcoreTasks);
            _taskOperations[0].End();
            _taskOperations.RemoveAt(0);
        }
        _cache.SetActiveTaskControl(HcTaskControl.None);
    }

    private void ProcessTask(IFramework _)
    {
        if (_taskOperations.Count is 0)
            return;

        // if the task is complete before executing, it was aborted, so end and remove the item from the list.
        if (_taskOperations[0].Finished)
        {
            _cache.SetActiveTaskControl(HcTaskControl.None);
            _taskOperations.RemoveAt(0);
            if (_taskOperations.Count is 0)
                return;
        }

        // Assuming we have tasks present, we should process the first in the list.
        var currentHcTask = _taskOperations[0];
        // if the current task has not yet begin, we should begin it.
        if (!currentHcTask.IsRunning)
        {
            currentHcTask.Begin();
            _cache.SetActiveTaskControl(currentHcTask.Config.Flags);
            ObservedTasks = QueuedTasks;
        }

        try
        {
            var taskRes = currentHcTask.PerformTask();
            if (currentHcTask.Finished)
            {
                _logger.LogDebug($"OutermostScope Finished! (Scope -> {currentHcTask.Name})", LoggerType.HardcoreTasks);
                currentHcTask.End();
                _cache.SetActiveTaskControl(HcTaskControl.None);
            }
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"HardcoreTask Error: {currentHcTask.Name}, Exception: {ex}");
            AbortCurrentTask();
        }
    }

    public void DrawCacheState()
    {
        CkGui.ColorText($"HcTaskManager Tasks: {QueuedTasks}", ImGuiColors.ParsedGold);
        ImGui.Separator();
        if (_taskOperations.Count is 0)
        {
            ImGui.Text("No Active Tasks.");
            return;
        }

        using (var active = ImRaii.TreeNode("Active Task"))
        {
            if (active)
            {
                using (ImRaii.Group())
                {
                    DrawTask(_taskOperations[0]);
                }
                ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGuiColors.ParsedGold.ToUint());
            }
        }
        using (var tasksNode = ImRaii.TreeNode("Task Operations"))
        {
            if (tasksNode)
            {
                using (ImRaii.Group())
                {
                    foreach (var task in _taskOperations)
                        DrawTask(task);
                }
                ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGuiColors.ParsedGold.ToUint());
            }
        }
    }

    private void DrawTask(HardcoreTaskBase task)
    {
        using var _ = ImRaii.PushIndent();
        switch (task)
        {
            case HardcoreTaskCollection collection:
                DrawCollectionTask(collection);
                break;
            case BranchingHardcoreTask branch:
                DrawBranchTask(branch);
                break;
            case HardcoreTaskGroup group:
                DrawGroupTask(group);
                break;
            case HardcoreTask single:
                DrawSingleTask(single);
                break;
            default:
                ImGui.Text($"Unknown Task Type: {task.GetType().Name}");
                break;
        }
    }

    private void DrawCollectionTask(HardcoreTaskCollection collection)
    {
        using var _ = ImRaii.TreeNode($"{collection.Name}##collectionTask-{collection.Name}");
        if (!_) return;

        using (var t = ImRaii.Table("Collection-" + collection.Name, 7, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t) return;
            ImGui.TableSetupColumn("Collection");
            ImGui.TableSetupColumn("Executing");
            ImGui.TableSetupColumn("Current Idx");
            ImGui.TableSetupColumn("Total Tasks");
            ImGui.TableSetupColumn("Completed");
            ImGui.TableSetupColumn("Timeout");
            ImGui.TableSetupColumn("Flags");
            ImGui.TableHeadersRow();

            ImGuiUtil.DrawFrameColumn(collection.Name);
            ImGuiUtil.DrawFrameColumn(collection.IsRunning.ToString());
            ImGuiUtil.DrawFrameColumn(collection.CurrentTaskIdx.ToString());
            ImGuiUtil.DrawFrameColumn(collection.TotalTasks.ToString());
            ImGuiUtil.DrawFrameColumn(collection.Finished.ToString());
            ImGuiUtil.DrawFrameColumn(collection.Config.TimeoutAt.ToString());
            ImGuiUtil.DrawFrameColumn(collection.Config.Flags.ToString());
        }
        using (ImRaii.Group())
        {
            foreach (var task in collection.StoredTasks)
                DrawTask(task);
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.Border));
    }

    private void DrawBranchTask(BranchingHardcoreTask branch)
    {
        using var _ = ImRaii.TreeNode($"{branch.Name}##branchTask-{branch.Name}");
        if (!_) return;

        using (var t = ImRaii.Table("Branch-" + branch.Name, 8, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t) return;
            ImGui.TableSetupColumn("Branch Name");
            ImGui.TableSetupColumn("Predicate");
            ImGui.TableSetupColumn("Executing");
            ImGui.TableSetupColumn("Current Idx");
            ImGui.TableSetupColumn("Total Tasks");
            ImGui.TableSetupColumn("Completed");
            ImGui.TableSetupColumn("Timeout");
            ImGui.TableSetupColumn("Flags");
            ImGui.TableHeadersRow();

            ImGuiUtil.DrawFrameColumn(branch.Name);
            ImGuiUtil.DrawFrameColumn((branch.Predicate()).ToString());
            ImGuiUtil.DrawFrameColumn(branch.IsRunning.ToString());
            ImGuiUtil.DrawFrameColumn(branch.CurrentTaskIdx.ToString());
            ImGuiUtil.DrawFrameColumn(branch.TotalTasks.ToString());
            ImGuiUtil.DrawFrameColumn(branch.Finished.ToString());
            ImGuiUtil.DrawFrameColumn(branch.Config.TimeoutAt.ToString());
            ImGuiUtil.DrawFrameColumn(branch.Config.Flags.ToString());
        }
        using (ImRaii.Group())
        {
            CkGui.ColorText("True Branch:", ImGuiColors.ParsedGreen);
            DrawTask(branch.TrueTask);
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGuiColors.ParsedGreen.ToUint());

        using (ImRaii.Group())
        {
            CkGui.ColorText("False Branch:", ImGuiColors.DalamudRed);
            DrawTask(branch.FalseTask);
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGuiColors.DalamudRed.ToUint());
    }

    private void DrawGroupTask(HardcoreTaskGroup group)
    {
        using var _ = ImRaii.TreeNode($"{group.Name}##groupTask-{group.Name}");
        if (!_) return;

        using (var t = ImRaii.Table("Group-" + group.Name, 7, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t) return;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Executing");
            ImGui.TableSetupColumn("Current Idx");
            ImGui.TableSetupColumn("Total Tasks");
            ImGui.TableSetupColumn("Completed");
            ImGui.TableSetupColumn("Timeout");
            ImGui.TableSetupColumn("Flags");
            ImGui.TableHeadersRow();

            ImGuiUtil.DrawFrameColumn(group.Name);
            ImGuiUtil.DrawFrameColumn(group.IsRunning.ToString());
            ImGuiUtil.DrawFrameColumn(group.CurrentTaskIdx.ToString());
            ImGuiUtil.DrawFrameColumn(group.TotalTasks.ToString());
            ImGuiUtil.DrawFrameColumn(group.Finished.ToString());
            ImGuiUtil.DrawFrameColumn(group.Config.TimeoutAt.ToString());
            ImGuiUtil.DrawFrameColumn(group.Config.Flags.ToString());
        }
    }

    private void DrawSingleTask(HardcoreTask task)
    {
        using var _ = ImRaii.TreeNode($"{task.Name}##singleTask-{task.Name}");
        if (!_) return;

        using (var t = ImRaii.Table("Task-" + task.Name, 7, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t) return;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Executing");
            ImGui.TableSetupColumn("Current Idx");
            ImGui.TableSetupColumn("Total Tasks");
            ImGui.TableSetupColumn("Completed");
            ImGui.TableSetupColumn("Timeout");
            ImGui.TableSetupColumn("Flags");
            ImGui.TableHeadersRow();

            ImGuiUtil.DrawFrameColumn(task.Name);
            ImGuiUtil.DrawFrameColumn(task.IsRunning.ToString());
            ImGuiUtil.DrawFrameColumn(task.CurrentTaskIdx.ToString());
            ImGuiUtil.DrawFrameColumn(task.TotalTasks.ToString());
            ImGuiUtil.DrawFrameColumn(task.Finished.ToString());
            ImGuiUtil.DrawFrameColumn(task.Config.TimeoutAt.ToString());
            ImGuiUtil.DrawFrameColumn(task.Config.Flags.ToString());
        }
    }
}
