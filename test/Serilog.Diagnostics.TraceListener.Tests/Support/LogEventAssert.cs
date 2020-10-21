using System.Linq;
using NUnit.Framework;
using Serilog.Events;

namespace Serilog.Diagnostics.TraceListener.Tests.Support
{
    /// <summary>
    ///     The LogEventAssert class contains a collection of static methods that
    ///     implement assertions against Serilog.Events.LogEvent
    /// </summary>
    public static class LogEventAssert
    {
        public static void HasLevel(LogEventLevel level, LogEvent logEvent)
        {
            Assert.That(logEvent.Level, Is.EqualTo(level), "A log level different from the expected was found.");
        }

        public static void HasMessage(string message, LogEvent logEvent)
        {
            Assert.That(logEvent.RenderMessage(), Is.EqualTo(message), "The rendered message was not as expected.");
        }

        public static void HasProperty(string propertyName, LogEvent logEvent)
        {
            Assert.That(logEvent.Properties, Contains.Key(propertyName), "Expected property was not found.");
        }

        public static void HasPropertyValue(object propertyValue, string propertyName, LogEvent logEvent)
        {
            HasProperty(propertyName, logEvent);

            LogEventPropertyValue value = logEvent.Properties[propertyName];
            Assert.That(value.LiteralValue(), Is.EqualTo(propertyValue), "The property value was not as expected");
        }

        public static void HasPropertyValueSequenceValue(object[] propertyValue, string propertyName, LogEvent logEvent)
        {
            HasProperty(propertyName, logEvent);

            var value = logEvent.Properties[propertyName];
            var sequence = ((SequenceValue) value).Elements.Select(pv => pv.LiteralValue());

            Assert.That(sequence, Is.EquivalentTo(propertyValue.Select(_ => _.ToString())), "The property value was not as expected");
        }
    }
}
