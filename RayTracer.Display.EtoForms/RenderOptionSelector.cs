using Eto.Forms;
using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RayTracer.Display.EtoForms;

internal class RenderOptionSelector : Panel, INotifyPropertyChanged
{
	public RenderOptionSelector()
	{
		Options = RenderOptions.Default;
		//Update the sub views whenever someone changes the properties of our render options
		PropertyChanged += (_, _) => UpdateSubViews();
		TableLayout tableLayout = new();
		Content = tableLayout;
		//Loop over each property in RenderOptions and create an editor for it
		foreach (PropertyInfo prop in typeof(RenderOptions).GetProperties())
		{
			PropertyEditorView view;
			Label              label = new() {ID = $"{prop.Name} label", Text = prop.Name };

			TableCell labelCell  = new(label);
			TableCell editorCell = new();
			tableLayout.Rows!.Add(new TableRow(labelCell, editorCell));

			//Switch to create the editor
			if (prop.PropertyType == typeof(int))
			{
				view = new IntEditor(this, prop, editorCell);
			}
			else
			{
				editorCell.Control = new Label { Text = $"{prop.PropertyType} not yet supported sorry!" };
				continue;
			}

			propertyEditors.Add(view);
		}

		UpdateSubViews();
	}

#region Property Editors

	private sealed class IntEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;

		/// <inheritdoc/>
		public IntEditor(RenderOptionSelector target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			int min = int.MinValue;
			int max = int.MaxValue;
			if (Prop.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
				min = 0;

			stepper              =  new NumericStepper { ID = $"{Prop.Name} stepper", Increment = 1, MaximumDecimalPlaces = 0, MinValue = min, MaxValue = max};
			stepper.ValueChanged += (sender, _) => Prop.SetValue(Target.Options, (int)((NumericStepper)sender!).Value);
			TableCell.Control    =  stepper;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (int)Prop.GetValue(Target.Options)!;
		}
	}

#endregion


#region Boilerplate

	private readonly List<PropertyEditorView> propertyEditors = new();

	private abstract class PropertyEditorView
	{
		protected PropertyEditorView(RenderOptionSelector target, PropertyInfo prop, TableCell tableCell)
		{
			Target          = target;
			Prop            = prop;
			TableCell = tableCell;
		}

		protected RenderOptionSelector Target    { get; }
		protected PropertyInfo         Prop      { get; }
		protected TableCell            TableCell { get; }

		internal abstract void UpdateDisplayedFromTarget();
	}

	private void UpdateSubViews()
	{
		foreach (PropertyEditorView propertyEditorView in propertyEditors) propertyEditorView.UpdateDisplayedFromTarget();
	}

	public readonly RenderOptions Options;

	public event PropertyChangedEventHandler? PropertyChanged;

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

#endregion
}