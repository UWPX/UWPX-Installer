using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Installer.Classes;
using Installer.Classes.Events;
using Windows.System;

namespace UWPX_Installer
{
    public partial class MainWindow: Window
    {
        //--------------------------------------------------------Attributes:-----------------------------------------------------------------\\
        #region --Attributes--
        private static string UWPX_CERTIFICATE_PATH = GetResourcePath("Resources/UWPX.cer");
        private static string UWPX_APPX_BUNDLE_PATH = GetResourcePath("Resources/UWPX.appxbundle");
        private const string UWPX_FAMILY_NAME = "790FabianSauter.UWPXAlpha_s1c5dt7qckd0e";

        private AppxInstaller installer;

        #endregion
        //--------------------------------------------------------Constructor:----------------------------------------------------------------\\
        #region --Constructors--
        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion
        //--------------------------------------------------------Set-, Get- Methods:---------------------------------------------------------\\
        #region --Set-, Get- Methods--
        private static string GetResourcePath(string filePath)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), filePath);
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
            installer.Install();
        }

        private void PrepInstaller()
        {
            install_btn.IsEnabled = false;
            installer = new AppxInstaller(UWPX_APPX_BUNDLE_PATH, UWPX_CERTIFICATE_PATH);
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
                TargetApplicationPackageFamilyName = UWPX_FAMILY_NAME
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

        #endregion
    }
}
