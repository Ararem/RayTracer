using Ararem.RayTracer.Core;
using JetBrains.Annotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Ararem.RayTracer.Core.AssemblyInfo;

[assembly: InternalsVisibleTo("Ararem.RayTracer.Display.EtoForms")]
[assembly: InternalsVisibleTo("Ararem.RayTracer.Display.Dev")]
[assembly: InternalsVisibleTo("Ararem.RayTracer.Display.SpectreConsole")]
[assembly: InternalsVisibleTo("Ararem.RayTracer.Impl")]

[assembly: AssemblyDescription(Description)]
[assembly: AssemblyConfiguration(Configuration)]
[assembly: AssemblyProduct(ProductName)]
[assembly: AssemblyTitle(CoreAppName)]
[assembly: AssemblyVersion(AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(AssemblyInfo.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfo.Version)]

namespace Ararem.RayTracer.Core;

[PublicAPI]
internal static class AssemblyInfo
{
#region Constant attribute things

	public const string ProductName = "Ararem.RayTracer",
						DevAppName  = "Ararem.RayTracer.Display.Dev",
						CoreAppName = "Ararem.RayTracer.Core",
						ImplAppName = "Ararem.RayTracer.Impl",
						Description = $"A GUI application for {CoreAppName}/{ImplAppName}. Allows for ray-traced (technically path-traced) rendering of scenes defined in {ImplAppName}, via the {CoreAppName} rendering engine.",
						Version     = "1.0.0.0",
						Configuration =
								#if DEBUG
								"Debug";
	#elif RELEASE
			"Release";
	#else
			"Unknown";
	#endif

#endregion

	public const           string?               Copyright   = null,               Licence    = null;
	public static readonly IReadOnlyList<string> Designers   = new[] { "Ararem" }, Developers = new[] { "Ararem" }, Documenters = new[] { "Ararem" };
	public const           string                ProjectLink = "https://www.github.com/Ararem/RayTracer";
}