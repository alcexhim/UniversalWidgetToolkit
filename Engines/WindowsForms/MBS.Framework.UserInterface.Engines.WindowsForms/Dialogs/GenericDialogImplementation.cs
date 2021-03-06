//
//  GenericDialogImplementation.cs
//
//  Author:
//       Mike Becker <alcexhim@gmail.com>
//
//  Copyright (c) 2019 Mike Becker
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MBS.Framework.Drawing;
using MBS.Framework.UserInterface.Controls;

namespace MBS.Framework.UserInterface.Engines.WindowsForms.Dialogs
{
	[ControlImplementation(typeof(CustomDialog))]
	public class GenericDialogImplementation : WindowsFormsDialogImplementation
	{
		public GenericDialogImplementation(Engine engine, Control control) : base(engine, control)
		{
		}

		protected override void DestroyInternal()
		{
			if ((Handle as WindowsFormsNativeDialog).Form != null)
			{
				(Handle as WindowsFormsNativeDialog).Form.Close();
			}
		}

		protected override bool AcceptInternal()
		{
			return true;
		}

		private class __wmG : System.Windows.Forms.CommonDialog
		{
			private Dialog _dialog = null;
			private System.Windows.Forms.Form f;
			public __wmG(Dialog dialog, System.Windows.Forms.Form f)
			{
				_dialog = dialog;
				this.f = f;
			}
			public override void Reset()
			{
			}

			protected override bool RunDialog(IntPtr hwndOwner)
			{
				System.Windows.Forms.DialogResult result = f.ShowDialog();
				f.DialogResult = result;
				if (result == System.Windows.Forms.DialogResult.Cancel)
					return false;
				return true;
			}
		}

		private class _FLPanel : System.Windows.Forms.FlowLayoutPanel
		{
			public _FLPanel()
			{
				base.DoubleBuffered = true;
			}
		}

		protected override WindowsFormsNativeDialog CreateDialogInternal(Dialog dialog, List<Button> buttons)
		{
			NativeControl hContainer = (new Controls.ContainerImplementation(Engine, dialog)).CreateControl(dialog);

			System.Windows.Forms.Control ctl = (hContainer as WindowsFormsNativeControl).Handle;
			System.Windows.Forms.Form f = new System.Windows.Forms.Form();
			f.Text = dialog.Text;

			if (dialog.Decorated)
			{
				if (dialog.Resizable)
				{
					f.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
				}
				else
				{
					f.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
				}
			}
			else
			{
				f.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			}

			f.FormClosing += F_FormClosing;
			(hContainer as WindowsFormsNativeControl).SetNamedHandle("dialog", f);

			f.BackColor = Theming.Theme.CurrentTheme.ColorTable.DialogBackground;

			ctl.Dock = System.Windows.Forms.DockStyle.Fill;
			f.Controls.Add(ctl);

			_FLPanel pnlButtons = new _FLPanel();
			pnlButtons.AutoSize = true;
			pnlButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			pnlButtons.Padding = new System.Windows.Forms.Padding(6, 12, 6, 12);
			pnlButtons.Paint += PnlButtons_Paint;
			pnlButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			for (int i = dialog.Buttons.Count - 1; i >= 0; i--)
			{
				if (!dialog.Buttons[i].IsCreated)
					Engine.CreateControl(dialog.Buttons[i]);

				System.Windows.Forms.Button btn = ((Engine.GetHandleForControl(dialog.Buttons[i]) as WindowsFormsNativeControl).Handle as System.Windows.Forms.Button);
				pnlButtons.Controls.Add(btn);

				if (IsSuggestedResponse(dialog.Buttons[i].ResponseValue) || dialog.DefaultButton == dialog.Buttons[i])
				{
					f.AcceptButton = btn;
				}
				if (dialog.Buttons[i].ResponseValue == (int)DialogResult.Cancel)
				{
					f.CancelButton = btn;
				}
			}
			pnlButtons.BackColor = System.Drawing.SystemColors.Control;
			pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
			pnlButtons.Visible = (dialog.Buttons.Count > 0);
			f.Controls.Add(pnlButtons);

			f.Font = System.Drawing.SystemFonts.MenuFont;

			f.MinimumSize = WindowsFormsEngine.Dimension2DToSystemDrawingSize(dialog.MinimumSize);
			f.MaximumSize = WindowsFormsEngine.Dimension2DToSystemDrawingSize(dialog.MaximumSize);
			f.Size = WindowsFormsEngine.Dimension2DToSystemDrawingSize(dialog.Size);
			if (dialog.Size != Dimension2D.Empty)
				f.AutoSize = true;

			WindowsFormsNativeDialog nc = new WindowsFormsNativeDialog(new __wmG(dialog, f), f);
			Engine.RegisterControlHandle(dialog, nc);
			return nc;
		}

		private bool IsSuggestedResponse(int responseValue)
		{
			return (
				(responseValue == (int)DialogResult.OK) ||
				(responseValue == (int)DialogResult.Yes) ||
				(responseValue == (int)DialogResult.Retry)
			);
		}

		void PnlButtons_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			MBS.Framework.UserInterface.Engines.WindowsForms.Theming.Theme.CurrentTheme.DrawContentAreaBackground(e.Graphics, ((System.Windows.Forms.Control)sender).ClientRectangle);
		}


		void F_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
		{
			(sender as System.Windows.Forms.Form).DialogResult = WindowsFormsEngine.DialogResultToSWFDialogResult((this.Control as Dialog).DialogResult);
		}

	}
}
