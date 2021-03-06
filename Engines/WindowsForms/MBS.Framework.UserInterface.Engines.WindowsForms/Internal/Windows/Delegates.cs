//
//  Delegates.cs
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
using System.Runtime.InteropServices;

namespace MBS.Framework.UserInterface.Engines.WindowsForms.Internal.Windows
{
	internal static class Delegates
	{
		public delegate bool EnumWindowsProc([In] IntPtr hWnd, [In] IntPtr lParam);

		// [UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int /*HRESULT*/ PFTASKDIALOGCALLBACK(IntPtr /*HWND*/ hwnd, uint msg, UIntPtr /*WPARAM*/ wParam, IntPtr /*LPARAM*/ lParam, IntPtr /*LONG_PTR*/ lpRefData);
	}
}
