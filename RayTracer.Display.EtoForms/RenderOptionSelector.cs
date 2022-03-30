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
		StackLayout stackLayout = new();
		Content = stackLayout;
		Collection<StackLayoutItem> stack = stackLayout.Items!;
		//Loop over each property in RenderOptions and create an editor for it
		foreach (PropertyInfo prop in typeof(RenderOptions).GetProperties())
		{
			PropertyEditorView view;

			//Create the UI element container that will hold the controls, and add it to the list
			StackLayoutItem item = new();
			stack.Add(item);

			//Switch to create the editor
			if (prop.PropertyType == typeof(int))
				view = new IntEditor(this, prop, item);
			else
				continue;
			propertyEditors.Add(view);
		}

		UpdateSubViews();
	}

#region Property Editors

	private sealed class IntEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;
		private readonly Label          label;

		/// <inheritdoc/>
		public IntEditor(RenderOptionSelector target, PropertyInfo prop, StackLayoutItem layoutItem) : base(target, prop, layoutItem)
		{
			int min = int.MinValue;
			int max = int.MaxValue;
			if (prop.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
				min = 0;

			label              = new Label {ID                       = $"{prop.Name} label", Text        = prop.Name };
			stepper            = new NumericStepper { ID             = $"{prop.Name} stepper", Increment = 1, MaximumDecimalPlaces = 0, MinValue = min, MaxValue = max};
			layoutItem.Control = new StackLayout(label, stepper) {ID = $"{prop.Name} container",  Orientation = Orientation.Horizontal };
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (double)((int)Prop.GetValue(Target.Options)!);
		}
	}

#endregion


#region Boilerplate

	private readonly List<PropertyEditorView> propertyEditors = new();

	private abstract class PropertyEditorView
	{
		protected PropertyEditorView(RenderOptionSelector target, PropertyInfo prop, StackLayoutItem layoutItem)
		{
			Target          = target;
			Prop            = prop;
			LayoutItem = layoutItem;
		}

		public RenderOptionSelector Target     { get; }
		public PropertyInfo         Prop       { get; }
		public StackLayoutItem      LayoutItem { get; }

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