# Printer Installer

Windows printer one-click installer for internal office printers.

## Features

- Office selector UI
- Embedded logo and app icon
- Runs with administrator privileges
- Installs printer driver, TCP/IP port, and printer
- Sets printer default to black and white
- Finance office password check
- Single-file self-extracting package support

## Build

Run from the repository root on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The final package is generated at:

```text
output\PrinterInstaller_OneFile_Slim_Green.exe
```

## Required Driver Files

The slim build expects the C3004 driver files under:

```text
drivers\extracted\MPC3004\disk1
```

