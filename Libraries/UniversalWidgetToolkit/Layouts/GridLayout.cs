using System;

namespace UniversalWidgetToolkit.Layouts
{
	public class GridLayout : Layout
	{
		public class Constraints : UniversalWidgetToolkit.Constraints
		{
			private int mvarRow = 0;
			public int Row { get { return mvarRow; } set { mvarRow = value; } }

			private int mvarColumn = 0;
			public int Column { get { return mvarColumn; } set { mvarColumn = value; } }

			private int mvarRowSpan = 0;
			public int RowSpan { get { return mvarRowSpan; } set { mvarRowSpan = value; } }

			private int mvarColumnSpan = 0;
			public int ColumnSpan { get { return mvarColumnSpan; } set { mvarColumnSpan = value; } }

			public Constraints(int row, int column, int rowSpan = 1, int columnSpan = 1)
			{
				mvarRow = row;
				mvarColumn = column;
				mvarRowSpan = rowSpan;
				mvarColumnSpan = columnSpan;
			}
		}

		private uint mvarRowSpacing = 6;
		public uint RowSpacing { get { return mvarRowSpacing; } set { mvarRowSpacing = value; } }

		private uint mvarColumnSpacing = 6;
		public uint ColumnSpacing { get { return mvarColumnSpacing; } set { mvarColumnSpacing = value; } }

		protected override UniversalWidgetToolkit.Drawing.Rectangle GetControlBoundsInternal (Control ctl)
		{
			throw new NotImplementedException ();
		}
		protected override void ResetControlBoundsInternal (Control ctl)
		{
			throw new NotImplementedException ();
		}
	}
}

