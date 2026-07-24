# PDF Utility - Build & Deployment Guide

This is a production-ready, lightweight, high-performance offline Windows Desktop PDF Utility application built on **.NET 8.0/9.0**, **WPF (C#)**, and **Clean Architecture**.

---

## 🛠️ Project Architecture

The solution is divided into highly decoupled, modular layers following **SOLID** and **DRY** principles:

1. **`PdfUtility.Core`**: Class library containing domain models (`AppSettings`, `HistoryEntry`), interface definitions (`ISettingsService`, `IFileConverterService`, `IPdfMergeService`, `IHistoryService`), and custom domain exceptions. Has zero external dependencies.
2. **`PdfUtility.Services`**: Implementation library using `PdfSharp` for PDF manipulation/merging, and `SixLabors.ImageSharp` for direct image loading, resizing, and orientation conversions.
3. **`PdfUtility.App`**: Windows WPF desktop executable implementing the **MVVM** pattern, styled with a modern Fluent Windows 11 look (rounded corners, automatic Dark/Light theme switching, progress animations), bootstrapped with **Microsoft Dependency Injection (IOC)** and **Serilog** rolling file logger.
4. **`PdfUtility.Tests`**: High-performance unit testing library containing robust integration/unit tests for all services, validations, and conversions.

---

## 🚀 How to Build & Run (Windows)

### Prerequisites
- Windows 10 (Build 17763+) or Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Visual Studio 2022 (with *.NET Desktop Development* workload checked)

### Step 1: Clone the Repository
```bash
git clone <repository-url>
cd PDFPuppy
```

### Step 2: Build the Solution
Using .NET CLI:
```bash
dotnet restore
dotnet build -c Release
```

### Step 3: Run the Application
```bash
dotnet run --project src/PdfUtility.App/PdfUtility.App.csproj
```

---

## 📦 How to Package & Deploy (MSIX)

To distribute the app as a secure, signed Windows Package (MSIX) following modern Windows standards:

1. Open the solution in **Visual Studio 2022**.
2. Right-click the solution, select **Add > New Project...**.
3. Search for and select **Windows Application Packaging Project**. Name it `PdfUtility.Package`.
4. Right-click the `Dependencies` node in the Packaging Project, select **Add Project Reference...**, and check `PdfUtility.App`.
5. To generate the installer: Right-click the Packaging Project, select **Publish > Create App Packages...**.
6. Follow the wizard to generate a sideloading `.msix` package or store-ready upload!

---

## 🔌 Offline Office-to-PDF Configuration

To convert Word (`.docx`), Excel (`.xlsx`), and PowerPoint (`.pptx`) documents without relying on Microsoft Office installed on the user's computer, we support **Portable LibreOffice Integration**:

1. Download the **LibreOffice Portable** zip/exe from [LibreOffice Portable](https://portableapps.com/apps/office/libreoffice_portable).
2. Extract it inside the application folder or adjacent to the executable.
3. Structure:
   ```
   PdfUtility.App.exe
   LibreOfficePortable/
       App/
           libreoffice/
               program/
                   soffice.exe
   ```
4. The application automatically detects `soffice.exe` inside this path, executing conversions in a headless background worker (completely silent, offline, and high fidelity).
5. **Fallback:** If LibreOffice is not present, the app automatically checks if local MS Office is installed and uses Interop with safe dynamic bindings.
