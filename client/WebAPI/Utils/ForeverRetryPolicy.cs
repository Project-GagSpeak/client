using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Services.Mediator;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI.Utils;
public class ForeverRetryPolicy : IRetryPolicy
{
    private readonly GagspeakMediator _mediator;
    private bool _sentDisconnected = false; // indicating weather a disconnection notif was sent

    public ForeverRetryPolicy(GagspeakMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary> 
    /// Determines the delay before the next retry attempt to connect to the signalR Hub.
    /// <para>
    /// Uses the retryContext.PreviousRetryCount to adjust the delay and decide when to notify application of disconnection.
    /// </para>
    /// </summary>
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        // random time to wait
        TimeSpan timeToWait = TimeSpan.FromSeconds(new Random().Next(10, 20));
        // if the previous retry count is 0, we are just starting, so we wait 3 seconds
        if (retryContext.PreviousRetryCount == 0)
        {
            // reset the sent disconnected flag
            _sentDisconnected = false;
            // wait 3 seconds
            timeToWait = TimeSpan.FromSeconds(3);
        }
        // otherwise, we increase the time to wait by 5 seconds each time
        else if (retryContext.PreviousRetryCount == 1) timeToWait = TimeSpan.FromSeconds(5);
        // if we are at 2, increase the time to wait by 10 seconds
        else if (retryContext.PreviousRetryCount == 2) timeToWait = TimeSpan.FromSeconds(10);
        // otherwise, if we are still waiting
        else
        {
            // if we havent sent a disconnected message yet, send one
            if (!_sentDisconnected)
            {
                // send a disconnected message
                _mediator.Publish(new NotificationMessage("Connection lost", "Connection lost to server", NotificationType.Warning, TimeSpan.FromSeconds(10)));
                _mediator.Publish(new MainHubDisconnectedMessage());
            }
            // set the sent disconnected flag to true
            _sentDisconnected = true;
        }

        // return the time to wait
        return timeToWait;
    }
}
