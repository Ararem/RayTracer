using Eto;
using Eto.Drawing;
using Eto.Forms;
using static Serilog.Log;

namespace RayTracer.Display.EtoForms.Appearance;

public static class Styles
{
	public const int GeneralFontSize = 10;

	/// <summary>Style for the title of the application</summary>
	public const string AppTitle = nameof(AppTitle);

	/// <summary>General textual control; e.g. button, label, description</summary>
	public const string GeneralTextual = nameof(GeneralTextual);

	/// <summary>General textual control, bolded; e.g. button, label, description</summary>
	public const string GeneralTextualBold = nameof(GeneralTextualBold);

	/// <summary>General textual control, italicised; e.g. button, label, description</summary>
	public const string GeneralTextualItalic = nameof(GeneralTextualItalic);

	/// <summary>General textual control, italicised; e.g. button, label, description</summary>
	public const string GeneralTextualUnderline = nameof(GeneralTextualUnderline);

	/// <summary>Consistent text width - monospace font</summary>
	public const string ConsistentTextWidth = nameof(ConsistentTextWidth);

	internal static void RegisterStyles()
	{
		Information("Registering styles");

		Verbose("Registering style {Style}", AppTitle);
		Style.Add(
				AppTitle, static (Label control) =>
				{
					control.Font      = new Font(FontFamilies.Sans, 40, FontStyle.Bold);
					control.TextColor = new Color(1f, 1f, 1f);
				}
		);

		Verbose("Registering style {Style}", GeneralTextual);
		Style.Add(GeneralTextual, static (CommonControl control) => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize); });

		Verbose("Registering style {Style}", GeneralTextualItalic);
		Style.Add(GeneralTextualItalic, static (CommonControl control) => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize, FontStyle.Italic); });

		Verbose("Registering style {Style}", GeneralTextualBold);
		Style.Add(GeneralTextualBold, static (CommonControl control) => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize, FontStyle.Bold); });

		Verbose("Registering style {Style}", GeneralTextualUnderline);
		Style.Add(GeneralTextualUnderline, static (CommonControl control) => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize, FontStyle.None, FontDecoration.Underline); });

		Verbose("Registering style {Style}", ConsistentTextWidth);
		Style.Add(ConsistentTextWidth, static (CommonControl control) => { control.Font = new Font(FontFamilies.Monospace, GeneralFontSize); });
	}
}