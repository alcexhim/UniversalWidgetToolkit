﻿using System;
using UniversalWidgetToolkit.Dialogs.Native;
using UniversalWidgetToolkit.Printing;

namespace UniversalWidgetToolkit.Dialogs
{
	namespace Native
	{
		public interface IPrintDialogImplementation
		{
			Printer GetSelectedPrinter();
			void SetSelectedPrinter(Printer value);

			PrintSettings GetSettings();
			void SetSettings(PrintSettings value);
		}
	}
	public class PrintDialog : CommonDialog
	{
		private Printer mvarSelectedPrinter = null;
		public Printer SelectedPrinter
		{
			get
			{
				IPrintDialogImplementation impl = (ControlImplementation as IPrintDialogImplementation);
				if (impl != null) mvarSelectedPrinter = impl.GetSelectedPrinter();
				return mvarSelectedPrinter;
			}
			set
			{
				IPrintDialogImplementation impl = (ControlImplementation as IPrintDialogImplementation);
				impl?.SetSelectedPrinter(value);
				mvarSelectedPrinter = value;
			}
		}

		private PrintSettings mvarSettings = null;
		public PrintSettings Settings
		{
			get
			{
				IPrintDialogImplementation impl = (ControlImplementation as IPrintDialogImplementation);
				if (impl != null) mvarSettings = impl.GetSettings();
				return mvarSettings;
			}
			set
			{
				IPrintDialogImplementation impl = (ControlImplementation as IPrintDialogImplementation);
				impl?.SetSettings(value);
				mvarSettings = value;
			}
		}
	}
}
