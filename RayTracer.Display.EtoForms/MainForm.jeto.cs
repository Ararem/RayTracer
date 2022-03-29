using Eto.Forms;
using Eto.Serialization.Json;
using System;

namespace RayTracer.Display.EtoForms;

public class MainForm : Form
{
	public MainForm()
	{
		JsonReader.Load(this);
	}

	protected void HandleClickMe(object sender, EventArgs e)
	{
		MessageBox.Show("I was clicked!");
	}

	protected void HandleAbout(object sender, EventArgs e)
	{
		new AboutDialog().ShowDialog(this);
	}

	protected void HandleQuit(object sender, EventArgs e)
	{
		Application.Instance.Quit();
	}
}