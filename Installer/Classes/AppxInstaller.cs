using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Installer.Classes.Events;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace Installer.Classes
{
    public class AppxInstaller: IDisposable
    {
        //--------------------------------------------------------Attributes:-----------------------------------------------------------------\\
        #region --Attributes--
        private readonly string APPX_BUNDLE_PATH;
        private readonly string CERT_PATH;
        private readonly string DEPENDENCIES_PATH;

        public AppxInstallerState state { get; private set; }
        private PackageManager pkgManager;
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
        public AppxInstaller(string appxBundlePath, string certPath, string dependenciesPath)
        {
            APPX_BUNDLE_PATH = appxBundlePath;
            CERT_PATH = certPath;
            DEPENDENCIES_PATH = dependenciesPath;
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

        private string GetArch()
        {
            string arch = Environment.GetEnvironmentVariable("Processor_Architecture");
            if (string.Equals(arch, "x86"))
            {
                if (!(Environment.GetEnvironmentVariable("ProgramFiles(Arm)") is null))
                {
                    arch = "arm64";
                }
                else if (!(Environment.GetEnvironmentVariable("ProgramFiles(x86)") is null))
                {
                    arch = "amd64";
                }
            }
            return arch.ToLowerInvariant();
        }

        private List<Uri> GetDependencies(string arch)
        {
            string archDepPath = Path.Combine(DEPENDENCIES_PATH, arch);
            if (!Directory.Exists(archDepPath))
            {
                return new List<Uri>();
            }

            List<Uri> dependencies = new List<Uri>();
            // Dependencies can have the .appx and .msix extension:
            dependencies.AddRange(Directory.GetFiles(archDepPath, @"*.appx", SearchOption.AllDirectories).Select(x => new Uri(x)));
            dependencies.AddRange(Directory.GetFiles(archDepPath, @"*.msix", SearchOption.AllDirectories).Select(x => new Uri(x)));
            return dependencies;
        }

        /// <summary>
        /// Based on the "Add-AppDevPackage.ps1" that generates when you publish an app with target sideloading.
        /// </summary>
        private List<Uri> GetDependencies()
        {
            List<Uri> dependencies = new List<Uri>();
            string arch = GetArch();
            if (string.Equals(arch, "x86") || string.Equals(arch, "amd64") || string.Equals(arch, "arm64"))
            {
                dependencies.AddRange(GetDependencies("x86"));
            }
            if (string.Equals(arch, "amd64"))
            {
                dependencies.AddRange(GetDependencies("x64"));
            }
            if (string.Equals(arch, "arm") || string.Equals(arch, "arm64"))
            {
                dependencies.AddRange(GetDependencies("arm"));
            }
            if (string.Equals(arch, "arm64"))
            {
                dependencies.AddRange(GetDependencies("arm64"));
            }

            return dependencies;
        }

        #endregion
        //--------------------------------------------------------Misc Methods:---------------------------------------------------------------\\
        #region --Misc Methods (Public)--
        public void Dispose()
        {
            Stop();
        }

        public void Install()
        {
            state = AppxInstallerState.NOT_STARTED;
            Task.Run(() =>
            {
                try
                {
                    if (state != AppxInstallerState.CANCELED)
                    {
                        InstallCertificate();
                    }
                    if (state != AppxInstallerState.CANCELED)
                    {
                        InstallAppx();
                    }
                }
                catch (Exception e)
                {
                    InstallationComplete?.Invoke(this, new InstallationCompleteEventArgs(null));
                    SetState(AppxInstallerState.ERROR, e);
                    return;
                }
            });
        }

        public void Update()
        {
            state = AppxInstallerState.NOT_STARTED;
            Task.Run(() =>
            {
                try
                {
                    if (state != AppxInstallerState.CANCELED)
                    {
                        // In case the certificate changed:
                        InstallCertificate();
                    }
                    if (state != AppxInstallerState.CANCELED)
                    {
                        UpdateAppx();
                    }
                }
                catch (Exception e)
                {
                    InstallationComplete?.Invoke(this, new InstallationCompleteEventArgs(null));
                    SetState(AppxInstallerState.ERROR, e);
                    return;
                }
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
        private void InstallAppx()
        {
            SetState(AppxInstallerState.INSTALLING_APPX);
            // How to add a reference for the PackageManger is taken from: https://stackoverflow.com/questions/54454214/how-to-access-windows-management-deployment-namespace-in-a-desktop-project-in-vs/54455263
            if (pkgManager is null)
            {
                pkgManager = new PackageManager();
            }
            operation = pkgManager.AddPackageAsync(new Uri(APPX_BUNDLE_PATH), GetDependencies(), DeploymentOptions.ForceTargetApplicationShutdown);
            HandleOperation();
        }

        /// <summary>
        /// Based on: https://docs.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.addpackageasync#Windows_Management_Deployment_PackageManager_AddPackageAsync_Windows_Foundation_Uri_Windows_Foundation_Collections_IIterable_Windows_Foundation_Uri__Windows_Management_Deployment_DeploymentOptions_
        /// </summary>
        private void UpdateAppx()
        {
            SetState(AppxInstallerState.UPDATING_APPX);
            // How to add a reference for the PackageManger is taken from: https://stackoverflow.com/questions/54454214/how-to-access-windows-management-deployment-namespace-in-a-desktop-project-in-vs/54455263
            if (pkgManager is null)
            {
                pkgManager = new PackageManager();
            }
            operation = pkgManager.UpdatePackageAsync(new Uri(APPX_BUNDLE_PATH), GetDependencies(), DeploymentOptions.ForceTargetApplicationShutdown);
            HandleOperation();
        }

        private void HandleOperation()
        {
            operation.Progress = (op, progress) => ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(progress));
            operation.Completed = (op, result) =>
            {
                InstallationComplete?.Invoke(this, new InstallationCompleteEventArgs(op.GetResults()));
                if (op.GetResults().IsRegistered)
                {
                    SetState(AppxInstallerState.SUCCESS);
                }
                else
                {
                    SetState(AppxInstallerState.CANCELED);
                }
            };
        }

        #endregion

        #region --Misc Methods (Protected)--


        #endregion
        //--------------------------------------------------------Events:---------------------------------------------------------------------\\
        #region --Events--


        #endregion
    }
}
