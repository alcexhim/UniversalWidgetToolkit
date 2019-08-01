﻿using System;
using System.Diagnostics.Contracts;

using UniversalWidgetToolkit;
using UniversalWidgetToolkit.Controls;
using UniversalWidgetToolkit.Controls.Native;
using System.Runtime.InteropServices;

namespace UniversalWidgetToolkit.Engines.GTK.Controls
{
	[ControlImplementation(typeof(Button))]
	public class ButtonImplementation : GTKNativeImplementation, IButtonControlImplementation
	{
		private Internal.GObject.Delegates.GCallback gc_Button_Clicked = null;
		public ButtonImplementation(Engine engine, Control control) : base(engine, control)
		{
			gc_Button_Clicked = new Internal.GObject.Delegates.GCallback(Button_Clicked);
		}

		private void Button_Clicked(IntPtr handle, IntPtr data)
		{
			Button button = (Application.Engine.GetControlByHandle(handle) as Button);
			// maybe it's the button not the tabpage?
			if (button != null)
			{
				EventArgs e = new EventArgs();
				InvokeMethod(button.ControlImplementation, "OnClick", e);
			}
		}

		protected override string GetControlTextInternal(Control control)
		{
			IntPtr handle = Engine.GetHandleForControl(control);
			IntPtr hTitle = Internal.GTK.Methods.GtkButton.gtk_button_get_label (handle);
			return Marshal.PtrToStringAuto (hTitle);
		}
		protected override void SetControlTextInternal(Control control, string text)
		{
			IntPtr handle = Engine.GetHandleForControl(control);
			IntPtr hTitle = Marshal.StringToHGlobalAuto (text);
			Internal.GTK.Methods.GtkButton.gtk_button_set_label(handle, hTitle);
		}

		protected override NativeControl CreateControlInternal(Control control)
		{
			Button ctl = (control as Button);
			Contract.Assert(ctl != null);

			IntPtr handle = Internal.GTK.Methods.GtkButton.gtk_button_new();
			Internal.GTK.Methods.GtkButton.gtk_button_set_always_show_image(handle, ctl.AlwaysShowImage);
			switch (ctl.BorderStyle)
			{
				case ButtonBorderStyle.None:
				{ 
					Internal.GTK.Methods.GtkButton.gtk_button_set_relief(handle, Internal.GTK.Constants.GtkReliefStyle.None);
					break;
				}
				case ButtonBorderStyle.Half:
				{ 
					Internal.GTK.Methods.GtkButton.gtk_button_set_relief(handle, Internal.GTK.Constants.GtkReliefStyle.Half);
					break;
				}
				case ButtonBorderStyle.Normal:
				{ 
					Internal.GTK.Methods.GtkButton.gtk_button_set_relief(handle, Internal.GTK.Constants.GtkReliefStyle.Normal);
					break;
				}
			}

			if (ctl.StockType != ButtonStockType.None) {
				Image image = new Image ();
				image.IconName = Engine.StockTypeToString ((StockType)ctl.StockType);
				image.IconSize = ctl.ImageSize;
				if (Engine.CreateControl (image)) {
					IntPtr hImage = Engine.GetHandleForControl (image);
					Internal.GTK.Methods.GtkButton.gtk_button_set_image(handle, hImage);
				}
			}

			// DON'T SET THIS... only Dialog buttons should get this by default
			// Internal.GTK.Methods.Methods.gtk_widget_set_can_default (handle, true);

			Internal.GObject.Methods.g_signal_connect(handle, "clicked", gc_Button_Clicked, new IntPtr(0xDEADBEEF));

			Internal.GTK.Methods.GtkButton.gtk_button_set_image_position (handle, (Engine as GTKEngine).RelativePositionToGtkPositionType(ctl.ImagePosition));

			switch (ctl.HorizontalAlignment) {
			case HorizontalAlignment.Left:
				{
					Internal.GTK.Methods.GtkWidget.gtk_widget_set_halign (handle, Internal.GTK.Constants.GtkAlign.Start);
					break;
				}
			case HorizontalAlignment.Center:
				{
					Internal.GTK.Methods.GtkWidget.gtk_widget_set_halign (handle, Internal.GTK.Constants.GtkAlign.Center);
					break;
				}
			case HorizontalAlignment.Right:
				{
					Internal.GTK.Methods.GtkWidget.gtk_widget_set_halign (handle, Internal.GTK.Constants.GtkAlign.End);
					break;
				}
			}

			// we do this to support older versions of Gtk+ that may not handle gtk_widget_set_focus_on_click
			Internal.GTK.Methods.GtkButton.gtk_button_set_focus_on_click (handle, ctl.FocusOnClick);

			return new GTKNativeControl(handle);
		}

		public RelativePosition GetImagePosition()
		{
			IntPtr handle = (Handle as GTKNativeControl).Handle;
			Internal.GTK.Constants.GtkPositionType value = Internal.GTK.Methods.GtkButton.gtk_button_get_image_position (handle);
			return (Engine as GTKEngine).GtkPositionTypeToRelativePosition(value);
		}
		public void SetImagePosition(RelativePosition value)
		{
			IntPtr handle = (Handle as GTKNativeControl).Handle;
			Internal.GTK.Constants.GtkPositionType value2 = (Engine as GTKEngine).RelativePositionToGtkPositionType(value);
			Internal.GTK.Methods.GtkButton.gtk_button_set_image_position (handle, value2);
		}
	}
}
