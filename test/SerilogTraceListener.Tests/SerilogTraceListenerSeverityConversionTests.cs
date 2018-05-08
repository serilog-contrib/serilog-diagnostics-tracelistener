using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace SerilogTraceListener.Tests
{
    [TestFixture]
#if NET45
    public class SerilogTraceListenerSeverityConversionTests_NET45
#elif NET46
    public class SerilogTraceListenerSeverityConversionTests_NET46
#elif NETCOREAPP1_0
    public class SerilogTraceListenerSeverityConversionTests_NETSTANDARD1_3
#endif
    {
        static IEnumerable<TraceEventType> allTraceEventTypes = Enum.GetValues(typeof(TraceEventType)).Cast<TraceEventType>();

        [Test]
        public void CanConvertAnyTraceEventType([ValueSource("allTraceEventTypes")] TraceEventType sourceType)
        {
            TestDelegate act = () => global::SerilogTraceListener.SerilogTraceListener.ToLogEventLevel(sourceType);

            Assert.DoesNotThrow(act);
        }
    }
}
