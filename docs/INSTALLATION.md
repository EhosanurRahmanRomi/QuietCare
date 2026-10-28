# Install, startup and updates

Use **QuietCare-Setup.exe** on Windows 10/11 x64. It includes the application, .NET runtime and a native Windows Installer package. Choose startup, desktop shortcut and opening the app after setup; all three are selected initially. Setup verifies the embedded package SHA-256 before passing it to the fixed Windows Installer engine. It does not download or execute a remote installer.

The permanent app is installed in `%LOCALAPPDATA%\Programs\QuietCareNext`. The internal executable name remains `QuietCare.Next.exe` so existing settings and the startup entry can migrate. Windows presents the product as **QuietCare**, with its original Q logo. The Start menu includes both QuietCare and QuietCare Performance Monitor. Windows Installed apps supplies the normal uninstaller.

With startup selected, the current-user `Run` entry `QuietCareNext` launches the installed executable with `--tray --monitor`. Windows startup approval is set to enabled. This opens the tiny monitor after sign-in without opening the main workspace. Monitor settings can turn it off. Startup is a user-session app; no boot driver, service, scheduled task or elevated resident agent is installed. The installer does not force a restart. Reboot acceptance is separate from checking the registration.

The tiny monitor stays above other windows according to your preference. Drag its reading area to move it, use the lightning control for immediate RAM Boost, or the chevron for details. The **Mini monitor** header button restores it without switching tools. Refresh rates are configurable from 100 to 10,000 ms. Faster settings cost more CPU; missing readings use a dash.

Updates remain deliberate: **About → Check for updates** contacts the official GitHub release API. Read the notes and run the new setup. Exit QuietCare through its tray before an update; the setup refuses to replace a running copy. The native MSI has a stable upgrade family, transactional replacement of older package versions and a newer-version guard. Every package release must increment the numeric MSI version. An unsigned package can still prompt Windows reputation warnings; this release is not Authenticode signed.

Uninstall from Windows Installed apps. Windows Installer removes package-owned files, shortcuts and the owned startup registration. It preserves unowned files, local settings, Recovery, logs and startup backups. Recovery data is not part of the application package and is never recursively deleted by setup. Native install/uninstall and both startup choices are tested using a separate fixture product and isolated directories/registry locations.

The release also contains **QuietCare.msi** for administrators who need the native package. Its default is current-user startup plus a desktop shortcut. Use `STARTUP=0 DESKTOP=0` to opt out during a quiet MSI installation. The EXE provides the friendly option screen. Neither artifact is a portable launcher.

Defender preference backups and any explicitly chosen off policy are machine settings, outside the application package. Restore protection/scan settings from Protection before uninstall if you want to undo them. Installation itself does not apply any Defender preference. [Controls and limits](DEFENDER-CONTROLS.md).

End any Focus session and check **Focus mode → Restore changes** before uninstalling. Normal session exit restores its owned priorities, notification profile and optional update policies; interrupted recovery may require administrator approval on the next launch. [Focus recovery](FOCUS-MODE.md).
