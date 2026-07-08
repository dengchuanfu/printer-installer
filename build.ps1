$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

$build = Join-Path $root 'build'
$output = Join-Path $root 'output'
$installerExe = Join-Path $build 'PrinterInstaller.exe'
$payload = Join-Path $build 'single_payload'
$payloadZip = Join-Path $build 'payload.zip'
$finalExe = Join-Path $output 'PrinterInstaller_OneFile_Slim_Green.exe'
$driverSource = Join-Path $root 'drivers\Ricoh_MP_C4502_5502_Pcl6\x64'
$driverPayload = Join-Path $payload 'drivers\Ricoh_MP_C4502_5502_Pcl6\x64'

if (Test-Path $build) {
    Remove-Item -Recurse -Force $build
}

if (Test-Path $output) {
    Remove-Item -Recurse -Force $output
}

New-Item -ItemType Directory -Force -Path $build | Out-Null
New-Item -ItemType Directory -Force -Path $output | Out-Null

& $csc `
    /target:winexe `
    /out:"$installerExe" `
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

New-Item -ItemType Directory -Force -Path $driverPayload | Out-Null
Copy-Item -Force $installerExe "$payload\PrinterInstaller.exe"
Copy-Item -Force "$root\src\printers.json" "$payload\printers.json"
Get-ChildItem $driverSource -File |
    Where-Object { $_.Name -in @(
        'OEMSETUP.INF',
        'rica5i.cat',
        'rica5Iui.dl_',
        'rica5Iui.irj',
        'rica5Iui.rdj',
        'rica5Iui.rcf',
        'rica5Iug.dl_',
        'rica5Iug.miz',
        'rica5Iur.dl_',
        'rica5Igr.dl_',
        'rica5Ici.dl_',
        'rica5Icd.dl_',
        'rica5Icd.psz',
        'rica5Icf.cfz',
        'rica5Icl.ini',
        'rica5Ich.chm',
        'rica5Icz.dlz',
        'rica5Icj.dl_',
        'rica5Ict.dl_',
        'rica5Icb.dl_',
        'rica5Ilm.dl_',
        'ricdb64.dl_',
        'mfricr64.dl_',
        'mpc42d64.dl_'
    ) } |
    Copy-Item -Destination $driverPayload -Force

Compress-Archive -Path "$payload\*" -DestinationPath $payloadZip -Force

& $csc `
    /target:winexe `
    /out:"$finalExe" `
    /win32icon:"$root\src\app.ico" `
    /win32manifest:"$root\src\app.manifest" `
    /reference:System.Windows.Forms.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    /resource:"$payloadZip,PrinterInstallerBundle.payload.zip" `
    "$root\src\BundleProgram.cs"

Remove-Item -Recurse -Force $build

Get-Item $finalExe
