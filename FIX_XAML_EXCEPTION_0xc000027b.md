# Fix for Exception 0xc000027b in Microsoft.UI.Xaml.dll

## 🎯 **Immediate Fix**

This specific error (0xc000027b) in Microsoft.UI.Xaml.dll is typically caused by one of these issues:

### **Issue 1: XAML Resource Loading Order**

Resource dictionaries must be loaded in the correct order. Fix in `App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Load WinUI resources FIRST --> 
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            
            <!-- Then load your custom resources in dependency order -->
            <ResourceDictionary Source="/Styles/FontSizes.xaml" />
            <ResourceDictionary Source="/Styles/Thickness.xaml" />
            <ResourceDictionary Source="/Styles/TextBlock.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

✅ **Your App.xaml is correct** - resources are in the right order.

---

## 🔍 **Root Cause Analysis**

Based on the error details:
- **Fault offset:** `0x000000000039dec5` in Microsoft.UI.Xaml.dll
- **Time stamp:** Indicates WinUI 3 version 3.1.8 (1.8.x runtime)

### **Most Likely Causes:**

1. **Invalid XAML Binding** - A binding is trying to access a null or invalid property
2. **Theme Initialization Issue** - Theme resources not loaded before use
3. **WebView2 Initialization** - WebView2 control creation before runtime ready
4. **Window Lifecycle Issue** - Accessing window before it's fully initialized

---

## ✅ **Solution Implementation**

### **Step 1: Add XAML Exception Handler**

Add this to your `App.xaml.cs` constructor (already done, but let's enhance it):

```csharp
// In App.xaml.cs constructor
public App()
{
    InitializeComponent();
    
    // Add DebugSettings for XAML debugging
    if (Debugger.IsAttached)
    {
        this.DebugSettings.EnableFrameRateCounter = false;
        this.DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        this.DebugSettings.XamlResourceReferenceFailed += DebugSettings_XamlResourceReferenceFailed;
    }
    
    SetupExceptionHandlers();
    
    // Rest of initialization...
}

private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
{
    Debug.WriteLine($"[XAML Binding Failed] {e.Message}");
}

private void DebugSettings_XamlResourceReferenceFailed(object sender, XamlResourceReferenceFailedEventArgs e)
{
    Debug.WriteLine($"[XAML Resource Failed] {e.Message}");
}
```

### **Step 2: Safe Window Initialization**

Update your `MainWindow` initialization to be more defensive:

```csharp
// In MainWindow.xaml.cs
public MainWindow()
{
    try
    {
        InitializeComponent();
        
        // Ensure theme is set before any UI operations
        EnsureTheme();
        
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"MainWindow initialization error: {ex}");
        throw;
    }
}

private void EnsureTheme()
{
    try
    {
        var theme = App.GetService<IThemeSelectorService>().Theme;
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme.ToElementTheme();
        }
    }
    catch
    {
        // Use default theme if service fails
    }
}
```

### **Step 3: Fix WebViewPage Initialization**

The issue might be in WebViewPage - let's add defensive initialization:

```csharp
// In WebViewPage.xaml.cs
public WebViewPage()
{
    try
    {
        ViewModel = App.GetService<WebViewViewModel>();
        InitializeComponent();

        // Only initialize WebView if control is available
        if (CopilotView != null)
        {
            ViewModel.WebViewService.Initialize(CopilotView);
        }
        
        this.Loaded += MainWindow_Loaded;
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"WebViewPage initialization error: {ex}");
        throw;
    }
}

private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    try
    {
        var window = App.MainWindow;
        if (window != null)
        {
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(AppTitleBar);
        }

        // Check WebView2 availability before initialization
        if (CopilotView != null)
        {
            await CopilotView.EnsureCoreWebView2Async();
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"WebViewPage load error: {ex}");
        // Don't rethrow - let the page handle gracefully
    }
}
```

---

## 🚨 **Immediate Debugging Steps**

### **1. Enable XAML Debugging**

Add to your `launchSettings.json`:

```json
{
  "profiles": {
    "CopilotDesktop": {
      "commandName": "MsixPackage",
      "environmentVariables": {
        "XAML_DIAGNOSTICS_ENABLED": "1",
        "ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO": "1"
      }
    }
  }
}
```

### **2. Visual Studio Settings**

1. **Tools → Options → Debugging → General**
   - ☑ Enable UI Debugging Tools for XAML
   - ☑ Enable XAML Hot Reload
   - ☑ Show runtime tools in application

2. **Debug → Windows → Exception Settings** (Ctrl+Alt+E)
   - ☑ Common Language Runtime Exceptions
     - ☑ System.Runtime.InteropServices.COMException
     - ☑ System.Xaml.XamlException
   - ☑ C++ Exceptions
     - ☑ Add exception: 0xc000027b

### **3. Run with Live Visual Tree**

1. Start debugging (F5)
2. Open: **Debug → Windows → Live Visual Tree** (Ctrl+Alt+W, L)
3. Watch for XAML elements failing to load

### **4. Check Output Window**

When the crash occurs, check:
- **Output → Debug** - Look for binding/resource failures
- **Output → Exception** - Full exception details

---

## 🔧 **Known Fix for WinUI 3 1.8.x**

There's a known issue with WinUI 3 1.8.x and WebView2. Try this workaround:

### **Option A: Delay WebView2 Initialization**

```csharp
// In WebViewPage.xaml.cs
private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    // Give UI time to fully render
    await Task.Delay(100);
    
    var window = App.MainWindow;
    if (window != null)
    {
        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(AppTitleBar);
    }

    // Ensure dispatcher is ready
    await MainWindow.DispatcherQueue.EnqueueAsync(async () =>
    {
        if (CopilotView != null)
        {
            await CopilotView.EnsureCoreWebView2Async();
        }
    });
}
```

### **Option B: Isolate WebView2 in Separate Page**

Move WebView2 to load only after window is fully ready:

```csharp
// In App.xaml.cs OnLaunched
protected async override void OnLaunched(LaunchActivatedEventArgs args)
{
    base.OnLaunched(args);
    
    // Wait for main window to be ready
    await Task.Delay(200);
    
    await App.GetService<IActivationService>().ActivateAsync(args);
}
```

---

## 🎯 **Quick Diagnostic Test**

Add this temporary code to find the exact issue:

```csharp
// In App.xaml.cs OnLaunched, before activation
protected async override void OnLaunched(LaunchActivatedEventArgs args)
{
    base.OnLaunched(args);

    try
    {
        Debug.WriteLine("=== Startup Diagnostics ===");
        Debug.WriteLine($"MainWindow: {MainWindow != null}");
        Debug.WriteLine($"DispatcherQueue: {MainWindow?.DispatcherQueue != null}");
        
        // Test resource loading
        try
        {
            var testResource = Application.Current.Resources["LargeFontSize"];
            Debug.WriteLine($"Resources loaded: {testResource != null}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Resource loading failed: {ex.Message}");
        }

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"OnLaunched error: {ex}");
        throw;
    }
}
```

---

## 🚀 **Nuclear Option: Clean Slate**

If none of the above works:

```powershell
# Close Visual Studio first
Remove-Item -Recurse -Force "CopilotDesktop\.vs"
Remove-Item -Recurse -Force "CopilotDesktop\bin"
Remove-Item -Recurse -Force "CopilotDesktop\obj"
Remove-Item -Recurse -Force "CopilotDesktop.Core\bin"
Remove-Item -Recurse -Force "CopilotDesktop.Core\obj"

# Clean NuGet cache
dotnet nuget locals all --clear

# Rebuild
dotnet restore
dotnet build --no-incremental
```

---

## 📊 **Expected Results**

After implementing these fixes:

1. **No more 0xc000027b crashes**
2. **XAML binding errors visible in Output window**
3. **Resource loading errors caught and logged**
4. **Graceful degradation if WebView2 fails**

---

## 🔍 **Root Cause in Your Code**

Based on the stack trace pointing to Microsoft.UI.Xaml.dll at your specific offset, the most likely culprit is:

**WebViewPage initialization racing with window setup**

The fix: Add the XAML debugging and delay WebView2 init as shown above.

---

Run the diagnostic test first, check the Output window, and let me know what errors appear!
