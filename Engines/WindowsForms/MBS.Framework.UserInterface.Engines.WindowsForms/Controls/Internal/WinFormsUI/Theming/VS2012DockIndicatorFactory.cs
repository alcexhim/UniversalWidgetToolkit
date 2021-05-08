using WeifenLuo.WinFormsUI.Docking;

namespace WeifenLuo.WinFormsUI.Theming
{
	internal class VS2012DockIndicatorFactory : DockPanelExtender.IDockIndicatorFactory
	{
		public DockPanel.DockDragHandler.DockIndicator CreateDockIndicator(DockPanel.DockDragHandler dockDragHandler)
		{
			return new DockPanel.DockDragHandler.DockIndicator(dockDragHandler) { Opacity = 0.7 };
		}
	}
}
