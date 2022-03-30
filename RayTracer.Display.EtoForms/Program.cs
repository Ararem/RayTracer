using System;
using Eto.Forms;
using Eto.Drawing;
using RayTracer.Display.EtoForms.Appearance;

namespace RayTracer.Display.EtoForms
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			KnownStyles.Register();
			new Application(Eto.Platform.Detect).Run(new MainForm());
		}
	}
}
