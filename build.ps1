if (-Not $PSScriptRoot) {
    return 'Run this script from the root of the project'
}
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot

dotnet clean
dotnet restore

$ModuleFilesFolder = Join-Path $PSScriptRoot 'Module'
if (-Not (Test-Path $ModuleFilesFolder)) {
    $null = New-Item -ItemType Directory -Path $ModuleFilesFolder -Force
}
Get-ChildItem -Path (Join-Path $PSScriptRoot 'Output') -File -Recurse | Remove-Item -Force

$moduleLibFolder = Join-Path $PSScriptRoot 'Output' 'lib'
if (-Not (Test-Path $moduleLibFolder)) {
    $null = New-Item -ItemType Directory -Path $moduleLibFolder -Force
}

$csproj = Get-Item (Join-Path $PSScriptRoot 'src' 'PSTextMate.csproj')
$outputfolder = Join-Path $PSScriptRoot 'packages'
if (-Not (Test-Path -Path $outputfolder)) {
    $null = New-Item -ItemType Directory -Path $outputfolder -Force
}

dotnet publish $csproj.FullName -c Release -o $outputfolder
Copy-Item -Path $ModuleFilesFolder/* -Destination (Join-Path $PSScriptRoot 'Output') -Force -Recurse -Include '*.psd1', '*.psm1', '*.ps1xml'

Get-ChildItem -Path $moduleLibFolder -File | Remove-Item -Force


Get-ChildItem -Path (Join-Path $outputfolder 'runtimes' 'win-x64' 'native') -Filter *.dll | Move-Item -Destination $moduleLibFolder -Force
Get-ChildItem -Path (Join-Path $outputfolder 'runtimes' 'osx' 'native') -Filter *.dylib | Move-Item -Destination $moduleLibFolder -Force
Get-ChildItem -Path (Join-Path $outputfolder 'runtimes' 'linux-x64' 'native') -Filter *.so | Copy-Item -Destination $moduleLibFolder -Force
Move-Item (Join-Path $outputfolder 'PSTextMate.dll') -Destination (Split-Path $moduleLibFolder) -Force
Get-ChildItem -Path $outputfolder -File |
    Where-Object { -Not $_.Name.StartsWith('System.Text') -And $_.Extension -notin '.json','.pdb' } |
        Move-Item -Destination $moduleLibFolder -Force

Pop-Location
