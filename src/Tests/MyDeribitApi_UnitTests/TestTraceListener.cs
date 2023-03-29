using System.Diagnostics;

namespace MyDeribitApi_UnitTests;

class TestTraceListener : DelimitedListTraceListener
{
    public TestTraceListener(TextWriter writer)
        : base(writer)
    {
    }

    public override void TraceEvent(
        TraceEventCache eventCache,
        string source,
        TraceEventType eventType,
        int id,
        string message)
    {
        WriteLine(message);
    }
}
