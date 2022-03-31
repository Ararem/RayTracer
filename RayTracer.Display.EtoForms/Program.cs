using Eto;
using Eto.Forms;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace RayTracer.Display.EtoForms;

internal class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		Core.Logger.Init();

		new Application(Platform.Detect!).Run(new MainForm());
		return 0;
	}
}