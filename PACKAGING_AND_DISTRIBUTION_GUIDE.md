# Packaging & Distribution Guide for CopilotDesktop

## 📦 **Overview**

Your WinUI 3 app can be packaged and distributed through multiple channels. Here's a complete guide for each option.

---

## 🎯 **Distribution Options Summary**

| Method | Best For | Pros | Cons |
|--------|----------|------|------|
| **Microsoft Store** | Public distribution | Auto-updates, trust, discoverability | Review process, revenue share |
| **MSIX Installer** | Enterprise/Direct | Full control, side-loading | Requires certificate |
| **.appinstaller** | Web distribution | Easy updates, one-click install | Requires web hosting |
| **GitHub Releases** | Open source | Free hosting, version control | Manual downloads |
| **Winget** | CLI users | Package manager integration | Requires manifest submission |
| **Portable** | Advanced users | No install required | Limited OS integration |

---

## 📋 **Option 1: Microsoft Store (Recommended)**

### **Benefits:**
- ✅ Automatic updates
- ✅ Built-in trust (no certificate needed)
- ✅ Discoverability
- ✅ Windows 11 integration
- ✅ License management

### **Step-by-Step Process:**

#### **1. Prepare Your App**

```powershell
# Ensure your app builds in Release mode
dotnet build -c Release

# Test MSIX package
cd CopilotDesktop
msbuild /t:Restore,Build,Publish /p:Configuration=Release /p:Platform=x64
```

#### **2. Partner Center Setup**

1. **Create Account:**
   - Go to: https://partner.microsoft.com/dashboard
   - Register as individual or company ($19 one-time fee)

2. **Reserve App Name:**
   - Dashboard → Apps and games → New product
   - Reserve "CopilotDesktop"

3. **Update Package.appxmanifest:**
   ```xml
   <Identity
     Name="YourPublisherId.CopilotDesktop"
     Publisher="CN=YOUR_PUBLISHER_ID"
     Version="1.0.0.0" />
   ```

#### **3. Create Store Submission**

**Required Assets:**
```
Store Listing:
  - Description (detailed)
  - Screenshots (4-10 images)
  - App icon (1:1 ratio, 300x300 minimum)
  - Privacy policy URL
  - Support email
  - Age rating

Technical:
  - MSIX package (.msixbundle or .msix)
  - Platform: x64, x86, ARM64
```

**Screenshot Requirements:**
- **1920x1080** or higher
- PNG format
- Show actual app functionality
- No placeholder images

#### **4. Build Store Package**

```powershell
# In Visual Studio
# Project → Publish → Create App Packages → Microsoft Store

# Or manually:
MakeAppx.exe pack /d "CopilotDesktop\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\AppX" /p "CopilotDesktop_1.0.0.0_x64.msix"
```

#### **5. Submit for Review**

- Upload .msix/.msixbundle
- Complete store listing
- Set pricing (Free or Paid)
- Submit for certification (1-3 days)

---

## 📋 **Option 2: MSIX Sideloading (Enterprise/Direct)**

### **Benefits:**
- ✅ Full control over distribution
- ✅ No Store restrictions
- ✅ Internal enterprise deployment
- ✅ Beta/test releases

### **Prerequisites:**

**1. Code Signing Certificate**

**Option A: Self-Signed (Testing Only)**
```powershell
# Create certificate
New-SelfSignedCertificate `
  -Subject "CN=Kaoses" `
  -Type CodeSigningCert `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -HashAlgorithm SHA256

# Export certificate
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object {$_.Subject -eq "CN=Kaoses"}
Export-Certificate -Cert $cert -FilePath "CopilotDesktop.cer"

# Install on target machines
Import-Certificate -FilePath "CopilotDesktop.cer" -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

**Option B: Commercial Certificate (Production)**
- Purchase from: DigiCert, Sectigo, SSL.com
- Cost: $50-300/year
- Trusted by all Windows machines

**2. Build MSIX Package**

```powershell
# Visual Studio approach
# Project → Publish → Create App Packages → Sideloading

# Manual approach
cd CopilotDesktop

# Create package
msbuild /t:Publish /p:Configuration=Release /p:Platform=x64 /p:AppxPackageSigningEnabled=true

# Sign package
signtool sign /fd SHA256 /f "YourCertificate.pfx" /p "Password" "CopilotDesktop_1.0.0.0_x64.msix"
```

**3. Distribution Methods**

**A. Direct Download:**
```
Provide users:
  - CopilotDesktop_1.0.0.0_x64.msix
  - CopilotDesktop.cer (if self-signed)
  - Installation instructions
```

**B. PowerShell Installation Script:**
```powershell
# install.ps1
# Install certificate (if needed)
if (Test-Path ".\CopilotDesktop.cer") {
    Import-Certificate -FilePath ".\CopilotDesktop.cer" -CertStoreLocation Cert:\LocalMachine\TrustedPeople
}

# Install app
Add-AppxPackage -Path ".\CopilotDesktop_1.0.0.0_x64.msix"

Write-Host "CopilotDesktop installed successfully!"
```

---

## 📋 **Option 3: .appinstaller (Web Distribution)**

### **Benefits:**
- ✅ One-click installation from web
- ✅ Automatic updates
- ✅ Version management
- ✅ Deep linking support

### **Setup Process:**

**1. Create .appinstaller File**

Create `CopilotDesktop.appinstaller`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller 
    xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"
    Version="1.0.0.0" 
    Uri="https://yourdomain.com/apps/CopilotDesktop.appinstaller">
    
  <MainPackage 
      Name="d2f5f4e5-d477-4cf4-91f6-d30c4153bc2a"
      Publisher="CN=Kaoses"
      Version="1.0.0.0"
      ProcessorArchitecture="x64"
      Uri="https://yourdomain.com/apps/CopilotDesktop_1.0.0.0_x64.msix" />
      
  <Dependencies>
    <!-- WebView2 dependency -->
    <Package 
        Name="Microsoft.WebView2" 
        Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US"
        Uri="https://go.microsoft.com/fwlink/?linkid=2196064"
        ProcessorArchitecture="x64" />
  </Dependencies>
  
  <UpdateSettings>
    <OnLaunch 
        HoursBetweenUpdateChecks="24"
        UpdateBlocksActivation="false"
        ShowPrompt="false" />
        
    <AutomaticBackgroundTask/>
  </UpdateSettings>
  
</AppInstaller>
```

**2. Host Files**

Upload to web server:
```
https://yourdomain.com/apps/
  ├── CopilotDesktop.appinstaller
  ├── CopilotDesktop_1.0.0.0_x64.msix
  └── CopilotDesktop.cer (if needed)
```

**3. Web Page Installation Link**

```html
<!DOCTYPE html>
<html>
<head>
    <title>Install CopilotDesktop</title>
</head>
<body>
    <h1>CopilotDesktop</h1>
    <p>Click the button below to install:</p>
    
    <!-- Direct installation -->
    <a href="ms-appinstaller:?source=https://yourdomain.com/apps/CopilotDesktop.appinstaller">
        <button>Install CopilotDesktop</button>
    </a>
    
    <!-- Alternative: Download -->
    <p>Or <a href="https://yourdomain.com/apps/CopilotDesktop_1.0.0.0_x64.msix">download manually</a></p>
</body>
</html>
```

**4. Updating Your App**

```xml
<!-- Update the .appinstaller file with new version -->
<MainPackage 
    Version="1.1.0.0"
    Uri="https://yourdomain.com/apps/CopilotDesktop_1.1.0.0_x64.msix" />
```

Users get automatic updates based on `UpdateSettings`.

---

## 📋 **Option 4: GitHub Releases**

### **Benefits:**
- ✅ Free hosting
- ✅ Version tracking
- ✅ Release notes
- ✅ Download statistics

### **Setup Process:**

**1. Create GitHub Workflow**

Create `.github/workflows/release.yml`:
```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: |
        msbuild CopilotDesktop.sln /t:Restore,Build,Publish `
          /p:Configuration=Release `
          /p:Platform=x64 `
          /p:AppxPackageDir=.\AppPackages\ `
          /p:AppxBundle=Always `
          /p:UapAppxPackageBuildMode=SideloadOnly
    
    - name: Create Release
      uses: softprops/action-gh-release@v1
      with:
        files: |
          CopilotDesktop/AppPackages/**/*.msix
          CopilotDesktop/AppPackages/**/*.msixbundle
        draft: false
        prerelease: false
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**2. Create Release**

```bash
# Tag and push
git tag v1.0.0
git push origin v1.0.0

# GitHub Actions will automatically build and create release
```

**3. Manual Release (Alternative)**

```powershell
# Build package
msbuild /t:Publish /p:Configuration=Release /p:Platform=x64

# Create release on GitHub
# Drag and drop .msix file to release assets
```

**4. Installation Instructions (README.md)**

```markdown
## Installation

### Download Latest Release

1. Go to [Releases](https://github.com/lazeras/CopilotDesktop/releases/latest)
2. Download `CopilotDesktop_1.0.0.0_x64.msix`
3. (If needed) Download and install certificate
4. Double-click the .msix file to install

### Requirements
- Windows 10 1809+ or Windows 11
- WebView2 Runtime (auto-installed)
```

---

## 📋 **Option 5: Winget Package Manager**

### **Benefits:**
- ✅ Command-line installation
- ✅ Integrated with Windows
- ✅ Developer-friendly

### **Setup Process:**

**1. Create Winget Manifest**

Create `manifests/l/Lazeras/CopilotDesktop/1.0.0/`:

**Lazeras.CopilotDesktop.yaml:**
```yaml
PackageIdentifier: Lazeras.CopilotDesktop
PackageVersion: 1.0.0
PackageLocale: en-US
Publisher: Kaoses
PublisherUrl: https://github.com/lazeras
PackageName: CopilotDesktop
PackageUrl: https://github.com/lazeras/CopilotDesktop
License: MIT
ShortDescription: Desktop application for Microsoft Copilot
Moniker: copilot-desktop
Tags:
  - copilot
  - ai
  - chatbot
Installers:
  - Architecture: x64
    InstallerType: msix
    InstallerUrl: https://github.com/lazeras/CopilotDesktop/releases/download/v1.0.0/CopilotDesktop_1.0.0.0_x64.msix
    InstallerSha256: [SHA256_HASH]
    SignatureSha256: [SIGNATURE_HASH]
ManifestType: singleton
ManifestVersion: 1.0.0
```

**2. Submit to Winget**

```bash
# Fork winget-pkgs repository
git clone https://github.com/microsoft/winget-pkgs
cd winget-pkgs

# Create manifest directory
mkdir -p manifests/l/Lazeras/CopilotDesktop/1.0.0

# Add manifests and create PR
```

**3. Users Install Via:**

```powershell
winget install Lazeras.CopilotDesktop
```

---

## 📋 **Option 6: ClickOnce (Alternative)**

### **Benefits:**
- ✅ Simple updates
- ✅ Web deployment
- ✅ No admin rights needed

### **Note:**
ClickOnce is legacy for WinUI 3. Use MSIX instead.

---

## 🔧 **Build Configuration**

### **Release Build Checklist**

Before distributing, ensure:

```xml
<!-- CopilotDesktop.csproj -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <Optimize>true</Optimize>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
  <DefineConstants>RELEASE</DefineConstants>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

### **Multi-Platform Builds**

```powershell
# Build all platforms
msbuild /t:Restore,Build /p:Configuration=Release /p:Platform=x64
msbuild /t:Restore,Build /p:Configuration=Release /p:Platform=x86
msbuild /t:Restore,Build /p:Configuration=Release /p:Platform=ARM64

# Create bundle
MakeAppx.exe bundle /d ".\AppPackages" /p "CopilotDesktop_1.0.0.0.msixbundle"
```

---

## 📊 **Recommended Distribution Strategy**

### **For Your App (CopilotDesktop):**

**Phase 1: Beta Testing**
```
Method: GitHub Releases + .appinstaller
Users: Early adopters, testers
Updates: Manual or auto via .appinstaller
```

**Phase 2: Public Release**
```
Method: Microsoft Store (Primary) + GitHub Releases (Secondary)
Users: General public
Updates: Automatic via Store
```

**Phase 3: Enterprise (Optional)**
```
Method: MSIX Sideloading
Users: Corporate environments
Updates: Managed deployment
```

---

## 📝 **Quick Start Commands**

### **Build Release Package:**
```powershell
# Visual Studio
# Right-click project → Publish → Create App Packages

# Command line
msbuild CopilotDesktop.sln /t:Restore,Build,Publish /p:Configuration=Release /p:Platform=x64 /p:AppxPackageDir=.\Packages\ /p:AppxBundle=Always
```

### **Sign Package:**
```powershell
signtool sign /fd SHA256 /f "Certificate.pfx" /p "Password" "CopilotDesktop_1.0.0.0_x64.msix"
```

### **Install Locally (Testing):**
```powershell
Add-AppxPackage -Path ".\CopilotDesktop_1.0.0.0_x64.msix"
```

### **Uninstall:**
```powershell
Get-AppxPackage -Name "*CopilotDesktop*" | Remove-AppxPackage
```

---

## 🔒 **Security Best Practices**

1. **Always sign packages** with valid certificate
2. **Use HTTPS** for .appinstaller hosting
3. **Validate dependencies** (WebView2, etc.)
4. **Test on clean VM** before distribution
5. **Include privacy policy** and terms of service
6. **Regular security updates**

---

## 📞 **Resources**

- [Windows App SDK Packaging Docs](https://learn.microsoft.com/windows/apps/package-and-deploy/)
- [Microsoft Store Submission](https://learn.microsoft.com/windows/apps/publish/)
- [App Installer File](https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview)
- [Code Signing](https://learn.microsoft.com/windows/msix/package/sign-app-package-using-signtool)
- [Winget Repository](https://github.com/microsoft/winget-pkgs)

---

## ✅ **Next Steps**

1. **Choose distribution method** (Store recommended)
2. **Obtain code signing certificate**
3. **Build release package**
4. **Test installation on clean machine**
5. **Submit/Deploy**

**For Store:** Start at https://partner.microsoft.com/dashboard  
**For GitHub:** Create first release at https://github.com/lazeras/CopilotDesktop/releases

Good luck with your distribution! 🚀
