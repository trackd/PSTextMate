if (-Not $PSScriptRoot) {
    return 'Run this script from the root of the project'
}
if (Test-Path $PSScriptRoot/packages) {
    Remove-Item -Recurse -Force -Path $PSScriptRoot/packages
}
if (Test-Path $PSScriptRoot/src/bin) {
    Remove-Item -Recurse -Force -Path $PSScriptRoot/src/bin
}
if (Test-Path $PSScriptRoot/src/obj) {
    Remove-Item -Recurse -Force -Path $PSScriptRoot/src/obj
}
if (Test-Path $PSScriptRoot/src/.vs) {
    Remove-Item -Recurse -Force -Path $PSScriptRoot/src/.vs
}
dotnet clean
dotnet publish "$PSScriptRoot/src/PwshSpectreConsole.SyntaxHighlight.csproj" -c Release -o $PSScriptRoot/packages
Get-ChildItem -Path $PSScriptRoot/packages/runtimes/win-x64/native -Filter *.dll | Copy-Item -Destination $PSScriptRoot/packages -Force
Get-ChildItem -Path $PSScriptRoot/packages/runtimes/osx/native -Filter *.dylib | Copy-Item -Destination $PSScriptRoot/packages -Force
Get-ChildItem -Path $PSScriptRoot/packages/runtimes/linux-x64/native -Filter *.so | Copy-Item -Destination $PSScriptRoot/packages -Force

Remove-Item $PSScriptRoot/packages/System.Text.*.dll -force
