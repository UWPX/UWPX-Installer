using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Installer.Classes;
using Installer.Classes.Events;
using Newtonsoft.Json;
using UWPX_Installer.Classes;
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
            install_btn.IsEnabled = false;
            installer = new AppxInstaller(GetResourcePath(info.appxBundlePath), GetResourcePath(info.certPath));
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

        private async Task LauchUwpxAsync()
        {
            UpdateProgressInvoke(100, "Launching UWPX...");
            LauncherOptions options = new LauncherOptions
            {
                TargetApplicationPackageFamilyName = info.appFamilyName
            };
            if (await Launcher.LaunchUriAsync(new Uri("xmpp:"), options))
            {
                UpdateProgressInvoke(100, "Done");
            }
            else
            {
                UpdateProgressInvoke(100, "Failed to launch UWPX");
            }
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

        private void OnInstallComplete(AppxInstaller sender, InstallationCompleteEventArgs args)
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
            Dispatcher.Invoke(() => install_btn.IsEnabled = true);
        }

        private async void OnInstallStateChanged(AppxInstaller sender, StateChangedEventArgs args)
        {
            if (args.STATE == AppxInstallerState.ERROR)
            {
                UpdateProgressInvoke(100, "Installation failed with a fatal error: " + (args.EXCEPTION is null ? "null" : args.EXCEPTION.Message));
                Dispatcher.Invoke(() => install_btn.IsEnabled = true);
            }
            else if (args.STATE == AppxInstallerState.SUCCESS && Dispatcher.Invoke(() => startOnceDone_chbx.IsChecked) == true)
            {
                await LauchUwpxAsync();
            }
        }

        private void OnInstallProgressChanged(AppxInstaller sender, ProgressChangedEventArgs args)
        {
            UpdateProgressInvoke(args.PROGRESS.percentage, args.PROGRESS.state.ToString());
        }

        private async void Lauch_btn_Click(object sender, RoutedEventArgs e)
        {
            await LauchUwpxAsync();
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

        #endregion
    }
}
