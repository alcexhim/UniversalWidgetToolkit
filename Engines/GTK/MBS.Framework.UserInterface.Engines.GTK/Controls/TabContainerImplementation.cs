﻿using System;
using MBS.Framework.UserInterface.Controls;
using MBS.Framework.UserInterface.Controls.Native;
using MBS.Framework.UserInterface.Layouts;

namespace MBS.Framework.UserInterface.Engines.GTK.Controls
{
	[ControlImplementation(typeof(TabContainer))]
	public class TabContainerImplementation : GTKNativeImplementation, ITabContainerControlImplementation
	{
		public TabContainerImplementation(Engine engine, Control control) : base(engine, control)
		{
		}

		public static void NotebookAppendPage(Engine engine, TabContainer ctl, IntPtr handle, TabPage page, int indexAfter = -1)
		{
			Container tabControlContainer = new Container();
			tabControlContainer.Layout = new BoxLayout(Orientation.Horizontal, 8);

			Label lblTabText = new Label(page.Text);
			lblTabText.WordWrap = WordWrapMode.Never;

			tabControlContainer.Controls.Add(lblTabText, new BoxLayout.Constraints(true, true, 8));

			System.Reflection.FieldInfo fiParent = tabControlContainer.GetType().BaseType.GetField("mvarParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			fiParent.SetValue(tabControlContainer, page);

			foreach (Control ctlTabButton in ctl.TabTitleControls)
			{
				tabControlContainer.Controls.Add(ctlTabButton);
			}

			engine.CreateControl(tabControlContainer);
			IntPtr hTabLabel = (engine.GetHandleForControl(tabControlContainer) as GTKNativeControl).Handle;

			ContainerImplementation cimpl = new ContainerImplementation(engine, page);
			cimpl.CreateControl(page);
			IntPtr container = (cimpl.Handle as GTKNativeControl).Handle;

			if (indexAfter == -1)
			{
				Internal.GTK.Methods.GtkNotebook.gtk_notebook_append_page(handle, container, hTabLabel);
				Internal.GTK.Methods.GtkWidget.gtk_widget_show_all (hTabLabel);
			}
			else
			{
			}
		}

		public void ClearTabPages()
		{
			if (!Control.IsCreated)
				return;

			IntPtr handle = (Engine.GetHandleForControl(Control) as GTKNativeControl).Handle;
			int pageCount = Internal.GTK.Methods.GtkNotebook.gtk_notebook_get_n_pages(handle);
			for (int i = 0; i < pageCount; i++)
			{
				Internal.GTK.Methods.GtkNotebook.gtk_notebook_remove_page(handle, i);
			}
		}

		public void InsertTabPage(int index, TabPage item)
		{
			if (!Control.IsCreated)
				return;

			IntPtr handle = (Engine.GetHandleForControl(Control) as GTKNativeControl).Handle;
			NotebookAppendPage(Engine, (Control as TabContainer), handle, item, index);
		}

		public void RemoveTabPage(TabPage tabPage)
		{
			throw new NotImplementedException();
		}

		protected override NativeControl CreateControlInternal(Control control)
		{
			TabContainer ctl = (control as TabContainer);
			IntPtr handle = Internal.GTK.Methods.GtkNotebook.gtk_notebook_new();

			foreach (TabPage tabPage in ctl.TabPages)
			{
				NotebookAppendPage(Engine, ctl, handle, tabPage);
			}

			return new GTKNativeControl(handle);
		}
	}
}