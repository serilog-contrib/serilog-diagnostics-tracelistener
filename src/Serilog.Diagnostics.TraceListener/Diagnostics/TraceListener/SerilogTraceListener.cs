// Copyright 2015-2020 Serilog Contributors
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Serilog.Events;

namespace Serilog.Diagnostics.TraceListener
{
    /// <summary>
    /// TraceListener implementation that directs all output to Serilog.
    /// </summary>
    public class SerilogTraceListener : System.Diagnostics.TraceListener
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
        const string NoMessageTraceEventMessageTemplate = "{TraceSource:l} {TraceEventType}: {TraceEventId}";
        const string TraceDataMessageTemplate = "{TraceData}";

        readonly ILogger _logger;

        /// <summary>
        ///     Creates a SerilogTraceListener that sets logger to null so we can still use Serilog's Logger.Log
        /// </summary>
        /// <remarks>
        ///     This is needed because TraceListeners are often configured through XML
        ///     where there would be no opportunity for constructor injection
        /// </remarks>
        public SerilogTraceListener()
        {
            _logger = null;
        }

        /// <summary>
        ///     Creates a SerilogTraceListener that uses the specified logger
        /// </summary>
        public SerilogTraceListener(ILogger logger)
        {
            _logger = logger.ForContext<SerilogTraceListener>();
        }

        /// <summary>
        ///     Creates a SerilogTraceListener for the context specified.
        /// </summary>
        /// <example>
        ///     &lt;listeners&gt;
        ///         &lt;add name="Serilog" type="SerilogTraceListener.SerilogTraceListener, SerilogTraceListener" initializeData="MyContext" /&gt;
        ///     &lt;/listeners&gt;
        /// </example>
        // ReSharper disable once UnusedMember.Global
        public SerilogTraceListener(string context)
        {
            _logger = Log.Logger.ForContext("SourceContext", context);
        }

        /// <summary>
        ///     Returns the logger which SerilogTraceListener uses.
        /// </summary>
        public ILogger GetLogger()
        {
            return _logger;
        }

        /// <inheritdoc />
        public override bool IsThreadSafe => true;

        /// <inheritdoc />
        public override void Fail(string message)
        {
            var properties = CreateFailProperties();
            Write(FailLevel, null, message, properties);
        }

        /// <inheritdoc />
        public override void Fail(string message, string detailMessage)
        {
            var properties = CreateFailProperties();
            SafeAddProperty(properties, FailDetailMessageProperty, detailMessage);
            Write(FailLevel, null, message, properties);
        }

        /// <inheritdoc />
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            object data)
        {
            if (!ShouldTrace(eventCache, source, eventType, id, "", null, data, null))
            {
                return;
            }

            var properties = CreateTraceProperties(source, eventType, id);
            WriteData(eventType, properties, data);
        }

        /// <inheritdoc />
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            params object[] data)
        {
            if (!ShouldTrace(eventCache, source, eventType, id, "", null, null, data))
            {
                return;
            }

            var properties = CreateTraceProperties(source, eventType, id);
            WriteData(eventType, properties, data);
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            if (!ShouldTrace(eventCache, source, eventType, id, "", null, null, null))
            {
                return;
            }

            var properties = CreateTraceProperties(source, eventType, id);
            Write(eventType, null, NoMessageTraceEventMessageTemplate, properties);
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message)
        {
            if (!ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }

            var properties = CreateTraceProperties(source, eventType, id);
            Write(eventType, null, message, properties);
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args)
        {
            if (!ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                return;
            }

            var properties = CreateTraceProperties(source, eventType, id);
            Exception exception;
            AddFormatArgs(properties, args, out exception);
            Write(eventType, exception, format, properties);
        }

#if !NETSTANDARD1_3
        /// <inheritdoc />
        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message,
            Guid relatedActivityId)
        {
            var eventType = TraceEventType.Transfer;
            var properties = CreateTraceProperties(source, eventType, id);
            SafeAddProperty(properties, RelatedActivityIdProperty, relatedActivityId);
            Write(eventType, null, message, properties);
        }
#endif

        /// <inheritdoc />
        public override void Write(object data)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, TraceDataProperty, data);
            Write(DefaultLogLevel, null, TraceDataMessageTemplate, properties);
        }

        /// <inheritdoc />
        public override void Write(string message)
        {
            var properties = CreateProperties();
            Write(DefaultLogLevel, null, message, properties);
        }

        /// <inheritdoc />
        public override void Write(object data, string category)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, TraceDataProperty, data);
            SafeAddProperty(properties, CategoryProperty, category);
            Write(DefaultLogLevel, null, TraceDataMessageTemplate, properties);
        }

        /// <inheritdoc />
        public override void Write(string message, string category)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, CategoryProperty, category);
            Write(DefaultLogLevel, null, message, properties);
        }

        /// <inheritdoc />
        public override void WriteLine(string message)
        {
            Write(message);
        }

        /// <inheritdoc />
        public override void WriteLine(object data)
        {
            Write(data);
        }

        /// <inheritdoc />
        public override void WriteLine(string message, string category)
        {
            Write(message, category);
        }

        /// <inheritdoc />
        public override void WriteLine(object data, string category)
        {
            Write(data, category);
        }

        void AddFormatArgs(IList<LogEventProperty> properties, object[] args, out Exception exception)
        {
            exception = null;
            if (args == null) return;

            for (var argIndex = 0; argIndex < args.Length; argIndex++)
            {
                SafeAddProperty(properties, argIndex.ToString(CultureInfo.InvariantCulture), args[argIndex]);
                // If there is any argument of type Exception (last wins), then use it
                if (args[argIndex] is Exception)
                {
                    exception = (Exception) args[argIndex];
                }
            }
        }

        IList<LogEventProperty> CreateFailProperties()
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, TraceEventTypeProperty, "Fail");
            return properties;
        }

        IList<LogEventProperty> CreateProperties()
        {
            var properties = new List<LogEventProperty>();
#if !NETSTANDARD1_3
            SafeAddProperty(properties, ActivityIdProperty, Trace.CorrelationManager.ActivityId);
#endif
            return properties;
        }

        IList<LogEventProperty> CreateTraceProperties(string source, TraceEventType eventType, int id)
        {
            var properties = CreateProperties();
            SafeAddProperty(properties, SourceProperty, source);
            SafeAddProperty(properties, TraceEventTypeProperty, eventType);
            SafeAddProperty(properties, EventIdProperty, id);
            return properties;
        }

        void SafeAddProperty(IList<LogEventProperty> properties, string name, object value)
        {
            var localLogger = _logger ?? Log.Logger;
            if (localLogger.BindProperty(name, value, false, out var property))
            {
                properties.Add(property);
            }
        }

        void Write(TraceEventType eventType, Exception exception, string messageTemplate,
            IList<LogEventProperty> properties)
        {
            var level = LevelMapping.ToLogEventLevel(eventType);
            Write(level, exception, messageTemplate, properties);
        }

        void Write(LogEventLevel level, Exception exception, string messageTemplate, IList<LogEventProperty> properties)
        {
            // If user has passed null, then still log (as an empty message)
            if (messageTemplate == null)
            {
                messageTemplate = string.Empty;
            }

            var localLogger = _logger ?? Log.Logger;
            // boundProperties will be empty and can be ignored
            if (localLogger.BindMessageTemplate(messageTemplate, null, out var parsedTemplate, out _))
            {
                var logEvent = new LogEvent(DateTimeOffset.Now, level, exception, parsedTemplate, properties);
                localLogger.Write(logEvent);
            }
        }

        bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage,
            object[] args, object data1, object[] data)
        {
            return Filter?.ShouldTrace(cache, source, eventType, id, formatOrMessage, args, data1, data) ?? true;
        }

        void WriteData(TraceEventType eventType, IList<LogEventProperty> properties, object data)
        {
            var level = LevelMapping.ToLogEventLevel(eventType);
            SafeAddProperty(properties, TraceDataProperty, data);
            Write(level, null, TraceDataMessageTemplate, properties);
        }
    }
}
