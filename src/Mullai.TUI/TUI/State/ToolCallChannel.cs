using System.Threading.Channels;
using Mullai.Abstractions.Observability;

namespace Mullai.TUI.TUI.State;

/// <summary>
/// Singleton channel that decouples <c>FunctionCallingMiddleware</c> from the TUI.
/// The middleware writes <see cref="ToolCallObservation"/> instances,
/// and <c>ChatController</c> reads them and pushes them into <c>ChatState</c>.
/// </summary>
public sealed class ToolCallChannel
{
    private static readonly ToolCallChannel _instance = new();

    private readonly Channel<ToolCallObservation> _channel =
        Channel.CreateBounded<ToolCallObservation>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    private ToolCallChannel() { }

    public static ToolCallChannel Instance => _instance;

    public ChannelWriter<ToolCallObservation> Writer => _channel.Writer;
    public ChannelReader<ToolCallObservation> Reader => _channel.Reader;
}
