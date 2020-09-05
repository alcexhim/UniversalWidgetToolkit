﻿//
//  Option.cs
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

namespace MBS.Framework.UserInterface
{
	public class RangeSetting : Setting
	{
		public double MinimumValue { get; set; } = 0.0;
		public double MaximumValue { get; set; } = 0.0;

		public RangeSetting(string name, string title, double defaultValue = 0.0, double minimumValue = 0.0, double maximumValue = 0.0) : base(name, title, defaultValue)
		{
			MinimumValue = minimumValue;
			MaximumValue = maximumValue;
		}
	}
	public class GroupSetting : Setting
	{
		public Setting.SettingCollection Options { get; } = new Setting.SettingCollection();

		public GroupSetting(string name, string title, Setting[] options = null) : base(name, title)
		{
			if (options != null)
			{
				foreach (Setting option in options)
				{
					Options.Add(option);
				}
			}
		}
	}
	public class BooleanSetting : Setting
	{
		public BooleanSetting(string name, string title, bool defaultValue = false) : base(name, title, defaultValue)
		{
		}

		public override void SetValue(object value, Guid? scopeId = null)
		{
			bool val = false;
			if (value != null)
			{
				val = (value.ToString().ToLower().Equals("true"));
			}
			base.SetValue(val);
		}
	}
	public class ChoiceSetting : Setting
	{
		public ChoiceSetting(string name, string title, ChoiceSettingValue defaultValue = null, ChoiceSettingValue[] values = null) : base(name, title, null)
		{
			if (values == null)
			{
				values = new ChoiceSettingValue[0];
			}
			if (defaultValue != null)
			{
				if (defaultValue.Value != null)
				{
					base.DefaultValue = defaultValue.Value.ToString();
				}
				else
				{
					base.DefaultValue = null;
				}
			}

			foreach (ChoiceSettingValue value in values)
			{
				ValidValues.Add(value);
			}
		}

		public class ChoiceSettingValue
		{
			public class ChoiceSettingValueCollection
				: System.Collections.ObjectModel.Collection<ChoiceSettingValue>
			{
			}

			public string Name { get; set; } = String.Empty;
			public string Title { get; set; } = String.Empty;
			public object Value { get; set; } = null;

			public ChoiceSettingValue(string name, string title, object value)
			{
				Name = name;
				Title = title;
				Value = value;
			}
		}

		public ChoiceSettingValue.ChoiceSettingValueCollection ValidValues { get; } = new ChoiceSettingValue.ChoiceSettingValueCollection();
		public ChoiceSettingValue SelectedValue { get; set; } = null;

		public bool RequireSelectionFromList { get; set; } = true;
	}
	public class TextSetting : Setting
	{
		public TextSetting(string name, string title, string defaultValue = "") : base(name, title, defaultValue)
		{
		}
	}
	public class CollectionSetting : Setting
	{
		public Setting.SettingCollection Settings { get; } = new Setting.SettingCollection();
		public SettingsGroup.SettingsGroupCollection Items { get; } = new SettingsGroup.SettingsGroupCollection();

		public CollectionSetting(string name, string title, SettingsGroup group) : base(name, title, null)
		{
			for (int i = 0; i < group.Settings.Count; i++)
			{
				Settings.Add(group.Settings[i]);
			}
		}
	}
	public abstract class Setting
	{
		public Setting(string name, string title, object defaultValue = null)
		{
			Name = name;
			Title = title;
			DefaultValue = defaultValue;
			mvarValue = defaultValue;
		}

		public string Name { get; set; } = String.Empty;
		public string Title { get; set; } = String.Empty;
		public string Description { get; set; } = String.Empty;

		public class SettingCollection
			: System.Collections.ObjectModel.Collection<Setting>
		{

			public Setting this[string name]
			{
				get
				{
					foreach (Setting item in this)
					{
						if (item.Title.Replace("_", String.Empty).Replace(' ', '_').Equals(name))
						{
							return item;
						}
					}
					return null;
				}
			}
		}

		protected Setting()
		{
		}

		public object DefaultValue { get; set; } = null;
		public SettingsValue.SettingsValueCollection ScopedValues { get; } = new SettingsValue.SettingsValueCollection();

		private object mvarValue = null;

		public virtual object GetValue(Guid? scopeId = null)
		{
			return mvarValue;
		}
		public virtual void SetValue(object value, Guid? scopeId = null)
		{
			if (scopeId != null)
			{
				if (ScopedValues.Contains(scopeId.Value))
				{
					ScopedValues[scopeId.Value].Value = value;
				}
				else
				{
					ScopedValues.Add(scopeId.Value, value);
				}
			}
			else
			{
				mvarValue = value;
			}
		}
		public T GetValue<T>(T defaultValue = default(T), Guid? scopeId = null)
		{
			try
			{
				return (T)GetValue(scopeId);
			}
			catch
			{
				return defaultValue;
			}
		}
		public void SetValue<T>(T value)
		{
			mvarValue = value;
		}
	}
}

