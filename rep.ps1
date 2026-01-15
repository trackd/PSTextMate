Push-Location $PSScriptRoot
& .\build.ps1
Import-Module ./output/PSTextMate.psd1
# $c = Get-Content ./tests/test-markdown.md -Raw
# $c | Show-TextMate -Verbose
# Get-Item ./tests/test-markdown.md | Show-TextMate -Verbose
Show-TextMate -Path ./tests/test-markdown.md -Verbose
Pop-Location
