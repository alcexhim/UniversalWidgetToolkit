//
//  IntPtrEqualityComparer.cs
//
//  Author:
//       This file is part of Gtk#
//
//  Copyright (c) 2019
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

namespace MBS.Framework.UserInterface.Engines.WindowsForms.Internal
{
	internal class IntPtrEqualityComparer : IEqualityComparer<IntPtr>
	{
		public static readonly IEqualityComparer<IntPtr> Instance = new IntPtrEqualityComparer();

		public bool Equals(IntPtr x, IntPtr y)
		{
			return x == y;
		}

		public int GetHashCode(IntPtr obj)
		{
			return obj.GetHashCode();
		}
	}
}
