<#
$lookup = [TextMateSharp.Grammars.RegistryOptions]::new('red')
$reg = [TextMateSharp.Registry.Registry]::new($lookup)
$theme = $reg.GetTheme()
$theme | Get-Member -MemberType Method
$grammar = $reg.LoadGrammar($lookup.GetScopeByExtension('.md'))
$grammar | Get-Member -MemberType Method
#>

Push-Location $PSScriptRoot
& ./build.ps1

Import-Module ./Module/PSTextMate.psd1
$md = @'
[fancy title](https://www.google.com)
'@

[PwshSpectreConsole.TextMate.Test]::DebugTextMate($md, [TextMateSharp.Grammars.ThemeName]::Dark, 'markdown')
Pop-Location


# $x = [Spectre.Console.Style]::new([Spectre.Console.Color]::Aqua, [Spectre.Console.Color]::Default, [Spectre.Console.Decoration]::Underline, 'https://foo.bar')
# [Spectre.Console.Markup]::new('hello', $x)
# [Spectre.Console.Markup]::new("[link=https://foo.com]$([Spectre.Console.Markup]::escape('[foo]'))[/]")
# [Spectre.Console.Markup]::new("[link=https://foo.com]$([Spectre.Console.Markup]::escape('[foo]'))[/]")
# [Spectre.Console.Markup]::new('[link=https://foo.com]foo[/]')
