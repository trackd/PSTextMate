param(
    [switch]$Clean
)
if (-Not $PSScriptRoot) {
    return 'Run this script from the root of the project'
}
dotnet clean
dotnet restore
dotnet publish "$PSScriptRoot/src/PSTextMate.csproj" -c Release -o $PSScriptRoot/packages
$null = & {
    Get-ChildItem -Path "$PSScriptRoot/Module/lib/" -File | Remove-Item -Force
    Get-ChildItem -Path $PSScriptRoot/packages/runtimes/win-x64/native -Filter *.dll | Copy-Item -Destination $PSScriptRoot/packages -Force
    Get-ChildItem -Path $PSScriptRoot/packages/runtimes/osx/native -Filter *.dylib | Copy-Item -Destination $PSScriptRoot/packages -Force
    Get-ChildItem -Path $PSScriptRoot/packages/runtimes/linux-x64/native -Filter *.so | Copy-Item -Destination $PSScriptRoot/packages -Force
    Remove-Item $PSScriptRoot/packages/System.Text.*.dll -Force
    Remove-Item "$PSScriptRoot/packages/PSTextMate.deps.json" -Force
    Remove-Item "$PSScriptRoot/packages/PSTextMate.pdb" -Force
}

if (-Not (Test-Path "$PSScriptRoot/Module/lib")) {
    $null = New-Item -ItemType Directory -Path "$PSScriptRoot/Module/lib/"
}
Get-Childitem -Path "$PSScriptRoot/packages" -File | Move-Item -Destination "$PSScriptRoot/Module/lib/" -Force
