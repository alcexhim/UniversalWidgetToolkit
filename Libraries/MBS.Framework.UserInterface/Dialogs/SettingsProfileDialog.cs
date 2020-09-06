﻿//
//  SettingsProfileDialog.cs
//
//  Author:
//       Michael Becker <alcexhim@gmail.com>
//
//  Copyright (c) 2020 Mike Becker's Software
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using MBS.Framework.UserInterface.Controls;
using MBS.Framework.UserInterface.Controls.ListView;

namespace MBS.Framework.UserInterface.Dialogs
{
	[ContainerLayout("~/Dialogs/SettingsProfileDialog.glade")]
	public class SettingsProfileDialog : CustomDialog
	{
		private Button cmdOK;
		private ListViewControl tvProfiles;
		private Toolbar tbProfiles;

		protected internal override void OnCreated(EventArgs e)
		{
			base.OnCreated(e);

			DefaultButton = cmdOK;

			for (int i = 0;  i < Application.SettingsProfiles.Count; i++)
			{
				SettingsProfile profile = Application.SettingsProfiles[i];
				TreeModelRow row = new TreeModelRow(new TreeModelRowColumn[]
				{
					new TreeModelRowColumn(tvProfiles.Model.Columns[0], profile.Title)
				});
				row.SetExtraData<SettingsProfile>("profile", profile);
				tvProfiles.Model.Rows.Add(row);
			}
		}

		public SettingsProfile SelectedProfile { get; set; } = null;

		[EventHandler(nameof(cmdOK), "Click")]
		private void cmdOK_Click(object sender, EventArgs e)
		{
			if (tvProfiles.SelectedRows.Count == 0)
			{
				MessageDialog.ShowDialog("Please select a profile before continuing.", "Error", MessageDialogButtons.OK, MessageDialogIcon.Error);
				DialogResult = DialogResult.None;
				return;
			}

			SelectedProfile = tvProfiles.SelectedRows[0].GetExtraData<SettingsProfile>("profile");
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
