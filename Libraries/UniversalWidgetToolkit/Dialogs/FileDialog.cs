using System;

namespace UniversalWidgetToolkit
{
	public enum FileDialogMode
	{
		Open,
		Save,
		SelectFolder,
		CreateFolder
	}
	public class FileDialog : CommonDialog
	{
		private FileDialogMode mvarMode = FileDialogMode.Open;
		public FileDialogMode Mode { get { return mvarMode; } set { mvarMode = value; } }

		private bool mvarMultiSelect = false;
		public bool MultiSelect { get { return mvarMultiSelect; } set { mvarMultiSelect = value; } }

		private System.Collections.Specialized.StringCollection mvarSelectedFileNames = new System.Collections.Specialized.StringCollection ();
		public System.Collections.Specialized.StringCollection SelectedFileNames { get { return mvarSelectedFileNames; } }

	}
}

