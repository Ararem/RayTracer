using Eto;
using Eto.Forms;
using RayTracer.Display.EtoForms;
using System;
using static Serilog.Log;
using Logger = RayTracer.Core.Logger;

internal class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		Logger.Init();
		Information("Commandline args: {Args}", args);

		Platform platform;
		try
		{
			Verbose("Getting platform");
			platform = Platform.Detect!;
			Verbose("Got Platform");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms platform");
			return -1;
		}
		// Verbose("Platform is {Platform}", platform);
		MainForm form;
		try
		{
			Verbose("Creating MainForm");
			form = new MainForm();
			Verbose("Created MainForm");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise MainForm");
			return -1;
		}
		// Verbose("MainForm is {MainForm}", form);


		Information("Running App with ");
		new Application(platform).Run(form);
		return 0;
	}
}