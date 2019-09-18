using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Shuriken.Wpf")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Shuriken")]
[assembly: AssemblyCopyright("© 2016-2019 Michael Damatov.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AllowPartiallyTrustedCallers]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("50F3B06C-90C5-4BFA-8652-F635959B7CFE")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.3.2.0")]
[assembly: AssemblyFileVersion("1.3.2")]

[assembly: XmlnsDefinition("http://schemas.shuriken/view-models", @"Shuriken")]
[assembly: XmlnsPrefix("http://schemas.shuriken/view-models", "sh")]