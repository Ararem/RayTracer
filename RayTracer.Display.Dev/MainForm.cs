using Eto.Forms;
using System;
using System.Diagnostics;
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

			Command quitAppCommand = new(QuitAppCommandExecuted)
			{
					MenuText = "Quit App",
					ID       = "[Command] Quit App",
					Shortcut = Application.Instance.CommonModifier | Keys.Q,
					ToolTip  = "Quits the application by sending the quit signal"
			};
			Closed        += MainFormClosed;
			Menu.QuitItem =  new ButtonMenuItem(quitAppCommand) { ID = "[MenuItem] Quit App" };

			Verbose("Quit handling added");
		}

		{
			Verbose("Setting up about app menu");
			Command aboutCommand = new(AboutAppCommandExecuted)
			{
					MenuText = "About App",
					ID       = "[Command] About App",
					ToolTip  = "Display information about the application in a popup dialog"
			};
			Menu.AboutItem = aboutCommand;
			Verbose("Set up about app menu");
		}

	#endregion
	}

	/// <summary>Callback for when the [Quit App] command is executed</summary>
	private void QuitAppCommandExecuted(object? sender, EventArgs e)
	{
		Debug("Closing main form");
		Close();
	}

	/// <summary>Callback for when the [About App] command is executed</summary>
	private void AboutAppCommandExecuted(object? o, EventArgs eventArgs)
	{
		new AboutDialog(Assembly.GetExecutingAssembly()).ShowDialog(this);
	}

	/// <summary>Quits the app, normally because the <see cref="MainForm"/> was closed (also gets called when the quit command button is pressed)</summary>
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

		Debug("Main form closed");
		if (Application.Instance.QuitIsSupported)
		{
			Verbose("Sending quit signal");
			Application.Instance.Quit();
			Verbose("Quit signal sent");
		}
		else
		{
			Error("Quit not supported");
		}
	}
}