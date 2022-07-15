using Eto.Drawing;
using Eto.Forms;
using RayTracer.Core;
using RayTracer.Impl;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Serilog.Log;

namespace RayTracer.Display.Dev;

/// <summary>Main form class that handles much of the UI stuff</summary>
internal sealed class MainForm : Form
{
	/// <summary>Main control that contains the tabs for each of the renders</summary>
	private readonly TabControl tabControlContent;

	public MainForm()
	{
	#region Important setup to make the app run like expected

		Debug("Setting up important app functionality");
		ID = "MainForm";
		{
			Verbose("Creating MenuBar");
			Menu = new MenuBar
			{
					//Application-specific menu, gets it's own section in the menu bar
					//Since it's not really needed, I don't set it
					// ApplicationMenu  = { Items = { new Command { ToolBarText = "AppMenu.Command.ToolbarText", MenuText = "AppMenu.Command.MenuText" } },Text = "AppMenu.Text"},

					//Same for the application items - on linux this is under the "File" section
					// ApplicationItems = { new Command { ToolBarText = "AppItems.Command.ToolbarText", MenuText = "AppItems.Command.MenuText" } }
					ID = $"{ID}.Menu"
			};
			Verbose("Created MenuBar: {MenuBar}", Menu);
		}

		{
			Verbose("Setting up quit handling");
			Menu.QuitItem = new ButtonMenuItem { ID = $"{Menu.ID}.QuitItem", Text = "Quit App" };
			Command quitAppCommand = new(QuitAppCommandExecuted)
			{
					MenuText = Menu.QuitItem.Text,
					ID       = $"{Menu.QuitItem.ID}.Command",
					Shortcut = Application.Instance.CommonModifier | Keys.Q,
					ToolTip  = "Quits the application by sending the quit signal"
			};
			Menu.QuitItem.Command =  quitAppCommand;
			Closed                += MainFormClosed;
			Verbose("Set Menu.QuitItem: {MenuItem}", Menu.QuitItem);
		}

		{
			Verbose("Setting up about app menu");
			Menu.AboutItem = new ButtonMenuItem { ID = $"{Menu.ID}.AboutItem", Text = "About App" };
			Command aboutCommand = new(AboutAppCommandExecuted)
			{
					MenuText = Menu.AboutItem.Text,
					ID       = $"{Menu.AboutItem.ID}.Command",
					ToolTip  = "Display information about the application in a popup dialog"
			};
			Menu.AboutItem.Command = aboutCommand;
			Verbose("Set Menu.AboutItem: {MenuItem}", Menu.AboutItem);
		}

		{
			Verbose("Setting icon");
			const string iconPath = "RayTracer.Display.Dev.Appearance.icon.png";
			Verbose("Icon path is {IconPath}", iconPath);
			Icon    = Icon.FromResource(iconPath);
			Icon.ID = $"{ID}.Icon";
			Verbose("Set icon: {Icon}", Icon);
		}

		{
			Verbose("Toolbar disabled: {Toolbar}", ToolBar = null);
		}

		{
			Verbose("Setting window parameters");
			Verbose("Resizeable: {Resizeable}",   Resizable   = true);
			Verbose("Maximizable: {Maximizable}", Maximizable = true);
			Verbose("Minimizable: {Minimizable}", Minimizable = true);
			Verbose("MinimumSize: {MinimumSize}", MinimumSize = new Size(0,    0));
			Verbose("Size: {Size}",               Size        = new Size(1280, 720));
			Verbose("Title: {Title}",             Title       = Application.Instance.Name);
			Verbose("Maximizing");
			Maximize();
		}

	#endregion

	#region Init everything else in the UI

		Content = tabControlContent = new TabControl
		{
				ID = "[TabControl] MainForm.Content",
				Pages =
				{
						new TabPage("Page 1 Content") { Text = "Page 1" },
						new TabPage("Page 2 Content") { Text = "Page 2" },
						new TabPage("Page 3 Content") { Text = "Page 3" },
						new TabPage("Page 4 Content") { Text = "Page 4" }
				}
		};

		//Create a way for the user to create a new render
		{
			Verbose("Setting up new render button");
			MenuItem newRenderMenuItem = new ButtonMenuItem { ID = $"{Menu.ID}.NewRenderItem", Text = "New Render" };
			Menu.Items.Add(newRenderMenuItem);
			Command newRenderCommand = new(CreateNewRenderCommandExecuted)
			{
					ID       = $"{newRenderMenuItem.ID}.Command",
					MenuText = newRenderMenuItem.Text
			};
			newRenderMenuItem.Command = newRenderCommand;
			Verbose("Set up create render button: {MenuItem}", newRenderMenuItem);
		}

		{
			Verbose("Setting up close render tab command");
			MenuItem closeTabMenuItem = new ButtonMenuItem { ID = $"{Menu.ID}.CloseTabItem", Text = "Close Tab" };
			Menu.Items.Add(closeTabMenuItem);
			Command closeTabCommand = new (CloseRenderTabExecuted)
			{
					ID       = $"{closeTabMenuItem.ID}.Command",
					MenuText = closeTabMenuItem.Text,
					ToolTip  = "Stops the render, and closes the tab associated with it",
					Shortcut = Keys.Alt | Keys.A
			};
			closeTabMenuItem.Command = closeTabCommand;
			Verbose("Added close tab command: {MenuItem}", closeTabMenuItem);
		}

	#endregion
	}

#region Tab management

	/// <summary>Adds a new render tab, with the specified initial value for the render options</summary>
	/// <param name="initialRenderOptions">Initial value for the render options</param>
	/// <param name="availableScenes">Array containing all the available scenes that can be selected</param>
	/// <param name="initialSceneIndex">Index of the scene that should be selected initially. Defaults to 0</param>
	private void AddNewRenderTab(RenderOptions initialRenderOptions, Scene[] availableScenes, int initialSceneIndex = 0)
	{
		Guid guid = Guid.NewGuid();
		Verbose("Adding new render tab with GUID {Guid}", guid);
		TabPage newPage = new()
		{
				ID   = $"{tabControlContent.ID}.Pages.Page_{guid}",
				Text = "New Render"
		};
		Verbose("TabPage: {TabPage}", newPage);
		tabControlContent.Pages.Add(newPage);
	}

#endregion

#region Callbacks

	private void CloseRenderTabExecuted(object? sender, EventArgs e)
	{
		if (tabControlContent.Pages.Count == 0)
		{
			Verbose("No tab to close");
			return;
		}

		TabPage page = tabControlContent.SelectedPage;
		Verbose("Closing tab {TabPage}", page);
		tabControlContent.Remove(page);
		page.Dispose();
	}

	/// <summary>Callback for when the [Create New Render] command is executed</summary>
	private void CreateNewRenderCommandExecuted(object? sender, EventArgs e)
	{
		Information("Creating new render");
		AddNewRenderTab(new RenderOptions(), BuiltinScenes.GetAll().ToArray());
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

#endregion
}