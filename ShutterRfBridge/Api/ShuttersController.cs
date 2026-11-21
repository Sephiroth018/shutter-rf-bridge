using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ShutterRfBridge.Remote;
using ShutterRfBridge.Transmission;

namespace ShutterRfBridge.Api;

/// <summary>
///     The endpoints the CCU calls, one shutter channel per route. Each queues its command and answers
///     <c>202 Accepted</c> at once; invalid route values give <c>400 Bad Request</c>.
/// </summary>
[ApiController]
[Route("shutters/{channel}")]
public class ShuttersController : ControllerBase
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
        short runtimeMs) =>
        Move(channel, RemoteCommand.Up, level, runtimeMs);

    [HttpPost("down/{level}/{runtimeMs}")]
    public IActionResult Down(
        [Range(0, RemoteFrame.MaxChannel)] int channel,
        [Range(ShutterMove.MinLevel, ShutterMove.MaxLevel)]
        int level,
        short runtimeMs) =>
        Move(channel, RemoteCommand.Down, level, runtimeMs);

    [HttpPost("stop")]
    public IActionResult Stop([Range(0, RemoteFrame.MaxChannel)] int channel) => Queue(new ShutterStop(channel));

    /// <summary>
    ///     Queues a move. CUxD sends run times as signed 16-bit numbers, so those above 32767 ms arrive negative; they're
    ///     read back as unsigned, which allows run times up to 65535 ms.
    /// </summary>
    private IActionResult Move(int channel, RemoteCommand command, int level, short runtimeMs)
    {
        if (runtimeMs == 0)
        {
            ModelState.AddModelError(nameof(runtimeMs), "The run time must not be 0.");
            return ValidationProblem();
        }

        var runTime = TimeSpan.FromMilliseconds(unchecked((ushort)runtimeMs));
        return Queue(new ShutterMove(channel, command, level, runTime));
    }

    private AcceptedResult Queue(ShutterCommand command)
    {
        _queue.Enqueue(command);
        return Accepted();
    }
}
