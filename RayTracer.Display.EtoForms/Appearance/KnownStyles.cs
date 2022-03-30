using Eto;
using Eto.Drawing;
using Eto.Forms;
using Eto.GtkSharp;

namespace RayTracer.Display.EtoForms.Appearance;

public static class KnownStyles
{
	internal static void Register()
	{
		Style.Add(TitleText, new StyleWidgetHandler<TextControl>(control =>
				{
					control!.Font = new Font(KnownFonts.DefaultFamily, 32f, FontStyle.Bold | FontStyle.Italic, FontDecoration.Underline);
					control.TextColor = Color.FromRgb(0x40d5ff);
				}));
	}
	public const string TitleText = nameof(TitleText);
}