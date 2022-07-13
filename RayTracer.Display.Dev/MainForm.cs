using Eto.Forms;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using static Serilog.Log;

namespace RayTracer.Display.Dev;

/// <summary>Main form class that handles much of the UI stuff</summary>
internal sealed class MainForm : Form
{
	public MainForm()
	{
	#region Important setup to make the app run like expected

		Debug("Setting up important app functionality");
		{
			Verbose("Creating MenuBar");
			Menu = new MenuBar
			{
					//Application-specific menu, gets it's own section in the menu bar
					//Since it's not really needed, I don't set it
					// ApplicationMenu  = { Items = { new Command { ToolBarText = "AppMenu.Command.ToolbarText", MenuText = "AppMenu.Command.MenuText" } },Text = "AppMenu.Text"},

					//Same for the application items - on linux this is under the "File" section
					// ApplicationItems = { new Command { ToolBarText = "AppItems.Command.ToolbarText", MenuText = "AppItems.Command.MenuText" } }
			};
		}

		{
			Verbose("Setting up quit handling");

			Command quitAppCommand = new(MainFormClosed)
			{
					MenuText = "Quit App",
					ID       = "[Command] Quit App",
					Shortcut = Application.Instance.CommonModifier | Keys.Q,
					ToolTip  = "Quits the application by sending the quit signal"
			};
			Closed                  += MainFormClosed;
			Menu.QuitItem           =  new ButtonMenuItem(quitAppCommand) { ID = "[MenuItem] Quit App" };

			Verbose("Quit handling added");
		}

		{
			Verbose("Setting up about app menu");

			Command aboutCommand = new();
			Verbose("Set up about app menu");
		}

	#endregion
	}

	private static void MainFormClosed(object? o, EventArgs eventArgs)
	{
		//To prevent recursive loops where this calls itself (since `Application.Quit` calls `MainForm.Closed`)
		//Walk up the stack to check if this method or Application.Quit are present, and if so, return immediately
		MethodBase thisMethod    = MethodBase.GetCurrentMethod()!;
		MethodBase appQuitMethod = typeof(Application).GetMethod(nameof(Application.Quit))!;
		if (new StackTrace(1, false).GetFrames().Select(f => f.GetMethod()).Any(m => (m == thisMethod) || (m == appQuitMethod)))
		{
			Verbose("Closed event recursion detected, returning immediately without sending quit signal");
			return;
		}
		Information("Main form closed");
		if (Application.Instance.QuitIsSupported)
		{
			Debug("Sending quit signal");
			Application.Instance.Quit();
		}
		else
		{
			Error("Quit not supported");
		}
	}
}