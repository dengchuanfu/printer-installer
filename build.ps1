$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

$output = Join-Path $root 'output'
$payload = Join-Path $root 'build\single_payload_min'
$payloadZip = Join-Path $root 'build\payload_min.zip'

New-Item -ItemType Directory -Force -Path $output | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $payloadZip) | Out-Null

& $csc `
    /target:winexe `
    /out:"$output\PrinterInstaller.exe" `
    /win32icon:"$root\src\app.ico" `
    /win32manifest:"$root\src\app.manifest" `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.Web.Extensions.dll `
    /resource:"$root\src\logo.png,PrinterInstaller.logo.png" `
    "$root\src\Program.cs"

if (Test-Path $payload) {
    Remove-Item -Recurse -Force $payload
}

New-Item -ItemType Directory -Force -Path "$payload\drivers\extracted\MPC3004\disk1" | Out-Null
Copy-Item -Force "$output\PrinterInstaller.exe" "$payload\PrinterInstaller.exe"
Copy-Item -Force "$root\src\printers.json" "$payload\printers.json"
Copy-Item -Recurse -Force "$root\drivers\extracted\MPC3004\disk1\*" "$payload\drivers\extracted\MPC3004\disk1\"

Compress-Archive -Path "$payload\*" -DestinationPath $payloadZip -Force

& $csc `
    /target:winexe `
    /out:"$output\PrinterInstaller_OneFile_Slim_Green.exe" `
    /win32icon:"$root\src\app.ico" `
    /win32manifest:"$root\src\app.manifest" `
    /reference:System.Windows.Forms.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    /resource:"$payloadZip,PrinterInstallerBundle.payload.zip" `
    /resource:"$root\src\logo.png,PrinterInstaller.logo.png" `
    "$root\src\BundleProgram.cs"

Get-Item "$output\PrinterInstaller_OneFile_Slim_Green.exe"

