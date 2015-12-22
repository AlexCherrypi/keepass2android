/*
This file is part of Keepass2Android, Copyright 2013 Philipp Crocoll. This file is based on Keepassdroid, Copyright Brian Pellin.

  Keepass2Android is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  Keepass2Android is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with Keepass2Android.  If not, see <http://www.gnu.org/licenses/>.
  */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.IO;
using KeePassLib.Cryptography.Cipher;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;
using keepass2android.Io;
using keepass2android.Utils;

namespace keepass2android
{

	public class IconSetPreference : ListPreference
	{
		private int selectedEntry;

		private class IconSet
		{
			public string PackageName { get; set; }
			public string DisplayName { get; set; }

			public Drawable GetIcon(Context context)
			{
				if (PackageName == context.PackageName)
					return context.Resources.GetDrawable(Resource.Drawable.ic00);
				Resources res = context.PackageManager.GetResourcesForApplication(PackageName);

				return res.GetDrawable(res.GetIdentifier("ic00", "drawable", PackageName));
			}
		}

		private class IconListPreferenceScreenAdapter : BaseAdapter
		{
			private readonly IconSetPreference _pref;

			public IconListPreferenceScreenAdapter(IconSetPreference pref, Context context)
			{
				_pref = pref;
			}


			private class CustomHolder : Java.Lang.Object
			{
				private TextView text = null;
				private RadioButton rButton = null;

				public CustomHolder(View row, int position, IconSetPreference pref)
				{
					text = (TextView)row.FindViewById(Resource.Id.image_list_view_row_text_view);
					text.Text = pref.IconSets[position].DisplayName;

					rButton = (RadioButton)row.FindViewById(Resource.Id.image_list_view_row_radio_button);
					rButton.Id = position;
					rButton.Clickable = false;
					rButton.Checked = (pref.selectedEntry == position);

					try
					{
						Drawable dr = pref.IconSets[position].GetIcon(row.Context);
						var bitmap = ((BitmapDrawable)dr).Bitmap;
						Drawable d = new BitmapDrawable(row.Resources, Bitmap.CreateScaledBitmap(bitmap, 64, 64, true));
						text.SetCompoundDrawablesWithIntrinsicBounds(d, null, null, null);
						text.Text = (" " + text.Text);
					}
					catch (Exception)
					{
					}


				}
			}

			public override Java.Lang.Object GetItem(int position)
			{
				return null;
			}

			public override long GetItemId(int position)
			{
				return position;
			}



			public override View GetView(int position, View convertView, ViewGroup parent)
			{
				View row = convertView;
				CustomHolder holder = null;
				int p = position;
				row = LayoutInflater.From(_pref.Context).Inflate(Resource.Layout.image_list_preference_row, parent, false);
				holder = new CustomHolder(row, position, _pref);

				row.Tag = holder;

				// row.setClickable(true);
				// row.setFocusable(true);
				// row.setFocusableInTouchMode(true);
				row.Click += (sender, args) =>
				{

					((View)sender).RequestFocus();

					Dialog mDialog = _pref.Dialog;
					mDialog.Dismiss();

					_pref.CallChangeListener(_pref.IconSets[p].PackageName);
					ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(_pref.Context);
					var edit = pref.Edit();
					edit.PutString(_pref.Key, _pref.IconSets[p].PackageName);
					edit.Commit();
					_pref.selectedEntry = p;
					
				};

				return row;
			}

			public override int Count
			{
				get { return _pref.IconSets.Count; }
			}
		}


		List<IconSet> _iconSets = null;
		List<IconSet> IconSets
		{
			get
			{
				if (_iconSets != null)
					return _iconSets;
				_iconSets = new List<IconSet>();

				_iconSets.Add(new IconSet()
				{
					DisplayName = AppNames.AppName,
					PackageName = Context.PackageName
				});

				foreach (var p in Context.PackageManager.GetInstalledPackages(0))
				{
					try
					{

						string packageName = p.PackageName;
						Resources res = Context.PackageManager.GetResourcesForApplication(packageName);
						int nameId = res.GetIdentifier("kp2a_iconset_name", "string", packageName);
						_iconSets.Add(new IconSet()
						{
							DisplayName = res.GetString(nameId),
							PackageName = packageName
						});
					}
					catch (Exception)
					{

					}
				}
				return _iconSets;
			}
		}
		protected IconSetPreference(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
		}

		private readonly Task _populatorTask;

		public IconSetPreference(Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			_populatorTask = Task.Factory.StartNew(() =>
			{
				SetEntries(IconSets.Select(s => s.DisplayName).ToArray());
				SetEntryValues(IconSets.Select(s => s.PackageName).ToArray());
			});
			
		}


		protected override void OnPrepareDialogBuilder(AlertDialog.Builder builder)
		{
			_populatorTask.Wait();
			base.OnPrepareDialogBuilder(builder);


			var iconListPreferenceAdapter = new IconListPreferenceScreenAdapter(this, Context);

			String selectedValue = PreferenceManager.GetDefaultSharedPreferences(Context).GetString(Key, "");
			for (int i = 0; i < IconSets.Count; i++)
			{
				if (selectedValue == IconSets[i].PackageName)
				{
					selectedEntry = i;
					break;
				}
			}

			builder.SetAdapter(iconListPreferenceAdapter, (sender, args) => { });
			builder.SetNeutralButton(Resource.String.IconSet_install, (sender, args) =>
			{
				Util.GotoUrl(Context, "market://search?q=keepass2android icon set");
			});


		}
	}

    //http://stackoverflow.com/a/27422401/292233
    public class SettingsFragment : PreferenceFragment
    {


		

        public class KeyboardSwitchPrefManager
        {
            private readonly Activity _act;
            private CheckBoxPreference _switchPref;
            private CheckBoxPreference _openKp2aAutoPref;
            private CheckBoxPreference _openOnlyOnSearchPref;
            private CheckBoxPreference _switchBackPref;
            private PreferenceScreen _screen;
            private PreferenceFragment _fragment;

            public KeyboardSwitchPrefManager(PreferenceFragment fragment)
            {
                var act = fragment.Activity;
                this._act = act;
                this._fragment = fragment;

                _switchPref = (CheckBoxPreference)_fragment.FindPreference("kp2a_switch_rooted");
                _openKp2aAutoPref =
                    (CheckBoxPreference)_fragment.FindPreference(act.GetString(Resource.String.OpenKp2aKeyboardAutomatically_key));
                _openOnlyOnSearchPref =
                    (CheckBoxPreference)
                    _fragment.FindPreference(act.GetString(Resource.String.OpenKp2aKeyboardAutomaticallyOnlyAfterSearch_key));
                _switchBackPref =
                    (CheckBoxPreference)_fragment.FindPreference(act.GetString(Resource.String.AutoSwitchBackKeyboard_key));
                _screen = (PreferenceScreen)_fragment.FindPreference(act.GetString(Resource.String.keyboardswitch_prefs_key));
                EnableSwitchPreferences(_switchPref.Checked);

                _switchPref.PreferenceChange += (sender, args) =>
                {
                    bool switchOnRooted = (bool)args.NewValue;
                    EnableSwitchPreferences(switchOnRooted);
                };
            }


            private void EnableSwitchPreferences(bool switchOnRooted)
            {
                if (!switchOnRooted)
                {
                    if (_fragment.FindPreference(_act.GetString(Resource.String.OpenKp2aKeyboardAutomatically_key)) == null)
                    {
                        _screen.AddPreference(_openKp2aAutoPref);
                    }
                    if (_fragment.FindPreference(_act.GetString(Resource.String.OpenKp2aKeyboardAutomaticallyOnlyAfterSearch_key)) != null)
                    {
                        _screen.RemovePreference(_openOnlyOnSearchPref);
                    }
                }
                else
                {
                    {
                        _screen.RemovePreference(_openKp2aAutoPref);
                    }
                    if (_fragment.FindPreference(_act.GetString(Resource.String.OpenKp2aKeyboardAutomaticallyOnlyAfterSearch_key)) == null)
                    {
                        _screen.AddPreference(_openOnlyOnSearchPref);
                    }
                }
                /*_openKp2aAutoPref.Enabled = !switchOnRooted;

                _openOnlyOnSearchPref.Enabled = switchOnRooted;

                _switchBackPref.Enabled = switchOnRooted;*/
            }
        }

        private KeyboardSwitchPrefManager _switchPrefManager;

        void OnRememberKeyFileHistoryChanged(object sender, Preference.PreferenceChangeEventArgs eventArgs)
        {
            if (!(bool)eventArgs.NewValue)
            {
                App.Kp2a.FileDbHelper.DeleteAllKeys();
            }
        }

        void OnShowUnlockedNotificationChanged(object sender, Preference.PreferenceChangeEventArgs eventArgs)
        {
            App.Kp2a.UpdateOngoingNotification();
        }


        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.Xml.preferences);

            // Re-use the change handlers for the application settings
            FindPreference(GetString(Resource.String.keyfile_key)).PreferenceChange += OnRememberKeyFileHistoryChanged;
            FindPreference(GetString(Resource.String.ShowUnlockedNotification_key)).PreferenceChange += OnShowUnlockedNotificationChanged;
            PrepareNoDonatePreference(Activity, FindPreference(GetString(Resource.String.NoDonateOption_key)));

	        FindPreference(GetString(Resource.String.design_key)).PreferenceChange += (sender, args) => Activity.Recreate();
            
            Database db = App.Kp2a.GetDb();
            if (db.Loaded)
            {
                Preference rounds = FindPreference(GetString(Resource.String.rounds_key));
                rounds.PreferenceChange += (sender, e) => SetRounds(db, e.Preference);
                rounds.Enabled = db.CanWrite;
                SetRounds(db, rounds);

                PrepareDefaultUsername(db);
                PrepareDatabaseName(db);
                PrepareMasterPassword();
                
                Preference algorithm = FindPreference(GetString(Resource.String.algorithm_key));
                SetAlgorithm(db, algorithm);

                UpdateImportDbPref();
                UpdateImportKeyfilePref();
            }

            try
            {
                //depending on Android version, we offer to use a transparent icon for QuickUnlock or use the notification priority (since API level 16)
                Preference hideQuickUnlockTranspIconPref = FindPreference(GetString(Resource.String.QuickUnlockIconHidden_key));
                Preference hideQuickUnlockIconPref = FindPreference(GetString(Resource.String.QuickUnlockIconHidden16_key));
                var quickUnlockScreen = ((PreferenceScreen)FindPreference(GetString(Resource.String.QuickUnlock_prefs_key)));
                if ((int)Android.OS.Build.VERSION.SdkInt >= 16)
                {
                    quickUnlockScreen.RemovePreference(hideQuickUnlockTranspIconPref);
                    FindPreference(GetString(Resource.String.ShowUnlockedNotification_key)).PreferenceChange += (sender, args) => App.Kp2a.UpdateOngoingNotification();
                    hideQuickUnlockIconPref.PreferenceChange += delegate { App.Kp2a.UpdateOngoingNotification(); };
                }
                else
                {
                    //old version: only show transparent quickUnlock and no option to hide unlocked icon:
                    quickUnlockScreen.RemovePreference(hideQuickUnlockIconPref);
                    FindPreference(GetString(Resource.String.QuickUnlockIconHidden_key)).PreferenceChange +=
                        delegate { App.Kp2a.UpdateOngoingNotification(); };

                    ((PreferenceScreen)FindPreference(GetString(Resource.String.display_prefs_key))).RemovePreference(
                        FindPreference(GetString(Resource.String.ShowUnlockedNotification_key)));
                }
            }
            catch (Exception ex)
            {
                Kp2aLog.Log(ex.ToString());
            }

            

            //AppSettingsActivity.PrepareKeyboardSwitchingPreferences(this);
            _switchPrefManager = new KeyboardSwitchPrefManager(this);
            PrepareSeparateNotificationsPreference();

			FindPreference("IconSetKey").PreferenceChange += (sender, args) =>
			{
				if (App.Kp2a.GetDb() != null)
					App.Kp2a.GetDb().DrawableFactory.Clear();

			};

            Preference cachingPreference = FindPreference(GetString(Resource.String.UseOfflineCache_key));
            cachingPreference.PreferenceChange += OnUseOfflineCacheChanged;

            try
            {
                //depending on Android version, we offer to use a transparent icon for QuickUnlock or use the notification priority (since API level 16)
                Preference hideQuickUnlockTranspIconPref = FindPreference(GetString(Resource.String.QuickUnlockIconHidden_key));
                Preference hideQuickUnlockIconPref = FindPreference(GetString(Resource.String.QuickUnlockIconHidden16_key));
                var quickUnlockScreen = ((PreferenceScreen)FindPreference(GetString(Resource.String.QuickUnlock_prefs_key)));
                if ((int)Android.OS.Build.VERSION.SdkInt >= 16)
                {
                    quickUnlockScreen.RemovePreference(hideQuickUnlockTranspIconPref);
                    FindPreference(GetString(Resource.String.ShowUnlockedNotification_key)).PreferenceChange += (sender, args) => App.Kp2a.UpdateOngoingNotification();
                    hideQuickUnlockIconPref.PreferenceChange += OnQuickUnlockHiddenChanged;
                }
                else
                {
                    //old version: only show transparent quickUnlock and no option to hide unlocked icon:
                    quickUnlockScreen.RemovePreference(hideQuickUnlockIconPref);
                    FindPreference(GetString(Resource.String.QuickUnlockIconHidden_key)).PreferenceChange +=
                        delegate { App.Kp2a.UpdateOngoingNotification(); };
                    ((PreferenceScreen)FindPreference(GetString(Resource.String.display_prefs_key))).RemovePreference(
                        FindPreference(GetString(Resource.String.ShowUnlockedNotification_key)));
                }
            }
            catch (Exception ex)
            {
                Kp2aLog.Log(ex.ToString());
            }

            
			
        }

        private void PrepareMasterPassword()
        {
            Preference changeMaster = FindPreference(GetString(Resource.String.master_pwd_key));
            if (App.Kp2a.GetDb().CanWrite)
            {
                changeMaster.Enabled = true;
                changeMaster.PreferenceClick += delegate { new SetPasswordDialog(Activity).Show(); };
            }
        }

        private void PrepareDatabaseName(Database db)
        {
            Preference databaseName = FindPreference(GetString(Resource.String.database_name_key));
            if (!db.DatabaseFormat.HasDatabaseName)
            {
                ((PreferenceScreen) FindPreference(GetString(Resource.String.db_key))).RemovePreference(databaseName);
            }
            else
            {
                databaseName.Enabled = db.CanWrite;
                ((EditTextPreference) databaseName).EditText.Text = db.KpDatabase.Name;
                ((EditTextPreference) databaseName).Text = db.KpDatabase.Name;
                databaseName.PreferenceChange += (sender, e) =>
                {
                    DateTime previousNameChanged = db.KpDatabase.NameChanged;
                    String previousName = db.KpDatabase.Name;
                    db.KpDatabase.Name = e.NewValue.ToString();

                    SaveDb save = new SaveDb(Activity, App.Kp2a, new ActionOnFinish((success, message) =>
                    {
                        if (!success)
                        {
                            db.KpDatabase.Name = previousName;
                            db.KpDatabase.NameChanged = previousNameChanged;
                            Toast.MakeText(Activity, message, ToastLength.Long).Show();
                        }
                        else
                        {
                            // Name is reflected in notification, so update it
                            App.Kp2a.UpdateOngoingNotification();
                        }
                    }));
                    ProgressTask pt = new ProgressTask(App.Kp2a, Activity, save);
                    pt.Run();
                };
            }
        }

        private void PrepareDefaultUsername(Database db)
        {
            Preference defaultUser = FindPreference(GetString(Resource.String.default_username_key));
            if (!db.DatabaseFormat.HasDefaultUsername)
            {
                ((PreferenceScreen) FindPreference(GetString(Resource.String.db_key))).RemovePreference(defaultUser);
            }
            else
            {
                defaultUser.Enabled = db.CanWrite;
                ((EditTextPreference) defaultUser).EditText.Text = db.KpDatabase.DefaultUserName;
                ((EditTextPreference) defaultUser).Text = db.KpDatabase.DefaultUserName;
                defaultUser.PreferenceChange += (sender, e) =>
                {
                    DateTime previousUsernameChanged = db.KpDatabase.DefaultUserNameChanged;
                    String previousUsername = db.KpDatabase.DefaultUserName;
                    db.KpDatabase.DefaultUserName = e.NewValue.ToString();

                    SaveDb save = new SaveDb(Activity, App.Kp2a, new ActionOnFinish((success, message) =>
                    {
                        if (!success)
                        {
                            db.KpDatabase.DefaultUserName = previousUsername;
                            db.KpDatabase.DefaultUserNameChanged = previousUsernameChanged;
                            Toast.MakeText(Activity, message, ToastLength.Long).Show();
                        }
                    }));
                    ProgressTask pt = new ProgressTask(App.Kp2a, Activity, save);
                    pt.Run();
                };
            }
        }

        public void PrepareSeparateNotificationsPreference()
        {
            try
            {
                //depending on Android version, we offer to show a combined notification (with action buttons) (since API level 16)
                Preference separateNotificationsPref = FindPreference(Activity.GetString(Resource.String.ShowSeparateNotifications_key));
                var passwordAccessScreen = ((PreferenceScreen)FindPreference(Activity.GetString(Resource.String.password_access_prefs_key)));
                if ((int)Build.VERSION.SdkInt < 16)
                {
                    passwordAccessScreen.RemovePreference(separateNotificationsPref);
                }
            }
            catch (Exception ex)
            {
                Kp2aLog.Log(ex.ToString());
            }
        }



        private void OnQuickUnlockHiddenChanged(object sender, Preference.PreferenceChangeEventArgs e)
        {
            App.Kp2a.UpdateOngoingNotification();
        }

        private void OnUseOfflineCacheChanged(object sender, Preference.PreferenceChangeEventArgs e)
        {
            //ensure the user gets a matching database
            if (App.Kp2a.GetDb().Loaded && !App.Kp2a.GetDb().Ioc.IsLocalFile())
                App.Kp2a.LockDatabase(false);

            if (!(bool)e.NewValue)
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
                builder.SetTitle(GetString(Resource.String.ClearOfflineCache_title));

                builder.SetMessage(GetString(Resource.String.ClearOfflineCache_question));

                builder.SetPositiveButton(App.Kp2a.GetResourceString(UiStringKey.yes), (o, args) =>
                {
                    try
                    {
                        App.Kp2a.ClearOfflineCache();
                    }
                    catch (Exception ex)
                    {
                        Kp2aLog.Log(ex.ToString());
                        Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
                    }
                }
                    );

                builder.SetNegativeButton(App.Kp2a.GetResourceString(UiStringKey.no), (o, args) =>
                {
                }
                );
                builder.SetCancelable(false);
                Dialog dialog = builder.Create();
                dialog.Show();


            }
        }

        private void UpdateImportKeyfilePref()
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(Activity);
            var rememberKeyfile = prefs.GetBoolean(GetString(Resource.String.keyfile_key), Resources.GetBoolean(Resource.Boolean.keyfile_default));

            Preference importDb = FindPreference("import_keyfile_prefs");
            importDb.Summary = "";

            if (!rememberKeyfile)
            {
                importDb.Summary = GetString(Resource.String.KeyfileMoveRequiresRememberKeyfile);
                importDb.Enabled = false;
                return;
            }
            CompositeKey masterKey = App.Kp2a.GetDb().KpDatabase.MasterKey;
            if (masterKey.ContainsType(typeof(KcpKeyFile)))
            {
                IOConnectionInfo iocKeyfile = ((KcpKeyFile)masterKey.GetUserKey(typeof(KcpKeyFile))).Ioc;
                if (iocKeyfile.IsLocalFile() && IoUtil.IsInInternalDirectory(iocKeyfile.Path, Activity))
                {
                    importDb.Enabled = false;
                    importDb.Summary = GetString(Resource.String.FileIsInInternalDirectory);
                }
                else
                {
                    importDb.Enabled = true;
                    importDb.PreferenceClick += (sender, args) => { MoveKeyfileToInternalFolder(); };
                }


            }
            else
            {
                importDb.Enabled = false;
            }
        }

        private void MoveKeyfileToInternalFolder()
        {
            Func<Action> copyAndReturnPostExecute = () =>
            {
                try
                {
                    CompositeKey masterKey = App.Kp2a.GetDb().KpDatabase.MasterKey;
                    var sourceIoc = ((KcpKeyFile)masterKey.GetUserKey(typeof(KcpKeyFile))).Ioc;
                    var newIoc = ImportFileToInternalDirectory(sourceIoc);
                    ((KcpKeyFile)masterKey.GetUserKey(typeof(KcpKeyFile))).ResetIoc(newIoc);
                    var keyfileString = IOConnectionInfo.SerializeToString(newIoc);
                    App.Kp2a.StoreOpenedFileAsRecent(App.Kp2a.GetDb().Ioc, keyfileString);
                    return () =>
                    {
                        UpdateImportKeyfilePref();
                        var builder = new AlertDialog.Builder(Activity);
                        builder
                            .SetMessage(Resource.String.KeyfileMoved);
                        builder.SetPositiveButton(Android.Resource.String.Ok, (sender, args) => { });
                        builder.Show();

                    };




                }
                catch (Exception e)
                {
                    return () =>
                    {
                        Toast.MakeText(Activity, App.Kp2a.GetResourceString(UiStringKey.ErrorOcurred) + " " + e.Message, ToastLength.Long).Show();
                    };
                }



            };

            new SimpleLoadingDialog(Activity, GetString(Resource.String.CopyingFile), false,
                                  copyAndReturnPostExecute
                ).Execute();

        }
        private void UpdateImportDbPref()
        {
            //Import db/key file preferences:
            Preference importDb = FindPreference("import_db_prefs");
            if (!App.Kp2a.GetDb().Ioc.IsLocalFile())
            {
                importDb.Summary = GetString(Resource.String.OnlyAvailableForLocalFiles);
                importDb.Enabled = false;
            }
            else
            {
                if (IoUtil.IsInInternalDirectory(App.Kp2a.GetDb().Ioc.Path, Activity))
                {
                    importDb.Summary = GetString(Resource.String.FileIsInInternalDirectory);
                    importDb.Enabled = false;
                }
                else
                {
                    importDb.Enabled = true;
                    importDb.PreferenceClick += delegate { MoveDbToInternalFolder(); };
                }
            }
        }

        private void MoveDbToInternalFolder()
        {
            Func<Action> copyAndReturnPostExecute = () =>
            {
                try
                {
                    var sourceIoc = App.Kp2a.GetDb().Ioc;
                    var newIoc = ImportFileToInternalDirectory(sourceIoc);
                    return () =>
                    {
                        var builder = new AlertDialog.Builder(Activity);
                        builder
                            .SetMessage(Resource.String.DatabaseFileMoved);
                        builder.SetPositiveButton(Android.Resource.String.Ok, (sender, args) =>
                                                                              PasswordActivity.Launch(Activity, newIoc, new NullTask()));
                        builder.Show();

                    };




                }
                catch (Exception e)
                {
                    return () =>
                    {
                        Toast.MakeText(Activity, App.Kp2a.GetResourceString(UiStringKey.ErrorOcurred) + " " + e.Message, ToastLength.Long).Show();
                    };
                }



            };

            new SimpleLoadingDialog(Activity, GetString(Resource.String.CopyingFile), false,
                                  copyAndReturnPostExecute
                ).Execute();

        }

        private IOConnectionInfo ImportFileToInternalDirectory(IOConnectionInfo sourceIoc)
        {
            string targetPath = UrlUtil.GetFileName(sourceIoc.Path);
            targetPath = targetPath.Trim("|\\?*<\":>+[]/'".ToCharArray());
            if (targetPath == "")
                targetPath = "imported";
            if (new File(Activity.FilesDir, targetPath).Exists())
            {
                int c = 1;
                var ext = UrlUtil.GetExtension(targetPath);
                var filenameWithoutExt = UrlUtil.StripExtension(targetPath);
                do
                {
                    c++;
                    targetPath = filenameWithoutExt + c;
                    if (!String.IsNullOrEmpty(ext))
                        targetPath += "." + ext;
                } while (new File(Activity.FilesDir, targetPath).Exists());
            }
            var targetIoc = IOConnectionInfo.FromPath(new File(Activity.FilesDir, targetPath).CanonicalPath);

            IoUtil.Copy(targetIoc, sourceIoc, App.Kp2a);
            return targetIoc;
        }


        private void SetRounds(Database db, Preference rounds)
        {
            rounds.Summary = db.KpDatabase.KeyEncryptionRounds.ToString(CultureInfo.InvariantCulture);
        }

        private void SetAlgorithm(Database db, Preference algorithm)
        {
            algorithm.Summary = CipherPool.GlobalPool.GetCipher(db.KpDatabase.DataCipherUuid).DisplayName;
        }

        public void PrepareNoDonatePreference(Context ctx, Preference preference)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(ctx);

            long usageCount = prefs.GetLong(ctx.GetString(Resource.String.UsageCount_key), 0);

#if DEBUG
            preference.Enabled = (usageCount > 1);
#else 
			preference.Enabled = (usageCount > 50);
#endif
            preference.PreferenceChange += delegate(object sender, Preference.PreferenceChangeEventArgs args)
            {
                if ((bool)args.NewValue)
                {
                    new AlertDialog.Builder(ctx)
                        .SetTitle(ctx.GetString(AppNames.AppNameResource))
                        .SetCancelable(false)
                        .SetPositiveButton(Android.Resource.String.Ok, delegate(object o, DialogClickEventArgs eventArgs)
                        {
                            Util.GotoDonateUrl(ctx);
                            ((Dialog)o).Dismiss();
                        })
                        .SetMessage(Resource.String.NoDonateOption_question)
                        .Create().Show();

                }
            };

        }

        
    }
	/// <summary>
	/// Activity to configure the application and database settings. The database must be unlocked, and this activity will close if it becomes locked.
	/// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/MyTheme")]			
	public class DatabaseSettingsActivity : LockCloseActivity 
	{

		public DatabaseSettingsActivity()
		{
			
		}

		public static void Launch(Activity ctx)
		{
			ctx.StartActivity(new Intent(ctx, typeof(DatabaseSettingsActivity)));
		}

	    protected override void OnCreate(Bundle savedInstanceState)
	    {
	        base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.preference);

            SetSupportActionBar(FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.mytoolbar));

	    }

	}
}

