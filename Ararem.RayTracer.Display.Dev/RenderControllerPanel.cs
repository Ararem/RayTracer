using Ararem.RayTracer.Core;
using Ararem.RayTracer.Core.Debugging;
using Ararem.RayTracer.Impl.Builtin;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ararem.RayTracer.Display.Dev;

public sealed partial class RenderJobPanel
{
	/// <summary>Panel class that controls an <see cref="RenderJob"/>. Allows editing the <see cref="Core.RenderOptions"/> and <see cref="Scene"/> for the render, as well as starting/stopping the render</summary>
	// TODO: Why am i modifying an immutable type with reflection (*cough* RenderOptions *cough*)
	private sealed class RenderControllerPanel : Panel
	{
		public RenderControllerPanel(RenderJobPanel parentJobPanel)
		{
			ParentJobPanel = parentJobPanel;
			Verbose("Creating property editors");
			Content = Layout = new DynamicLayout
			{
					ID      = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding = DefaultPadding
			};
			DynamicGroup group = Layout.BeginGroup("Render Options", spacing: DefaultSpacing, padding: DefaultPadding);
			Layout.BeginScrollable(spacing: DefaultSpacing, padding: DefaultPadding);

			HandleIntProperty(nameof(RenderOptions.RenderWidth),  1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.RenderHeight), 1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.Passes),       1, int.MaxValue);
			HandleBoolProperty(nameof(RenderOptions.InfinitePasses));
			HandleIntProperty(nameof(RenderOptions.ConcurrencyLevel),     1, Environment.ProcessorCount);
			HandleIntProperty(nameof(RenderOptions.MaxBounceDepth),       1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.LightSampleCountHint), 1, int.MaxValue);
			HandleFloatProperty(nameof(RenderOptions.KMin), 0f, float.PositiveInfinity);
			HandleFloatProperty(nameof(RenderOptions.KMax), 0f, float.PositiveInfinity);
			HandleEnumProperty<GraphicsDebugVisualisation>(nameof(RenderOptions.DebugVisualisation));

			Layout.Add(null, yscale: true);
			Verbose("Created property editors");

			Verbose("Creating scene select dropdown");
			Label label = GetNameLabel("Scene");

			SelectedSceneDropdown = new DropDown
			{
					ID    = $"{Layout.ID}/SelectedSceneDropdown",
					Style = nameof(Monospace)
			};
			SelectedSceneDropdown.SelectedValueChanged += SelectedSceneDropdownChanged;
			//A bit funky how we do this, but it works I guess
			List<Scene> allScenes = BuiltinScenes.GetAll().ToList();
			SelectedSceneDropdown.DataStore = allScenes;
			Scene initial = allScenes.First(s => string.Equals(s.Name, SelectedScene.Name, StringComparison.Ordinal));
			int   index   = allScenes.ToList().IndexOf(initial);
			SelectedSceneDropdown.SelectedIndex = index;
			Layout.AddRow(label, SelectedSceneDropdown);
			Verbose("Created scene select dropdown: {Dropdown}", SelectedSceneDropdown);

			Layout.EndScrollable();

			//TODO: Button to save image
			Verbose("Creating toggle render button");
			ToggleRenderStateButton = new Button
			{
					ID    = $"{Layout.ID}/ToggleRenderButton",
					Style = nameof(Bold),
					Text  = "[Toggle Render]"
			};
			Layout.AddCentered(ToggleRenderStateButton);
			Command toggleRenderStateCommand = new(ToggleRenderButtonClicked)
			{
					ID = $"{ToggleRenderStateButton.ID}.Command"
			};
			ToggleRenderStateButton.Command = toggleRenderStateCommand;
			Verbose("Created toggle render button: {Control}", ToggleRenderStateButton);

			Layout.EndGroup();
			Layout.Create();
			group.GroupBox.Style = nameof(Force_Heading);
		}

		private void SelectedSceneDropdownChanged(object? sender, EventArgs eventArgs)
		{
			TrackEvent(sender, eventArgs);
			Scene newScene = (Scene)SelectedSceneDropdown.SelectedValue;
			Verbose("Selected scene changed to {Scene}", newScene);
			SelectedScene = newScene;
		}

		/// <summary>The <see cref="RenderJobPanel"/> that contains this instance as a child object (aka the panel that created this panel)</summary>
		public RenderJobPanel ParentJobPanel { get; }

		/// <summary>Main <see cref="DynamicLayout"/> for this panel. Is the same as accessing <see cref="Panel.Content"/></summary>
		public DynamicLayout Layout { get; }

		public DropDown SelectedSceneDropdown   { get; }
		public Button   ToggleRenderStateButton { get; }

		/// <summary>The <see cref="CancellationTokenSource"/> that is used to cancel the <see cref="Core.RenderJob"/></summary>
		public CancellationTokenSource RenderJobCTS { get; private set; } = new();

		/// <summary>Called whenever the "Toggle Render" button is pressed</summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ToggleRenderButtonClicked(object? sender, EventArgs eventArgs)
		{
			TrackEvent(sender, eventArgs);
			bool? currentlyCompleted = RenderJob?.RenderCompleted;
			LogExpression(currentlyCompleted);
			switch (currentlyCompleted)
			{
				case false:
					Debug("Render was running, cancelling and recreating task source {RenderJobCancellationTokenSource}", RenderJobCTS);
					RenderJobCTS.Cancel();
					RenderJobCTS.Dispose();
					RenderJobCTS = new CancellationTokenSource();
					break;
				case true:
					Debug("Render was completed, creating new render with RenderOptions {@RenderOptions} and Scene {Scene}", RenderOptions, SelectedScene);
					RenderJob = new RenderJob(SelectedScene, RenderOptions);
					RenderJob.StartOrGetRenderAsync(RenderJobCTS.Token);
					break;
				case null:
					Debug("Render was  null, creating new render with RenderOptions {@RenderOptions} and Scene {Scene}", RenderOptions, SelectedScene);
					RenderJob = new RenderJob(SelectedScene, RenderOptions);
					RenderJob.StartOrGetRenderAsync(RenderJobCTS.Token);
					break;
			}
		}

		public void Update()
		{
			//Enable/disable controls depending on if we are rendering or not
			Stopwatch sw = Stopwatch.StartNew();
			for (int i = 0; i < renderOptionEditors.Count; i++)
			{
				RenderOptionEditor editor = renderOptionEditors[i];
				bool enabled = RenderJob is null or { RenderCompleted: true } //Render isn't running
							   || editor.CanModifyWhileRunning;               //Always allowed to modify
				if (editor.Control.GetType().GetProperty("ReadOnly", typeof(bool)) is {} readonlyProp)
				{
					bool shouldBeReadonly = !enabled;
					if((bool)readonlyProp.GetValue(editor.Control)! == shouldBeReadonly) continue; //Only log and modify if different
					readonlyProp.SetValue(editor.Control, shouldBeReadonly);
					Verbose("{Control}.Readonly set to {Value}", editor.Control, shouldBeReadonly);
				}
				else
				{
					if(editor.Control.Enabled == enabled) continue; ////Only log and modify if different
					editor.Control.Enabled = enabled;
					Verbose("{Control}.Enabled set to {Value}", editor.Control, enabled);
				}
			}

			{
				bool enabled = RenderJob is null or { RenderCompleted: true };
				if (SelectedSceneDropdown.Enabled != enabled)
				{
					SelectedSceneDropdown.Enabled = enabled;
					Verbose("{Control}.Enabled set to {Value}", SelectedSceneDropdown, enabled);
				}
			}

			//Update the text for the toggle render state button
			{
				string newText = RenderJob?.RenderCompleted switch
				{
						null  => "Start render",
						false => "Stop render",
						true  => "Restart new render"
				};
				if (ToggleRenderStateButton.Text != newText)
				{
					ToggleRenderStateButton.Text = newText;
					Verbose("{Control}.Text set to {NewValue}", ToggleRenderStateButton, newText);
				}
			}
			Invalidate(true); //Mark for redraw
			Verbose("[{Sender}] Editors updated in {Elapsed:#00.000 'ms'}", this, sw.Elapsed.TotalMilliseconds);
		}

	#region Ease-of-use properties (shortcut properties)

		private Scene SelectedScene
		{
			get => ParentJobPanel.SelectedScene;
			set => ParentJobPanel.SelectedScene = value;
		}

		private RenderOptions RenderOptions => ParentJobPanel.RenderOptions;

		private RenderJob? RenderJob
		{
			get => ParentJobPanel.RenderJob;
			set => ParentJobPanel.RenderJob = value;
		}

	#endregion

	#region Property editing

		private Label GetNameLabel(string propertyName, (object min, object max)? maybeRange = null) =>
				new()
				{
						ID            = $"{Layout.ID}/{propertyName}.Label",
						Text          = propertyName,
						ToolTip       = maybeRange is {} range ? $"Valid range is [{range.min}..{range.max}]" : null,
						Style         = nameof(Italic),
						TextAlignment = TextAlignment.Center
				};

		private void HandleIntProperty(string propertyName, int min, int max)
		{
			Verbose("Trying to add integer property editor for {Property}", propertyName);
			PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
			MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
			bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
			object?      boxedPropertyValue    = property.GetValue(RenderOptions);
			int          initialValue          = boxedPropertyValue as int? ?? throw new ArgumentException($"Expected an integer but got {boxedPropertyValue}");
			Label        label                 = GetNameLabel(propertyName, (min, max));
			NumericStepper stepper = new()
			{
					//Can't assign a format string of "n0" because the comma breaks things once you get above 999 :(
					ID                   = $"{Layout.ID}/{propertyName}.Stepper",
					Increment            = 1.0,
					MaximumDecimalPlaces = 0,
					MinValue             = min,
					MaxValue             = max,
					Style                = nameof(Monospace),
					ToolTip              = label.ToolTip
			};
			stepper.ValueChanged += delegate
			{
				int newValue = (int)stepper.Value;
				LogPropChanged(property, newValue);
				property.SetValue(RenderOptions, newValue);
			};
			stepper.Value = initialValue;
			RenderOptionEditor renderOptionEditor = new(stepper, canModifyWhileRunning);
			renderOptionEditors.Add(renderOptionEditor);

			Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
			Layout.AddRow(label, renderOptionEditor.Control);
		}

		private void HandleBoolProperty(string propertyName)
		{
			Verbose("Trying to add boolean property editor for {Property}", propertyName);
			PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
			MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
			bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));

			object? boxedPropertyValue = property.GetValue(RenderOptions);
			bool    initialValue       = boxedPropertyValue as bool? ?? throw new ArgumentException($"Expected a bool but got {boxedPropertyValue}");

			Label label = GetNameLabel(propertyName);
			CheckBox checkBox = new()
			{
					ID         = $"{Layout.ID}/{propertyName}.CheckBox",
					ThreeState = false,
					Style      = nameof(General)
			};
			checkBox.CheckedChanged += delegate
			{
				bool value = checkBox.Checked ?? throw new InvalidOperationException("CheckBox.Checked was null");
				LogPropChanged(property, value);
				property.SetValue(RenderOptions, value);
			};
			checkBox.Checked = initialValue;
			RenderOptionEditor renderOptionEditor = new(checkBox, canModifyWhileRunning);
			renderOptionEditors.Add(renderOptionEditor);

			Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
			Layout.AddRow(label, renderOptionEditor.Control);
		}

		private void HandleFloatProperty(string propertyName, float min, float max)
		{
			Verbose("Trying to add float property editor for {Property}", propertyName);
			PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
			MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
			bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
			object?      boxedPropertyValue    = property.GetValue(RenderOptions);
			float        initialValue          = boxedPropertyValue as float? ?? throw new ArgumentException($"Expected a float but got {boxedPropertyValue}");

			Label label = GetNameLabel(propertyName, (min, max));
			NumericStepper stepper = new()
			{
					ID                   = $"{Layout.ID}/{propertyName}.Stepper",
					Increment            = 1.0,
					MaximumDecimalPlaces = 5,
					DecimalPlaces        = 5,
					MinValue             = min,
					MaxValue             = max,
					ToolTip              = label.ToolTip,
					Style                = nameof(Monospace)
			};
			stepper.ValueChanged += delegate
			{
				float newVal = (float)stepper.Value;
				LogPropChanged(property, newVal);
				property.SetValue(RenderOptions, newVal);
			};
			stepper.Value = initialValue;
			RenderOptionEditor renderOptionEditor = new(stepper, canModifyWhileRunning);
			renderOptionEditors.Add(renderOptionEditor);

			Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
			Layout.AddRow(label, renderOptionEditor.Control);
		}

		private void HandleEnumProperty<T>(string propertyName) where T : Enum
		{
			Verbose("Trying to add enum property editor for {Property}", propertyName);
			PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
			MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
			bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
			Label        label                 = GetNameLabel(propertyName);
			object?      boxedPropertyValue    = property.GetValue(RenderOptions);
			if (boxedPropertyValue is not T initialValue) throw new ArgumentException($"Expected an enum but got {boxedPropertyValue}");

			EnumDropDown<T> dropDown = new()
			{
					ID    = $"{Layout.ID}/{propertyName}.Dropdown",
					Style = nameof(Monospace)
			};
			dropDown.SelectedValueChanged += delegate
			{
				T newVal = dropDown.SelectedValue;
				LogPropChanged(property, newVal);
				property.SetValue(RenderOptions, newVal);
			};
			dropDown.SelectedValue = initialValue;
			RenderOptionEditor renderOptionEditor = new(dropDown, canModifyWhileRunning);
			renderOptionEditors.Add(renderOptionEditor);

			Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
			Layout.AddRow(label, renderOptionEditor.Control);
		}

		private static void LogPropChanged<T>(PropertyInfo property, T newVal)
		{
			Verbose("Property {Property} changed => {NewValue}", property, newVal);
		}

		/// <summary>List containing all the render option editors</summary>
		private readonly List<RenderOptionEditor> renderOptionEditors = new();

		private sealed record RenderOptionEditor(CommonControl Control, bool CanModifyWhileRunning);

	#endregion

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			RenderJobCTS.Cancel(); //Cancel the render job so that it doesn't keep running
		}
	}
}