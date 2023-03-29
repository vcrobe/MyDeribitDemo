using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MyDeribitApiLibrary;

public class MyDeribitApi
{
    static readonly object EmptyObject = new();
    const string EndMessage = "END";

    readonly ILogger<MyDeribitApi>? logger;



    /// <summary>
    /// Connect to the server
    /// </summary>
    public async Task ConnectAsync(ClientWebSocket socket)
    {
        logger?.LogDebug("Trying to connect to the server");
        var url = new Uri("wss://test.deribit.com/ws/api/v2");

        await socket.ConnectAsync(url, CancellationToken.None);

        logger?.LogDebug("\tConnection to the server stablished");
    }

    /// <summary>
    /// Disconnect from the server
    /// </summary>
    public async Task DisconnectAsync(
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        logger?.LogDebug("Trying to disconnect from the server");

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, EndMessage, cancellationToken);

        logger?.LogDebug("\tDisconnection from the server successfully");
    }

    public MyDeribitApi(ILogger<MyDeribitApi>? logger)
    {
        this.logger = logger;
    }

    const int readBufferSize = 4096;
    byte[] readBuffer = new byte[readBufferSize];

    /// <summary>
    /// Sends a public/test message to the Deribit API
    /// </summary>
    /// <returns>
    /// True if the test succeed, false otherwise
    /// </returns>
    public async Task<bool> DoTestDeribitApi(
        WebSocket socket,
        CancellationToken cancellationToken,
        string expectedApiVersion)
    {
        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();

        var request = new Request<object>
        {
            Id = 1,
            Method = "public/test",
            Parameters = EmptyObject,
        };

        var jsonMessage = JsonSerializer.Serialize(request, jsonOptions);

        Trace.TraceInformation(jsonMessage);

        var buffer = new byte[jsonMessage.Length];
        Encoding.Default.GetBytes(jsonMessage, 0, jsonMessage.Length, buffer, 0);

        Trace.TraceInformation("Sending message to the server");

        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();

        Trace.TraceInformation("Waiting for response from the server");

        var rawResponse = await ReceiveMessageAsync(socket, cancellationToken);

        var result = JsonSerializer.Deserialize<TestResponse>(rawResponse, jsonOptions);

        if (result == null)
            throw new Exception($"Unexpected response: {rawResponse}");

        var apiVersion = result.Result["version"];
        return apiVersion == expectedApiVersion;
    }


    readonly StringBuilder stringBuilder = new();

    /// <summary>
    /// Read the whole response from the socket
    /// </summary>
    public async Task<string> ReceiveMessageAsync(
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        WebSocketReceiveResult response;
        stringBuilder.Clear();

        do
        {
            response = await socket.ReceiveAsync(readBuffer, cancellationToken);

            var result = Encoding.Default.GetString(readBuffer, 0, response.Count);

            stringBuilder.Append(result);
        }
        while (!response.EndOfMessage);

        var jsonResult = stringBuilder.ToString();

        return jsonResult;
    }

    static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = new LowerCaseJsonNamingPolicy(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

public class TestResponse : Response
{
    [JsonPropertyName("result")]
    public Dictionary<string, string> Result { get; set; }
}

public class Response
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("jsonrpc")]
    public string JsonRpcVersion { get; set; } = "";

    [JsonPropertyName("usIn")]
    public ulong UsIn { get; set; }

    [JsonPropertyName("usOut")]
    public ulong UsOut { get; set; }

    [JsonPropertyName("usDiff")]
    public int UsDiff { get; set; }

    [JsonPropertyName("error")]
    public ResponseError? Error { get; set; } = null;
}

public class ResponseError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    /// <summary>
    /// Per official documentation:
    /// data: any type
    /// Additional data about the error. This field may be omitted.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
