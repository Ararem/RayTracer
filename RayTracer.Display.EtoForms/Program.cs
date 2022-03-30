using System;
using Eto.Forms;

namespace RayTracer.Display.EtoForms
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			new Application(Eto.Platform.Detect!).Run(new MainForm());
		}
	}
}
