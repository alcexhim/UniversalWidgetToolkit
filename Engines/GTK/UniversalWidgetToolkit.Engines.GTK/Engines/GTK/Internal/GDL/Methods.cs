﻿using System;
using System.Runtime.InteropServices;

namespace UniversalWidgetToolkit.Engines.GTK.Internal.GDL
{
	public static class Methods
	{
		private const string LIBRARY_FILENAME = "libgdl-3";

		[DllImport(LIBRARY_FILENAME)]
		public static extern IntPtr gdl_dock_new();

		[DllImport(LIBRARY_FILENAME)]
		public static extern IntPtr gdl_dock_bar_new(IntPtr hDock);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">The item to bind.</param>
		/// <param name="dock">The GdlDock widget to bind it to. Note that this widget must be a type of GdlDock.</param>
		[DllImport(LIBRARY_FILENAME)]
		public static extern void gdl_dock_add_item(IntPtr item, IntPtr dock, Constants.GdlDockPlacement placement);

		/// <summary>
		/// Creates a new dock item widget.
		/// </summary>
		/// <returns>The newly created dock item widget.</returns>
		/// <param name="name">Unique name for identifying the dock object.</param>
		/// <param name="long_name">Human readable name for the dock object.</param>
		/// <param name="behavior">General behavior for the dock item (i.e. whether it can float, if it's locked, etc.), as specified by <see cref="Constants.GdlDockItemBehavior"/> flags.</param>
		[DllImport(LIBRARY_FILENAME)]
		public static extern IntPtr gdl_dock_item_new(string name, string long_name, Constants.GdlDockItemBehavior behavior);

		/// <summary>
		/// Binds this dock item to a new dock master.
		/// </summary>
		/// <param name="item">The item to bind.</param>
		/// <param name="dock">The GdlDock widget to bind it to. Note that this widget must be a type of GdlDock.</param>
		[DllImport(LIBRARY_FILENAME)]
		public static extern void gdl_dock_item_bind(IntPtr item, IntPtr dock);
	}
}