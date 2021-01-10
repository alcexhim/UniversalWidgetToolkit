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
using System.Collections.Generic;
using MBS.Framework.UserInterface.Dialogs;

namespace MBS.Framework.UserInterface
{
	public class CommandSetting : Setting
	{
		public string CommandID { get; set; } = null;
		public ButtonStylePresets StylePreset { get; set; } = ButtonStylePresets.None;

		public CommandSetting(string name, string title, string commandID) : base(name, title)
		{
			CommandID = commandID;
		}
	}
	public class RangeSetting : Setting
	{
		public decimal? MinimumValue { get; set; } = null;
		public decimal? MaximumValue { get; set; } = null;

		public RangeSetting(string name, string title, decimal defaultValue = 0.0M, decimal? minimumValue = null, decimal? maximumValue = null) : base(name, title, defaultValue)
		{
			MinimumValue = minimumValue;
			MaximumValue = maximumValue;
		}
	}
	public class GroupSetting : Setting
	{
		public Setting.SettingCollection Options { get; } = new Setting.SettingCollection();
		public Setting.SettingCollection HeaderSettings { get; } = new Setting.SettingCollection();

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
	public class FileSetting : TextSetting
	{
		public bool RequireExistingFile { get; set; } = true;
		public FileDialogMode Mode { get; set; } = FileDialogMode.Open;

		public FileSetting(string name, string title, string defaultValue = "", bool requireExistingFile = true) : base(name, title, defaultValue)
		{
			RequireExistingFile = requireExistingFile;
		}
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

		public Guid ID { get; set; } = Guid.Empty;

		[Obsolete("This is no longer implemented. Please set a proper ID in order for your setting to be saved.")]
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

			private Dictionary<Guid, Setting> _itemsByID = new Dictionary<Guid, Setting>();
			protected override void ClearItems()
			{
				base.ClearItems();
				_itemsByID.Clear();
			}
			protected override void InsertItem(int index, Setting item)
			{
				base.InsertItem(index, item);
				_itemsByID[item.ID] = item;
			}
			protected override void RemoveItem(int index)
			{
				_itemsByID.Remove(this[index].ID);
				base.RemoveItem(index);
			}
			public bool Contains(Guid id)
			{
				return _itemsByID.ContainsKey(id);
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
			if (scopeId != null)
			{
				if (ScopedValues.Contains(scopeId.Value))
				{
					return ScopedValues[scopeId.Value].Value;
				}
			}
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
				object val = GetValue(scopeId);
				if (val is T)
					return (T)val;
				if (val is string)
				{
					return (val as string).Parse<T>();
				}
				return defaultValue;
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

