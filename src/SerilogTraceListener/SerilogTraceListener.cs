// Copyright 2015 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog;
using Serilog.Core.Pipeline;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace SerilogTraceListener
{
    /// <summary>
    ///     TraceListener implementation that directs all output to Serilog.
    /// </summary>
    public class SerilogTraceListener : TraceListener
    {
        const string ActivityIdProperty = "ActivityId";
        const string CategoryProperty = "Category";
        const string EventIdProperty = "TraceEventId";
        const string FailDetailMessageProperty = "FailDetails";
        const string RelatedActivityIdProperty = "RelatedActivityId";
        const string SourceProperty = "TraceSource";
        const string TraceDataProperty = "TraceData";
        const string TraceEventTypeProperty = "TraceEventType";
        const LogEventLevel DefaultLogLevel = LogEventLevel.Debug;
        const LogEventLevel FailLevel = LogEventLevel.Fatal;
        const string MessagelessTraceEventMessageTemplate = "{TraceSource:l} {TraceEventType}: {TraceEventId}";
        const string TraceDataMessageTemplate = "{TraceData}";
        ILogger logger;

        /// <summary>
        ///     Creates a SerilogTraceListener that uses the logger from `Serilog.Log`
        /// </summary>
        /// <remarks>
        ///     This is needed because TraceListeners are often configured through XML
        ///     where there would be no opportunity for constructor injection
        /// </remarks>
        public SerilogTraceListener() : this(Log.Logger)
        {
        }

        /// <summary>
        ///     Creates a SerilogTraceListener that uses the specified logger
        /// </summary>
        public SerilogTraceListener(ILogger logger)
        {
            this.logger = logger.ForContext<SerilogTraceListener>();
        }

        /// <summary>
        ///     Creates a SerilogTraceListener for the context specified.
        /// </summary>
        /// <example>
        ///     &lt;listeners&gt;
        ///         &lt;add name="Serilog" type="SerilogTraceListener.SerilogTraceListener, SerilogTraceListener" initializeData="MyContext" /&gt;
        ///     &lt;/listeners&gt;
        /// </example>
        public SerilogTraceListener(string context)
        {
            this.logger = Log.Logger.ForContext("SourceContext", context);
        }

        public override bool IsThreadSafe
        {
            get { return true; }
        }

        public override void Fail(string message)
        {
            var properties = CreateFailProperties();
            Write(FailLevel, null, message, properties);
        }

        public override void Fail(string message, string detailMessage)
        {
            var properties = CreateFailProperties();
            SafeAddProperty(properties, FailDetailMessageProperty, detailMessage);
            Write(FailLevel, null, message, properties);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            var properties = CreateTraceProperties(source, eventType, id);
            WriteData(eventType, properties, data);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            var properties = CreateTraceProperties(source, eventType, id);
            WriteData(eventType, properties, data);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            var properties = CreateTraceProperties(source, eventType, id);
            Write(eventType, null, MessagelessTraceEventMessageTemplate, properties);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            var properties = CreateTraceProperties(source, eventType, id);
            Write(eventType, null, message, properties);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            var properties = CreateTraceProperties(source, eventType, id);
            Exception exception;
            AddFormatArgs(properties, args, out exception);
            Write(eventType, exception, format, properties);
        }

#if !NETSTANDARD1_3
        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            var eventType = TraceEventType.Transfer;
            var properties = CreateTraceProperties(source, eventType, id);
            SafeAddProperty(properties, RelatedActivityIdProperty, relatedActivityId);
            Write(eventType, null, message, properties);
        }
#endif

        public override void Write(object data)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, TraceDataProperty, data);
            Write(DefaultLogLevel, null, TraceDataMessageTemplate, properties);
        }

        public override void Write(string message)
        {
            var properties = CreateProperties();
            Write(DefaultLogLevel, null, message, properties);
        }

        public override void Write(object data, string category)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, TraceDataProperty, data);
            SafeAddProperty(properties, CategoryProperty, category);
            Write(DefaultLogLevel, null, TraceDataMessageTemplate, properties);
        }

        public override void Write(string message, string category)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, CategoryProperty, category);
            Write(DefaultLogLevel, null, message, properties);
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }

        public override void WriteLine(object data)
        {
            Write(data);
        }

        public override void WriteLine(string message, string category)
        {
            Write(message, category);
        }

        public override void WriteLine(object data, string category)
        {
            Write(data, category);
        }

        private void AddFormatArgs(IList<LogEventProperty> properties, object[] args, out Exception exception)
        {
            exception = null;
            if (args != null)
            {
                for (var argIndex = 0; argIndex < args.Length; argIndex++)
                {
                    SafeAddProperty(properties, argIndex.ToString(CultureInfo.InvariantCulture), args[argIndex]);
                    // If there is any argument of type Exception (last wins), then use it
                    if (args[argIndex] is Exception)
                    {
                        exception = (Exception)args[argIndex];
                    }
                }
            }
        }

        private IList<LogEventProperty> CreateFailProperties()
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, TraceEventTypeProperty, "Fail");
            return properties;
        }

        private IList<LogEventProperty> CreateProperties()
        {
            var properties = new List<LogEventProperty>();
#if !NETSTANDARD1_3
            SafeAddProperty(properties, ActivityIdProperty, Trace.CorrelationManager.ActivityId);
#endif
            return properties;
        }

        private IList<LogEventProperty> CreateTraceProperties(string source, TraceEventType eventType, int id)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, SourceProperty, source);
            SafeAddProperty(properties, TraceEventTypeProperty, eventType);
            SafeAddProperty(properties, EventIdProperty, id);
            return properties;
        }

        private void SafeAddProperty(IList<LogEventProperty> properties, string name, object value)
        {
            LogEventProperty property;
            if (logger.BindProperty(name, value, false, out property))
            {
                properties.Add(property);
            }
        }

        private void Write(TraceEventType eventType, Exception exception, string messageTemplate, IList<LogEventProperty> properties)
        {
            var level = ToLogEventLevel(eventType);
            Write(level, exception, messageTemplate, properties);
        }

        private void Write(LogEventLevel level, Exception exception, string messageTemplate, IList<LogEventProperty> properties)
        {
            // If user has passed null, then still log (as an empty message)
            if (messageTemplate == null)
            {
                messageTemplate = string.Empty;
            }
            MessageTemplate parsedTemplate;
            IEnumerable<LogEventProperty> boundProperties;
            // boundProperties will be empty and can be ignored
            if (logger.BindMessageTemplate(messageTemplate, null, out parsedTemplate, out boundProperties))
            {
                var logEvent = new LogEvent(DateTimeOffset.Now, level, exception, parsedTemplate, properties);
                logger.Write(logEvent);
            }
        }

        private void WriteData(TraceEventType eventType, IList<LogEventProperty> properties, object data)
        {
            var level = ToLogEventLevel(eventType);
            SafeAddProperty(properties, TraceDataProperty, data);
            Write(level, null, TraceDataMessageTemplate, properties);
        }

        internal static LogEventLevel ToLogEventLevel(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    {
                        return LogEventLevel.Fatal;
                    }
                case TraceEventType.Error:
                    {
                        return LogEventLevel.Error;
                    }
                case TraceEventType.Information:
                    {
                        return LogEventLevel.Information;
                    }
                case TraceEventType.Warning:
                    {
                        return LogEventLevel.Warning;
                    }
                case TraceEventType.Verbose:
                    {
                        return LogEventLevel.Verbose;
                    }
                default:
                    {
                        return LogEventLevel.Debug;
                    }
            }
        }
    }
}
