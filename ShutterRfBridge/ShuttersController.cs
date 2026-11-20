using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ShutterRfBridge;

/// <summary>
///     The endpoints the CCU calls, one shutter channel per route. Each queues its command and answers
///     <c>202 Accepted</c> at once; invalid route values give <c>400 Bad Request</c>.
/// </summary>
[ApiController]
[Route("shutters/{channel}")]
internal sealed class ShuttersController : ControllerBase
{
    private readonly TransmitQueue _queue;

    public ShuttersController(TransmitQueue queue)
    {
        _queue = queue;
    }

    [HttpPost("up/{level}/{runtimeMs}")]
    public IActionResult Up(
        [Range(0, RemoteFrame.MaxChannel)] int channel,
        [Range(ShutterMove.MinLevel, ShutterMove.MaxLevel)]
        int level,
        [Range(1, int.MaxValue)] int runtimeMs) =>
        Queue(new ShutterMove(channel, RemoteCommand.Up, level, TimeSpan.FromMilliseconds(runtimeMs)));

    [HttpPost("down/{level}/{runtimeMs}")]
    public IActionResult Down(
        [Range(0, RemoteFrame.MaxChannel)] int channel,
        [Range(ShutterMove.MinLevel, ShutterMove.MaxLevel)]
        int level,
        [Range(1, int.MaxValue)] int runtimeMs) =>
        Queue(new ShutterMove(channel, RemoteCommand.Down, level, TimeSpan.FromMilliseconds(runtimeMs)));

    [HttpPost("stop")]
    public IActionResult Stop([Range(0, RemoteFrame.MaxChannel)] int channel) => Queue(new ShutterStop(channel));

    private AcceptedResult Queue(ShutterCommand command)
    {
        _queue.Enqueue(command);
        return Accepted();
    }
}
