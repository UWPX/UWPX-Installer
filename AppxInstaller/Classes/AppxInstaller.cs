using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AppxInstaller.Classes.Events;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace AppxInstaller.Classes
{
    public class AppxInstaller: IDisposable
    {
        //--------------------------------------------------------Attributes:-----------------------------------------------------------------\\
        #region --Attributes--
        private readonly string APPX_PATH;
        private readonly string CERT_PATH;

        public AppxInstallerState state { get; private set; }
        private IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> operation;

        public delegate void ProgressChangedHandler(AppxInstaller sender, ProgressChangedEventArgs args);
        public delegate void InstallationCompleteHandler(AppxInstaller sender, InstallationCompleteEventArgs args);
        public delegate void StateChangedHandler(AppxInstaller sender, StateChangedEventArgs args);

        public event ProgressChangedHandler ProgressChanged;
        public event InstallationCompleteHandler InstallationComplete;
        public event StateChangedHandler StateChanged;

        #endregion
        //--------------------------------------------------------Constructor:----------------------------------------------------------------\\
        #region --Constructors--
        public AppxInstaller(string appxPath, string certPath)
        {
            APPX_PATH = appxPath;
            CERT_PATH = certPath;
            state = AppxInstallerState.NOT_STARTED;
        }

        #endregion
        //--------------------------------------------------------Set-, Get- Methods:---------------------------------------------------------\\
        #region --Set-, Get- Methods--
        private void SetState(AppxInstallerState newState, Exception e = null)
        {
            if (newState != state)
            {
                state = newState;
                StateChanged?.Invoke(this, new StateChangedEventArgs(newState, e));
            }
        }

        #endregion
        //--------------------------------------------------------Misc Methods:---------------------------------------------------------------\\
        #region --Misc Methods (Public)--
        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            state = AppxInstallerState.NOT_STARTED;
            Task.Run(async () =>
            {
                try
                {
                    if (state != AppxInstallerState.CANCELED)
                    {
                        InstallCertificate();
                    }
                    if (state != AppxInstallerState.CANCELED)
                    {
                        await InstallAppxAsync();
                    }
                }
                catch (Exception e)
                {
                    InstallationComplete?.Invoke(this, new InstallationCompleteEventArgs(null));
                    SetState(AppxInstallerState.ERROR, e);
                    return;
                }
                SetState(AppxInstallerState.SUCCESS);
            });
        }

        public void Stop()
        {
            SetState(AppxInstallerState.CANCELED);
            if (!(operation is null) && operation.Status == AsyncStatus.Started)
            {
                operation.Cancel();
            }
        }
        #endregion

        #region --Misc Methods (Private)--
        private void InstallCertificate()
        {
            SetState(AppxInstallerState.INSTALLING_CERTIFICATE);
            using (X509Store store = new X509Store(StoreName.AuthRoot, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                using (X509Certificate2 cert = new X509Certificate2(CERT_PATH))
                {
                    store.Add(cert);
                }
            }
        }

        /// <summary>
        /// Based on: https://docs.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.addpackageasync#Windows_Management_Deployment_PackageManager_AddPackageAsync_Windows_Foundation_Uri_Windows_Foundation_Collections_IIterable_Windows_Foundation_Uri__Windows_Management_Deployment_DeploymentOptions_
        /// </summary>
        private async Task InstallAppxAsync()
        {
            SetState(AppxInstallerState.INSTALLING_APPX);
            // How to add a reference for the PackageManger is taken from: https://stackoverflow.com/questions/54454214/how-to-access-windows-management-deployment-namespace-in-a-desktop-project-in-vs/54455263
            PackageManager pkgManager = new PackageManager();
            operation = pkgManager.AddPackageAsync(new Uri(APPX_PATH), null, DeploymentOptions.ForceTargetApplicationShutdown);

            operation.Progress = (op, progress) => ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(progress));

            DeploymentResult result = await operation;
            InstallationComplete?.Invoke(this, new InstallationCompleteEventArgs(result));
        }

        #endregion

        #region --Misc Methods (Protected)--


        #endregion
        //--------------------------------------------------------Events:---------------------------------------------------------------------\\
        #region --Events--


        #endregion
    }
}
