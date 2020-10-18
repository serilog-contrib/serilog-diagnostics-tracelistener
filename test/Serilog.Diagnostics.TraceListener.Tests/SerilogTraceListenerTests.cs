﻿using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Diagnostics.TraceListener.Tests.Support;

namespace Serilog.Diagnostics.TraceListener.Tests
{
    [TestFixture]
    public class SerilogTraceListenerTests
    {
        const TraceEventType WarningEventType = TraceEventType.Warning;
        readonly string _category = Some.String("category");
        readonly int _id = Some.Int();
        readonly string _message = Some.String("message");
        readonly string _source = Some.String("source");
        readonly TraceEventCache _traceEventCache = new TraceEventCache();

        SerilogTraceListener _traceListener;
        LogEvent _loggedEvent;

        [SetUp]
        public void SetUp()
        {
            var delegatingSink = new DelegatingSink(evt => { _loggedEvent = evt; });
            var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(delegatingSink).CreateLogger();
            Log.Logger = logger;    //for the NULL-constructor "ItStillWorksIfTheClrUsedTheNullConstructor" test below

            _loggedEvent = null;
            _traceListener = new SerilogTraceListener(logger);
        }

        [TearDown]
        public void TearDown()
        {
            _traceListener.Dispose();
        }

        [Test]
        public void CapturesWrite()
        {
            _traceListener.Write(_message);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
            LogEventAssert.HasPropertyValue(typeof(SerilogTraceListener).ToString(), "SourceContext", _loggedEvent);
        }

        [Test]
        public void CapturesWriteWithCategory()
        {
            _traceListener.Write(_message, _category);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
            LogEventAssert.HasPropertyValue(_category, "Category", _loggedEvent);
        }

        [Test]
        public void CapturesWriteOfObject()
        {
            var writtenObject = Tuple.Create(Some.String());
            _traceListener.Write(writtenObject);

            LogEventAssert.HasMessage("\"" + writtenObject + "\"", _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
        }

        [Test]
        public void WriteOfObjectHasTraceDataProperty()
        {
            var writtenObject = new[] {
                Some.Int(),
                Some.Int()
            };
            _traceListener.Write(writtenObject);

            LogEventAssert.HasMessage($"[{writtenObject[0]}, {writtenObject[1]}]", _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);

            var sequence = ((SequenceValue)_loggedEvent.Properties["TraceData"]).Elements.Select(pv => pv.LiteralValue());
            Assert.That(sequence, Is.EquivalentTo(writtenObject), "The property value was not as expected");
        }

        [Test]
        public void CapturesWriteOfObjectWithCategory()
        {
            var writtenObject = Tuple.Create(Some.String());
            _traceListener.Write(writtenObject, _category);

            LogEventAssert.HasMessage("\"" + writtenObject + "\"", _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
            LogEventAssert.HasPropertyValue(_category, "Category", _loggedEvent);
        }

        [Test]
        public void CapturesWriteLine()
        {
            _traceListener.WriteLine(_message);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
        }

        [Test]
        public void CapturesWriteLineWithCategory()
        {
            _traceListener.WriteLine(_message, _category);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
            LogEventAssert.HasPropertyValue(_category, "Category", _loggedEvent);
        }

        [Test]
        public void CapturesWriteLineOfObject()
        {
            var writtenObject = Tuple.Create(Some.String());
            _traceListener.WriteLine(writtenObject);

            LogEventAssert.HasMessage("\"" + writtenObject + "\"", _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
        }

        [Test]
        public void CapturesWriteLineOfObjectWithCategory()
        {
            var writtenObject = Tuple.Create(Some.String());
            _traceListener.WriteLine(writtenObject, _category);

            LogEventAssert.HasMessage("\"" + writtenObject + "\"", _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
            LogEventAssert.HasPropertyValue(_category, "Category", _loggedEvent);
        }

        [Test]
        public void CapturesFail()
        {
            _traceListener.Fail(_message);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Fatal, _loggedEvent);
        }

        [Test]
        public void CapturesFailWithDetailedDescription()
        {
            var detailMessage = Some.String();

            _traceListener.Fail(_message, detailMessage);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Fatal, _loggedEvent);
            LogEventAssert.HasPropertyValue(detailMessage, "FailDetails", _loggedEvent);
        }

#if !NETCOREAPP1_0
        [Test]
        public void ContinuesLoggingAfterCloseIsCalled()
        {
            _traceListener.Close();

            _traceListener.Write(_message);

            LogEventAssert.HasMessage(_message, _loggedEvent);
        }
#endif

        [Test]
        public void CapturesTraceEvent()
        {
            _traceListener.TraceEvent(_traceEventCache, _source, WarningEventType, _id);

            LogEventAssert.HasMessage($"{_source} {WarningEventType}: {_id}", _loggedEvent);

            LogEventAssert.HasLevel(LogEventLevel.Warning, _loggedEvent);

            LogEventAssert.HasPropertyValue(_id, "TraceEventId", _loggedEvent);
            LogEventAssert.HasPropertyValue(_source, "TraceSource", _loggedEvent);
            LogEventAssert.HasPropertyValue(WarningEventType, "TraceEventType", _loggedEvent);
        }

        [Test]
        public void CapturesTraceEventWithMessage()
        {
            _traceListener.TraceEvent(_traceEventCache, _source, WarningEventType, _id, _message);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Warning, _loggedEvent);
            LogEventAssert.HasPropertyValue(_id, "TraceEventId", _loggedEvent);
            LogEventAssert.HasPropertyValue(_source, "TraceSource", _loggedEvent);
            LogEventAssert.HasPropertyValue(WarningEventType, "TraceEventType", _loggedEvent);
        }

        [Test]
        public void CapturesTraceEventWithFormatMessage()
        {
            const string format = "{0}-{1}-{2}";
            var args = new object[]
            {
                Some.Int(),
                Some.Int(),
                Some.Int()
            };

            _traceListener.TraceEvent(_traceEventCache, _source, WarningEventType, _id, format, args);

            LogEventAssert.HasMessage(string.Format(format, args[0], args[1], args[2]), _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Warning, _loggedEvent);
            LogEventAssert.HasPropertyValue(_id, "TraceEventId", _loggedEvent);
            LogEventAssert.HasPropertyValue(_source, "TraceSource", _loggedEvent);
            LogEventAssert.HasPropertyValue(WarningEventType, "TraceEventType", _loggedEvent);
            LogEventAssert.HasPropertyValue(args[0], "0", _loggedEvent);
            LogEventAssert.HasPropertyValue(args[1], "1", _loggedEvent);
            LogEventAssert.HasPropertyValue(args[2], "2", _loggedEvent);
        }

#if !NETCOREAPP1_0
        [Test]
        public void CapturesTraceTransfer()
        {
            var relatedActivityId = Some.Guid();

            _traceListener.TraceTransfer(_traceEventCache, _source, _id, _message, relatedActivityId);

            LogEventAssert.HasMessage(_message, _loggedEvent);
            LogEventAssert.HasLevel(LogEventLevel.Debug, _loggedEvent);
            LogEventAssert.HasPropertyValue(_id, "TraceEventId", _loggedEvent);
            LogEventAssert.HasPropertyValue(_source, "TraceSource", _loggedEvent);
            LogEventAssert.HasPropertyValue(relatedActivityId, "RelatedActivityId", _loggedEvent);
            LogEventAssert.HasPropertyValue(TraceEventType.Transfer, "TraceEventType", _loggedEvent);
        }
#endif

        [Test]
        public void CapturesTraceData()
        {
            var data = new
            {
                Info = Some.String()
            };

            _traceListener.TraceData(_traceEventCache, _source, WarningEventType, _id, data);

            LogEventAssert.HasMessage(
                "\"" + data + "\"",
                _loggedEvent);

            LogEventAssert.HasLevel(LogEventLevel.Warning, _loggedEvent);

            LogEventAssert.HasPropertyValue(_id, "TraceEventId", _loggedEvent);
            LogEventAssert.HasPropertyValue(_source, "TraceSource", _loggedEvent);
            LogEventAssert.HasPropertyValue(data.ToString(), "TraceData", _loggedEvent);
            LogEventAssert.HasPropertyValue(WarningEventType, "TraceEventType", _loggedEvent);
        }

        [Test]
        public void CapturesTraceDataArrayOfInt()
        {
            var data = new[]
            {
                Some.Int(),
                Some.Int()
            };

            _traceListener.TraceData(_traceEventCache, _source, WarningEventType, _id, data);

            LogEventAssert.HasMessage(
                $"[{data[0]}, {data[1]}]",
                _loggedEvent);

            LogEventAssert.HasLevel(LogEventLevel.Warning, _loggedEvent);

            LogEventAssert.HasPropertyValue(_id, "TraceEventId", _loggedEvent);
            LogEventAssert.HasPropertyValue(_source, "TraceSource", _loggedEvent);

            var sequence = ((SequenceValue)_loggedEvent.Properties["TraceData"]).Elements.Select(pv => pv.LiteralValue());
            Assert.That(sequence, Is.EquivalentTo(data), "The property value was not as expected");
        }

        [Test]
        public void CapturesTraceDataWithMultipleData()
        {
            var data1 = new
            {
                Info = Some.String()
            };
            var data2 = new
            {
                Info = Some.String()
            };
            var data3 = new
            {
                Info = Some.Int()
            };

            _traceListener.TraceData(_traceEventCache, _source, WarningEventType, _id, data1, data2, data3);

            // The square-brackets ('[' ,']') are because of serilog behavior and are not how the stock TraceListener would behave.
            LogEventAssert.HasMessage(
                $"[\"{string.Join("\", \"", data1, data2, data3)}\"]",
                _loggedEvent);

            LogEventAssert.HasLevel(LogEventLevel.Warning, _loggedEvent);

            LogEventAssert.HasPropertyValue(_id, "TraceEventId", _loggedEvent);
            LogEventAssert.HasPropertyValue(_source, "TraceSource", _loggedEvent);
            LogEventAssert.HasPropertyValueSequenceValue(new object[]
            {
                data1,
                data2,
                data3
            }, "TraceData", _loggedEvent);
            LogEventAssert.HasPropertyValue(WarningEventType, "TraceEventType", _loggedEvent);
        }

        [Test]
        public void CanLogFromTraceSourceInformation()
        {
            var logMessage = "a simple message";
            var traceSource = new TraceSource("test", SourceLevels.All);
            traceSource.Listeners.Clear();
            traceSource.Listeners.Add(_traceListener);

            traceSource.TraceInformation(logMessage);

            LogEventAssert.HasLevel(LogEventLevel.Information, _loggedEvent);
            LogEventAssert.HasMessage(logMessage, _loggedEvent);
            LogEventAssert.HasPropertyValue("test", "TraceSource", _loggedEvent);
        }

        [Test]
        public void CorrectlyOrdersFormatArgs()
        {
            const string format = "{1}-{0}-{1}";
            var args = new object[]
            {
                Some.Int(),
                Some.Int(),
                Some.Int()
            };

            _traceListener.TraceEvent(_traceEventCache, _source, WarningEventType, _id, format, args);

            LogEventAssert.HasMessage(args[1] + "-" + args[0] + "-" + args[1], _loggedEvent);
            LogEventAssert.HasPropertyValue(args[2], "2", _loggedEvent);
        }

        [Test]
        public void HandlesEmptyFormatString()
        {
            const string format = "";
            var args = new object[]
            {
                Some.Int(),
                Some.Int(),
                Some.Int()
            };

            _traceListener.TraceEvent(_traceEventCache, _source, WarningEventType, _id, format, args);

            LogEventAssert.HasMessage(string.Empty, _loggedEvent);
            LogEventAssert.HasPropertyValue(args[0], "0", _loggedEvent);
            LogEventAssert.HasPropertyValue(args[1], "1", _loggedEvent);
            LogEventAssert.HasPropertyValue(args[2], "2", _loggedEvent);
        }

        [Test]
        public void HandlesNullFormatString()
        {
            var args = new object[]
            {
                Some.Int(),
                Some.Int(),
                Some.Int()
            };

            // ReSharper disable once AssignNullToNotNullAttribute
            _traceListener.TraceEvent(_traceEventCache, _source, WarningEventType, _id, null, args);

            LogEventAssert.HasMessage(string.Empty, _loggedEvent);
            LogEventAssert.HasPropertyValue(args[0], "0", _loggedEvent);
            LogEventAssert.HasPropertyValue(args[1], "1", _loggedEvent);
            LogEventAssert.HasPropertyValue(args[2], "2", _loggedEvent);
        }

        [Test]
        public void ItRespectsTraceFilters()
        {
            _traceListener.Filter = new DummyFilter(id => id % 2 == 0);

            _traceListener.TraceData(new TraceEventCache(), _source, TraceEventType.Information, 3, _message);
            Assert.Null(_loggedEvent);

            _traceListener.TraceData(new TraceEventCache(), _source, TraceEventType.Information, 4, _message);
            Assert.NotNull(_loggedEvent);
        }

        [Test]
        public void ItStillWorksIfTheClrUsedTheNullConstructor()
        {
            var traceListener = new SerilogTraceListener();
            traceListener.Write("Test");
            Assert.NotNull(_loggedEvent);
        }

        class DummyFilter: TraceFilter
        {
            readonly Predicate<int> _predicate;

            public DummyFilter(Predicate<int> predicate)
            {
                _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            }

            public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
            {
                return _predicate(id);
            }
        }
    }
}
