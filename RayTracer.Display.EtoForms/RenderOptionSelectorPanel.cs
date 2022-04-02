using Eto.Drawing;
using Eto.Forms;
using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Serilog.Log;

namespace RayTracer.Display.EtoForms;

/// <summary>
///  Panel that allows modification of the settings of a <see cref="RenderOptions"/> instance
/// </summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable")] //Don't think it applies and I dont care
internal sealed class RenderOptionSelectorPanel : Panel, INotifyPropertyChanged
{
	/// <summary>
	///  Creates a new <see cref="RenderOptionSelectorPanel"/> panel
	/// </summary>
	/// <param name="options">
	///  Optional initial value for the render options. If <see langword="null"/>, will be set to
	///  <see cref="Core.Graphics.RenderOptions.Default"/>
	/// </param>
	public RenderOptionSelectorPanel(RenderOptions? options = null)
	{
		if (options == null)
		{
			Verbose("No options supplied, using default value {DefaultValue}", RenderOptions.Default);
			renderOptions = RenderOptions.Default;
		}
		else
		{
			Verbose("Custom render options supplied: {RenderOptions}", options);
			renderOptions = options;
		}

		//Update the sub views whenever someone changes the properties of our render options
		Verbose("Adding PropertyChanged  view updater event");
		PropertyChanged += (_, _) => UpdateEditorsFromVariable();

		Verbose("Creating new table layout");
		TableLayout tableLayout = new() { Padding = 10, Spacing = new Size(10, 5) };

		//Loop over each property in RenderOptions and create an editor for it
		//TODO: Perhaps tooltips or something similar
		Verbose("Creating property editors");
		foreach (PropertyInfo prop in typeof(RenderOptions).GetProperties())
		{
			PropertyEditorView editorView;
			Label              label = new() { ID = $"{prop.Name} label", Text = prop.Name };

			TableCell labelCell  = new(label);
			TableCell editorCell = new();
			tableLayout.Rows!.Add(new TableRow(labelCell, editorCell) { ScaleHeight = false });

			//Switch to create the editor
			if (prop.PropertyType == typeof(int))
			{
				editorView = new IntEditor(this, prop, editorCell);
			}
			else if (prop.PropertyType == typeof(float))
			{
				editorView = new FloatEditor(this, prop, editorCell);
			}
			else if (prop.PropertyType.IsEnum)
			{
				//Hey it's a little funky but it works
				editorView = (PropertyEditorView)Activator.CreateInstance(typeof(EnumEditor<>).MakeGenericType(prop.PropertyType), this, prop, editorCell)!;
			}
			else
			{
				editorCell.Control = new Label { Text = $"{prop.PropertyType} not yet supported sorry!" };
				continue;
			}

			Verbose("Created editor for property {Property}: {Editor}", prop, editorView);
			propertyEditors.Add(editorView);
		}

		tableLayout.Rows!.Add(new TableRow()); //Add empty row so that scaling looks nice (last row sizes to fil gap)

		Verbose("Creating start render button");
		Button startRenderButton = new();

		Verbose("Creating StackPanelLayout for content");
		Content = new StackLayout
		{
				Items   = { tableLayout, startRenderButton },
				Spacing = 10
		};

		UpdateEditorsFromVariable();
	}

#region Property Editors

	private sealed class IntEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;

		/// <inheritdoc/>
		public IntEditor(RenderOptionSelectorPanel target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			int min = int.MinValue;
			int max = int.MaxValue;
			if (typeof(RenderOptions).GetConstructors()[0].GetParameters().FirstOrDefault(p => p.Name == prop.Name)?.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
				min = 0;

			Verbose("{Property}: Min = {Min}, Max = {Max}", prop, min, max);

			stepper              =  new NumericStepper { ID = $"{Prop.Name} stepper", Increment = 1, MaximumDecimalPlaces = 0, MinValue = min, MaxValue = max };
			stepper.ValueChanged += (sender, _) => Prop.SetValue(Target.RenderOptions, (int)((NumericStepper)sender!).Value);
			TableCell.Control    =  stepper;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (int)Prop.GetValue(Target.RenderOptions)!;
		}
	}

	private sealed class EnumEditor<T> : PropertyEditorView where T: struct, Enum
	{
		private readonly EnumDropDown<T> dropDown;

		/// <inheritdoc/>
		public EnumEditor(RenderOptionSelectorPanel target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			Verbose("{Property}: Possible enum values are {Values}", prop, Enum.GetValues<T>());

			dropDown                      =  new EnumDropDown<T> { ID = $"{Prop.Name} dropdown" };
			dropDown.SelectedValueChanged += (sender, _) => Prop.SetValue(Target.RenderOptions, ((EnumDropDown<T>)sender!).SelectedValue);
			TableCell.Control             =  dropDown;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			dropDown.SelectedValue = (T)Prop.GetValue(Target.RenderOptions)!;
		}
	}

	private sealed class FloatEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;

		/// <inheritdoc/>
		public FloatEditor(RenderOptionSelectorPanel target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			float min = float.MinValue;
			float max = float.MaxValue;
			// //It's a record so must have a backing field, don't have to worry about null refs
			// FieldInfo backingField = prop.DeclaringType!.GetField($"<{Prop.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
			// if ((Prop.GetCustomAttribute<NonNegativeValueAttribute>() ?? backingField.GetCustomAttribute<NonNegativeValueAttribute>()) is not null)
			//Honestly why the hell do I have to do this microsoft????
			//Just apply it to the field, property and parameter, please for the love of god
			if (typeof(RenderOptions).GetConstructors()[0].GetParameters().FirstOrDefault(p => p.Name == prop.Name)?.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
				min = 0f;

			Verbose("{Property}: Min = {Min}, Max = {Max}", prop, min, max);

			stepper              =  new NumericStepper { ID = $"{Prop.Name} stepper", Increment = 1, MinValue = min, MaxValue = max };
			stepper.ValueChanged += (sender, _) => Prop.SetValue(Target.RenderOptions, (float)((NumericStepper)sender!).Value);
			TableCell.Control    =  stepper;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (float)Prop.GetValue(Target.RenderOptions)!;
		}
	}

#endregion


#region Boilerplate

	private readonly List<PropertyEditorView> propertyEditors = new();

	private abstract class PropertyEditorView
	{
		protected PropertyEditorView(RenderOptionSelectorPanel target, PropertyInfo prop, TableCell tableCell)
		{
			Target    = target;
			Prop      = prop;
			TableCell = tableCell;
		}

		protected internal RenderOptionSelectorPanel Target    { get; }
		protected internal PropertyInfo              Prop      { get; }
		protected internal TableCell                 TableCell { get; }

		internal abstract void UpdateDisplayedFromTarget();
	}

	private void UpdateEditorsFromVariable()
	{
		Debug("Updating editor views from variables");
		foreach (PropertyEditorView propertyEditorView in propertyEditors)
		{
			Verbose("Updating property view for property {Property}", propertyEditorView.Prop);
			propertyEditorView.UpdateDisplayedFromTarget();
		}
	}

	private RenderOptions renderOptions;

	/// <summary>
	///  Gets the render options displayed by this instance
	/// </summary>
	public RenderOptions RenderOptions
	{
		get => renderOptions;
		set
		{
			renderOptions = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	[NotifyPropertyChangedInvocator]
	// ReSharper disable once MemberCanBePrivate.Global
	public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

#endregion
}