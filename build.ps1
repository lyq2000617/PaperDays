param([switch]$Test)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = if ($Test) { 'PaperDays.Tests.exe' } else { 'PaperDays.exe' }
$flags = @('/nologo','/target:winexe','/platform:anycpu',"/win32manifest:$PSScriptRoot\app.manifest","/win32icon:$PSScriptRoot\calendar.ico","/out:$PSScriptRoot\$output")
if ($Test) { $flags += '/define:TESTS' }
foreach ($reference in @('Microsoft.CSharp','Accessibility','System.Windows.Forms','System.Drawing','System.Web.Extensions','System.Xml.Linq','System.IO.Compression','System.IO.Compression.FileSystem')) { $flags += "/reference:$reference.dll" }
$source = @(Get-ChildItem "$PSScriptRoot\src" -Filter *.cs | ForEach-Object FullName)
& $compiler @flags @source
if ($LASTEXITCODE -ne 0) { throw '构建失败' }
