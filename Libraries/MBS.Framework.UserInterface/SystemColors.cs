//
//  SystemColors.cs
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
using MBS.Framework.Drawing;

namespace MBS.Framework.UserInterface
{
	public sealed class SystemColors
	{
		public static Color HighlightBackground { get { return ((UIApplication)Application.Instance).Engine.GetSystemColor(SystemColor.HighlightBackground); } }
		public static Color HighlightForeground { get { return ((UIApplication)Application.Instance).Engine.GetSystemColor(SystemColor.HighlightForeground); } }
		public static Color WindowBackground { get { return ((UIApplication)Application.Instance).Engine.GetSystemColor(SystemColor.WindowBackground); } }
		public static Color WindowForeground { get { return ((UIApplication)Application.Instance).Engine.GetSystemColor(SystemColor.WindowForeground); } }
	}
}
