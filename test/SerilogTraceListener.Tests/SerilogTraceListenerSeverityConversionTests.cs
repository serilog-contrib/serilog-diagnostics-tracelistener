using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace SerilogTraceListener.Tests
{
    [TestFixture]
    public class SerilogTraceListenerSeverityConversionTests
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