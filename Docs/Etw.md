# Logging and Performance Monitoring

The [Shuriken](../) library makes extensive use of the Event Tracing for Windows (ETW) for logging as well as for reporting performance.

## Logging

The following table contains the explosed events:

|Event Name|Event ID|Event Level|Opcode|Payload|Message|
---|---:|---|---|---|---
|`MonitorStart`|1|Informational|Start||Monitor has been started.|
|`MonitorStop`|2|Informational|Stop||Monitor has been stopped.|
|`MonitorSuspend`|3|Informational|Suspend||Monitor has been suspended.|
|`MonitorResume`|4|Informational|Resume||Monitor has been resumed.|
|`StoppingDueToFailedUpdate`|5|Error|Info|`exception`|Stopping the monitoring because of an exception while updating the values: {0}|
|`StoppingDueToFailedChangeNotifications`|6|Error|Info|`exception`|Stopping the monitoring because of an exception while sending the change notifications: {0}|
|`FailedAttachingSystemEvent`|7|Warning|Info|`systemEvent`, `exception`|Failed attaching the system event '{0}': {1}|
|`MissingMonitoringScope`|8|Warning|Info|`eventName`, `scope`|The {0} event handler is assigned, but the {1} is not available.|
|`UnableInitiallyToReadProperty`|9|Warning|Info|`type`, `property`, `exception`|Cannot initially get the value of the '{1}' property of the '{0}' object: {2}|
|`UnableSubsequentlyToReadProperty`|10|Warning|Info|`type`, `property`, `exception`|Cannot get the value of the '{1}' property of the '{0}' object: {2}|
|`UnableInitiallyToInvokeCommandMethod`|11|Warning|Info|`type`, `property`, `method`, `exception`|Cannot initially invoke the '{2}' method of the '{1}' property of the '{0}' object: {3}|
|`UnableSubsequentlyToInvokeCommandMethod`|12|Warning|Info|`type`, `property`, `method`, `exception`|Cannot invoke the '{2}' method of the '{1}' property of the '{0}' object: {3}|
|`UnableToAnalyzeProperty`|13|Warning|Info|`type`, `property`, `exception`|Cannot analyze the value of the '{1}' property of the '{0}' object: {2}|
|`UnableToRaisePropertyChangeNotification`|14|Warning|Info|`type`, `property`, `exception`|Cannot raise the change notification for the '{1}' property of the '{0}' object: {2}|
|`UnableToRaiseCommandPropertyChangeNotification`|15|Warning|Info|`type`, `property`, `exception`|Cannot raise the change notification for the '{1}' command property of the '{0}' object: {2}|
|`CommandFailed`|16|Warning|Info|`exception`|Command execution failed: {0}|

All logging events are traced to the *Operational* channel.

## Measuring Performance

The following table contains the explosed events:

|Event Name|Event ID|Payload|Remarks|
---|---:|---|---
|`PerformanceMonitoredProperties`|19|`count`|Number of monitored properties. Lower value is better. A high value can indicate an unnecessary observation or memory leaks (e.g. due to missing UI virtualization).|
|`PerformanceCycleTime`|20|`elapsedMilliseconds`|Complete cycle time [ms]. Lower value is better. For smooth performance it should not exceed 15ms.|
|`PerformanceLists`|21|`capacityThreadAffine`, `countThreadAffine`, `capacityThreadSafe`, `countThreadSafe`|Total number and number of used slots.|
|`PerformanceListsWithChangedProperties`|22|`capacityThreadAffine`, `countThreadAffine`, `capacityThreadSafe`, `countThreadSafe`|Total number and number of used slots with changed properties.|
|`PerformanceListsWithItemsToBeRemoved`|23|`capacityThreadAffine`, `countThreadAffine`, `capacityThreadSafe`, `countThreadSafe`|Total number and number of used slots with items to be removed.|

All performance-related events are traced to the *Analytic* channel.

## Capturing Events from the Application

Use the static events of the `Shuriken.Diagnostics.EventListener` class to receive notifications when events are traced.

### Using the Output Window in Visual Studio

Capture events from the *Operational* channel and create debug message using the `ToDebugMessage` extension method (also defined in the `Shuriken.Diagnostics.EventListener` class). Consider surrounding the tracing with the `#if DEBUG` directive when the event handler only invokes the `Debug.WriteLine` method:

```csharp
[STAThread]
static void Main()
{
#if DEBUG
    Shuriken.Diagnostics.EventListener.OperationalEvent += (_, e) => Debug.WriteLine(e.ToDebugMessage());
#endif

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

*Note:* use this approach to be notified when observed properties, commands' `CanExecute` and `Execute` methods throw exceptions.