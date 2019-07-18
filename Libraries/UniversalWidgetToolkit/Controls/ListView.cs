using System;

namespace UniversalWidgetToolkit.Controls
{
	namespace Native
	{
		public interface IListViewNativeImplementation
		{
			void UpdateTreeModel(NativeControl handle, TreeModelChangedEventArgs e);
		}
	}

	public delegate void ListViewRowActivatedEventHandler(object sender, ListViewRowActivatedEventArgs e);
	public class ListViewRowActivatedEventArgs : EventArgs
	{
		/// <summary>
		/// The row that was activated.
		/// </summary>
		/// <value>The row that was activated.</value>
		public TreeModelRow Row { get; private set; } = null;

		public ListViewRowActivatedEventArgs(TreeModelRow row)
		{
			Row = row;
		}
	}

	public abstract class ListViewColumn
	{
		public class ListViewColumnCollection
			: System.Collections.ObjectModel.Collection<ListViewColumn>
		{

		}

		private TreeModelColumn mvarColumn = null;
		public TreeModelColumn Column { get { return mvarColumn; } set { mvarColumn = value; } }

		private string mvarTitle = String.Empty;
		public string Title { get { return mvarTitle; } set { mvarTitle = value; } }

		public ListViewColumn(TreeModelColumn column, string title = "")
		{
			mvarColumn = column;
			mvarTitle = title;
		}
	}
	public class ListViewColumnText
		: ListViewColumn
	{
		public ListViewColumnText(TreeModelColumn column, string title = "") : base(column, title)
		{
		}
	}
	public class ListView : SystemControl
	{
		public ListView()
		{
			this.SelectedRows = new TreeModelRow.TreeModelSelectedRowCollection(this);
		}

		private DefaultTreeModel mvarModel = null;
		public DefaultTreeModel Model { get { return mvarModel; } set { mvarModel = value; mvarModel.TreeModelChanged += MvarModel_TreeModelChanged; } }

		public event TreeModelChangedEventHandler TreeModelChanged;
		public void OnTreeModelChanged(object sender, TreeModelChangedEventArgs e)
		{
			TreeModelChanged?.Invoke(sender, e);
		}
		public event ListViewRowActivatedEventHandler RowActivated;
		public virtual void OnRowActivated(ListViewRowActivatedEventArgs e)
		{
			RowActivated?.Invoke(this, e);
		}

		public event EventHandler SelectionChanged;
		public virtual void OnSelectionChanged(EventArgs e)
		{
			SelectionChanged?.Invoke(this, e);
		}

		private void MvarModel_TreeModelChanged(object sender, TreeModelChangedEventArgs e)
		{
			OnTreeModelChanged(sender, e);

			switch (e.Action)
			{
				case TreeModelChangedAction.Add:
				{
					foreach (TreeModelRow row in e.Rows)
					{
						row.ParentControl = this;
					}
					break;
				}
			}

			(NativeImplementation as Native.IListViewNativeImplementation)?.UpdateTreeModel(NativeImplementation.Handle, e);
		}

		private ListViewColumn.ListViewColumnCollection mvarColumns = new ListViewColumn.ListViewColumnCollection();
		public ListViewColumn.ListViewColumnCollection Columns { get { return mvarColumns; } }

		public ColumnHeaderStyle HeaderStyle { get; set; } = ColumnHeaderStyle.Clickable;
		public TreeModelRow.TreeModelSelectedRowCollection SelectedRows { get; private set; } = null;

		public ListViewMode Mode { get; set; } = ListViewMode.Detail;

		/// <summary>
		/// Selects the specified <see cref="TreeModelRow"/>.
		/// </summary>
		/// <param name="row">Tree model row.</param>
		public void Select(TreeModelRow row)
		{
			SelectedRows.Add(row);
		}
	}
}
