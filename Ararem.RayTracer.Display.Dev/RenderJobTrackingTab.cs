using Ararem.RayTracer.Core;
using Ararem.RayTracer.Core.Debugging;
using Ararem.RayTracer.Display.Dev.Resources;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Ararem.RayTracer.Display.Dev.Resources.StyleManager;
using static Serilog.Log;

namespace Ararem.RayTracer.Display.Dev;

public class RenderJobTrackingTab : Panel
{
	private readonly DynamicLayout mainDynamicLayout;

	//TODO: Save image button
	public RenderJobTrackingTab(string id)
	{
		DynamicGroup renderOptionsGroup;
		DynamicGroup renderBufferGroup;
		DynamicGroup renderStatsGroup;
		ID      = id;
		Padding = DefaultPadding;
		{
			Content = mainDynamicLayout = new DynamicLayout
			{
					ID = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding =DefaultPadding
			};
		}
		//Split the layout into 3 horizontal groups - options, stats, image
		mainDynamicLayout.BeginHorizontal();

		{
			Verbose("Creating property editor panel");
			renderOptionsGroup           = mainDynamicLayout.BeginGroup("Render Options", spacing: DefaultSpacing, padding: DefaultPadding);
			mainDynamicLayout.BeginScrollable(spacing: DefaultSpacing, padding: DefaultPadding);

			HandleIntProperty(nameof(RenderOptions.RenderWidth),  1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.RenderHeight), 1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.Passes),       1, int.MaxValue);
			HandleBoolProperty(nameof(RenderOptions.InfinitePasses));
			HandleIntProperty(nameof(RenderOptions.ConcurrencyLevel),     1, Environment.ProcessorCount);
			HandleIntProperty(nameof(RenderOptions.MaxBounceDepth),       0, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.LightSampleCountHint), 1, int.MaxValue);
			HandleFloatProperty(nameof(RenderOptions.KMin), 0f, float.PositiveInfinity);
			HandleFloatProperty(nameof(RenderOptions.KMax), 0f, float.PositiveInfinity);
			HandleEnumProperty<GraphicsDebugVisualisation>(nameof(RenderOptions.DebugVisualisation));
			UpdateEditorsCanBeModified();

			mainDynamicLayout.Add(null, yscale: true);
			mainDynamicLayout.EndScrollable();
			Verbose("Created property editors");


			Verbose("Creating toggle render button");
			Button toggleRenderStateButton = new()
			{
					ID    = $"{mainDynamicLayout.ID}/ToggleRenderButton",
					Style = nameof(Bold),
					Text  = "[Toggle Render]"
			};
			mainDynamicLayout.AddCentered(toggleRenderStateButton);
			Command toggleRenderStateCommand = new(ToggleRenderButtonClicked)
			{
					ID = $"{toggleRenderStateButton.ID}.Command"
			};
			toggleRenderStateButton.Command = toggleRenderStateCommand;
			Verbose("Created toggle render button: {Control}", toggleRenderStateButton);

			mainDynamicLayout.EndGroup();
		}

		{
			Verbose("Creating render stats view");
			renderStatsGroup = mainDynamicLayout.BeginGroup("Render Stats", spacing: DefaultSpacing, padding: DefaultPadding);
			mainDynamicLayout.BeginScrollable(spacing: DefaultSpacing, padding: DefaultPadding);
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.Add("Testasdasdasdasdasdasdasdasdasdasdasdaads");
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.EndScrollable();
			mainDynamicLayout.EndGroup();

			Verbose("Created render stats view");
		}

		{
			renderBufferGroup = mainDynamicLayout.BeginGroup("Render Buffer", spacing: DefaultSpacing, padding: DefaultPadding);
			mainDynamicLayout.Add(ResourceManager.AppIcon);
			mainDynamicLayout.EndGroup();
		}

		mainDynamicLayout.EndHorizontal();

		{
			//Have to create the dynamic layouts before we try to access the instantiated controls, else they're null
			mainDynamicLayout.Create();
			renderOptionsGroup.GroupBox.Style = nameof(Force_Heading);
			renderStatsGroup.GroupBox.Style  = nameof(Force_Heading);
			renderBufferGroup.GroupBox.Style  = nameof(Force_Heading);
		}
	}

	/// <summary>Render options that affect how the <see cref="RenderJob"/> is rendered</summary>
	public RenderOptions RenderOptions { get; } = new();

	/// <summary>The current render job (if any)</summary>
	public AsyncRenderJob? RenderJob { get; } = null;

	private bool IsRendering => RenderJob?.RenderCompleted == false;

	private void ToggleRenderButtonClicked(object? sender, EventArgs eventArgs)
	{
		Debug("{CallbackName}() from {Sender}: {@EventArgs}", nameof(ToggleRenderButtonClicked), sender, eventArgs);
	}

#region RenderOption property editing

	private Label GetNameLabel(string propertyName, (object min, object max)? maybeRange = null) =>
			new()
			{
					ID            = $"{mainDynamicLayout.ID}/{propertyName}.Label",
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
				ID                   = $"{mainDynamicLayout.ID}/{propertyName}.Stepper",
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
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
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
				ID         = $"{mainDynamicLayout.ID}/{propertyName}.CheckBox",
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
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
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
				ID                   = $"{mainDynamicLayout.ID}/{propertyName}.Stepper",
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
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
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
				ID    = $"{mainDynamicLayout.ID}/{propertyName}.Dropdown",
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
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
	}

	private static void LogPropChanged<T>(PropertyInfo property, T newVal)
	{
		Verbose("Property {Property} changed => {NewValue}", property, newVal);
	}

	/// <summary>List containing all the render option editors</summary>
	private readonly List<RenderOptionEditor> renderOptionEditors = new();

	private sealed record RenderOptionEditor(CommonControl Control, bool CanModifyWhileRunning);

	/// <summary>Updates all the property editors, locking them if they can't be modified</summary>
	private void UpdateEditorsCanBeModified()
	{
		for (int i = 0; i < renderOptionEditors.Count; i++)
		{
			RenderOptionEditor editor           = renderOptionEditors[i];
			bool               shouldBeReadonly = !editor.CanModifyWhileRunning && IsRendering;
			if (editor.Control.GetType().GetProperty("ReadOnly", typeof(bool)) is {} readonlyProp)
				readonlyProp.SetValue(editor.Control, shouldBeReadonly);
			else editor.Control.Enabled = !shouldBeReadonly;
		}
	}

#endregion
}