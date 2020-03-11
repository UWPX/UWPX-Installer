# UWPX-Installer
This project contains the sourcecode for the [UWPX](http://git.uwpx.org).  
It should make it easier to [sideload](https://docs.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps#sideload-your-app-package) UWP apps by performing a one click installation.

## Why
This installer acts as an easy way to install UWPX and the required certificate. Since by default for UWP apps it's required to install their certificate first if you want to sideload them.  
Installing a certificate is to complex for the normal user. This installer automates this process.

## How to use
### Developer
* Install Visual Studio 2019 with .net Core and WPF for C#
* Head over to [Resources](UWPX-Installer/Resources/README.md)

### User
* Head over to [Releases](https://github.com/UWPX/UWPX-Client/releases) and download a copy of the installer for the latest UWPX release.  
* Double click to execute.
* **Ignore the warning from Windows telling you this programm is not save.**
* Click on `Install` or `Update`.
* Done

## Why is Windows warning me when I try to run this installer
Since I do not own an official certificate which I can use to sign this installer, I had to create my own.  
Windows is warning you, that they are not able validate me as a developer.

## Examples
![Example1](https://user-images.githubusercontent.com/11741404/76404128-4570a880-6386-11ea-9b00-5d486788addc.png)
![Example2](https://user-images.githubusercontent.com/11741404/71642510-9ee49900-2cac-11ea-987c-f2057cb47d93.gif)
