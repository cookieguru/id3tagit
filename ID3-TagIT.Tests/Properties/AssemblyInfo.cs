using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("ID3-TagIT.Tests")]
// The app keeps its state in statics (Declarations), so tests can't run side by side.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
