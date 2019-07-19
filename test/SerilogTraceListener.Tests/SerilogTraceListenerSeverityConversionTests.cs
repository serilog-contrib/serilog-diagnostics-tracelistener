using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Serilog.Events;

namespace SerilogTraceListener.Tests
{
    [TestFixture]
    public class SerilogTraceListenerSeverityConversionTests
    {
        static readonly IEnumerable<TraceEventType> AllTraceEventTypes = Enum.GetValues(typeof(TraceEventType)).Cast<TraceEventType>();

        [Test]
        public void CanConvertAnyTraceEventType([ValueSource(nameof(AllTraceEventTypes))] TraceEventType sourceType)
        {
            var mapped = LevelMapping.ToLogEventLevel(sourceType);
            Assert.That(Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>().Contains(mapped));
        }
    }
}
