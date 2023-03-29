using System.Text.Json.Serialization;

namespace MyDeribitApiLibrary;

public record Request<T> : IRequest<T>
    where T : class
{
    public long Id { get; set; }

    [JsonPropertyName("jsonrpc")]
    public string JsonRpcVersion { get; set; } = "2.0";

    public string Method { get; set; } = "";

    [JsonPropertyName("params")]
    public T? Parameters { get; set; } = default;
}