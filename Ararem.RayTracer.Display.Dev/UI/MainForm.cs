using Ararem.RayTracer.Core;
using Ararem.RayTracer.Display.Dev.Resources;
using Eto.Drawing;
using Eto.Forms;
using JetBrains.Annotations;
using LibArarem.Core.Logging;
using NetFabric.Hyperlinq;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Enumerable = System.Linq.Enumerable;

namespace Ararem.RayTracer.Display.Dev.UI;

/// <summary>Main form class that handles much of the UI stuff</summary>
internal sealed class MainForm : Form
{
	private readonly ILogger log;

	/// <summary>Main control that contains the tabs for each of the renders</summary>
	private DocumentControl documentControlContent;

	public MainForm()
	{
		log = LogUtils.WithInstanceContext(this);
	}

	/// <inheritdoc/>
	protected override void OnPreLoad(EventArgs e)
	{
		log.TrackEvent(this, e);
		//Load the components
		{
			log.Verbose("Creating MenuBar");
			Menu = new MenuBar
			{
					//Application-specific menu, gets it's own section in the menu bar
					//Since it's not really needed, I don't set it
					// ApplicationMenu  = { Items = { new Command { ToolBarText = "AppMenu.Command.ToolbarText", MenuText = "AppMenu.Command.MenuText" } },Text = "AppMenu.Text"},

					//Same for the application items - on linux this is under the "File" section
					// ApplicationItems = { new Command { ToolBarText = "AppItems.Command.ToolbarText", MenuText = "AppItems.Command.MenuText" } },
					ID = $"{ID}/MenuBar"
			};
			log.Verbose("Created MenuBar");
		}

		{
			log.Verbose("Setting up quit handling");
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
			log.Verbose("Set up quit handling");
		}

		{
			log.Verbose("Setting up about app menu");
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
			log.Verbose("Set about app menu");
		}

		{
			log.Verbose("Setting icon");
			Icon = ResourceManager.AppIconPng;
			log.Verbose("Set icon");
		}

		{
			log.Verbose("Toolbar (disabled)");
		}

		{
			log.Verbose("Setting window parameters");
			Resizable   = true;
			Maximizable = true;
			Minimizable = true;
			MinimumSize = new Size(0,    0);
			Size        = new Size(1280, 720);
			Title       = Application.Instance.Name;
			Maximize();
			log.Verbose("Window parameters set");
		}

		{
			log.Verbose("Creating main (dynamic) layout");
			DynamicLayout layout = new()
			{
					ID      = $"{ID}/Layout",
					Padding = DefaultPadding,
					Spacing = DefaultSpacing
			};
			Content = layout;
			layout.BeginVertical();
			layout.Add(
					new Label
					{
							ID                = $"{layout.ID}/TitleLabel",
							Text              = AssemblyInfo.ProductName,
							Style             = nameof(AppTitle),
							TextAlignment     = TextAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center
					}
			);
			layout.Add(
					documentControlContent = new DocumentControl
					{
							ID = "MainForm/Content/DocumentControl"
					}
			);
			layout.EndVertical();
			log.Verbose("Finished adding to main dynamic layout");
		}

		//Tab management
		{
			log.Verbose("Setting up new tab button");
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
			log.Verbose("Set up new tab button");

			log.Verbose("Setting up close render tab command");
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
			log.Verbose("Added close tab command");

			log.Verbose("Adding page closed handler");
			documentControlContent.PageClosed += DocumentPageOnClosed;

			log.Verbose("Creating initial tab");
			newTabCommand.Execute();
			log.Verbose("Initial tab created");
		}

		{
			log.Verbose("Creating update UI timer with interval {Interval:0.000' s'} ({Fps:00' FPS'})", 1f / TargetRefreshRate, TargetRefreshRate);
			// Periodically update the previews using a timer
			// updateUiTimer = new UITimer(UpdateUi)
			// {
			// ID = $"{ID}/UITimer", Interval = 1f / TargetRefreshRate
			// };
			// updateUiTimer.Start();
			new Thread(UpdateUiThread)
			{
					Name         = nameof(UpdateUiThread),
					IsBackground = true,
					Priority     = ThreadPriority.BelowNormal
			}.Start();
		}
		log.Verbose("Load complete");
		base.OnPreLoad(e);
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		log.Verbose("Dispose({Disposing})", disposing);
		base.Dispose(disposing);
	}

#region UI Updating

	private void UpdateUiThread()
	{
		Stopwatch sw    = new();
		try
		{
			while (true)
			{
				if (IsDisposed)
				{
					log.Verbose("MainForm disposed, exiting UI thread loop");
					return;
				}
				sw.Restart();
				Application.Instance.Invoke(UpdateUi);
				sw.Stop();

				TimeSpan expected  = TargetFrameDuration;
				TimeSpan actual    = sw.Elapsed;
				if (actual <= expected) continue;

				double   ratio     = actual / expected;
				TimeSpan overshoot = actual - expected;
				double   fps       = TimeSpan.FromSeconds(1) / actual;
				log.Warning("Update took {Overshoot} longer than expected: {Elapsed}/{Fps:00.0' FPS'} ({Ratio:P2})", overshoot, actual, fps, ratio);
				//Skip an iteration of the update to allow the application a bit of time for responsiveness
				Thread.Sleep(TargetFrameDuration);
				Thread.Sleep(10);
			}
		}
		catch (Exception e)
		{
			log.Error(e, "Was unable to schedule UI update");
		}
	}

	/// <summary>Target refreshes-per-second that we want</summary>
	private static double TargetRefreshRate => 30; //Fps

	private static TimeSpan TargetFrameDuration => TimeSpan.FromSeconds(1 / TargetRefreshRate);

	private void UpdateUi()
	{
		if (IsDisposed)
		{
			log.Verbose("MainForm disposed, skipping update");
			return;
		}

		using IDisposable _  = LogUtils.MarkContextAsExtremelyVerbose(); //Extra verbose because it will be called each frame
		Stopwatch         sw = Stopwatch.StartNew();
		log.Verbose("Updating UI");
		documentControlContent.Pages.AsValueEnumerable().ForEach(
				(page, index) =>
				{
					Control content = page.Content;
					if (content is not RenderJobPanel renderJobPanel) log.Warning($"Page at [{{Index}}] was not {nameof(RenderJobPanel)}, was {{Control}}", index, page);
					else renderJobPanel.UpdateUi();
				}
		);
		Invalidate();
		log.Verbose("Updated panel in {Elapsed:#00.000 'ms'}", sw.Elapsed.TotalMilliseconds);
	}

#endregion

#region Callbacks

	private void CloseRenderTabExecuted(object? sender, EventArgs eventArgs)
	{
		log.TrackEvent(sender, eventArgs);

		//TODO: Dispose the RenderJobPanel too once closed
		DocumentPage oldPage = documentControlContent.SelectedPage;
		oldPage.Detach();
		log.Debug("Closing tab {Control}", oldPage);
		/*
		 * HACK: Since DocumentControl doesn't provide a way to properly close tabs as if the close button was pressed, I gotta do a workaround
		 *
		 * Extracted from GTK handler source:
		 * internal void ClosePage(Gtk.Widget control, DocumentPage page)
    	 * {
    	 *   this.Control.RemovePage(this.Control.PageNum(control));
    	 *   this.SetShowTabs();
    	 *   if (!this.Widget.Loaded)
    	 *     return;
    	 *   this.Callback.OnPageClosed(this.Widget, new DocumentPageEventArgs(page)); //<<======== This is what I'm calling
    	 * }
		 */
		try
		{
			/*
			 * Yes this is very slow and unsafe, but there's not really a better way
			 * `dynamic` won't let me call OnPageClosed() because it can't find the method:
			 * Microsoft.CSharp.RuntimeBinder.RuntimeBinderException: 'Eto.Forms.Control.Callback' does not contain a definition for 'OnPageClosed'
   			 *	at void CallSite.Target(Closure, CallSite, object, DocumentControl, DocumentPageEventArgs)
   			 *	at void System.Dynamic.UpdateDelegates.UpdateAndExecuteVoid3<T0, T1, T2>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
			 *
			 * I think it's trying to resolve the method from the base type (which is Eto.Forms.Control.Callback), which doesn't have the method OnPageClosed(),
			 * despite the object instance being a DocumentControl.Callback, which does have the method OnPageClosed()
			 */
			dynamic callbackObject = ((dynamic)documentControlContent).Handler.Callback;
			((object)callbackObject).GetType().GetMethod("OnPageClosed")!.Invoke(callbackObject, new object[] { documentControlContent, new DocumentPageEventArgs(oldPage) });
		}
		catch (Exception e)
		{
			log.Warning(e, "Attempt to manually calling OnPageClosed() for {Control} callback failed", documentControlContent);
		}
	}

	private void DocumentPageOnClosed(object? sender, DocumentPageEventArgs eventArgs)
	{
		log.TrackEvent(sender, eventArgs);
		log.Information("Closed tab {Control}", eventArgs.Page);
		//The UI items all collapse and everything looks kinda weird when we have 0 tabs open, so we get around this by closing the current one and opening a new tab whenever we are on the last tab
		if (documentControlContent.Pages.Count == 0)
		{
			log.Debug("Just closed last tab, recreating to ensure we don't get below 1");
			CreateNewTabCommandExecuted(DocumentPageOnClosed, EventArgs.Empty);
		}

		eventArgs.Page.Content.Dispose();
	}

	/// <summary>Callback for when the [Create New Render] command is executed</summary>
	private void CreateNewTabCommandExecuted(object? sender, EventArgs eventArgs)
	{
		log.TrackEvent(sender, eventArgs);
		Guid guid = Guid.NewGuid();
		DocumentPage newPage = new()
		{
				ID      = $"Page_{guid}.Page",
				Text    = $"Render {guid}",
				Image   = Icon,
				Content = new RenderJobPanel { ID = $"Page_{guid}" }
		};
		//TODO: Tab selection thingy text styles
		documentControlContent.Pages.Add(newPage);
		log.Information("Added new render tab: {Control}", newPage);
	}

	/// <summary>Callback for when the [Quit App] command is executed</summary>
	[ContractAnnotation("=> halt")]
	private void QuitAppCommandExecuted(object? sender, EventArgs eventArgs)
	{
		log.TrackEvent(sender, eventArgs);
		log.Debug("Closing main form");
		Close();
	}

	/// <summary>Callback for when the [About App] command is executed</summary>
	private void AboutAppCommandExecuted(object? sender, EventArgs eventArgs)
	{
		log.TrackEvent(sender, eventArgs);
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
	private void MainFormClosed(object? sender, EventArgs eventArgs)
	{
		log.TrackEvent(sender, eventArgs);

		//To prevent recursive loops where this calls itself (since `Application.Quit` calls `MainForm.Closed`)
		//Walk up the stack to check if this method or Application.Quit are present, and if so, return immediately
		MethodBase thisMethod    = MethodBase.GetCurrentMethod()!;
		MethodBase appQuitMethod = typeof(Application).GetMethod(nameof(Application.Quit))!;
		if (Enumerable.Any(new StackTrace(1, false).GetFrames().Select(f => f.GetMethod()), m => (m == thisMethod) || (m == appQuitMethod)))
		{
			log.Verbose("Closed event recursion detected, returning immediately without sending quit signal");
			return;
		}

		log.Information("Main form closed");
		if (Application.Instance.QuitIsSupported)
		{
			log.Verbose("Sending quit signal");
			Application.Instance.Quit();
			log.Verbose("Quit signal sent");
		}
		else
		{
			log.Error("Quit not supported ☹️");
		}
	}

#endregion
}