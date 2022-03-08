using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using System.Reflection;
using Terminal.Gui;
using Terminal.Gui.TextValidateProviders;
using static Terminal.Gui.Dim;

namespace RayTracer.Display.Terminal.GUI;

/// <summary>
///  TopLevel class that is used to confirm the settings that will be used to render the app
/// </summary>
internal sealed class SettingsConfirmer : Toplevel
{
	private static readonly ITextValidateProvider IntValidator = new TextRegexProvider(@"^-?0*?((?'integer'\d+)|(?'PreFloat'\d+\.\d*)|(?'PostFloat'\.\d+))$") { ValidateOnInput = true };

	public SettingsConfirmer()
	{
		Id = "Settings Confirmer TopLevel";
		//Create all the UI elements here
		Window mainWin = new("Confirm Render settings")
		{
				//Window covers the whole screen
				X  = 0, Y = 0, Width = Fill(), Height = Fill(),
				Id = "Settings Confirmer Window"
		};

		TableView table = new()
		{
				X  = 0, Y = 0,
				Id = "Property setter table"
		};
		//TODO: TABLEEEEEEEEEE
		//Generate all the property inputs here
		View? prev = null;
		foreach (PropertyInfo propInfo in typeof(RenderOptions).GetProperties())
		{
			string name      = propInfo.Name;
			Label  nameLabel = new(name) { Id = $"{name} name label" };
			var    view      = new View { X   = 0, Width = Fill(), Height = 1, Id = $"{name} container view" };
			view.Y = prev is null ? 0 : Pos.Bottom(view); //Try place below the previous property layout
			view.Add(nameLabel);
			if (propInfo.PropertyType == typeof(int))
			{
				int min        = int.MinValue, max = int.MaxValue;
				int defaultVal = (int)propInfo.GetValue(RenderOptions.Default)!;
				//If the value can't be neg, set the min to 0
				if (propInfo.GetCustomAttribute(typeof(NonNegativeValueAttribute)) is not null) min = 0;
				if (propInfo.GetCustomAttribute<ValueRangeAttribute>() is { } range) (min, max)     = ((int)range.From, (int)range.To);

				//Create the text field that only allows numbers to be input
				TextValidateField entryField = new(IntValidator) { Text = defaultVal.ToString(), Id = $"Int {name} entry field" };
				//Whenever the user finishes inputting, ensure it's in our valid range
				entryField.Leave += args =>
				{
					var f = (TextValidateField)args.View;
					//If we didn't manage to parse the value as an int, reset it to the default
					if (!int.TryParse(f.Text.ToString(), out int v)) f.Text = defaultVal.ToString();
					if (v < min) f.Text                                     = (v = min).ToString();
					if (v > max) f.Text                                     = (v = max).ToString();
					//Now we've got a valid value, update the property on the instance
					propInfo.SetValue(Options, v);
				};
				view.Add(entryField);
			}
			else if (propInfo.PropertyType == typeof(bool))
			{
				CheckBox toggle = new() { Id = $"Bool {name} toggle" };
				toggle.Toggled += b => propInfo.SetValue(Options, b);
				view.Add(toggle);
			}
			else
			{
				view.Add(new Label($"ERROR: Type {propInfo.PropertyType} unsupported") { Id = $"Prop {name} error label" });
			}

			mainWin.Add(view);
			prev = view;
		}

		Add(mainWin);
	}

	public RenderOptions Options { get; } = RenderOptions.Default; //Start with default values
	public Scene?        Scene   { get; } = null;                  //Can only be null if the user quits without selecting the scene (i hope)

	// private sealed class IntValidateProvider : ITextValidateProvider
	// {
	// 	private List<Rune> text;
	// 	private int        min, max;
	// 	public int value => ustring.Make(text).ToString()
	//
	// 	/// <summary>Empty Constructor.</summary>
	// 	public IntValidateProvider(int min, int max, int startingValue)
	// 	{
	// 	}
	//
	//
	// 	/// <inheritdoc/>
	// 	public ustring Text
	// 	{
	// 		get => ustring.Make(text);
	// 		set
	// 		{
	// 			text = value != ustring.Empty ? value.ToRuneList() : null;
	// 			SetupText();
	// 		}
	// 	}
	//
	// 	/// <inheritdoc/>
	// 	public ustring DisplayText => Text;
	//
	// 	/// <summary>
	// 	///  When true, validates with the regex pattern on each input, preventing the input if it's not valid.
	// 	/// </summary>
	// 	public bool ValidateOnInput { get; } = true;
	//
	// 	/// <inheritdoc/>
	// 	public bool IsValid => Validate(text);
	//
	// 	/// <inheritdoc/>
	// 	public bool Fixed => false;
	//
	// 	/// <inheritdoc/>
	// 	public int Cursor(int pos)
	// 	{
	// 		if (pos < 0)
	// 			return CursorStart();
	// 		return pos >= text.Count ? CursorEnd() : pos;
	// 	}
	//
	// 	/// <inheritdoc/>
	// 	public int CursorStart() => 0;
	//
	// 	/// <inheritdoc/>
	// 	public int CursorEnd() => text.Count;
	//
	// 	/// <inheritdoc/>
	// 	public int CursorLeft(int pos) => pos > 0 ? pos - 1 : pos;
	//
	// 	/// <inheritdoc/>
	// 	public int CursorRight(int pos) => pos < text.Count ? pos + 1 : pos;
	//
	// 	/// <inheritdoc/>
	// 	public bool Delete(int pos)
	// 	{
	// 		if ((text.Count > 0) && (pos < text.Count))
	// 			text.RemoveAt(pos);
	// 		return true;
	// 	}
	//
	// 	/// <inheritdoc/>
	// 	public bool InsertAt(char ch, int pos)
	// 	{
	// 		List<Rune> list = text.ToList();
	// 		list.Insert(pos, ch);
	// 		if (!Validate(list) && ValidateOnInput)
	// 			return false;
	// 		text.Insert(pos, ch);
	// 		return true;
	// 	}
	//
	// 	private bool Validate(List<Rune> text) => regex.Match(ustring.Make(text).ToString()).Success;
	//
	// 	private void SetupText()
	// 	{
	// 		if ((text != null) && IsValid)
	// 			return;
	// 		text = new List<Rune>();
	// 	}
	//
	// 	/// <summary>Compiles the regex pattern for validation./&gt;</summary>
	// 	private void CompileMask() => regex = new Regex(ustring.Make((IList<Rune>)pattern).ToString(), RegexOptions.Compiled);
	// }
	//
}