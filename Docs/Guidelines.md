# Guidelines

The [Shuriken](/michael-damatov/shuriken) library provides the base class `ObservableObject` (which implements the `INotifyPropertyChanged` interface).

In order to provide public observable properties (e.g. for binding) use the following guidelines to define properties:

|Case|Guideline|Behavior when annotated with the `[Observable]` attribute|
---|---|---
|Regular properties|**Always** annotate the property with the `[Observable]` attribute.|The property values are tracked automatically.|
|Immutable properties|**Never** annotate the property with the `[Observable]` attribute. It's not needed to track the property values as they never change.|The property values are tracked, however, as property values never change it just consumes valuable resources.|
|Indexer|**Never** annotate the property with the `[Observable]` attribute. An indexer cannot be tracked automatically. Use the `NotifyIndexerChange` method to send notifications.|The indexer values are *not* tracked.|
|`Command` properties\*|**Consider** annotating the property with the `[Observable]` attribute. Even if the property is immutable (never changes) the `CanExecute` can change. However, if the `CanExecute` never changes the property should not be annotated with the `[Observable]` attribute.|The property values (the `Command` objects) as well as the `CanExecute` are tracked automatically.<br>*Note:* the `CanExecute` always returns `false` while the command is being executed.|
|`AsyncCommand` properties\*|**Always** annotate the property with the `[Observable]` attribute. Even if the property is immutable (never changes) the `CanExecute` can change.|The property values (the `AsyncCommand` objects) as well as the `CanExecute` are tracked automatically.<br>*Note:* the `CanExecute` always returns `false` while the command is being executed.|
|`Command<T>` properties\*|**Consider not** annotating the property with the `[Observable]` attribute. Changes of `CanExecute(T)` cannot be tracked automatically. Use the `NotifyCanExecuteChanged` method to send notifications. Only if the property is not immutable it should be annotated with the `[Observable]` attribute.|The property values (the `Command<T>` objects) are tracked, but the `CanExecute(T)` are not tracked.<br>*Note:* the `CanExecute(T)` always returns `false` while the command is being executed.|
|`AsyncCommand<T>` properties\*|**Always** annotate the property with the `[Observable]` attribute. Even if the property is immutable (never changes) and the `CanExecute(T)` cannot be tracked automatically the method will always return `false` while the command is being executed.|The property values (the `AsyncCommand<T>` objects) are tracked, but the `CanExecute(T)` are not tracked.<br>*Note:* the `CanExecute(T)` always returns `false` while the command is being executed.|
|`ICommand` properties\*|**Consider not** annotating the property with the `[Observable]` attribute. Changes of `CanExecute(object)` cannot be tracked automatically. Only if the property is not immutable it should be annotated with the `[Observable]` attribute.|The property values (the `ICommand` objects) are tracked, but the `CanExecute(object)` are not tracked.|

\* declared property types

*Note:* the `[Observable]` annotation does nothing if the class doesn't derive from the `ObservableObject`