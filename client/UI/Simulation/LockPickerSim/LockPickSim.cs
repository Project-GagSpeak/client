using ImGuiNET;

namespace GagSpeak.UI.Simulation;

public class LockPickingMinigame
{
    private int lockPickCount = 0;
    private DateTime lastPickGenerated = DateTime.Now;
    private int[] lockPattern; // Array storing correct pattern for the lock
    private List<int> playerAttempt; // Tracks player's current attempt sequence
    private bool gameActive;
    private int lockSlotCount; // Number of slots in the lock

    public LockPickingMinigame()
    {
        lockPickCount = 1; // Initial starting pick
        StartNewLock();
    }

    private void StartNewLock()
    {
        lockSlotCount = new Random().Next(5, 9); // Choose slots between 5 and 8
        lockPattern = GenerateLockPattern(lockSlotCount);
        playerAttempt = new List<int>();
        gameActive = true;
    }

    private int[] GenerateLockPattern(int slotCount)
    {
        Random random = new Random();
        return Enumerable.Range(0, slotCount).OrderBy(x => random.Next()).ToArray(); // Random order of slots
    }

    private void GenerateLockPick()
    {
        // Generate a lock pick every 6 hours or based on a specific condition
        if ((DateTime.Now - lastPickGenerated).TotalHours >= 6)
        {
            lockPickCount++;
            lastPickGenerated = DateTime.Now;
        }
    }

    public void DrawLockPickingUI()
    {
        GenerateLockPick(); // Check if a new lock pick should be added

        ImGui.Text($"Lock Picks: {lockPickCount}");

        if (!gameActive)
        {
            ImGui.Text("Lock picked successfully or failed. Start a new lock.");
            if (ImGui.Button("Start New Lock"))
            {
                StartNewLock();
            }
            return;
        }

        ImGui.Text("Pick the lock by selecting slots in the correct order!");

        for (int i = 0; i < lockSlotCount; i++)
        {
            ImGui.SameLine();
            if (ImGui.Button($"Slot {i + 1}"))
            {
                AttemptPick(i);
            }
        }

        ImGui.NewLine();

        if (playerAttempt.Count > 0)
        {
            ImGui.Text("Your Current Attempt: " + string.Join(", ", playerAttempt));
        }
    }

    private void AttemptPick(int slot)
    {
        if (lockPickCount <= 0)
        {
            ImGui.Text("Out of lock picks!");
            return;
        }

        playerAttempt.Add(slot);

        // Check current attempt with lock pattern
        if (playerAttempt[playerAttempt.Count - 1] != lockPattern[playerAttempt.Count - 1])
        {
            // Failed attempt, break pick and reset
            lockPickCount--;
            playerAttempt.Clear();
            ImGui.Text("Lock pick broke! Incorrect slot.");
        }
        else if (playerAttempt.Count == lockPattern.Length)
        {
            // Successfully completed the lock pattern
            gameActive = false;
            ImGui.Text("Lock successfully picked!");
        }
    }
}
