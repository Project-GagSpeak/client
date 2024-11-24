using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.Simulation;

public class LockPickAnimation
{
    private bool animationActive;
    private float animationProgress;
    private Vector2 progressBarStart;
    private Vector2 progressBarEnd;
    private float animationSpeed = 0.05f; // Speed of the progress bar animation

    public LockPickAnimation()
    {
        progressBarStart = new Vector2(100, 250); // Starting position of the progress bar
        progressBarEnd = new Vector2(300, 250); // End position of the progress bar
        Reset(); // Initialize animation state
    }

    public void Reset()
    {
        animationActive = false;
        animationProgress = 0.0f;
    }

    public void StartFailedAnimation()
    {
        // You can customize this method to start a different animation or change color
        animationActive = true;
        animationProgress = 0.0f; // Reset animation progress
    }

    public void StartSuccessAnimation()
    {
        animationActive = true;
        animationProgress = 0.0f; // Reset animation progress
    }

    public void Render()
    {
        if (!animationActive) return;

        // Increment animation progress
        animationProgress += animationSpeed;

        // Calculate the current end point based on animation progress
        Vector2 currentEnd = Vector2.Lerp(progressBarStart, progressBarEnd, animationProgress);

        var drawList = ImGui.GetWindowDrawList();

        // Draw the progress bar background
        drawList.AddRectFilled(progressBarStart, progressBarEnd, ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 1.0f)));

        // Draw the animated progress bar
        drawList.AddRectFilled(progressBarStart, currentEnd, ImGui.GetColorU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f)));

        // Stop the animation when the progress bar reaches the end
        if (animationProgress >= 1.0f)
        {
            animationActive = false; // End animation
        }
    }
}
