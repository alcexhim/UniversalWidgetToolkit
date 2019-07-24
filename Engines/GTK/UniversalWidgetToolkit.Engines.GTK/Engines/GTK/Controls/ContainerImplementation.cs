﻿using System;
using System.Collections.Generic;
using UniversalWidgetToolkit.Layouts;

namespace UniversalWidgetToolkit.Engines.GTK.Controls
{
	[NativeImplementation(typeof(Container))]
	public class ContainerImplementation : GTKNativeImplementation
	{
		public ContainerImplementation(Engine engine, Container control)
			: base(engine, control)
		{
		}

		private Dictionary<Layout, IntPtr> handlesByLayout = new Dictionary<Layout, IntPtr>();

		private IntPtr mvarContainerHandle = IntPtr.Zero;

		private void ApplyLayout(IntPtr hContainer, Control ctl, Layout layout)
		{
			IntPtr ctlHandle = Engine.GetHandleForControl(ctl);

			if (layout is Layouts.BoxLayout)
			{
				Layouts.BoxLayout box = (layout as Layouts.BoxLayout);
				Internal.GTK.Methods.gtk_box_set_spacing(hContainer, box.Spacing);
				Internal.GTK.Methods.gtk_box_set_homogeneous(hContainer, box.Homogeneous);

				BoxLayout.Constraints c = (box.GetControlConstraints(ctl) as BoxLayout.Constraints);
				if (c == null) c = new BoxLayout.Constraints();

				int padding = c.Padding == 0 ? ctl.Padding.All : c.Padding;

				switch (c.PackType)
				{
					case BoxLayout.PackType.Start:
					{
						Internal.GTK.Methods.gtk_box_pack_start(hContainer, ctlHandle, c.Expand, c.Fill, padding);
						break;
					}
					case BoxLayout.PackType.End:
					{
						Internal.GTK.Methods.gtk_box_pack_end(hContainer, ctlHandle, c.Expand, c.Fill, padding);
						break;
					}
				}
			}
			else if (layout is Layouts.AbsoluteLayout)
			{
				Layouts.AbsoluteLayout.Constraints constraints = (Layouts.AbsoluteLayout.Constraints)layout.GetControlConstraints(ctl);
				if (constraints == null) constraints = new Layouts.AbsoluteLayout.Constraints(0, 0, 0, 0);
				Internal.GTK.Methods.gtk_fixed_put(hContainer, ctlHandle, constraints.X, constraints.Y);
			}
			else if (layout is Layouts.GridLayout)
			{
				Layouts.GridLayout.Constraints constraints = (Layouts.GridLayout.Constraints)layout.GetControlConstraints(ctl);
				if (constraints != null)
				{
					// GtkTable has been deprecated. Use GtkGrid instead. It provides the same capabilities as GtkTable for arranging widgets in a rectangular grid, but does support height-for-width geometry management.
					Internal.GTK.Methods.gtk_grid_attach(hContainer, ctlHandle, constraints.Column, constraints.Row, constraints.ColumnSpan, constraints.RowSpan);
					// Internal.GTK.Methods.gtk_table_attach(hContainer, ctlHandle, (uint)constraints.Column, (uint)(constraints.Column + constraints.ColumnSpan), (uint)constraints.Row, (uint)(constraints.Row + constraints.RowSpan), Internal.GTK.Constants.GtkAttachOptions.Expand, Internal.GTK.Constants.GtkAttachOptions.Fill, 0, 0);

					if ((constraints.Expand & ExpandMode.Horizontal) == ExpandMode.Horizontal)
					{
						Internal.GTK.Methods.gtk_widget_set_hexpand(ctlHandle, true);
					}
					if ((constraints.Expand & ExpandMode.Vertical) == ExpandMode.Horizontal)
					{
						Internal.GTK.Methods.gtk_widget_set_vexpand(ctlHandle, true);
					}
				}
			}
			else
			{
				Internal.GTK.Methods.gtk_container_add(hContainer, ctlHandle);
			}
		}


		protected override NativeControl CreateControlInternal(Control control)
		{
			IntPtr hContainer = IntPtr.Zero;
			Container container = (control as Container);

			Layout layout = container.Layout;
			if (container.Layout == null) layout = new Layouts.AbsoluteLayout();

			if (layout is Layouts.BoxLayout)
			{
				Layouts.BoxLayout box = (layout as Layouts.BoxLayout);
				Internal.GTK.Constants.GtkOrientation orientation = Internal.GTK.Constants.GtkOrientation.Vertical;
				switch (box.Orientation)
				{
					case Orientation.Horizontal:
					{
						orientation = Internal.GTK.Constants.GtkOrientation.Horizontal;
						break;
					}
					case Orientation.Vertical:
					{
						orientation = Internal.GTK.Constants.GtkOrientation.Vertical;
						break;
					}
				}
				hContainer = Internal.GTK.Methods.gtk_box_new(orientation, ((Layouts.BoxLayout)layout).Homogeneous, ((Layouts.BoxLayout)layout).Spacing);
			}
			else if (layout is Layouts.AbsoluteLayout)
			{
				Layouts.AbsoluteLayout abs = (layout as Layouts.AbsoluteLayout);
				hContainer = Internal.GTK.Methods.gtk_fixed_new();
			}
			else if (layout is Layouts.GridLayout)
			{
				Layouts.GridLayout grid = (layout as Layouts.GridLayout);
				// GtkTable has been deprecated. Use GtkGrid instead. It provides the same capabilities as GtkTable for arranging widgets in a rectangular grid, but does support height-for-width geometry management.
				hContainer = Internal.GTK.Methods.gtk_grid_new();
				// hContainer = Internal.GTK.Methods.gtk_table_new();
				Internal.GTK.Methods.gtk_grid_set_row_spacing(hContainer, (uint)grid.RowSpacing);
				Internal.GTK.Methods.gtk_grid_set_column_spacing(hContainer, (uint)grid.ColumnSpacing);
			}

			if (hContainer != IntPtr.Zero)
			{
				mvarContainerHandle = hContainer;
				handlesByLayout[layout] = hContainer;

				foreach (Control ctl in container.Controls)
				{
					bool ret = Engine.CreateControl(ctl);
					if (!ret) continue;

					ApplyLayout(hContainer, ctl, layout);
				}
			}

			return new GTKNativeControl(hContainer);
		}
	}
}
