using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("ID3-TagIT.SmokeTests")]
// Each test drives a real copy of the app on the desktop; only one may have the keyboard.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
