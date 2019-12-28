namespace AppxInstaller.Classes
{
    public enum AppxInstallerState
    {
        NOT_STARTED,
        INSTALLING_CERTIFICATE,
        INSTALLING_APPX,
        ERROR,
        SUCCESS,
        CANCELED
    }
}
