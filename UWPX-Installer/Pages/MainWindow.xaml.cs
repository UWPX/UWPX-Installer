using System.IO;
using System.Reflection;
using System.Windows;
using Installer.Classes;
using Installer.Classes.Events;

namespace UWPX_Installer
{
    public partial class MainWindow: Window
    {
        //--------------------------------------------------------Attributes:-----------------------------------------------------------------\\
        #region --Attributes--
        private static string UWPX_CERTIFICATE_PATH = GetResourcePath("Resources/UWPX.cer");
        private static string UWPX_APPX_BUNDLE_PATH = GetResourcePath("Resources/UWPX.appxbundle");

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

        private void OnInstallStateChanged(AppxInstaller sender, StateChangedEventArgs args)
        {
            if (args.STATE == AppxInstallerState.ERROR)
            {
                UpdateProgressInvoke(100, "Installation failed with a fatal error: " + (args.EXCEPTION is null ? "null" : args.EXCEPTION.Message));
                Dispatcher.Invoke(() => install_btn.IsEnabled = true);
            }
        }

        private void OnInstallProgressChanged(AppxInstaller sender, ProgressChangedEventArgs args)
        {
            UpdateProgressInvoke(args.PROGRESS.percentage * 100, args.PROGRESS.state.ToString());
        }

        #endregion
    }
}
