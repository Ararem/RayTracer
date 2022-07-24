using Ararem.RayTracer.Core;
using Eto.Drawing;
using Eto.Forms;
using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Serilog.Log;
using static Ararem.RayTracer.Display.Dev.Resources.StyleManager;
using ResourceManager = Ararem.RayTracer.Display.Dev.Resources.ResourceManager;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>Main form class that handles much of the UI stuff</summary>
internal sealed class MainForm : Form
{
	/// <summary>Main control that contains the tabs for each of the renders</summary>
	private readonly DocumentControl tabControlContent;

	public MainForm()
	{
	#region Important setup to make the app run like expected

		Debug("Setting up main form");
		ID = "MainForm";
		{
			Verbose("Creating MenuBar");
			Menu = new MenuBar
			{
					//Application-specific menu, gets it's own section in the menu bar
					//Since it's not really needed, I don't set it
					// ApplicationMenu  = { Items = { new Command { ToolBarText = "AppMenu.Command.ToolbarText", MenuText = "AppMenu.Command.MenuText" } },Text = "AppMenu.Text"},

					//Same for the application items - on linux this is under the "File" section
					// ApplicationItems = { new Command { ToolBarText = "AppItems.Command.ToolbarText", MenuText = "AppItems.Command.MenuText" } },
					ID = $"{ID}/Menu"
			};
			Verbose("Created MenuBar: {MenuBar}", Menu);
		}

		{
			Verbose("Setting up quit handling");
			Menu.QuitItem = new ButtonMenuItem
			{
					ID       = $"{Menu.ID}/QuitItem",
					Text     = "&Quit App",
					Shortcut = Application.Instance.CommonModifier | Keys.Q,
					ToolTip  = "Quits the application"
			};
			Command quitAppCommand = new(QuitAppCommandExecuted) { ID = $"{Menu.QuitItem.ID}.Command" };
			Menu.QuitItem.Command =  quitAppCommand;
			Closed                += MainFormClosed;
			Verbose("Set Menu.QuitItem: {MenuItem}", Menu.QuitItem);
		}

		{
			Verbose("Setting up about app menu");
			Menu.AboutItem = new ButtonMenuItem
			{
					ID       = $"{Menu.ID}/AboutItem",
					Text     = "&About App",
					ToolTip  = "Display information about the application in a popup dialog",
					Shortcut = Keys.Alt | Keys.Slash //TODO: Shift doesn't work?
			};
			Command aboutCommand = new(AboutAppCommandExecuted)
			{
					ID = $"{Menu.AboutItem.ID}.Command"
			};
			Menu.AboutItem.Command = aboutCommand;
			Verbose("Set Menu.AboutItem: {MenuItem}", Menu.AboutItem);
		}

		{
			Verbose("Setting icon");
			Icon = ResourceManager.AppIconPng;
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

		{
			DynamicLayout layout = new()
			{
					ID            = $"{ID}/Layout",
					Padding       = DefaultPadding,
					Spacing = DefaultSpacing
			};
			Content = layout;
			layout.BeginVertical();
			layout.Add(
					new Label
					{
							ID = $"{layout.ID}/TitleLabel",
							Text  = AssemblyInfo.ProductName,
							Style = nameof(AppTitle),
							TextAlignment= TextAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center
					}
			);
			layout.BeginScrollable();
			layout.Add(
					tabControlContent = new DocumentControl
					{
							ID = "MainForm/Content/DocumentControl",
					}
			);
			tabControlContent.PageClosed += DocumentPageOnClosed;
			layout.EndScrollable();
			layout.EndVertical();
		}


		//Tab management
		{
			Verbose("Setting up new tab button");
			MenuItem newTabMenuItem = new ButtonMenuItem
			{
					ID       = $"{Menu.ID}/NewTabItem",
					Text     = "&New Tab",
					Shortcut = Application.Instance.CommonModifier | Keys.N,
					ToolTip  = "Creates a new render tab"
			};
			Menu.ApplicationItems.Add(newTabMenuItem);
			Command newTabCommand = new(CreateNewTabCommandExecuted) { ID = $"{newTabMenuItem.ID}.Command" };
			newTabMenuItem.Command = newTabCommand;
			Verbose("Set up new tab button: {MenuItem}", newTabMenuItem);
			newTabCommand.Execute();

			Verbose("Setting up close render tab command");
			MenuItem closeTabMenuItem = new ButtonMenuItem
			{
					ID       = $"{Menu.ID}/CloseTabItem",
					Text     = "&Close Tab",
					ToolTip  = "Closes the currently selected tab and stops the render associated with it (if possible)",
					Shortcut = Application.Instance.CommonModifier | Keys.W
			};
			Menu.ApplicationItems.Add(closeTabMenuItem);
			Command closeTabCommand = new(CloseRenderTabExecuted) { ID = $"{closeTabMenuItem.ID}.Command" };
			closeTabMenuItem.Command = closeTabCommand;
			Verbose("Added close tab command: {MenuItem}", closeTabMenuItem);
		}

	#endregion
	}

#region Callbacks
	private void CloseRenderTabExecuted(object? sender, EventArgs eventArgs)
	{
		Debug("{CallbackName}() from {Sender}: {@EventArgs}", nameof(CloseRenderTabExecuted), sender, eventArgs);

		DocumentPage oldPage = tabControlContent.SelectedPage;
		Verbose("Closing and disposing tab {TabPage}", oldPage);
		tabControlContent.Pages.Remove(oldPage);
		oldPage.Dispose();
	}

	private void DocumentPageOnClosed(object? sender, DocumentPageEventArgs eventArgs)
	{
		Debug("{CallbackName}() from {Sender}: {@EventArgs}", nameof(DocumentPageOnClosed), sender, eventArgs);

		//The UI items all collapse and everything looks kinda weird when we have 0 tabs open, so we get around this by closing the current one and opening a new tab whenever we are on the last tab
		if (tabControlContent.Pages.Count == 0)
		{
			Verbose("Just closed last tab, recreating to ensure we don't get below 1");
			CreateNewTabCommandExecuted(null, EventArgs.Empty);
		}
	}

	/// <summary>Callback for when the [Create New Render] command is executed</summary>
	private void CreateNewTabCommandExecuted(object? sender, EventArgs eventArgs)
	{
		Debug("{CallbackName}() from {Sender}: {@EventArgs}", nameof(CreateNewTabCommandExecuted), sender, eventArgs);
		Guid guid = Guid.NewGuid();
		Verbose("Adding new render tab with GUID {Guid}", guid);
		DocumentPage newPage = new()
		{
				ID    = $"{tabControlContent.ID}.Pages/Page_{guid}",
				Text  = $"Render {guid}",
				Image = Icon, //TODO: Icon reflects the render buffer...
		};
		//TODO: Tab selection thingy text styles
		Verbose("New TabPage: {TabPage}", newPage);
		tabControlContent.Pages.Add(newPage);

		RenderJobTrackingTab tracker = new($"{newPage.ID}/{nameof(RenderJobTrackingTab)}");
		Verbose("Render Tracker: {RenderTracker}", tracker);
		newPage.Content = tracker;
	}

	/// <summary>Callback for when the [Quit App] command is executed</summary>
	[ContractAnnotation("=> halt")]
	private void QuitAppCommandExecuted(object? sender, EventArgs eventArgs)
	{
		Debug("{CallbackName}() from {Sender}: {@EventArgs}", nameof(QuitAppCommandExecuted), sender, eventArgs);
		Verbose("Closing main form");
		Close();
	}

	/// <summary>Callback for when the [About App] command is executed</summary>
	private void AboutAppCommandExecuted(object? sender, EventArgs eventArgs)
	{
		Debug("{CallbackName}() from {Sender}: {@EventArgs}", nameof(AboutAppCommandExecuted), sender, eventArgs);
		new AboutDialog
		{
				Copyright          = AssemblyInfo.Copyright,
				Designers          = AssemblyInfo.Designers.ToArray(),
				Developers         = AssemblyInfo.Developers.ToArray(),
				Documenters        = AssemblyInfo.Documenters.ToArray(),
				License            = AssemblyInfo.Licence,
				Logo               = Icon,
				Version            = AssemblyInfo.Version,
				ID                 = $"{ID}/AboutDialog",
				ProgramDescription = AssemblyInfo.Description,
				Title              = "About",
				Website            = new Uri(AssemblyInfo.ProjectLink),
				ProgramName        = AssemblyInfo.DevAppName,
				WebsiteLabel       = "Project Website"
		}.ShowDialog(this);
	}

	[ContractAnnotation("=> halt")]
	private static void MainFormClosed(object? sender, EventArgs eventArgs)
	{
		Debug("{CallbackName}() from {Sender}: {@EventArgs}", nameof(MainFormClosed), sender, eventArgs);

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