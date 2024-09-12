Push-Location $PSScriptRoot
& ./build.ps1

Import-Module ./Module/PSTextMate.psd1
$md = @'
[fancy title](https://www.google.com)
'@

[PwshSpectreConsole.TextMate.Debug]::RenderDebug($md, [TextMateSharp.Grammars.ThemeName]::Dark, 'markdown')
Pop-Location
