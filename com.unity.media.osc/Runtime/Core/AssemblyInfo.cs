using System.Runtime.CompilerServices;

// Internal access needed for runtime assemblies
[assembly: InternalsVisibleTo("Unity.Media.Osc.Net")]
[assembly: InternalsVisibleTo("Unity.Media.Osc.Components")]

// Internal access needed for editor scripts
[assembly: InternalsVisibleTo("Unity.Media.Osc.Editor")]

// Internal access needed for testing
[assembly: InternalsVisibleTo("InternalsVisible.ToDynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Unity.Media.Osc.Tests.Editor")]
