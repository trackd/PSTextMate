Get-ChildItem $PSScriptRoot/Public -Filter *.ps1 | ForEach-Object { . $_.FullName }
