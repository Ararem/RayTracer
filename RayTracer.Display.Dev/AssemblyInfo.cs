using System.Reflection;
using static RayTracer.Display.Dev.AssemblyInfo;

[assembly: AssemblyDescription(Description)]
[assembly: AssemblyConfiguration(Configuration)]
[assembly: AssemblyProduct(ProductName)]
[assembly: AssemblyTitle(ThisAppName)]
[assembly: AssemblyVersion(Version)]
[assembly: AssemblyFileVersion(Version)]
[assembly: AssemblyInformationalVersion(Version)]

namespace RayTracer.Display.Dev;

internal static class AssemblyInfo
{
	public const string ProductName = "Ararem.RayTracer";
	public const string ThisAppName = "Ararem.RayTracer.Display.Dev";
	public const string Description = "A GUI application for Ararem.RayTracer.Core/Ararem.RayTracer.Impl";
	public const string Version     = "1.0.0.0";
	public const string Configuration =
			#if DEBUG
			"Debug";
	#elif RELEASE
			"Release";
	#else
			"Unknown";
	#endif
}