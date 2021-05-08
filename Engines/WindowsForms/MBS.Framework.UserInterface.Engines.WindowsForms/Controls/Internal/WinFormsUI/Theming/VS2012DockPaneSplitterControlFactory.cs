using WeifenLuo.WinFormsUI.Docking;

namespace WeifenLuo.WinFormsUI.Theming
{
	internal class VS2012DockPaneSplitterControlFactory : DockPanelExtender.IDockPaneSplitterControlFactory
	{
		public DockPane.SplitterControlBase CreateSplitterControl(DockPane pane)
		{
			return new VS2012SplitterControl(pane);
		}
	}
}
