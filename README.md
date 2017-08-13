# Shuriken [![NuGet](https://img.shields.io/nuget/v/Shuriken.svg)](https://www.nuget.org/packages/Shuriken) [![ReSharper-Gallery](https://img.shields.io/badge/resharper--gallery-v1.1.0-lightgrey.svg)](https://resharper-plugins.jetbrains.com/packages/Shuriken.Annotations)

Fully automated MVVM library without code rewriting. There is no magic behind it: a background thread monitors object properties (explicitly annotated as `[Observable]`), checks their values by comparing with their previous values, and raises change notifications "in the name" of the object.

The library utilizes the three-phase approach in each cycle.

### Phase 1: Collecting property values

The UI thread is used to retrieve property values for currently observed objects. The single context switch is used to read property values. If the object is known to be thread-safe its property values are read directly, i.e. without first switching to the UI thread.

The reflection is _not_ used to retrieve property values. During the object type registration a static method is emitted to read the property value.

### Phase 2: Analyzing changes

The library compares the current property values with their previous value and creates the list if changed properties. The analysis is made in the monitoring thread.

### Phase 3: Raising change notifications

If the list of changed properties is not empty, a single context switch is made to the UI thread to raise all change notifications. The change notifications are always raised in the UI thread even for objects that are known to be thread-safe.

### Registration

The _object type_ registration is triggered automatically when the `PropertyChanged` event is attached for the first time. The object type is scanned for public properties that are annotated with the `[Observable]` attribute. For each discovered property a static method is emitted. The scanning result is cached for that object type.

When the first `PropertyChanged` event is attached, the object is treated as _observed_. The library uses weak references to observed objects. When the last `PropertyChanged` event is detached, the library stops the object observation.

## Examples

### Simple Observable Object

The object should derive the base `ObservableObject` and annotate the properties with the `[Observable]` attribute:

```csharp
public class Person : ObservableObject
{
    [Observable]
    public string Name { get; set; }

    [Observable]
    public int Age { get; set; }
}
```

### Thread-Safe Observable Object

Such an object should pass `true` to the base constructor. Note that the library _does not verify_ whether the object is truly thread-safe. It uses the information only to avoid context switches to the UI thread to read values of the observable properties.

```csharp
public class ThreadSafePerson : ObservableObject
{
    internal ThreadSafePerson() : base(true) { }

    [Observable]
    public string Name { get; set; }

    [Observable]
    public int Age { get; set; }
}
```

### Initialization

The program bootstrap should establish a _monitoring scope_ around the application `Run` method. The typical `Main` method looks like this:

```csharp
[STAThread]
static void Main()
{
    var app = new App();
    app.InitializeComponent();

    var applicationMonitorScope = new ApplicationMonitorScope(new WpfNotificationContext(app.Dispatcher));
    try
    {
        app.Run();
    }
    finally
    {
        applicationMonitorScope.Dispose().GetAwaiter().GetResult();
    }
}
```

## Major Benefits
- No need to call `NotifyPropertyChange` in property setters, just annotate the property with `[Observable]` attribute. The library tracks property values, compares them with their previous value and issues property change notification (if needed) in the name of the object.
- No need to track internal dependencies between observable properties, i.e. "if the property `X` changes the property `Y` must be notified as well". This ensures less error-prone code.
- No need to notify `Command` properties that the `CanExecute` method can return another value.

## Best Practices
- do not observe properties with never changing values
- do not observe commands with never changing "can execute" results
- avoid observable properties with heavy-weight getters
- do not observe properties/commands when the UI must react instantly, call `NotifyPropertyChange` instead
- use fewer observable properties/commands (prefer converters over properties)
- use virtualization where possible
- consider suspending when the app is minimized
- do not annotate indexers with `[Observable]` attribute

See [In-depth look into the guidelines](docs/Guidelines.md)

## Logging and Performance Monitoring

The Shuriken library makes extensive use of the Event Tracing for Windows (ETW) for logging as well as for reporting performance.

*Note:* when observed properties throw exceptions the monitoring is not interrupted, the exceptions are just logged. The same approach is also applied when command `CanExecute` or `Execute` methods fail. So it is strongly recommended to turn on event capturing and writing to the Output window (at least for debug sessions).

See [In-depth look into logging and performance monitoring](docs/Etw.md)

## Installation
Use the NuGet package manager to install the package.

:bulb: *ReSharper users*: use the Extension Manager to install the external annotations for the library.

## Limitations
The library currently supports the WPF only.

## Bugs? Questions? Suggestions?
Please feel free to [report them](https://github.com/michael-damatov/shuriken/issues).
