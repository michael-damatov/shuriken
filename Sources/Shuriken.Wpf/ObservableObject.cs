using System.Windows.Input;

namespace Shuriken
{
    /// <summary>
    /// Represents a base class for observable objects.
    /// </summary>
    /// <remarks>
    /// In order to provide public observable properties (e.g. for binding) use the following guidelines to define properties:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Case</term>
    ///         <description>Guideline</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Regular properties</term>
    ///         <description>
    ///             ALWAYS annotate the property with the <see cref="ObservableAttribute"/>. The property values are tracked automatically.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Immutable properties</term>
    ///         <description>
    ///             NEVER annotate the property with the <see cref="ObservableAttribute"/>. It's not needed to track the property values as they
    ///             never change.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Indexers</term>
    ///         <description>
    ///             NEVER annotate the property with the <see cref="ObservableAttribute"/>. An indexer cannot be tracked automatically. Use the
    ///             <see cref="NotifyIndexerChange"/> method to send notifications.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Command"/> properties (declared property type)</term>
    ///         <description>
    ///             CONSIDER annotating the property with the <see cref="ObservableAttribute"/>. Even if the property is immutable (never changes)
    ///             the <see cref="ParameterlessCommand.CanExecute()"/> can change. However, if the <see cref="ParameterlessCommand.CanExecute()"/>
    ///             never changes (e.g. only <c>false</c> while the command is being executed) the property should not be annotated with the
    ///             <see cref="ObservableAttribute"/>.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="AsyncCommand"/> properties (declared property type)</term>
    ///         <description>
    ///             ALWAYS annotate the property with the <see cref="ObservableAttribute"/>. Even if the property is immutable (never changes) the
    ///             <see cref="ParameterlessCommand.CanExecute()"/> can change. The <see cref="ParameterlessCommand.CanExecute()"/> always returns
    ///             <c>false</c> while the command is being executed.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Command{T}"/> properties (declared property type)</term>
    ///         <description>
    ///             CONSIDER not annotating the property with the <see cref="ObservableAttribute"/>. Changes of
    ///             <see cref="ParameterizedCommand{T}.CanExecute(T)"/> cannot be tracked automatically. Use the
    ///             <see cref="CommandBase.NotifyCanExecuteChanged"/> method to send notifications. Only if the property is not immutable it should be
    ///             annotated with the <see cref="ObservableAttribute"/>.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="AsyncCommand{T}"/> properties (declared property type)</term>
    ///         <description>
    ///             ALWAYS annotate the property with the <see cref="ObservableAttribute"/>. Even if the property is immutable (never changes) and the
    ///             <see cref="ParameterizedCommand{T}.CanExecute(T)"/> cannot be tracked automatically the method will always return <c>false</c>
    ///             while the command is being executed.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="ICommand"/> properties (declared property type)</term>
    ///         <description>
    ///             CONSIDER not annotating the property with the <see cref="ObservableAttribute"/>. Changes of
    ///             <see cref="ICommand.CanExecute"/> cannot be tracked automatically. Only if the property is not immutable it should be annotated
    ///             with the <see cref="ObservableAttribute"/>.
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    public abstract partial class ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableObject" /> class.
        /// </summary>
        /// <param name="isThreadSafe">if set to <c>true</c> the object is considered thread-safe.</param>
        protected ObservableObject(bool isThreadSafe = false) => IsThreadSafe = isThreadSafe;

        /// <summary>
        /// Gets a value indicating whether this instance is thread-safe.
        /// </summary>
        protected internal bool IsThreadSafe { get; }
    }
}