if (-Not $PSScriptRoot) {
    return 'Run this script from the root of the project'
}
Push-Location $PSScriptRoot

dotnet clean
dotnet restore

$moduleLibFolder = Join-Path $PSScriptRoot 'Module' 'lib'
if (-Not (Test-Path $moduleLibFolder)) {
    $null = New-Item -ItemType Directory -Path $moduleLibFolder -Force
}

$csproj = Get-Item (Join-Path $PSScriptRoot 'src' 'PSTextMate.csproj')
$outputfolder = Join-Path $PSScriptRoot 'packages'
if (-Not (Test-Path -Path $outputfolder)) {
    $null = New-Item -ItemType Directory -Path $outputfolder -Force
}
dotnet publish $csproj.FullName -c Release -o $outputfolder

Get-ChildItem -Path $moduleLibFolder -File | Remove-Item -Force


Get-ChildItem -Path (Join-Path $outputfolder 'runtimes' 'win-x64' 'native') -Filter *.dll | Move-Item -Destination $moduleLibFolder -Force
Get-ChildItem -Path (Join-Path $outputfolder 'runtimes' 'osx' 'native') -Filter *.dylib | Move-Item -Destination $moduleLibFolder -Force
Get-ChildItem -Path (Join-Path $outputfolder 'runtimes' 'linux-x64' 'native') -Filter *.so | Copy-Item -Destination $moduleLibFolder -Force
Move-Item (Join-Path $outputfolder 'PSTextMate.dll') -Destination (Split-Path $moduleLibFolder) -Force
Get-ChildItem -Path $outputfolder -File |
    Where-Object { -Not $_.Name.StartsWith('System.Text') -And $_.Extension -notin '.json','.pdb' } |
        Move-Item -Destination $moduleLibFolder -Force

Pop-Location
