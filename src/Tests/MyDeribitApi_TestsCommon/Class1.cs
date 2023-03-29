using System.Net.WebSockets;
using System.Text;
using NSubstitute;
using NSubstitute.Core;

namespace MyDeribitApi_TestsCommon;

public class Class1
{
    public static WebSocket CreateWebSocketMockedWaitForSendAndReceive(
        int waitMilliseconds,
        string jsonReceivedMock = "")
    {
        var ws = Substitute.For<WebSocket>();

        ws.SendAsync(
            Arg.Any<ArraySegment<byte>>(),
            Arg.Any<WebSocketMessageType>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                await Task.Delay(waitMilliseconds);
            });

        ws.ReceiveAsync(
            Arg.Any<ArraySegment<byte>>(),
            Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                return await GetMyTask(x, jsonReceivedMock);
            });

        return ws;
    }

    static async Task<WebSocketReceiveResult> GetMyTask(
        CallInfo x,
        string jsonReceivedMock)
    {
        await Task.Delay(1);

        var buffer = Encoding.Default.GetBytes(jsonReceivedMock);
        var array = x.ArgAt<ArraySegment<byte>>(0).Array;

        for (int i = 0; i < buffer.Length; i++)
            array[i] = buffer[i];

        var result = new WebSocketReceiveResult(buffer.Length, WebSocketMessageType.Text, true);

        return result;
    }
}
