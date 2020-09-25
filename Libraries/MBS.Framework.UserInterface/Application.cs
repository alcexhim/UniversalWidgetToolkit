﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MBS.Framework.UserInterface.Dialogs;
using MBS.Framework.UserInterface.Input.Keyboard;
using UniversalEditor;
using UniversalEditor.Accessors;
using UniversalEditor.DataFormats.Markup.XML;
using UniversalEditor.ObjectModels.Markup;
using UniversalEditor.ObjectModels.PropertyList;

namespace MBS.Framework.UserInterface
{
    public static class Application
    {
		private static Engine mvarEngine = null;
		public static Engine Engine { get { return mvarEngine; } }

		public static CommandLine CommandLine { get; private set; } = null;

		public static Feature.FeatureCollection Features { get; } = new Feature.FeatureCollection();

		public static SettingsProvider.SettingsProviderCollection SettingsProviders { get; } = new SettingsProvider.SettingsProviderCollection();

		private static int mvarExitCode = 0;
		public static int ExitCode { get { return mvarExitCode; } }

		public static bool Exited { get; internal set; } = false;

		public static Guid ID { get; set; } = Guid.Empty;
		public static string UniqueName { get; set; } = null;
		public static string ShortName { get; set; }
		public static string Title { get; set; } = String.Empty;

		public static SettingsProfile.SettingsProfileCollection SettingsProfiles { get; } = new SettingsProfile.SettingsProfileCollection();

		public static DpiAwareness DpiAwareness { get; set; } = DpiAwareness.Default;
		internal static bool ShouldDpiScale
		{
			// TODO: implement other forms of DpiAwareness
			get { return false; } // DpiAwareness == DpiAwareness.Default && Application.DpiAwareness == DpiAwareness.Default && System.Environment.OSVersion.Platform == PlatformID.Unix; }
		}

		private static string mvarBasePath = null;
		public static string BasePath
		{
			get
			{
				if (mvarBasePath == null)
				{
					// Set up the base path for the current application. Should this be able to be
					// overridden with a switch (/basepath:...) ?
					mvarBasePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				}
				return mvarBasePath;
			}
		}

		private static string mvarDataPath = null;
		public static string DataPath
		{
			get
			{
				if (mvarDataPath == null)
				{
					mvarDataPath = String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), new string[]
					{
						// The directory that serves as a common repository for application-specific data for the current roaming user.
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						ShortName
					});
				}
				return mvarDataPath;
			}
		}

		public static string[] EnumerateDataPaths()
		{
			return new string[]
			{
				// first look in the application root directory since this will be overridden by everything else
				BasePath,
				// then look in /usr/share/universal-editor or C:\ProgramData\Mike Becker's Software\Universal Editor
				String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), new string[]
				{
					System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
					ShortName
				}),
				// then look in ~/.local/share/universal-editor or C:\Users\USERNAME\AppData\Local\Mike Becker's Software\Universal Editor
				String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), new string[]
				{
					System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
					ShortName
				}),
				// then look in ~/.universal-editor or C:\Users\USERNAME\AppData\Roaming\Mike Becker's Software\Universal Editor
				String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), new string[]
				{
					System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
					ShortName
				})
			};
		}
		public static Accessor[] EnumerateDataFiles(string filter)
		{
			List<Accessor> xmlFilesList = new List<Accessor>();

			// TODO: change "universal-editor" string to platform-dependent "universal-editor" on *nix or "Mike Becker's Software/Universal Editor" on Windowds
			string[] paths = EnumerateDataPaths();

			foreach (string path in paths)
			{
				// skip this one if the path doesn't exist
				if (!System.IO.Directory.Exists(path)) continue;

				string[] xmlfilesPath = null;
				try
				{
					xmlfilesPath = System.IO.Directory.GetFiles(path, filter, System.IO.SearchOption.AllDirectories);
				}
				catch (UnauthorizedAccessException ex)
				{
					Console.WriteLine("UE: warning: access to data path {0} denied", path);
					continue;
				}

				foreach (string s in xmlfilesPath)
				{
					xmlFilesList.Add(new FileAccessor(s));
				}
			}

			MBS.Framework.Reflection.ManifestResourceStream[] streams = MBS.Framework.Reflection.GetAvailableManifestResourceStreams();
			for (int j = 0; j < streams.Length; j++)
			{
				if (streams[j].Name.Match(Application.ConfigurationFileNameFilter) || streams[j].Name.EndsWith(".xml"))
				{
					StreamAccessor sa = new StreamAccessor(streams[j].Stream);
					sa.FileName = streams[j].Name;
					xmlFilesList.Add(sa);
				}
			}
			return xmlFilesList.ToArray();
		}

		/// <summary>
		/// The aggregated raw markup of all the various XML files loaded in the current search path.
		/// </summary>
		private static MarkupObjectModel mvarRawMarkup = new MarkupObjectModel();
		public static MarkupObjectModel RawMarkup { get { return mvarRawMarkup; } }

		private static Language mvarDefaultLanguage = null;
		/// <summary>
		/// The default <see cref="Language"/> used to display translatable text in this application.
		/// </summary>
		public static Language DefaultLanguage { get { return mvarDefaultLanguage; } set { mvarDefaultLanguage = value; } }

		private static Language.LanguageCollection mvarLanguages = new Language.LanguageCollection();
		/// <summary>
		/// The languages defined for this application. Translations can be added through XML files in the ~/Languages folder.
		/// </summary>
		public static Language.LanguageCollection Languages { get { return mvarLanguages; } }

		private static CommandBar.CommandBarCollection mvarCommandBars = new CommandBar.CommandBarCollection();
		/// <summary>
		/// The command bars loaded in this application, which can each hold multiple <see cref="CommandItem"/>s.
		/// </summary>
		public static CommandBar.CommandBarCollection CommandBars { get { return mvarCommandBars; } }


		private static void InitializeCommandBar(MarkupTagElement tag)
		{
			MarkupAttribute attID = tag.Attributes["ID"];
			if (attID == null) return;

			CommandBar cb = new CommandBar();
			cb.ID = attID.Value;

			MarkupAttribute attTitle = tag.Attributes["Title"];
			if (attTitle != null)
			{
				cb.Title = attTitle.Value;
			}
			else
			{
				cb.Title = cb.ID;
			}

			MarkupTagElement tagItems = tag.Elements["Items"] as MarkupTagElement;
			if (tagItems != null)
			{
				foreach (MarkupElement elItem in tagItems.Elements)
				{
					MarkupTagElement tagItem = (elItem as MarkupTagElement);
					if (tagItem == null) continue;

					InitializeCommandBarItem(tagItem, null, cb);
				}
			}

			mvarCommandBars.Add(cb);
		}

		internal static void InitializeCommandBarItem(MarkupTagElement tag, Command parent, CommandBar parentCommandBar)
		{
			CommandItem item = CommandItem.FromMarkup(tag);
			CommandItem.AddToCommandBar(item, parent, parentCommandBar);
		}

		private static ApplicationMainMenu mvarMainMenu = new ApplicationMainMenu();
		/// <summary>
		/// The main menu of this application, which can hold multiple <see cref="CommandItem"/>s.
		/// </summary>
		public static ApplicationMainMenu MainMenu { get { return mvarMainMenu; } }

		public static void UpdateSplashScreenStatus(string value)
		{
			// TODO: implement this
			splasher.SetStatus(value);
		}
		public static void UpdateSplashScreenStatus(string value, int progressValue, int progressMinimum = 0, int progressMaximum = 100)
		{
			// TODO: implement this
			splasher.SetStatus(value, progressValue, progressMinimum, progressMaximum);
		}

		public static string ConfigurationFileNameFilter { get; set; } = null;

		/// <summary>
		/// Enumerates and loads the XML configuration files for the application. Blatantly stolen^W^WAdapted from Universal Editor.
		/// </summary>
		private static void InitializeXMLConfiguration()
		{
			OnBeforeConfigurationLoaded(EventArgs.Empty);

			#region Load the XML files
			string configurationFileNameFilter = ConfigurationFileNameFilter; 
			if (configurationFileNameFilter == null) configurationFileNameFilter = System.Configuration.ConfigurationManager.AppSettings["ApplicationFramework.Configuration.ConfigurationFileNameFilter"];
			if (configurationFileNameFilter == null) configurationFileNameFilter = "*.xml";

			Accessor[] xmlfiles = EnumerateDataFiles(configurationFileNameFilter);

			UpdateSplashScreenStatus("Loading XML configuration files", 0, 0, xmlfiles.Length);

			XMLDataFormat xdf = new XMLDataFormat();
			foreach (Accessor xmlfile in xmlfiles)
			{
				MarkupObjectModel markup = new MarkupObjectModel();
				Document doc = new Document(markup, xdf, xmlfile);
				doc.Accessor.DefaultEncoding = UniversalEditor.IO.Encoding.UTF8;

				doc.Accessor.Open();
				doc.Load();
				doc.Close();

				markup.CopyTo(mvarRawMarkup);

				UpdateSplashScreenStatus("Loading XML configuration files", Array.IndexOf(xmlfiles, xmlfile) + 1, 0, xmlfiles.Length);
			}

			#endregion

			#region Initialize the configuration with the loaded data
			#region Commands
			UpdateSplashScreenStatus("Loading available commands");
			MarkupTagElement tagCommands = (mvarRawMarkup.FindElement("ApplicationFramework", "Commands") as MarkupTagElement);
			if (tagCommands != null)
			{
				foreach (MarkupElement elCommand in tagCommands.Elements)
				{
					MarkupTagElement tagCommand = (elCommand as MarkupTagElement);
					if (tagCommand == null) continue;
					if (tagCommand.FullName != "Command") continue;

					MarkupAttribute attID = tagCommand.Attributes["ID"];
					if (attID == null) continue;

					Command cmd = Command.FromMarkup(tagCommand);
					Application.Commands.Add(cmd);
				}
			}
			#endregion
			#region Settings providers
			UpdateSplashScreenStatus("Loading settings providers");
			MarkupTagElement tagSettingsProviders = (mvarRawMarkup.FindElement("ApplicationFramework", "SettingsProviders") as MarkupTagElement);
			if (tagSettingsProviders != null)
			{
				foreach (MarkupElement elSettingsProvider in tagSettingsProviders.Elements)
				{
					LoadSettingsProviderXML(elSettingsProvider as MarkupTagElement);
				}
			}
			#endregion
			#region Main Menu Items
			UpdateSplashScreenStatus("Loading main menu items");

			MarkupTagElement tagMainMenuItems = (mvarRawMarkup.FindElement("ApplicationFramework", "MainMenu", "Items") as MarkupTagElement);
			if (tagMainMenuItems != null)
			{
				foreach (MarkupElement elItem in tagMainMenuItems.Elements)
				{
					MarkupTagElement tagItem = (elItem as MarkupTagElement);
					if (tagItem == null) continue;
					InitializeCommandBarItem(tagItem, null, null);
				}
			}

			UpdateSplashScreenStatus("Loading Quick Access Toolbar items");

			MarkupTagElement tagQuickAccessToolbarItems = (mvarRawMarkup.FindElement("ApplicationFramework", "QuickAccessToolbar", "Items") as MarkupTagElement);
			if (tagQuickAccessToolbarItems != null)
			{
				foreach (MarkupElement elItem in tagQuickAccessToolbarItems.Elements)
				{
					MarkupTagElement tagItem = (elItem as MarkupTagElement);
					if (tagItem == null) continue;

					QuickAccessToolbarItems.Add(CommandItem.FromMarkup(tagItem));
				}
			}

			UpdateSplashScreenStatus("Loading command bars");

			MarkupTagElement tagCommandBars = (mvarRawMarkup.FindElement("ApplicationFramework", "CommandBars") as MarkupTagElement);
			if (tagCommandBars != null)
			{
				foreach (MarkupElement elCommandBar in tagCommandBars.Elements)
				{
					MarkupTagElement tagCommandBar = (elCommandBar as MarkupTagElement);
					if (tagCommandBar == null) continue;
					if (tagCommandBar.FullName != "CommandBar") continue;
					InitializeCommandBar(tagCommandBar);
				}
			}
			#endregion
			#region Languages
			UpdateSplashScreenStatus("Loading languages and translations");

			MarkupTagElement tagLanguages = (mvarRawMarkup.FindElement("ApplicationFramework", "Languages") as MarkupTagElement);
			if (tagLanguages != null)
			{
				foreach (MarkupElement elLanguage in tagLanguages.Elements)
				{
					MarkupTagElement tagLanguage = (elLanguage as MarkupTagElement);
					if (tagLanguage == null) continue;
					if (tagLanguage.FullName != "Language") continue;
					InitializeLanguage(tagLanguage);
				}

				MarkupAttribute attDefaultLanguageID = tagLanguages.Attributes["DefaultLanguageID"];
				if (attDefaultLanguageID != null)
				{
					mvarDefaultLanguage = mvarLanguages[attDefaultLanguageID.Value];
				}
			}

			UpdateSplashScreenStatus("Setting language");

			if (mvarDefaultLanguage == null)
			{
				mvarDefaultLanguage = new Language();
			}
			else
			{
				foreach (Command cmd in Application.Commands)
				{
					cmd.Title = mvarDefaultLanguage.GetCommandTitle(cmd.ID, cmd.ID);
				}
			}
			#endregion

			#region Plugins
			UpdateSplashScreenStatus("Loading plugins");

			MarkupTagElement tagPlugins = (mvarRawMarkup.FindElement("ApplicationFramework", "Plugins") as MarkupTagElement);
			if (tagPlugins != null)
			{
				foreach (MarkupElement elPlugin in tagPlugins.Elements)
				{
					MarkupTagElement tagPlugin = (elPlugin as MarkupTagElement);
					if (tagPlugin == null) continue;
					if (tagPlugin.FullName != "Plugin") continue;
					InitializePlugin(tagPlugin);
				}
			}
			#endregion

			// UpdateSplashScreenStatus("Finalizing configuration");
			// ConfigurationManager.Load();
			#endregion

			Application.Title = DefaultLanguage?.GetStringTableEntry("Application.Title", Application.Title);

			OnAfterConfigurationLoaded(EventArgs.Empty);
		}

		private static SettingsProvider LoadSettingsProviderXML(MarkupTagElement tag)
		{
			if (tag == null) return null;
			if (tag.FullName != "SettingsProvider") return null;

			MarkupAttribute attID = tag.Attributes["ID"];
			if (attID == null) return null;

			Guid id = new Guid(attID.Value);
			if (Application.SettingsProviders.Contains(id))
				return null;

			CustomSettingsProvider csp = new CustomSettingsProvider();
			csp.ID = id;
			foreach (MarkupElement el in tag.Elements)
			{
				MarkupTagElement tag2 = (el as MarkupTagElement);
				if (tag2 == null) continue;
				if (tag2.FullName == "SettingsGroup")
				{
					SettingsGroup sg = new SettingsGroup();
					sg.Path = ParsePath(tag2.Elements["Path"] as MarkupTagElement);

					MarkupTagElement tagSettings = (tag2.Elements["Settings"] as MarkupTagElement);
					if (tagSettings != null)
					{
						foreach (MarkupElement el2 in tagSettings.Elements)
						{
							Setting s = LoadSettingXML(el2 as MarkupTagElement);
							if (s != null)
								sg.Settings.Add(s);
						}
					}
					csp.SettingsGroups.Add(sg);
				}
			}
			Application.SettingsProviders.Add(csp);
			return csp;
		}

		private static Setting LoadSettingXML(MarkupTagElement tag)
		{
			if (tag == null) return null;

			MarkupAttribute attSettingID = tag.Attributes["ID"];
			MarkupAttribute attSettingName = tag.Attributes["Name"];
			MarkupAttribute attSettingTitle = tag.Attributes["Title"];
			MarkupAttribute attSettingDescription = tag.Attributes["Description"];

			MarkupAttribute attDefaultValue = tag.Attributes["DefaultValue"];

			Setting s = null;
			switch (tag.FullName)
			{
				case "BooleanSetting":
				{
					s = new BooleanSetting(attSettingName?.Value, attSettingTitle?.Value);
					if (attDefaultValue != null)
						s.DefaultValue = bool.Parse(attDefaultValue.Value);
					break;
				}
				case "TextSetting":
				{
					s = new TextSetting(attSettingName?.Value, attSettingTitle?.Value);
					if (attDefaultValue != null)
						s.DefaultValue = attDefaultValue.Value;
					break;
				}
				case "FileSetting":
				{
					s = new FileSetting(attSettingName?.Value, attSettingTitle?.Value);
					if (attDefaultValue != null)
						s.DefaultValue = attDefaultValue.Value;
					break;
				}
				case "CommandSetting":
				{
					MarkupAttribute attCommandID = tag.Attributes["CommandID"];
					MarkupAttribute attStylePreset = tag.Attributes["StylePreset"];
					s = new CommandSetting(attSettingName?.Value, attSettingTitle?.Value, attCommandID?.Value);
					if (attStylePreset != null)
					{
						((CommandSetting)s).StylePreset = (ButtonStylePresets)Enum.Parse(typeof(ButtonStylePresets), attStylePreset.Value);
					}
					break;
				}
				case "RangeSetting":
				{
					s = new RangeSetting(attSettingName?.Value, attSettingTitle?.Value);
					MarkupAttribute attMinimumValue = tag.Attributes["MinimumValue"];
					if (attMinimumValue != null)
						((RangeSetting)s).MinimumValue = decimal.Parse(attMinimumValue.Value);

					MarkupAttribute attMaximumValue = tag.Attributes["MaximumValue"];
					if (attMaximumValue != null)
						((RangeSetting)s).MaximumValue = decimal.Parse(attMaximumValue.Value);

					if (attDefaultValue != null)
						((RangeSetting)s).DefaultValue = decimal.Parse(attDefaultValue.Value);
					break;
				}
				case "GroupSetting":
				{
					s = new GroupSetting(attSettingName?.Value, attSettingTitle?.Value);
					MarkupTagElement tagSettings = tag.Elements["Settings"] as MarkupTagElement;
					if (tagSettings != null)
					{
						foreach (MarkupElement el in tagSettings.Elements)
						{
							Setting s2 = LoadSettingXML(el as MarkupTagElement);
							if (s2 != null)
							{
								(s as GroupSetting).Options.Add(s2);
							}
						}
					}
					MarkupTagElement tagHeaderSettings = tag.Elements["HeaderSettings"] as MarkupTagElement;
					if (tagHeaderSettings != null)
					{
						foreach (MarkupElement el in tagHeaderSettings.Elements)
						{
							Setting s2 = LoadSettingXML(el as MarkupTagElement);
							if (s2 != null)
							{
								(s as GroupSetting).HeaderSettings.Add(s2);
							}
						}
					}
					break;
				}
			}

			if (s != null)
			{
				if (attSettingDescription != null)
					s.Description = attSettingDescription.Value;
			}
			return s;
		}

		private static string[] ParsePath(MarkupTagElement tag)
		{
			if (tag == null) return null;
			if (tag.FullName != "Path") return null;

			List<string> path = new List<string>();
			foreach (MarkupElement el in tag.Elements)
			{
				MarkupTagElement tag2 = (el as MarkupTagElement);
				if (tag2 == null) continue;
				if (tag2.FullName != "Part") continue;

				path.Add(tag2.Value);
			}
			return path.ToArray();
		}

		public static CustomPlugin.CustomPluginCollection CustomPlugins { get; } = new CustomPlugin.CustomPluginCollection();

		private static void InitializePlugin(MarkupTagElement tag)
		{
			CustomPlugin plugin = new CustomPlugin();
			plugin.ID = new Guid(tag.Attributes["ID"]?.Value);
			plugin.Title = tag.Attributes["Title"]?.Value;

			MarkupTagElement tagProvidedFeatures = tag.Elements["ProvidedFeatures"] as MarkupTagElement;
			if (tagProvidedFeatures != null)
			{
				for (int i = 0; i < tagProvidedFeatures.Elements.Count; i++)
				{
					MarkupTagElement tagProvidedFeature = (tagProvidedFeatures.Elements[i] as MarkupTagElement);
					if (tagProvidedFeature == null) continue;
					if (tagProvidedFeature.FullName != "ProvidedFeature") continue;

					string featureId = tagProvidedFeature.Attributes["FeatureID"]?.Value;
					if (featureId == null) continue;

					plugin.ProvidedFeatures.Add(new Feature(new Guid(featureId), tagProvidedFeature.Attributes["Title"]?.Value));
				}
			}

			MarkupTagElement tagConfiguration = tag.Elements["Configuration"] as MarkupTagElement;
			if (tagConfiguration != null)
			{
				MarkupObjectModel cfg = new MarkupObjectModel();
				cfg.Elements.Add(tagConfiguration);

				PropertyListObjectModel plom = new PropertyListObjectModel();
				MemoryAccessor ma = new MemoryAccessor();
				Document.Save(cfg, new XMLDataFormat(), ma);
				ma.Position = 0;

				Document.Load(plom, new UniversalEditor.DataFormats.PropertyList.XML.XMLPropertyListDataFormat(), ma);

				plugin.Configuration = plom;
			}

			CustomPlugins.Add(plugin);
		}

		private static void InitializeLanguage(MarkupTagElement tag)
		{
			MarkupAttribute attID = tag.Attributes["ID"];
			if (attID == null) return;

			Language lang = mvarLanguages[attID.Value];
			if (lang == null)
			{
				lang = new Language();
				lang.ID = attID.Value;
				mvarLanguages.Add(lang);
			}

			MarkupAttribute attTitle = tag.Attributes["Title"];
			if (attTitle != null)
			{
				lang.Title = attTitle.Value;
			}
			else
			{
				lang.Title = lang.ID;
			}

			MarkupTagElement tagStringTable = (tag.Elements["StringTable"] as MarkupTagElement);
			if (tagStringTable != null)
			{
				foreach (MarkupElement elStringTableEntry in tagStringTable.Elements)
				{
					MarkupTagElement tagStringTableEntry = (elStringTableEntry as MarkupTagElement);
					if (tagStringTableEntry == null) continue;
					if (tagStringTableEntry.FullName != "StringTableEntry") continue;

					MarkupAttribute attStringTableEntryID = tagStringTableEntry.Attributes["ID"];
					if (attStringTableEntryID == null) continue;

					MarkupAttribute attStringTableEntryValue = tagStringTableEntry.Attributes["Value"];
					if (attStringTableEntryValue == null) continue;

					lang.SetStringTableEntry(attStringTableEntryID.Value, attStringTableEntryValue.Value);
				}
			}

			MarkupTagElement tagCommands = (tag.Elements["Commands"] as MarkupTagElement);
			if (tagCommands != null)
			{
				foreach (MarkupElement elCommand in tagCommands.Elements)
				{
					MarkupTagElement tagCommand = (elCommand as MarkupTagElement);
					if (tagCommand == null) continue;
					if (tagCommand.FullName != "Command") continue;

					MarkupAttribute attCommandID = tagCommand.Attributes["ID"];
					if (attCommandID == null) continue;

					MarkupAttribute attCommandTitle = tagCommand.Attributes["Title"];
					if (attCommandTitle == null) continue;

					lang.SetCommandTitle(attCommandID.Value, attCommandTitle.Value);
				}
			}
		}

		public static event EventHandler BeforeConfigurationLoaded;
		private static void OnBeforeConfigurationLoaded(EventArgs e)
		{
			BeforeConfigurationLoaded?.Invoke(typeof(Application), e);
		}

		public static event EventHandler AfterConfigurationLoaded;
		private static void OnAfterConfigurationLoaded(EventArgs e)
		{
			AfterConfigurationLoaded?.Invoke(typeof(Application), e);
		}

		/// <summary>
		/// The event that is called the first time an applicati
		/// </summary>
		public static event EventHandler Startup;
		private static void OnStartup(EventArgs e)
		{
			Startup?.Invoke(typeof(Application), e);
		}

		private static SplashScreenWindow splasher = null;
		private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

		private static void ShowSplashScreen()
		{
			sw.Reset();
			sw.Start();
			// if (LocalConfiguration.SplashScreen.Enabled)
			// {
			splasher = new SplashScreenWindow();
			splasher.Show();
			// }
		}
		internal static void HideSplashScreen()
		{
			while (splasher == null)
			{
				// System.Threading.Thread.Sleep(500);
			}
			splasher.Hide();
			splasher = null;

			sw.Stop();
			Console.WriteLine("stopwatch: went from rip to ready in {0}", sw.Elapsed);
		}


		public static event ApplicationActivatedEventHandler Activated;
		private static void OnActivated(ApplicationActivatedEventArgs e)
		{
			if (e.FirstRun)
			{
				ShowSplashScreen();
				Application.DoEvents();

				System.Threading.Thread t = new System.Threading.Thread(t_threadStart);
				t.Start();

				while (splasher != null)
				{
					Application.DoEvents();
					System.Threading.Thread.Sleep(25); // don't remove this
				}
			}

			Activated?.Invoke(typeof(Application), e);
		}

		private static void t_threadStart(object obj)
		{
			InitializeXMLConfiguration();

			HideSplashScreen();
		}

		/// <summary>
		/// Gets a collection of <see cref="Context" /> objects representing system, application, user, and custom contexts for settings and other items.
		/// </summary>
		/// <value>A collection of <see cref="Context" /> objects representing contexts for settings and other items.</value>
		public static Context.ContextCollection Contexts { get; } = new Context.ContextCollection();

		private static void Application_MenuBar_Item_Click(object sender, EventArgs e)
		{
			CommandMenuItem mi = (sender as CommandMenuItem);
			if (mi == null)
				return;

			Command cmd = Application.Commands[mi.Name];
			if (cmd == null)
			{
				Console.WriteLine("unknown cmd '" + mi.Name + "'");
				return;
			}

			cmd.Execute();
		}

		private static List<Window> _windows = new List<Window>();
		private static System.Collections.ObjectModel.ReadOnlyCollection<Window> _windowsRO = null;
		public static System.Collections.ObjectModel.ReadOnlyCollection<Window> Windows
		{
			get
			{
				if (_windowsRO == null)
				{
					_windowsRO = new System.Collections.ObjectModel.ReadOnlyCollection<Window>(_windows);
				}
				return _windowsRO;
			}
		}
		internal static void AddWindow(Window window)
		{
			_windows.Add(window);
		}

		public static ContextChangedEventHandler ContextAdded;
		private static void OnContextAdded(ContextChangedEventArgs e)
		{
			ContextAdded?.Invoke(typeof(Application), e);
		}

		public static ContextChangedEventHandler ContextRemoved;
		private static void OnContextRemoved(ContextChangedEventArgs e)
		{
			ContextRemoved?.Invoke(typeof(Application), e);
		}

		private static Dictionary<Context, List<MenuItem>> _listContextMenuItems = new Dictionary<Context, List<MenuItem>>();
		private static Dictionary<Context, List<Command>> _listContextCommands = new Dictionary<Context, List<Command>>();

		/// <summary>
		/// Handles updating the menus, toolbars, keyboard shortcuts, and other UI elements associated with the application <see cref="Context" />.
		/// </summary>
		internal static void AddContext(Context ctx)
		{
			if (!_listContextMenuItems.ContainsKey(ctx))
			{
				_listContextMenuItems[ctx] = new List<MenuItem>();
			}
			if (!_listContextCommands.ContainsKey(ctx))
			{
				_listContextCommands[ctx] = new List<Command>();
			}

			foreach (Command cmd in ctx.Commands)
			{
				Command actual = Application.Commands[cmd.ID];
				if (actual != null)
				{
					for (int i = 0; i < cmd.Items.Count; i++)
					{
						if (!actual.Items.Contains(cmd.Items[i]))
						{
							CommandItem.AddToCommandBar(cmd.Items[i], actual, null);
						}
					}
				}
				else
				{
					_listContextCommands[ctx].Add(cmd);
					Application.Commands.Add(cmd);
				}
			}

			foreach (CommandItem ci in ctx.MenuItems)
			{
				MenuItem[] mi = MenuItem.LoadMenuItem(ci, Application_MenuBar_Item_Click);
				foreach (Window w in Application.Windows)
				{
					int insertIndex = -1;
					if (ci.InsertAfterID != null)
					{
						insertIndex = w.MenuBar.Items.IndexOf(w.MenuBar.Items[ci.InsertAfterID]) + 1;
					}
					else if (ci.InsertBeforeID != null)
					{
						insertIndex = w.MenuBar.Items.IndexOf(w.MenuBar.Items[ci.InsertBeforeID]);
					}

					for (int i = 0; i < mi.Length; i++)
					{
						_listContextMenuItems[ctx].Add(mi[i]);

						if (insertIndex != -1)
						{
							w.MenuBar.Items.Insert(insertIndex, mi[i]);
						}
						else
						{
							w.MenuBar.Items.Add(mi[i]);
						}
						insertIndex++;
					}
				}
			}

			OnContextAdded(new ContextChangedEventArgs(ctx));
		}
		/// <summary>
		/// Handles updating the menus, toolbars, keyboard shortcuts, and other UI elements associated with the application <see cref="Context" />.
		/// </summary>
		internal static void RemoveContext(Context ctx)
		{
			if (_listContextMenuItems.ContainsKey(ctx))
			{
				foreach (Window w in Application.Windows)
				{
					foreach (MenuItem mi in _listContextMenuItems[ctx])
					{
						w.MenuBar.Items.Remove(mi);
					}
				}
			}
			_listContextMenuItems[ctx].Clear();

			foreach (Command cmd in _listContextCommands[ctx])
			{
				Application.Commands.Remove(cmd);
			}
		}


		private static Dictionary<string, List<EventHandler>> _CommandEventHandlers = new Dictionary<string, List<EventHandler>>();

		public static Command.CommandCollection Commands { get; } = new Command.CommandCollection();
		public static CommandItem.CommandItemCollection QuickAccessToolbarItems { get; } = new CommandItem.CommandItemCollection();

		public static bool AttachCommandEventHandler(string commandID, EventHandler handler)
		{
			Command cmd = Commands[commandID];
			if (cmd != null)
			{
				cmd.Executed += handler;
				return true;
			}
			Console.WriteLine("attempted to attach handler for unknown command '" + commandID + "'");

			// handle command event handlers attached without a Command instance
			if (!_CommandEventHandlers.ContainsKey(commandID))
			{
				_CommandEventHandlers.Add(commandID, new List<EventHandler>());
			}
			if (!_CommandEventHandlers[commandID].Contains(handler))
			{
				_CommandEventHandlers[commandID].Add(handler);
			}
			return false;
		}
		public static void ExecuteCommand(string id, KeyValuePair<string, object>[] namedParameters = null)
		{
			Command cmd = Commands[id];

			// handle command event handlers attached without a Command instance
			if (_CommandEventHandlers.ContainsKey(id))
			{
				List<EventHandler> c = _CommandEventHandlers[id];
				for (int i = 0;  i < c.Count; i++)
				{
					c[i](cmd, new CommandEventArgs(cmd, namedParameters));
				}
				return;
			}

			// handle command event handlers attached in a context, most recently added first
			for (int i = Contexts.Count - 1; i >= 0; i--)
			{
				if (Contexts[i].ExecuteCommand(id))
					return;
			}

			if (cmd == null)
				return;

			cmd.Execute ();
		}

		public static string ExpandRelativePath(string relativePath)
		{
			if (relativePath == null) relativePath = String.Empty;
			if (relativePath.StartsWith("~" + System.IO.Path.DirectorySeparatorChar.ToString()) || relativePath.StartsWith("~" + System.IO.Path.AltDirectorySeparatorChar.ToString()))
			{
				string[] potentialFileNames = EnumerateDataPaths();
				for (int i = potentialFileNames.Length - 1; i >= 0; i--)
				{
					potentialFileNames[i] = potentialFileNames[i] + System.IO.Path.DirectorySeparatorChar.ToString() + relativePath.Substring(2);
					Console.WriteLine("Looking for " + potentialFileNames[i]);

					if (System.IO.File.Exists(potentialFileNames[i]))
					{
						Console.WriteLine("Using " + potentialFileNames[i]);
						return potentialFileNames[i];
					}
				}
			}
			if (System.IO.File.Exists(relativePath))
			{
				return relativePath;
			}
			return null;
		}

		public static event EventHandler ApplicationExited;

		private static void OnApplicationExited(EventArgs e)
		{
			foreach (SettingsProvider provider in Application.SettingsProviders) {
				provider.SaveSettings ();
			}

			if (ApplicationExited != null) ApplicationExited(null, e);
		}

		private static void InitializeSettingsProfiles()
		{
			SettingsProfiles.Add(new SettingsProfile());
			SettingsProfiles[0].ID = SettingsProfile.AllUsersGUID;
			SettingsProfiles[0].Title = "(All Users)";

			SettingsProfiles.Add(new SettingsProfile());
			SettingsProfiles[1].ID = SettingsProfile.ThisUserGUID;
			SettingsProfiles[1].Title = "(This User)";

			string[] dataPaths = EnumerateDataPaths();
			for (int i = 0; i < dataPaths.Length; i++)
			{
				if (System.IO.File.Exists(dataPaths[i] + System.IO.Path.DirectorySeparatorChar.ToString() + "settings" + System.IO.Path.DirectorySeparatorChar.ToString() + "profiles.lst"))
				{
					string[] lines = System.IO.File.ReadAllLines(dataPaths[i] + System.IO.Path.DirectorySeparatorChar.ToString() + "settings" + System.IO.Path.DirectorySeparatorChar.ToString() + "profiles.lst");
					for (int j = 0; j < lines.Length; j++)
					{
						if (lines[j] == String.Empty || lines[j].StartsWith("#"))
							continue;

						string[] split = lines[j].Split(new char[] { '=' });
						if (split.Length == 2)
						{
							SettingsProfile profile = new SettingsProfile();
							profile.ID = new Guid(split[0].Trim());
							profile.Title = split[1].Trim();
							SettingsProfiles.Add(profile);
						}
					}
				}
			}
		}

		// [DebuggerNonUserCode()]
		public static void Initialize()
		{
			if (mvarEngine == null)
			{
				Engine[] engines = Engine.Get();
				if (engines.Length > 0) mvarEngine = engines[0];

				if (mvarEngine == null) throw new ArgumentNullException("Application.Engine", "No engines were found or could be loaded");
			}

			if (mvarEngine != null)
			{
				Console.WriteLine("Using engine {0}", mvarEngine.GetType().FullName);
				mvarEngine.Initialize();
			}

			InitializeSettingsProfiles();

			// after initialization, load option providers

			List<SettingsProvider> listOptionProviders = new List<SettingsProvider>();
			System.Collections.Specialized.StringCollection listOptionProviderTypeNames = new System.Collections.Specialized.StringCollection ();

			// load the already-known list
			foreach (SettingsProvider provider in Application.SettingsProviders) {
				listOptionProviders.Add (provider);
				listOptionProviderTypeNames.Add (provider.GetType ().FullName);
			}


			Type[] types = MBS.Framework.Reflection.GetAvailableTypes(new Type[] { typeof(SettingsProvider) });

			foreach (Type type in types) {
				if (type == null)
					continue;

				if (type.IsSubclassOf (typeof(SettingsProvider)) && !type.IsAbstract) {
					if (!listOptionProviderTypeNames.Contains (type.FullName)) {
						try {
							SettingsProvider provider = (type.Assembly.CreateInstance (type.FullName) as SettingsProvider);
							if (provider == null) {
								Console.Error.WriteLine ("ue: reflection: couldn't load OptionProvider '{0}'", type.FullName);
								continue;
							}
							listOptionProviderTypeNames.Add (type.FullName);
							listOptionProviders.Add (provider);
							Console.WriteLine ("loaded option provider \"{0}\"", type.FullName);
						} catch (System.Reflection.TargetInvocationException ex) {
							Console.WriteLine ("binding error: " + ex.InnerException.Message);
							if (ex.InnerException.InnerException != null)
							{
								Console.WriteLine("^--- {0}", ex.InnerException.InnerException.Message);
								Console.WriteLine();
								Console.WriteLine(" *** STACK TRACE *** ");
								Console.WriteLine(ex.StackTrace);
								Console.WriteLine(" ******************* ");
								Console.WriteLine();
							}
						} catch (Exception ex) {
							Console.WriteLine ("error while loading SettingsProvider '" + type.FullName + "': " + ex.Message);
						}
					} else {
						Console.WriteLine ("skipping already loaded SettingsProvider '{0}'", type.FullName);
					}
				}
			}

			foreach (SettingsProvider provider in listOptionProviders) {
				if (provider is ApplicationSettingsProvider) {
					Application.SettingsProviders.Add (provider);
					provider.LoadSettings ();
				}
			}

			Plugin[] plugins = Plugin.Get();
			for (int i = 0; i < plugins.Length; i++)
			{
				Console.WriteLine("initializing plugin '{0}'", plugins[i].GetType().FullName);
				plugins[i].Initialize();

				if (plugins[i].Context != null)
					Application.Contexts.Add(plugins[i].Context);
			}
		}

		static Application()
		{
			CommandLine = new DefaultCommandLine();

			Type tKnownContexts = typeof(KnownContexts);
			System.Reflection.PropertyInfo[] pis = tKnownContexts.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			for (int i = 0; i < pis.Length; i++)
			{
				Context ctx = (Context)pis[i].GetValue(null, null);
				Application.Contexts.Add(ctx);
			}

			Engine[] engines = Engine.Get();
			if (engines.Length > 0) mvarEngine = engines[0];
			
			string sv = System.Reflection.Assembly.GetEntryAssembly().Location;
			if (sv.StartsWith("/")) sv = sv.Substring(1);
			
			sv = sv.Replace(".", "_");
			sv = sv.Replace("\\", ".");
			sv = sv.Replace("/", ".");
			
			// ID = Guid.NewGuid();
			// sv = sv + ID.ToString().Replace("-", String.Empty);
			UniqueName = sv;

			// configure UWT-provided features
			pis = typeof(KnownFeatures).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			for (int i = 0; i < pis.Length; i++)
			{
				Feature feature = (Feature)pis[i].GetValue(null, null);
				Features.Add(feature);
			}
		}

		// [DebuggerNonUserCode()]
		public static int Start(Window waitForClose = null)
		{
			Console.WriteLine("Application_Start");
			if (waitForClose != null)
			{
				if (mvarEngine.IsControlDisposed(waitForClose))
					mvarEngine.CreateControl (waitForClose);

				waitForClose.Show();
			}

			int exitCode = mvarEngine.Start(waitForClose);
			
			mvarExitCode = exitCode;
			OnApplicationExited(EventArgs.Empty);

			return exitCode;
		}

		public static event System.ComponentModel.CancelEventHandler BeforeShutdown;
		private static void OnBeforeShutdown(System.ComponentModel.CancelEventArgs e)
		{
			BeforeShutdown?.Invoke(typeof(Application), e);
		}

		public static event EventHandler Shutdown;
		private static void OnShutdown(EventArgs e)
		{
			Shutdown?.Invoke(typeof(Application), e);
		}

		public static bool Stopping { get; private set; } = false;

		public static void Stop(int exitCode = 0)
		{
			if (Stopping)
				return;

			Stopping = true;
			if (mvarEngine == null)
				return; // why bother getting an engine? we're stopping...

			System.ComponentModel.CancelEventArgs ce = new System.ComponentModel.CancelEventArgs();
			OnBeforeShutdown(ce);
			if (ce.Cancel)
			{
				Stopping = false;
				return;
			}

			mvarEngine.Stop(exitCode);
			OnShutdown(EventArgs.Empty);
			Stopping = false;
		}

		public static void DoEvents()
		{
			mvarEngine?.DoEvents();
		}

		public static T GetSetting<T>(string name, T defaultValue = default(T))
		{
			try 
			{
				object value = GetSetting(name);
				if (value == null) {
					return defaultValue;
				}
				return (T)value;
			}
			catch {
				return defaultValue;
			}
		}
		public static void SetSetting<T>(string name, T value)
		{
			SetSetting (name, (object)value);
		}

		public static SettingsGroup FindSettingGroup(string name, out string realName, out string groupPath)
		{
			string[] namePath = name.Split (new char[] { ':' });
			realName = namePath [namePath.Length - 1];
			groupPath = String.Join (":", namePath, 0, namePath.Length - 1);

			foreach (SettingsProvider provider in SettingsProviders) {
				foreach (SettingsGroup group in provider.SettingsGroups) {
					string path = String.Join (":", group.Path);
					path = path.Replace (' ', '_');
					if (path.Equals (groupPath)) {
						return group;
					}
				}
			}

			realName = null;
			groupPath = null;
			return null;
		}

		public static object GetSetting(string name)
		{
			string realName = null;
			string groupPath = null;

			SettingsGroup group = FindSettingGroup (name, out realName, out groupPath);
			if (group == null)
				return null;
			if (group.Settings [realName] != null) {
				return group.Settings [realName].GetValue ();
			}
			return null;
		}
		public static void SetSetting(string name, object value)
		{
			string realName = null;
			string groupPath = null;

			SettingsGroup group = FindSettingGroup (name, out realName, out groupPath);
			if (group == null)
				return;
			if (group.Settings [realName] != null) {
				group.Settings [realName].SetValue (value);
			}
		}

		public static Process Launch(Uri uri)
		{
			return Launch(uri.ToString());
		}
		/// <summary>
		/// Launch the application represented by the given path.
		/// </summary>
		/// <param name="path">Path.</param>
		public static Process Launch(string path)
		{
			Process p = new Process();
			p.StartInfo.FileName = path;
			p.Start();

			return p;
		}

		/// <summary>
		/// Displays the application's Help in the system native Help viewer, navigating to the appropriate <see cref="HelpTopic" /> if specified.
		/// </summary>
		public static void ShowHelp(HelpTopic topic = null)
		{
			Engine.ShowHelp(topic);
		}

		public static bool ShowSettingsDialog(string[] path = null)
		{
			SettingsDialog dlg = new SettingsDialog();
			if (dlg.ShowDialog(path) == DialogResult.OK)
			{
				return true;
			}
			return false;
		}
	}
}
