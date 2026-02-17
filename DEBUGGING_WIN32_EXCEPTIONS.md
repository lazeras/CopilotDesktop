# Debugging Win32 Exceptions in WinUI 3

## 🐛 Common Win32 Exception Causes

### **"An unhandled Win32 exception occurred in [PID] CopilotDesktop.exe"**

This error typically occurs due to:

1. **WebView2 Runtime Issues**
   - WebView2 not installed
   - WebView2 initialization failure
   - Version mismatch

2. **Window Handle Issues**
   - Invalid HWND
   - Window destroyed prematurely
   - Cross-thread access

3. **COM/WinRT Interop Issues**
   - COM initialization failure
   - RPC errors
   - Threading apartment issues

4. **Resource Issues**
   - Missing XAML resources
   - Invalid image paths
   - Font loading failures

---

## ✅ Solutions Implemented

### **1. Comprehensive Exception Handling**

The app now catches exceptions from multiple sources:

- ✅ **UnhandledException** - WinUI exceptions
- ✅ **AppDomain.UnhandledException** - Background thread exceptions
- ✅ **TaskScheduler.UnobservedTaskException** - Async Task exceptions
- ✅ **FirstChanceException** - Debugging (when debugger attached)

### **2. Detailed Logging**

Exceptions are logged with:
- Exception type and message
- HRESULT error code (Win32 error)
- Full stack trace
- Inner exceptions
- Timestamp

**Log Location:** `%LocalAppData%\CopilotDesktop\Logs\errors_YYYY-MM-DD.log`

### **3. User-Friendly Error Dialogs**

When not debugging, users see a friendly error dialog instead of crash.

---

## 🔧 Visual Studio Configuration

### **Step 1: Exception Settings**

1. **Open Exception Settings:**
   - Menu: `Debug` → `Windows` → `Exception Settings`
   - Keyboard: `Ctrl+Alt+E`

2. **Configure Common Language Runtime Exceptions:**
   ```
   ☐ Common Language Runtime Exceptions
     ☑ System.AccessViolationException
     ☑ System.ComponentModel.Win32Exception
     ☑ System.Runtime.InteropServices.COMException
     ☑ System.Runtime.InteropServices.SEHException
   ```

3. **Enable Break on Win32 Exceptions:**
   ```
   ☑ Win32 Exceptions
     ☑ 0xC0000005 (Access Violation)
     ☑ 0x80004005 (E_FAIL)
     ☑ 0x8000FFFF (E_UNEXPECTED)
   ```

### **Step 2: Enable Native Code Debugging**

1. **Project Properties:**
   - Right-click `CopilotDesktop` → `Properties`

2. **Debug Settings:**
   - Debug tab
   - Check: **Enable native code debugging**
   - Check: **Enable unmanaged code debugging**

### **Step 3: Configure Just My Code**

1. **Tools → Options → Debugging → General**
   - ☑ Enable Just My Code (for cleaner debugging)
   - ☑ Enable source server support
   - ☑ Enable source link support

### **Step 4: Output Window Configuration**

1. **View → Output** (`Ctrl+Alt+O`)
2. **Show output from:** `Debug`
3. All exception details will appear here

---

## 📋 Debugging Checklist

When you get a Win32 exception, check:

### **1. WebView2 Status**
```powershell
# Check if WebView2 is installed
Get-AppxPackage -Name "*WebView2*"

# Check registry
Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
```

### **2. Application Logs**
- Check: `%LocalAppData%\CopilotDesktop\Logs\errors_*.log`
- Look for HRESULT codes

### **3. Event Viewer**
- Run: `eventvwr.msc`
- Navigate: `Windows Logs` → `Application`
- Filter by Source: `CopilotDesktop`

### **4. Debug Output**
- View → Output window
- Look for `[ERROR]` or `[FirstChance]` messages

---

## 🔍 Common Error Codes

| HRESULT | Name | Description | Solution |
|---------|------|-------------|----------|
| `0x80004005` | E_FAIL | Unspecified error | Check logs for details |
| `0x80070057` | E_INVALIDARG | Invalid argument | Check parameter values |
| `0x8000FFFF` | E_UNEXPECTED | Catastrophic failure | Restart app |
| `0x80070005` | E_ACCESSDENIED | Access denied | Check permissions |
| `0xC0000005` | ACCESS_VIOLATION | Invalid memory access | Native code issue |
| `0x8007000E` | E_OUTOFMEMORY | Out of memory | Reduce memory usage |

---

## 🚀 Testing Exception Handling

Add this test method to verify exception handling works:

```csharp
// In App.xaml.cs
#if DEBUG
private void TestExceptionHandling()
{
    // Test WinUI exception
    MainWindow.DispatcherQueue.TryEnqueue(() =>
    {
        throw new InvalidOperationException("Test WinUI exception");
    });

    // Test background thread exception
    Task.Run(() =>
    {
        throw new Exception("Test background exception");
    });

    // Test Win32 exception
    Marshal.ThrowExceptionForHR(unchecked((int)0x80004005));
}
#endif
```

---

## 📊 Exception Analysis

### **Analyze Log Files**

```powershell
# Get recent errors
Get-Content "$env:LOCALAPPDATA\CopilotDesktop\Logs\errors_*.log" -Tail 50

# Count errors by type
Select-String -Path "$env:LOCALAPPDATA\CopilotDesktop\Logs\*.log" -Pattern "Exception Type:" | 
  ForEach-Object { ($_ -split ': ')[1] } | 
  Group-Object | 
  Sort-Object Count -Descending
```

---

## 🔧 Additional Debugging Tools

### **1. WinDbg (Advanced)**

For native debugging:
```
windbg -pn CopilotDesktop.exe
```

### **2. PerfView**

For performance and exception analysis:
```
PerfView /OnlyProviders=*Microsoft-Windows-DotNETRuntime:0x8000:4 collect
```

### **3. Process Monitor (Sysinternals)**

To capture file/registry access issues:
```
procmon.exe /AcceptEula /LoadConfig CopilotDesktop.pmc
```

---

## ✅ Prevention Best Practices

1. **Always check WebView2 before use:**
   ```csharp
   await CopilotView.EnsureCoreWebView2Async();
   ```

2. **Use DispatcherQueue for UI updates:**
   ```csharp
   MainWindow.DispatcherQueue.TryEnqueue(() => { /* UI code */ });
   ```

3. **Handle COM exceptions:**
   ```csharp
   try
   {
       // COM operation
   }
   catch (COMException ex) when (ex.HResult == unchecked((int)0x80004005))
   {
       // Handle specific error
   }
   ```

4. **Validate HWNDs:**
   ```csharp
   if (hwnd != IntPtr.Zero)
   {
       // Use handle
   }
   ```

---

## 📞 Getting Help

If exceptions persist:

1. **Check logs:** `%LocalAppData%\CopilotDesktop\Logs`
2. **Enable First-Chance exceptions** in debugger
3. **Use Output window** to see exception details
4. **Check Event Viewer** for system-level errors
5. **Verify WebView2** is installed and up to date

---

## 🎯 Quick Fix for Your Current Issue

Based on your error, try these immediate steps:

### **Option 1: Check WebView2**
```powershell
# Install/Update WebView2
winget install --id Microsoft.EdgeWebView2Runtime
```

### **Option 2: Clean and Rebuild**
```powershell
# From solution directory
dotnet clean
Remove-Item -Recurse -Force .\CopilotDesktop\bin,.\CopilotDesktop\obj
dotnet build
```

### **Option 3: Reset Debug Settings**
1. Delete: `CopilotDesktop\.vs` folder
2. Close Visual Studio
3. Reopen and rebuild

### **Option 4: Check for Missing Dependencies**
```powershell
# Verify all packages restored
dotnet restore
```

---

## 📝 Debug Workflow

When exception occurs:

1. **Break on Exception** (VS will stop)
2. **Check Exception Details** window
3. **View Call Stack** (`Ctrl+Alt+C`)
4. **Check Locals/Autos** variables
5. **Look at Output window** for HRESULT
6. **Check log file** for details
7. **Search HRESULT** online if unknown

---

The enhanced exception handling is now active and will:
- ✅ Catch all unhandled exceptions
- ✅ Log detailed error information
- ✅ Show user-friendly error dialogs
- ✅ Prevent app crashes
- ✅ Help you debug Win32 exceptions effectively

Run your app and check `%LocalAppData%\CopilotDesktop\Logs` for detailed exception information!
