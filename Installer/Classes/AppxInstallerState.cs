﻿namespace Installer.Classes
{
    public enum AppxInstallerState
    {
        NOT_STARTED,
        INSTALLING_CERTIFICATE,
        INSTALLING_APPX,
        UPDATING_APPX,
        ERROR,
        SUCCESS,
        CANCELED
    }
}
