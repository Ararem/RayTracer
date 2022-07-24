using Eto.Drawing;
using Eto.Forms;
using JetBrains.Annotations;
using System.Reflection;
using static Serilog.Log;

namespace Ararem.RayTracer.Display.Dev.Resources;

public static class ResourceManager
{
	static ResourceManager()
	{
		Information("Initialising Resource Manager");
		// Assembly resourceAssembly = Assembly.GetExecutingAssembly();
		// Debug("Assembly for resources is {Assembly}", resourceAssembly);
		{
			const string iconPathPng = "Ararem.RayTracer.Display.Dev.Resources.icon.png";
			Verbose("App Icon Png ({Path}): {Icon}", iconPathPng, AppIcon = Icon.FromResource(iconPathPng));

		}
		Information("Initialised Resource Manager");
	}
	public static Icon AppIcon { get; }
}