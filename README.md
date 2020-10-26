# Serilog `TraceListener` [![NuGet](https://img.shields.io/nuget/v/SerilogTraceListener.svg?maxAge=2592000)](https://nuget.org/packages/SerilogTraceListener) [![Build status](https://ci.appveyor.com/api/projects/status/4f33pmp3txc8jnnk?svg=true)](https://ci.appveyor.com/project/NicholasBlumhardt/serilogtracelistener)

This library provides a `System.Diagnostics.TraceListener` implementation that outputs to Serilog. This means that output from third-party libraries using `System.Diagnostics.Trace` can be collected through the Serilog pipeline.

### Getting started

Before using this package, [Serilog](http://serilog.net) needs to be installed and configured in the application.

### Installing from NuGet

The package on NuGet is _SerilogTraceListener_:

```powershell
Install-Package SerilogTraceListener -DependencyVersion Highest
```

### Enabling the listener (code)

After configuring Serilog, create a `SerilogTraceListener` and add it to the `System.Diagnostics.Trace.Listeners` collection:

```csharp
var listener = new Serilog.Diagnostics.SerilogTraceListener();
Trace.Listeners.Add(listener);
```

This will write the events through the static `Log` class. Alternatively, a specific logger instance can be used instead:

```csharp
Serilog.ILogger logger = ...
var listener = new Serilog.Diagnostics.SerilogTraceListener(logger);
```

### Enabling the listener (XML)

To enable the listener through XML in `App.config` or `Web.config`, add it to the `system.diagnostics/trace/listeners` collection:

```xml
  <system.diagnostics>
    <trace autoflush="true">
      <listeners>
        <add name="Serilog"
             type="Serilog.Diagnostics.SerilogTraceListener, Serilog.Diagnostics.TraceListener"
             initializeData="Some.Source.Context" />
      </listeners>
    </trace>
  </system.diagnostics>
```

A `SourceContext` value can optionally be provided through `initializeData`.
