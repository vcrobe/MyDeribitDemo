using System.Diagnostics;
using MyDeribitApiLibrary;
using MyDeribitApi_TestsCommon;

namespace MyDeribitApi_UnitTests;

[TestClass]
public class TestAsyncTests
{
    StringWriter? writer;

    void SetupTrace()
    {
        writer = new StringWriter();

        Trace.Listeners.Clear();
        Trace.Listeners.Add(new TestTraceListener(writer));
    }

    string[] GetTraceResult()
    {
        var traceResult = writer
            .GetStringBuilder()
            .ToString();

        return traceResult.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Verify the task is cancelled before sending the request to the server
    /// </summary>
    [TestMethod]
    public async Task TestDoTestDeribitApi_CancellationTokenCanceledBeforeSendingRequest()
    {
        // Arrange
        SetupTrace();

        var tokenSource = new CancellationTokenSource(0);
        var ws = Class1.CreateWebSocketMockedWaitForSendAndReceive(10);
        var api = new MyDeribitApi(null);

        // Act

        try
        {
            await api.DoTestDeribitApi(ws, tokenSource.Token, "2.0");
            Assert.Fail("Expected a TaskCanceledException");
        }
        catch (TaskCanceledException)
        {
            // Assert

            var traceResult = GetTraceResult();

            // We expect no items in the trace because the method fast failed!
            Assert.AreEqual(0, traceResult.Length);
        }
        catch
        {
            Assert.Fail("Expected a TaskCanceledException");
        }
    }

    /// <summary>
    /// Verify the task is cancelled after sending the request to the server and before reading the response from the server
    /// The method cannot wait to read the response from the server
    /// </summary>
    [TestMethod]
    public async Task TestDoTestDeribitApi_CancellationTokenCanceledAfterSendingRequest()
    {
        // Arrange
        SetupTrace();

        var tokenSource = new CancellationTokenSource(400);
        var ws = Class1.CreateWebSocketMockedWaitForSendAndReceive(500);
        var api = new MyDeribitApi(null);

        // Act

        try
        {
            await api.DoTestDeribitApi(ws, tokenSource.Token, "2.0");
            Assert.Fail("Expected a TaskCanceledException");
        }
        catch (TaskCanceledException)
        {
            // Assert

            var traceResult = GetTraceResult();

            // We expect two items in the trace:
            // - The trace of the json message for the request
            // - The trace before calling socket.SendAsync()
            Assert.AreEqual(2, traceResult.Length);
        }
        catch
        {
            Assert.Fail("Expected a TaskCanceledException");
        }
    }

    /// <summary>
    /// Verify the message created to send throu the websocket is the expected
    /// Verify the version of the API is the expected
    /// </summary>
    [TestMethod]
    public async Task TestDoTestDeribitApi_MessageOk()
    {
        // Arrange
        SetupTrace();

        const string jsonResult = "{\"jsonrpc\": \"2.0\",\"id\": 35,\"result\": {\"version\":\"2.0\"} }";

        var ws = Class1.CreateWebSocketMockedWaitForSendAndReceive(0, jsonResult);
        var api = new MyDeribitApi(null);

        // Act

        var methodResult = await api.DoTestDeribitApi(ws, CancellationToken.None, "1.0");

        // Assert

        var traceResult = GetTraceResult();

        // We expect 3 items in the trace
        // - the json message to send in the request
        // - the message before sending the request
        // - the message before waiting for the response
        Assert.AreEqual(3, traceResult.Length);

        // Verify the method is sending the expected request to the server!
        const string expectedResult = "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"public/test\",\"params\":{}}";

        Assert.AreEqual(expectedResult, traceResult[0]);

        // We expect the method was executed successfully and version of the response match the version expected by our app!
        Assert.IsTrue(methodResult);
    }

    /// <summary>
    /// Verify that if the version of the API is not the expected the method returns false
    /// </summary>
    [TestMethod]
    public async Task TestDoTestDeribitApi_ApiVersionMismatch()
    {
        // Arrange
        SetupTrace();

        const string jsonResult = "{\"jsonrpc\": \"2.0\",\"id\": 1,\"result\": {\"version\":\"1.5\"} }";

        var ws = Class1.CreateWebSocketMockedWaitForSendAndReceive(0, jsonResult);
        var api = new MyDeribitApi(null);

        // Act

        var methodResult = await api.DoTestDeribitApi(ws, CancellationToken.None, "1.0");

        // Assert

        var traceResult = GetTraceResult();

        // We expect 3 items in the trace
        // - the json message to send in the request
        // - the message before sending the request
        // - the message before waiting for the response
        Assert.AreEqual(3, traceResult.Length);

        // Verify the method is sending the expected request to the server!
        const string expectedResult = "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"public/test\",\"params\":{}}";

        Assert.AreEqual(expectedResult, traceResult[0]);

        // We expect the method was executed successfully but the version of the response don't match the version expected by our app so it returns false!
        Assert.IsFalse(methodResult);
    }
}
