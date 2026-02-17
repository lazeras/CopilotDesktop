# WebView2 Runtime Deployment with MSIX - Complete Guide

## ⚠️ Current Issue
The `uap10:ExternalDependency` approach in Package.appxmanifest is **not valid**. The schema doesn't allow `ExternalDependency` as a direct child of `Dependencies`.

## ✅ Correct Solutions for WebView2 Deployment

### **Solution 1: Use .appinstaller File (Recommended)**

Create a file named `CopilotDesktop.appinstaller` in your project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller 
    xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"
    Version="1.0.0.0" 
    Uri="https://yourserver.com/CopilotDesktop.appinstaller">
    
  <MainBundle 
      Name="d2f5f4e5-d477-4cf4-91f6-d30c4153bc2a"
      Publisher="CN=Kaoses"
      Version="1.0.0.0"
      Uri="https://yourserver.com/CopilotDesktop.msix" />
      
  <Dependencies>
    <!-- WebView2 Runtime Dependency -->
    <Package 
        Name="Microsoft.WebView2" 
        Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US"
        Uri="https://go.microsoft.com/fwlink/?linkid=2196064"
        ProcessorArchitecture="x64" />
  </Dependencies>
  
  <UpdateSettings>
    <OnLaunch 
        HoursBetweenUpdateChecks="0"
        UpdateBlocksActivation="true"
        ShowPrompt="true" />
  </UpdateSettings>
  
</AppInstaller>
```

**Benefits:**
- ✅ WebView2 automatically installed with your app
- ✅ Automatic updates for both app and WebView2
- ✅ Works with Microsoft Store and web deployment
- ✅ Users can install via single link

**Deployment URL:**
- Users navigate to: `ms-appinstaller:?source=https://yourserver.com/CopilotDesktop.appinstaller`

---

### **Solution 2: Package.appxmanifest Only (Remove WebView2 Dependency)**

**Current Fix:** Remove the invalid `uap10:ExternalDependency` from your manifest.

Your `Package.appxmanifest` should look like this:

```xml
<Dependencies>
  <TargetDeviceFamily Name="Windows.Universal" MinVersion="10.0.17763.0" MaxVersionTested="10.0.19041.0" />
  <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.19041.0" />
</Dependencies>
```

**Then handle WebView2 installation in code:**

Update your `App.xaml.cs` constructor:

```csharp
public App()
{
    InitializeComponent();
    
    // Check and prompt for WebView2 Runtime
    EnsureWebView2RuntimeInstalled();

    Host = Microsoft.Extensions.Hosting.Host.
    // ... rest of code
}

private async void EnsureWebView2RuntimeInstalled()
{
    try
    {
        var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        // WebView2 is installed
    }
    catch
    {
        // WebView2 not installed - prompt user
        var dialog = new ContentDialog
        {
            Title = "WebView2 Runtime Required",
            Content = "This application requires Microsoft Edge WebView2 Runtime. Would you like to download it now?",
            PrimaryButtonText = "Download",
            CloseButtonText = "Cancel",
            XamlRoot = MainWindow.Content.XamlRoot
        };
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await Windows.System.Launcher.LaunchUriAsync(
                new Uri("https://go.microsoft.com/fwlink/?linkid=2124701"));
        }
    }
}
```

---

### **Solution 3: Bundle WebView2 with Your App (Evergreen Standalone)**

Add the WebView2 Runtime installer to your MSIX package.

1. **Download WebView2 Runtime Standalone Installer:**
   - [WebView2 Runtime Installer](https://developer.microsoft.com/microsoft-edge/webview2/#download-section)

2. **Add to your project:**
   - Create folder: `CopilotDesktop\Assets\WebView2`
   - Add installer: `MicrosoftEdgeWebview2Setup.exe`
   - Set Build Action: `Content`
   - Copy to Output: `Always`

3. **Install on first run:**

```csharp
private async Task InstallWebView2Runtime()
{
    var installerPath = Path.Combine(AppContext.BaseDirectory, "Assets", "WebView2", "MicrosoftEdgeWebview2Setup.exe");
    
    if (File.Exists(installerPath))
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/silent /install",
                UseShellExecute = true,
                Verb = "runas" // Run as administrator
            }
        };
        
        process.Start();
        await process.WaitForExitAsync();
    }
}
```

---

## 📋 Recommended Approach

**For Distribution via Web/Sideloading:**
→ Use **Solution 1** (.appinstaller file)

**For Microsoft Store:**
→ Use **Solution 2** (remove dependency, check at runtime)

**For Enterprise/Offline:**
→ Use **Solution 3** (bundle installer)

---

## 🔧 To Fix Your Current Manifest Error

**Immediate Action Required:**

1. Open `CopilotDesktop\Package.appxmanifest`
2. Remove these lines (lines 31-34):
```xml
<!-- WebView2 Runtime Dependency -->
<uap10:ExternalDependency Name="Microsoft.WebView2"
                           Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US"
                           MinVersion="0.0.0.0" />
```

3. Remove the `uap10` namespace if not used elsewhere:
```xml
xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10"
```

4. Remove `uap10` from IgnorableNamespaces:
```xml
IgnorableNamespaces="uap rescap genTemplate"
```

---

## 📚 References

- [WebView2 Distribution Guide](https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution)
- [App Installer File Schema](https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview)
- [MSIX Packaging Documentation](https://learn.microsoft.com/windows/msix/)

---

## ✅ Next Steps

1. Fix the manifest error by removing invalid ExternalDependency
2. Choose one of the three solutions above
3. Implement the chosen solution
4. Test deployment

Let me know which solution you'd like to implement!
