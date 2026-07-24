# Troubleshooting: Windows Application Control Policy Block

If you receive the following error when trying to run `PdfUtility.App.exe`:
> *"An Application Control policy has blocked this file."*

This is because your Windows computer has **Windows Defender Application Control (WDAC)** or **AppLocker** enabled. These enterprise-grade security policies prevent the execution of unsigned `.exe` files outside of trusted, system-controlled folders (like `C:\Windows` or `C:\Program Files`).

Here is a list of highly effective, safe, and immediate workarounds to launch and test your application:

---

### Workaround 1: Run via the trusted .NET CLI (Highly Recommended)
Instead of launching the raw compiled `.exe` directly, use the official, signed Microsoft `dotnet.exe` runner. Since `dotnet.exe` is already signed by Microsoft, Application Control policies almost always trust it!

1. Open your terminal or Command Prompt in the repository root folder (`E:\IHN\PDFPuppy`).
2. Run the following command:
   ```cmd
   dotnet run --project src\PdfUtility.App\PdfUtility.App.csproj
   ```
This will compile and launch the application directly through the trusted `.NET Host` process.

---

### Workaround 2: Launch via Visual Studio Debugger
If you have **Visual Studio 2022** installed:
1. Open the `PdfUtility.sln` solution in Visual Studio.
2. Select `PdfUtility.App` as the Startup Project.
3. Click the green **Start** button (or press `F5`).
Visual Studio launches the app under its own trusted debugging host, bypassing path execution rules.

---

### Workaround 3: Move the App to a Whitelisted Directory
Many AppLocker/WDAC policies allow standard execution inside specific Windows system directories but block external drives (like your `E:\` drive).

Try copying your compiled output folder `net8.0-windows` to one of these locations and running it there:
- `C:\Program Files\PDFUtility\` (Requires Admin privileges, but is almost always whitelisted by default policies).
- `C:\Users\<YourUsername>\AppData\Local\Programs\`

---

### Workaround 4: Package and Sideload via MSIX (Production Method)
An MSIX package installs the app into Windows' trusted app repository (`C:\Program Files\WindowsApps`) and registers it safely with the OS, which is automatically trusted by default security policies.

1. Open `PdfUtility.sln` in Visual Studio 2022.
2. Right-click the solution, click **Add > New Project...** and add a **Windows Application Packaging Project**.
3. Under the package's **Dependencies**, reference the `PdfUtility.App` project.
4. Build and install the sideload MSIX package locally.

---

### Workaround 5: Unblock the File in Windows Properties
Sometimes Windows flags files downloaded from or compiled in specific security zones as "untrusted."
1. Go to `E:\IHN\PDFPuppy\src\PdfUtility.App\bin\Debug\net8.0-windows\`.
2. Right-click on `PdfUtility.App.exe` and select **Properties**.
3. Under the **General** tab, look at the very bottom for a security warning saying: *"This file came from another computer and might be blocked..."*
4. Check the **Unblock** box and click **Apply** / **OK**.
