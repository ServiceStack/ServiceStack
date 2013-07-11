The zip files in this directory contain release builds of the ServiceStack client side libraries:
    - ServiceStack.Text.dll
    - ServiceStack.Interfaces.dll
    - ServiceStack.Common.dll
    
Due to MonoTouch's AOT compilation process, which relies on stripping code that is not linked directly, and ServiceStack.Text's serialization code which relies on reflection, every supported ServiceStack generic value type serializers have been explicitly preserved in these builds. This case cause large binaries to be produced, and sometimes can cause errors related to 'Could not AOT the assembly' when compiling to native code. This can potentially be resolved by explicitly including in SS.Text only the value types that your DTOs utilize, by commenting out unneeded types within the ServiceStack.Text.JsConfig.RegisterForAOT() method and rebuilding your own dlls.

See here for more details: https://bugzilla.xamarin.com/show_bug.cgi?id=9628
