﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using MBS.Framework.UserInterface;
using MBS.Framework.UserInterface.Controls;
using MBS.Framework.UserInterface.Controls.Native;
using MBS.Framework.UserInterface.Input.Keyboard;
using MBS.Framework.UserInterface.Input.Mouse;
using MBS.Framework.UserInterface.Engines.GTK.Internal.GLib;
using MBS.Framework.UserInterface.Engines.GTK.Internal.GTK;
using MBS.Framework.UserInterface.Controls.ListView;
using MBS.Framework.UserInterface.Controls.ListView.Native;

namespace MBS.Framework.UserInterface.Engines.GTK.Controls
{
	[ControlImplementation(typeof(ListViewControl))]
	public class ListViewImplementation : GTKNativeImplementation, IListViewNativeImplementation, MBS.Framework.UserInterface.Native.ITreeModelRowCollectionNativeImplementation
	{
		private enum ImplementedAsType
		{
			TreeView,
			IconView
		}
		private static ImplementedAsType ImplementedAs(ListViewControl tv)
		{
			switch (tv.Mode)
			{
				case ListViewMode.LargeIcon:
				case ListViewMode.SmallIcon:
				case ListViewMode.Thumbnail:
				case ListViewMode.Tile:
				{
					return ImplementedAsType.IconView; 
				}
				case ListViewMode.List:
				case ListViewMode.Detail:
				{
					return ImplementedAsType.TreeView;
				}
			}
			throw new ArgumentException(String.Format("ListViewMode not supported {0}", tv.Mode));
		}
		
		public ListViewImplementation(Engine engine, Control control) : base(engine, control)
		{
			block_selection_func_handler =  new Internal.GTK.Delegates.GtkTreeSelectionFunc(block_selection_func);
			gtk_cell_renderer_edited_d = new Action<IntPtr, string, string, IntPtr>(gtk_cell_renderer_edited);
		}

		private void SetSelectionModeInternal(IntPtr handle, ListViewControl tv, SelectionMode value)
		{
			switch (ImplementedAs(tv))
			{
				case ImplementedAsType.TreeView:
				{
					IntPtr hTreeSelection = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_selection(handle);
					if (hTreeSelection != IntPtr.Zero)
					{
						Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_set_mode(hTreeSelection, GTKEngine.SelectionModeToGtkSelectionMode(tv.SelectionMode));
					}
					break;
				}
				case ImplementedAsType.IconView:
				{
					Internal.GTK.Methods.GtkIconView.gtk_icon_view_set_selection_mode(handle, GTKEngine.SelectionModeToGtkSelectionMode(tv.SelectionMode));
					break;
				}
			}
			
		}

		private SelectionMode GetSelectionModeInternal(IntPtr handle, ListViewControl tv)
		{
			switch (ImplementedAs(tv))
			{
				case ImplementedAsType.TreeView:
				{
					IntPtr hTreeSelection = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_selection(handle);
					Internal.GTK.Constants.GtkSelectionMode mode = Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_get_mode(hTreeSelection);
					return GTKEngine.GtkSelectionModeToSelectionMode(mode);
				}
				case ImplementedAsType.IconView:
				{
					Internal.GTK.Constants.GtkSelectionMode mode = Internal.GTK.Methods.GtkIconView.gtk_icon_view_get_selection_mode(handle);
					return GTKEngine.GtkSelectionModeToSelectionMode(mode);
				}
			}
			throw new InvalidOperationException();
		}

		public void SetSelectionMode(SelectionMode value)
		{
			IntPtr handle = (Handle as GTKNativeControl).GetNamedHandle("TreeView");
			ListViewControl tv = Control as ListViewControl;
			SetSelectionModeInternal(handle, tv, value);
		}
		public SelectionMode GetSelectionMode()
		{
			IntPtr handle = (Handle as GTKNativeControl).GetNamedHandle("TreeView");
			ListViewControl tv = Control as ListViewControl;
			return GetSelectionModeInternal(handle, tv);
		}

		public ListViewHitTestInfo HitTest(double x, double y)
		{
			IntPtr hTreeView = GetHandleForControl(Control as ListViewControl);
			return HitTestInternal(hTreeView, x, y);
		}
		private ListViewHitTestInfo HitTestInternal(IntPtr handle, double x, double y)
		{
			IntPtr hPath = IntPtr.Zero;
			IntPtr hColumn = IntPtr.Zero;
			int cx = 0, cy = 0;
			
			TreeModelRow row = null;
			TreeModelColumn column = null;
			bool ret = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_path_at_pos(handle, (int)x, (int)y, ref hPath, ref hColumn, ref cx, ref cy);
			if (ret)
			{
				Internal.GTK.Structures.GtkTreeIter iter = new Internal.GTK.Structures.GtkTreeIter();
				IntPtr hTreeModel = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_model(handle);
				bool retIter = Internal.GTK.Methods.GtkTreeModel.gtk_tree_model_get_iter(hTreeModel, ref iter, hPath);
				if (retIter)
				{
					row = (Engine as GTKEngine).GetTreeModelRowForGtkTreeIter(iter);
				}
			}
			return new ListViewHitTestInfo(row, column);
		}
		
		/// <summary>
		/// We catch the GtkTreeView.OnMouseDown event to prevent selection from changing
		/// if it is a multi-select view.
		/// </summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			ListViewControl tv = Control as ListViewControl;
			IntPtr handle = (Handle as GTKNativeControl).GetNamedHandle("TreeView");
			if (tv == null) return;

			TreeModelRow row = HitTestInternal(handle, e.X, e.Y).Row;
			if (row == null)
			{
				// added 2021-02-26 15:27:08 by beckermj - nautilus clears the selections when you click in the tree area but not on an item
				tv.SelectedRows.Clear();
				InvokeMethod(tv, "OnSelectionChanged", new object[] { EventArgs.Empty });
			}
			else
			{
				// stop the selection
				// we need to check if the row we're mouse-downing on is in
				// the selection list or not
				if (tv.SelectedRows.Contains(row))
				{
					// if it is, we should cancel the selection because we might
					// be dragging
					BlockSelection();
					prevSelectedRow = row;
				}
				else
				{
					// if it isn't, we're safe to allow the selection
					UnblockSelection();
					prevSelectedRow = null;
				}
			}

			if (e.Buttons == MouseButtons.Secondary)
			{
				// hack to set the selected item before the BeforeContextMenu event fires
				if (tv.SelectedRows.Count == 0)
				{
					ListViewHitTestInfo lvhti = HitTest(e.X, e.Y);
					if (lvhti.Row != null)
					{
						tv.SelectedRows.Add(lvhti.Row);
					}
				}
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseDoubleClick (MouseEventArgs e)
		{
			base.OnMouseDoubleClick (e);

			// always do this in case the user decides to cancel the event
			if (e.Cancel) return;

			if (e.Buttons == MouseButtons.Primary && e.ModifierKeys == KeyboardModifierKey.None) {

				ListViewControl tv = Control as ListViewControl;
				IntPtr handle = (Handle as GTKNativeControl).GetNamedHandle("TreeView");
				if (tv == null) return;

				ListViewHitTestInfo lvhi = tv.HitTest(e.X, e.Y);
				if (lvhi?.Row == null) return;

				lvhi.Row.Expanded = !lvhi.Row.Expanded;
			}
		}
		private TreeModelRow prevSelectedRow = null;
		protected override void OnMouseUp(MouseEventArgs e)
		{
			ListViewControl tv = Control as ListViewControl;
			UnblockSelection();
			if (e.ModifierKeys == KeyboardModifierKey.None)
			{
				if (prevSelectedRow != null)
				{
					tv.SelectedRows.Clear();
					tv.SelectedRows.Add(prevSelectedRow);
					prevSelectedRow = null;
				}
			}
		}

		private Internal.GTK.Delegates.GtkTreeSelectionFunc block_selection_func_handler = null;
		private bool block_selection_func(IntPtr /*GtkTreeSelection*/ selection, IntPtr /*GtkTreeModel*/ model, IntPtr /*GtkTreePath*/ path, bool path_currently_selected, IntPtr data)
		{
			return false;
		}

		private void BlockSelection()
		{
			IntPtr handle = (Handle as GTKNativeControl).GetNamedHandle("TreeView");
			IntPtr hTreeSelection = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_selection(handle);
			Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_set_select_function(hTreeSelection, block_selection_func_handler, IntPtr.Zero, IntPtr.Zero);
		}
		private void UnblockSelection()
		{
			IntPtr handle = (Handle as GTKNativeControl).GetNamedHandle("TreeView");
			IntPtr hTreeSelection = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_selection(handle);
			Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_set_select_function(hTreeSelection, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		}

		public void UpdateTreeModel ()
		{
			UpdateTreeModel ((Handle as GTKNativeControl).GetNamedHandle("TreeView"));
		}

		public void UpdateTreeModelColumn(TreeModelRowColumn rc)
		{
			TreeModel tm = (rc.Parent.ParentControl as ListViewControl).Model;
			GTKNativeTreeModel ntm = Engine.GetHandleForTreeModel(tm) as GTKNativeTreeModel;
			Internal.GTK.Structures.GtkTreeIter hIter = (Engine as GTKEngine).GetGtkTreeIterForTreeModelRow(rc.Parent);

			IntPtr hTreeStore = ntm.Handle;

			Internal.GLib.Structures.Value val = Internal.GLib.Structures.Value.FromObject(rc.Value);

			Internal.GTK.Methods.GtkTreeStore.gtk_tree_store_set_value(hTreeStore, ref hIter, tm.Columns.IndexOf(rc.Column), ref val);
		}

		private Dictionary<ListViewColumn, IntPtr> _ColumnHandles = new Dictionary<ListViewColumn, IntPtr>();
		private Dictionary<IntPtr, ListViewColumn> _HandleColumns = new Dictionary<IntPtr, ListViewColumn>();
		private IntPtr GetHandleForTreeViewColumn(ListViewColumn column)
		{
			if (!_ColumnHandles.ContainsKey(column))
				return IntPtr.Zero;
			return _ColumnHandles[column];
		}
		private ListViewColumn GetTreeViewColumnForHandle(IntPtr handle)
		{
			if (!_HandleColumns.ContainsKey(handle))
				return null;
			return _HandleColumns[handle];
		}
		private void RegisterTreeViewColumn(ListViewColumn column, IntPtr handle)
		{
			_ColumnHandles[column] = handle;
			_HandleColumns[handle] = column;
		}

		public bool IsColumnResizable(ListViewColumn column)
		{
			IntPtr hColumn = GetHandleForTreeViewColumn(column);
			return Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_get_resizable(hColumn);
		}
		public void SetColumnResizable(ListViewColumn column, bool value)
		{
			IntPtr hColumn = GetHandleForTreeViewColumn(column);
			Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_set_resizable(hColumn, value);
		}
		public bool IsColumnReorderable(ListViewColumn column)
		{
			IntPtr hColumn = GetHandleForTreeViewColumn(column);
			return Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_get_reorderable(hColumn);
		}
		public void SetColumnReorderable(ListViewColumn column, bool value)
		{
			IntPtr hColumn = GetHandleForTreeViewColumn(column);
			Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_set_reorderable(hColumn, value);
		}

		private Action<IntPtr, string, string, IntPtr> gtk_cell_renderer_edited_d = null;
		private void gtk_cell_renderer_edited(IntPtr /*GtkCellRendererText*/ renderer, string path, string new_text, IntPtr user_data)
		{
			ListViewControl lv = (Control as ListViewControl);
			if (lv.Model == null)
				return;

			IntPtr hTreeModel = GetHandleForTreeModel(lv.Model);
			IntPtr hPath = Internal.GTK.Methods.GtkTreePath.gtk_tree_path_new_from_string(path);
			Internal.GTK.Structures.GtkTreeIter iter = new Internal.GTK.Structures.GtkTreeIter();
			Internal.GTK.Methods.GtkTreeModel.gtk_tree_model_get_iter(hTreeModel, ref iter, hPath);

			TreeModelRow row = (Engine as GTKEngine).GetTreeModelRowForGtkTreeIter(iter);
			int columnIndex = user_data.ToInt32();
			if (columnIndex < row.RowColumns.Count)
			{
				TreeModelRowColumn rc = row.RowColumns[user_data.ToInt32()];
				if (row != null)
				{
					TreeModelRowColumnEditingEventArgs ee = new TreeModelRowColumnEditingEventArgs(row, rc, rc.Value, new_text);
					OnRowColumnEditing(ee);
					if (!ee.Cancel)
					{
						// we don't simply  `if (cancel) return;`  here because we need to free the tree path
						rc.Value = new_text;
						OnRowColumnEdited(new TreeModelRowColumnEditedEventArgs(row, rc, rc.Value, new_text));
					}
				}
			}

			// we created it ourselves (new_from_string), so we have to free it ourselves
			Internal.GTK.Methods.GtkTreePath.gtk_tree_path_free(hPath);
		}

		protected virtual void OnRowColumnEditing(TreeModelRowColumnEditingEventArgs e)
		{
			InvokeMethod((Control as ListViewControl), "OnRowColumnEditing", new object[] { e });
		}
		protected virtual void OnRowColumnEdited(TreeModelRowColumnEditedEventArgs e)
		{
			InvokeMethod((Control as ListViewControl), "OnRowColumnEdited", new object[] { e });
		}

		protected void UpdateTreeModel (IntPtr handle)
		{
			ListViewControl tv = (Control as ListViewControl);

			if (tv.Model != null)
			{
				GTKNativeTreeModel ntm = Engine.CreateTreeModel(tv.Model) as GTKNativeTreeModel;
				IntPtr hTreeStore = ntm.Handle;

				switch (ImplementedAs(tv))
				{
					case ImplementedAsType.TreeView:
					{
						// HACK: get rid of all existing columns in the TreeView before adding new ones
						// this fixes a regression where certain treeviews (perhaps from containerlayout-generated UI) end up having duplicated columns
						while (Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_n_columns(handle) > 0)
						{
							Internal.GTK.Methods.GtkTreeView.gtk_tree_view_remove_column(handle, Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_column(handle, 0));
						}

						foreach (ListViewColumn tvc in tv.Columns)
						{
							TreeModelColumn c = tvc.Column;
							if (tv.Model != null)
							{
								int columnIndex = tv.Model.Columns.IndexOf(tvc.Column);

								IntPtr hColumn = Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_new();
								Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_set_title(hColumn, tvc.Title);
								Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_clear(hColumn);

								if (tvc is ListViewColumnText)
								{
									IntPtr renderer = IntPtr.Zero;
									ListViewColumnText tvct = (tvc as ListViewColumnText);

									if (tvct.ValidValues.Count > 0)
									{
										renderer = Internal.GTK.Methods.GtkCellRendererCombo.gtk_cell_renderer_combo_new();

										DefaultTreeModel model = new DefaultTreeModel(new Type[] { typeof(string) });
										model.TreeModelChanged += Model_TreeModelChanged;

										NativeTreeModel nTreeModel = (Engine as GTKEngine).CreateTreeModel(model);
										IntPtr hModel = (nTreeModel as GTKNativeTreeModel).Handle;

										TreeModelForListViewColumn[tvct] = model;

										Internal.GObject.Methods.g_object_set_property(renderer, "model", hModel);

										Internal.GLib.Structures.Value valTextColumn = new Internal.GLib.Structures.Value((int)0);
										Internal.GObject.Methods.g_object_set_property(renderer, "text_column", ref valTextColumn);

										AddColumnValidValues(tvct, tvct.ValidValues);
									}
									else
									{
										renderer = Internal.GTK.Methods.GtkCellRendererText.gtk_cell_renderer_text_new();
									}

									if (renderer != null)
									{
										Internal.GObject.Methods.g_signal_connect(renderer, "edited", gtk_cell_renderer_edited_d, new IntPtr(tv.Model.Columns.IndexOf(c)));
										RegisterCellRendererForColumn(tvc, renderer);

										Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_pack_start(hColumn, renderer, true);
										Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_add_attribute(hColumn, renderer, "text", columnIndex);
									}
								}
								else if (tvc is ListViewColumnCheckBox)
								{
									IntPtr renderer = Internal.GTK.Methods.GtkCellRendererToggle.gtk_cell_renderer_toggle_new();
									Internal.GTK.Methods.GtkCellRendererToggle.gtk_cell_renderer_toggle_set_activatable(renderer, true);

									Internal.GObject.Methods.g_signal_connect(renderer, "edited", gtk_cell_renderer_edited_d, new IntPtr(tv.Model.Columns.IndexOf(c)));
									RegisterCellRendererForColumn(tvc, renderer);

									Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_pack_start(hColumn, renderer, true);
									// Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_add_attribute(hColumn, renderer, "active", columnIndex);
								}

								Internal.GTK.Methods.GtkTreeView.gtk_tree_view_insert_column(handle, hColumn, -1);

								RegisterTreeViewColumn(tvc, hColumn);

								Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_set_sort_column_id(hColumn, columnIndex);
								Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_set_resizable(hColumn, tvc.Resizable);
								Internal.GTK.Methods.GtkTreeViewColumn.gtk_tree_view_column_set_reorderable(hColumn, tvc.Reorderable);

								SetColumnEditable(tvc, tvc.Editable);
							}
						}

						Internal.GTK.Methods.GtkTreeView.gtk_tree_view_set_model(handle, hTreeStore);
						break;
					}
					case ImplementedAsType.IconView:
					{
						Internal.GTK.Methods.GtkIconView.gtk_icon_view_set_model(handle, hTreeStore);

						Internal.GTK.Methods.GtkIconView.gtk_icon_view_set_item_width(handle, 96);
						Internal.GTK.Methods.GtkIconView.gtk_icon_view_set_text_column(handle, 0);
						break;
					}
				}
			}
			Internal.GTK.Methods.GtkWidget.gtk_widget_show_all (handle);
		}

		void Model_TreeModelChanged(object sender, TreeModelChangedEventArgs e)
		{
			TreeModelRow.TreeModelRowCollection coll = (sender as TreeModelRow.TreeModelRowCollection);
			(((UIApplication)Application.Instance).Engine as GTKEngine).UpdateTreeModel(coll.Model, e);
		}


		private Dictionary<ListViewColumnText, DefaultTreeModel> TreeModelForListViewColumn = new Dictionary<ListViewColumnText, DefaultTreeModel>();
		public void AddColumnValidValues(ListViewColumnText tvc, System.Collections.IList list)
		{
			if (!TreeModelForListViewColumn.ContainsKey(tvc))
				return;

			DefaultTreeModel model = TreeModelForListViewColumn[tvc];
			for (int i = 0; i < list.Count; i++)
			{
				model.Rows.Add(new TreeModelRow(new TreeModelRowColumn[]
				{
					new TreeModelRowColumn(model.Columns[0], list[i])
				}));
			}
		}
		public void RemoveColumnValidValues(ListViewColumnText tvc, System.Collections.IList list)
		{
			if (!TreeModelForListViewColumn.ContainsKey(tvc))
				return;

			DefaultTreeModel model = TreeModelForListViewColumn[tvc];
			return;

			for (int i = 0; i < list.Count; i++)
			{
				model.Rows.Remove(new TreeModelRow(new TreeModelRowColumn[]
				{
					new TreeModelRowColumn(model.Columns[0], list[i])
				}));
			}
		}

		private Dictionary<ListViewColumn, IntPtr> _CellRenderersForColumn = new Dictionary<ListViewColumn, IntPtr>();
		private Dictionary<IntPtr, ListViewColumn> _ColumnsForCellRenderer = new Dictionary<IntPtr, ListViewColumn>();
		private void RegisterCellRendererForColumn(ListViewColumn column, IntPtr hrenderer)
		{
			_CellRenderersForColumn[column] = hrenderer;
			_ColumnsForCellRenderer[hrenderer] = column;
		}

		public void SetColumnEditable(ListViewColumn tvc, bool editable)
		{
			if (!_CellRenderersForColumn.ContainsKey(tvc))
				return;

			IntPtr hRenderer = _CellRenderersForColumn[tvc];

			Internal.GLib.Structures.Value valEditable = new Internal.GLib.Structures.Value(editable);
			Internal.GObject.Methods.g_object_set_property(hRenderer, "editable", ref valEditable);
		}

		protected override NativeControl CreateControlInternal(Control control)
		{
			ListViewControl tv = (control as ListViewControl);
			IntPtr handle = IntPtr.Zero;

			// TODO: fix GtkTreeView implementation
			// Internal.GTK.Methods.Methods.gtk_tree_store_insert_with_valuesv(hTreeStore, ref hIterFirst, IntPtr.Zero, 0, ref columns, values, values.Length);
			tv.SelectedRows.ItemRequested += SelectedRows_ItemRequested;
			tv.SelectedRows.Cleared += SelectedRows_Cleared;

			switch (ImplementedAs (tv)) {
			case ImplementedAsType.TreeView: {
					handle = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_new ();

					Internal.GTK.Methods.GtkTreeView.gtk_tree_view_set_headers_visible (handle, (tv.HeaderStyle != ColumnHeaderStyle.None));
					Internal.GTK.Methods.GtkTreeView.gtk_tree_view_set_headers_clickable (handle, (tv.HeaderStyle == ColumnHeaderStyle.Clickable));
					break;
				}
			case ImplementedAsType.IconView: {
					handle = Internal.GTK.Methods.GtkIconView.gtk_icon_view_new ();
					break;
				}
			}

			UpdateTreeModel (handle);

			IntPtr hHAdjustment = Internal.GTK.Methods.GtkAdjustment.gtk_adjustment_new(0, 0, 100, 1, 10, 10);
			IntPtr hVAdjustment = Internal.GTK.Methods.GtkAdjustment.gtk_adjustment_new(0, 0, 100, 1, 10, 10);

			IntPtr hScrolledWindow = Internal.GTK.Methods.GtkScrolledWindow.gtk_scrolled_window_new(hHAdjustment, hVAdjustment);
			Internal.GTK.Methods.GtkContainer.gtk_container_add(hScrolledWindow, handle);

			// connect the signals
			switch (ImplementedAs(tv))
			{
				case ImplementedAsType.TreeView:
				{
					Internal.GObject.Methods.g_signal_connect(handle, "row_activated", (Internal.GTK.Delegates.GtkTreeViewRowActivatedFunc)gc_row_activated);
					Internal.GObject.Methods.g_signal_connect(handle, "cursor_changed", (Internal.GTK.Delegates.GtkTreeViewFunc)gc_cursor_or_selection_changed);
					break;
				}
				case ImplementedAsType.IconView:
				{
					Internal.GObject.Methods.g_signal_connect(handle, "item_activated", (Internal.GTK.Delegates.GtkTreeViewRowActivatedFunc)gc_row_activated);
					Internal.GObject.Methods.g_signal_connect(handle, "selection_changed", (Internal.GTK.Delegates.GtkTreeViewFunc)gc_cursor_or_selection_changed);
					break;
				}
			}
			RegisterListViewHandle(tv, handle);
			
			SetSelectionModeInternal(handle, tv, tv.SelectionMode);
			
			GTKNativeControl native = new GTKNativeControl(hScrolledWindow,
			new KeyValuePair<string, IntPtr>[]
			{
				new KeyValuePair<string, IntPtr>("TreeView", handle),
				new KeyValuePair<string, IntPtr>("ScrolledWindow", hScrolledWindow)
			});
			return native;
		}

		private static void SelectedRows_ItemRequested(object sender, TreeModelRowItemRequestedEventArgs e)
		{
			TreeModelRow.TreeModelSelectedRowCollection coll = (sender as TreeModelRow.TreeModelSelectedRowCollection);
			ControlImplementation impl = coll.Parent.ControlImplementation;
			GTKEngine engine = (impl.Engine as GTKEngine);

			if (coll.Parent != null)
			{
				IntPtr hTreeView = GetHandleForControl(coll.Parent);
				IntPtr hTreeModel = IntPtr.Zero;
				IntPtr hListRows = IntPtr.Zero;
				int count = 0;
				if (coll.Parent.Mode == ListViewMode.Detail)
				{
					hTreeModel = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_model(hTreeView);
					
					IntPtr hTreeSelection = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_selection(hTreeView);
					count = Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_count_selected_rows(hTreeSelection);
					hListRows = Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_get_selected_rows(hTreeSelection, ref hTreeModel);
				}
				else if (coll.Parent.Mode == ListViewMode.LargeIcon)
				{
					hTreeModel = Internal.GTK.Methods.GtkIconView.gtk_icon_view_get_model(hTreeView);
					hListRows = Internal.GTK.Methods.GtkIconView.gtk_icon_view_get_selected_items(hTreeView);
					
					if (hListRows != IntPtr.Zero)
						count = (int)Internal.GLib.Methods.g_list_length(hListRows);
				}

				if (hTreeModel == IntPtr.Zero)
				{
					Console.Error.WriteLine("uwt: gtk: tree model is null");
					return;
				}

				
				if (e.Index == -1 && e.Count == 1 && e.Item != null)
				{
					// we are adding a new row to the selected collection
					Internal.GTK.Structures.GtkTreeIter iter = engine.GetGtkTreeIterForTreeModelRow(e.Item);
					switch (ImplementedAs(coll.Parent))
					{
						case ImplementedAsType.TreeView:
						{
							IntPtr hTreeSelection = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_selection(hTreeView);
							Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_select_iter(hTreeSelection, ref iter);
							break;
						}
						case ImplementedAsType.IconView:
						{
							break;
						}
					}
					return;
				}
				else if (hListRows != IntPtr.Zero)
				{
					e.Count = count;
					if (count > 0 && e.Index > -1)
					{
						IntPtr hTreePath = Internal.GLib.Methods.g_list_nth_data(hListRows, (uint)e.Index);
						Internal.GTK.Structures.GtkTreeIter iter = new Internal.GTK.Structures.GtkTreeIter();
						bool ret = Internal.GTK.Methods.GtkTreeModel.gtk_tree_model_get_iter(hTreeModel, ref iter, hTreePath);
						if (ret)
						{
							TreeModelRow row = engine.GetTreeModelRowForGtkTreeIter(iter);
							e.Item = row;
						}
					}
					else if (count > 0 && e.Index == -1 && e.Item != null)
					{
						// we are checking if selection contains a row
						bool found = false;
						for (int i = 0; i < count; i++)
						{
							IntPtr hTreePath = Internal.GLib.Methods.g_list_nth_data(hListRows, (uint)i);
							Internal.GTK.Structures.GtkTreeIter iter = new Internal.GTK.Structures.GtkTreeIter();
							bool ret = Internal.GTK.Methods.GtkTreeModel.gtk_tree_model_get_iter(hTreeModel, ref iter, hTreePath);
							if (ret)
							{
								TreeModelRow row = engine.GetTreeModelRowForGtkTreeIter(iter);
								if (row == e.Item)
								{
									found = true;
									break;
								}
							}
						}
						if (!found) e.Item = null;
					}
					else
					{
						e.Item = null;
					}
				}
			}
		}
		private static void SelectedRows_Cleared(object sender, EventArgs e)
		{
			TreeModelRow.TreeModelSelectedRowCollection coll = (sender as TreeModelRow.TreeModelSelectedRowCollection);
			if (coll.Parent != null)
			{
				IntPtr hTreeView = GetHandleForControl(coll.Parent);
				if (coll.Parent.Mode == ListViewMode.Detail)
				{
					IntPtr hTreeSelection = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_selection(hTreeView);
					Internal.GTK.Methods.GtkTreeSelection.gtk_tree_selection_unselect_all(hTreeSelection);
				}
				else if (coll.Parent.Mode == ListViewMode.LargeIcon)
				{
				}
			}
		}

		// TODO: figure out why _ControlsByHandle is ONLY SOMETIMES
		//       (for certain instances) but CONSISTENTLY (for the
		//       same instance, multiple times), null...
		// also FIXME: why the hell are we storing all this separately anyway? we have Engine.RegisterControlHandle for a reason...
		private static Dictionary<IntPtr, ListViewControl> _ControlsByHandle = new Dictionary<IntPtr, ListViewControl>();
		private static Dictionary<ListViewControl, IntPtr> _HandlesByControl = new Dictionary<ListViewControl, IntPtr>();
		private static void RegisterListViewHandle(ListViewControl lv, IntPtr handle)
		{
			_ControlsByHandle[handle] = lv;
			_HandlesByControl[lv] = handle;
		}
		private static IntPtr GetHandleForControl(ListViewControl lv)
		{
			if (_HandlesByControl.ContainsKey(lv))
				return _HandlesByControl[lv];
			return IntPtr.Zero;
		}
		private static ListViewControl GetControlByHandle(IntPtr handle)
		{
			if (_ControlsByHandle.ContainsKey(handle))
				return _ControlsByHandle[handle];
			return null;
		}

		private List<TreeModelRow> _oldSelection = null;

		private static void gc_cursor_or_selection_changed(IntPtr handle)
		{
			ListViewControl ctl = GetControlByHandle(handle);
			if (ctl != null)
			{
				// TODO: figure out how we can fake an OnSelectionChanging event, save the previous selection, and then restore the previous selection
				//       if user cancels OnSelectionChanging event
				ListViewImplementation impl = (ctl.ControlImplementation as ListViewImplementation);
				bool changed = false;
				if (impl != null)
				{
					if (impl._oldSelection != null)
					{
						for (int i = 0; i < ctl.SelectedRows.Count; i++)
						{
							if (!impl._oldSelection.Contains(ctl.SelectedRows[i]))
							{
								changed = true;
								break;
							}
						}
					}
					else
					{
						changed = true;
					}
				}

				if (changed)
				{
					ListViewSelectionChangingEventArgs e = new ListViewSelectionChangingEventArgs(impl._oldSelection?.ToArray(), new List<TreeModelRow>(ctl.SelectedRows).ToArray());
					ctl.OnSelectionChanging(e);
					if (e.Cancel)
					{
						// restore the selection
						ctl.SelectedRows.Clear();
						if (impl._oldSelection != null)
						{
							for (int i = 0; i < impl._oldSelection.Count; i++)
							{
								ctl.SelectedRows.Add(impl._oldSelection[i]);
							}
						}
						return;
					}
				}

				impl._oldSelection = new List<TreeModelRow>(ctl.SelectedRows);
				ctl.OnSelectionChanged(EventArgs.Empty);
			}
		}
		
		private static void gc_row_activated(IntPtr handle, IntPtr /*GtkTreePath*/ path, IntPtr /*GtkTreeViewColumn*/ column)
		{
			ListViewControl lv = (GetControlByHandle(handle) as ListViewControl);
			if (lv == null) return;

			IntPtr hModel = (lv.ControlImplementation.Engine.GetHandleForTreeModel(lv.Model) as GTKNativeTreeModel).Handle;

			Internal.GTK.Structures.GtkTreeIter iter = new Internal.GTK.Structures.GtkTreeIter();

			Internal.GTK.Methods.GtkTreeModel.gtk_tree_model_get_iter(hModel, ref iter, path);
			TreeModelRow row = (lv.ControlImplementation.Engine as GTKEngine).GetTreeModelRowForGtkTreeIter(iter);

			lv.OnRowActivated(new ListViewRowActivatedEventArgs(row));
		}

		protected override void OnKeyDown (KeyEventArgs e)
		{
			base.OnKeyDown (e);

			ListViewControl lv = Control as ListViewControl;
			if (lv == null) return;

			if (lv.SelectedRows.Count != 1) return;

			if (e.Key == KeyboardKey.ArrowRight) {
				lv.SelectedRows [0].Expanded = true;
				e.Cancel = true;
			}
			else if (e.Key == KeyboardKey.ArrowLeft) {
				if (!lv.SelectedRows [0].Expanded) {
					// we're already closed, so move selection up to our parent
					TreeModelRow rowCurrent = lv.SelectedRows [0];
					if (rowCurrent.ParentRow != null) {
						lv.SelectedRows.Clear ();
						lv.SelectedRows.Add (rowCurrent.ParentRow);
					}
				} else {
					lv.SelectedRows [0].Expanded = false;
				}
				e.Cancel = true;
			}
		}

		private TreeModel TreeModelFromHandle(IntPtr handle)
		{
			return Engine.TreeModelFromHandle(new GTKNativeTreeModel(handle));
		}
		private IntPtr GetHandleForTreeModel(TreeModel tm)
		{
			GTKNativeTreeModel ntm = (Engine.GetHandleForTreeModel(tm) as GTKNativeTreeModel);
			if (ntm == null) return IntPtr.Zero;

			return ntm.Handle;
		}
		
		/// <summary>
		/// Registers the control with the given handle as a drag source. Overridden for GtkTreeView (which in UWT is always a child of a GtkScrolledWindow).
		/// </summary>
		/// <remarks>
		/// We need to override this to handle GTK tree view DnD. Not mentioned in the docs at all. Took about a half hour to figure out... Also to properly identify
		/// control handle since the GtkTreeView in UWT is always a child of a GtkScrolledWindow.
		/// 
		/// Even still, we need to figure out how to actually make use of the dragged row. How to pass data? Also why does it only work with the primary mouse button...
		/// </remarks>
		internal override void RegisterDragSourceGTK(IntPtr hScrolledWindow, Internal.GDK.Constants.GdkModifierType modifiers, Internal.GTK.Structures.GtkTargetEntry[] targets, Internal.GDK.Constants.GdkDragAction actions)
		{
			IntPtr hTreeView = GetHTreeView(hScrolledWindow);
			ListViewControl tv = (GetControlByHandle(hTreeView) as ListViewControl);
			switch (ImplementedAs(tv))
			{
				case ImplementedAsType.IconView:
				{
					Internal.GTK.Methods.GtkIconView.gtk_icon_view_enable_model_drag_source(hTreeView, modifiers, targets, targets.Length, actions);
					break;
				}
				case ImplementedAsType.TreeView:
				{
					Internal.GTK.Methods.GtkTreeView.gtk_tree_view_enable_model_drag_source(hTreeView, modifiers, targets, targets.Length, actions);
					break;
				}
			}
		}

		public void UpdateTreeModel(NativeControl handle, TreeModelChangedEventArgs e)
		{
			IntPtr hTreeView = (handle as GTKNativeControl).GetNamedHandle("TreeView");
			if (hTreeView == IntPtr.Zero) return;

			ListViewControl ctl = (_ControlsByHandle[hTreeView] as ListViewControl);
			if (ctl == null) {
				Console.Error.WriteLine("UpdateTreeModel: _ControlsByHandle[" + hTreeView.ToString() + "] is null");
				return;
			}
			
			
			IntPtr hTreeModel = IntPtr.Zero;
			if (ctl.Mode == ListViewMode.Detail)
			{
				hTreeModel = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_model(hTreeView);
			}
			else if (ctl.Mode == ListViewMode.LargeIcon)
			{
				hTreeModel = Internal.GTK.Methods.GtkIconView.gtk_icon_view_get_model(hTreeView);
			}
			TreeModel tm = TreeModelFromHandle(hTreeModel);
			(((UIApplication)Application.Instance).Engine as GTKEngine).UpdateTreeModel(tm, e);
		}

		private IntPtr GetHTreeView(IntPtr hScrolledWindow)
		{
			IntPtr hList = Internal.GTK.Methods.GtkContainer.gtk_container_get_children(hScrolledWindow);
			IntPtr hTreeView = Internal.GLib.Methods.g_list_nth_data(hList, 0);
			return hTreeView;
		}


		public bool IsRowExpanded(TreeModelRow row)
		{
			ListViewControl lv = Control as ListViewControl;
			if (lv == null)
				return false;

			IntPtr hTreeView = GetHandleForControl (lv);
			IntPtr hTreeModel = GetHandleForTreeModel (lv.Model);

			Internal.GTK.Structures.GtkTreeIter hIterRow = (Engine as GTKEngine).GetGtkTreeIterForTreeModelRow(row);
			IntPtr hRowPath = Internal.GTK.Methods.GtkTreeModel.gtk_tree_model_get_path (hTreeModel, ref hIterRow);

			bool value = Internal.GTK.Methods.GtkTreeView.gtk_tree_view_row_expanded (hTreeView, hRowPath);
			return value;
		}
		public void SetRowExpanded(TreeModelRow row, bool expanded)
		{
			ListViewControl lv = Control as ListViewControl;
			if (lv == null)
				return;

			IntPtr hTreeView = GetHandleForControl (lv);
			IntPtr hTreeModel = GetHandleForTreeModel (lv.Model);

			Internal.GTK.Structures.GtkTreeIter hIterRow = (Engine as GTKEngine).GetGtkTreeIterForTreeModelRow(row);
			IntPtr hRowPath = Internal.GTK.Methods.GtkTreeModel.gtk_tree_model_get_path (hTreeModel, ref hIterRow);

			if (expanded) {
				Internal.GTK.Methods.GtkTreeView.gtk_tree_view_expand_row (hTreeView, hRowPath, false);
			} else {
				Internal.GTK.Methods.GtkTreeView.gtk_tree_view_collapse_row (hTreeView, hRowPath);
			}
		}

		protected override void OnCreated (EventArgs e)
		{
			base.OnCreated (e);

			if (Internal.GTK.Methods.Gtk.LIBRARY_FILENAME == Internal.GTK.Methods.Gtk.LIBRARY_FILENAME_V2)
			{
			}
			else
			{
				IntPtr hCtrl = (Handle as GTKNativeControl).GetNamedHandle("TreeView");
				IntPtr hStyleContext = Internal.GTK.Methods.GtkWidget.gtk_widget_get_style_context (hCtrl);
				foreach (ControlStyleClass cls in Control.Style.Classes) {
					Internal.GTK.Methods.GtkStyleContext.gtk_style_context_add_class (hStyleContext, cls.Value);
				}
			}
		}

		public bool GetSingleClickActivation()
		{
			switch (ImplementedAs(Control as ListViewControl))
			{
				case ImplementedAsType.IconView:
				{
					break;
				}
				case ImplementedAsType.TreeView:
				{
					return Internal.GTK.Methods.GtkTreeView.gtk_tree_view_get_activate_on_single_click((Handle as GTKNativeControl).Handle);
				}
			}
			throw new NotImplementedException();
		}
		public void SetSingleClickActivation(bool value)
		{
			switch (ImplementedAs(Control as ListViewControl))
			{
				case ImplementedAsType.IconView:
				{
					break;
				}
				case ImplementedAsType.TreeView:
				{
					Internal.GTK.Methods.GtkTreeView.gtk_tree_view_set_activate_on_single_click((Handle as GTKNativeControl).GetNamedHandle("TreeView"), value);
					break;
				}
			}
		}
	}
}
