using Eto.Forms;
using System;
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
			Verbose("Setting up app quit handling");
			Command quitAppCommand = new()
			{
					MenuText = "Quit App",
					ID       = "[Command] Quit App",
					Shortcut = Application.Instance!.CommonModifier | Keys.Q,
					ToolTip  = "Quits the application by sending the quit signal"
			};

			Closed                  += MainFormClosed;
			quitAppCommand.Executed += MainFormClosed;

			Menu.QuitItem = new ButtonMenuItem(quitAppCommand){ID = "[MenuItem] Quit App"};
			Verbose("Quit handling added");

		}
	#endregion
	}

	private static void MainFormClosed(object? o, EventArgs eventArgs)
	{
		Information("Main form closed");
		if (Application.Instance.QuitIsSupported)
		{
			Debug("Sending quit signal");
			Application.Instance.Quit();
		}
		else
		{
			Debug("Quit not supported");
		}
	}

}