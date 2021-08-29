using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Installer.Classes;
using Installer.Classes.Events;
using Newtonsoft.Json;
using UWPX_Installer.Classes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;
using Windows.System;

namespace UWPX_Installer
{
    public partial class MainWindow: Window
    {
        //--------------------------------------------------------Attributes:-----------------------------------------------------------------\\
        #region --Attributes--
        private static string APPX_INFO_PATH = GetResourcePath("ReleaseInfo.json");
        private ReleaseInfo info;

        private AppxInstaller installer;

        #endregion
        //--------------------------------------------------------Constructor:----------------------------------------------------------------\\
        #region --Constructors--
        public MainWindow()
        {
            InitializeComponent();
            LoadInfo();
            _ = EnableButtonsAsync();
        }

        #endregion
        //--------------------------------------------------------Set-, Get- Methods:---------------------------------------------------------\\
        #region --Set-, Get- Methods--
        private static string GetResourcePath(string filePath)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Resources", filePath);
        }

        #endregion
        //--------------------------------------------------------Misc Methods:---------------------------------------------------------------\\
        #region --Misc Methods (Public)--


        #endregion

        #region --Misc Methods (Private)--
        private void Install()
        {
            PrepInstaller();
            installer.Install();
        }

        private void Update()
        {
            PrepInstaller();
            installer.Update();
        }

        private void PrepInstaller()
        {
            DisableButtons();
            installer = new AppxInstaller(GetResourcePath(info.appxBundlePath), GetResourcePath(info.certPath), GetResourcePath(info.dependenciesPath));
            installer.ProgressChanged += OnInstallProgressChanged;
            installer.StateChanged += OnInstallStateChanged;
            installer.InstallationComplete += OnInstallComplete;
        }

        private void UpdateProgressInvoke(double value, string statusText)
        {
            Dispatcher.Invoke(() =>
            {
                progress_pbar.Value = value;
                status_tbx.Text = statusText;
            });
        }

        private async Task LauchAppAsync()
        {
            UpdateProgressInvoke(100, "Launching app...");
            AppListEntry app = await GetAppByPackageFamilyNameAsync(info.packageFamilyName);
            if (app is null)
            {
                UpdateProgressInvoke(100, "Failed to launch app. App not found.");
            }
            else
            {
                await app.LaunchAsync();
                UpdateProgressInvoke(100, "Done");
            }
        }

        private static async Task<AppListEntry> GetAppByPackageFamilyNameAsync(string packageFamilyName)
        {
            PackageManager pkgManager = new PackageManager();
            Package pkg = pkgManager.FindPackages(packageFamilyName).FirstOrDefault();
            if (pkg is null)
            {
                return null;
            }

            IReadOnlyList<AppListEntry> apps = await pkg.GetAppListEntriesAsync();
            return apps.FirstOrDefault();
        }

        private void LoadInfo()
        {
            using (StreamReader r = new StreamReader(GetResourcePath(APPX_INFO_PATH)))
            {
                info = JsonConvert.DeserializeObject<ReleaseInfo>(r.ReadToEnd());
            }
            releaseDate_run.Text = info.releaseDate;
            version_link.Inlines.Clear();
            version_link.Inlines.Add(new Run(info.version));
            version_link.NavigateUri = new Uri(info.changelogUrl);
        }

        private async Task EnableButtonsAsync()
        {
            install_btn.IsEnabled = true;
            update_btn.IsEnabled = true;
            lauch_btn.IsEnabled = !(await GetAppByPackageFamilyNameAsync(info.packageFamilyName) is null);
        }

        private void DisableButtons()
        {
            install_btn.IsEnabled = false;
            update_btn.IsEnabled = false;
            lauch_btn.IsEnabled = false;
        }

        #endregion

        #region --Misc Methods (Protected)--


        #endregion
        //--------------------------------------------------------Events:---------------------------------------------------------------------\\
        #region --Events--
        private void Install_btn_Click(object sender, RoutedEventArgs e)
        {
            Install();
        }

        private void Update_btn_Click(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private async void OnInstallComplete(AppxInstaller sender, InstallationCompleteEventArgs args)
        {
            string msg;
            if (args.RESULT is null)
            {
                msg = "Installation failed with a fatal error!";
            }
            else if (args.RESULT.IsRegistered)
            {
                msg = "Done";
            }
            else
            {
                msg = "Installation failed with: " + args.RESULT.ErrorText;
            }
            UpdateProgressInvoke(100, msg);
            await Dispatcher.Invoke(async () => await EnableButtonsAsync());
        }

        private async void OnInstallStateChanged(AppxInstaller sender, StateChangedEventArgs args)
        {
            if (args.STATE == AppxInstallerState.ERROR)
            {
                UpdateProgressInvoke(100, "Installation failed with a fatal error: " + (args.EXCEPTION is null ? "null" : args.EXCEPTION.Message));
                await Dispatcher.Invoke(async () => await EnableButtonsAsync());
            }
            else if (args.STATE == AppxInstallerState.SUCCESS && Dispatcher.Invoke(() => startOnceDone_chbx.IsChecked) == true)
            {
                await LauchAppAsync();
            }
        }

        private void OnInstallProgressChanged(AppxInstaller sender, ProgressChangedEventArgs args)
        {
            UpdateProgressInvoke(args.PROGRESS.percentage, args.PROGRESS.state.ToString());
        }

        private async void Lauch_btn_Click(object sender, RoutedEventArgs e)
        {
            await LauchAppAsync();
        }

        private async void twitter_link_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://twitter.com/UWPX_APP"));
        }

        private async void github_link_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/UWPX/UWPX-Client"));
        }

        private async void version_link_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(info.changelogUrl));
        }

        private async void store_btn_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://store.uwpx.org"));
        }

        #endregion
    }
}
