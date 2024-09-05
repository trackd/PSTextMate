$GrammarNames = [TextMateSharp.Grammars.RegistryOptions]::new('red').GetAvailableGrammarDefinitions().Name
function Show-CodeBlock {
    param(
        [Parameter(Mandatory, ValueFromPipeline, ParameterSetName = 'String')]
        [String]$String,
        [Parameter(Mandatory, ValueFromPipelineByPropertyName, ParameterSetName = 'File')]
        [Alias('FullName')]
        [String]$File,
        [Parameter(ParameterSetName = 'String')]
        [ValidateScript({ $GrammarNames -contains $_ })]
        [String] $Language = 'powershell',
        [TextMateSharp.Grammars.ThemeName] $Theme = 'Dark'
    )
    process {
        if ($PSCmdlet.ParameterSetName -eq 'File') {
            $Path = Get-Item $PSCmdlet.GetUnresolvedProviderPathFromPSPath($File)
            [PwshSpectreConsole.SyntaxHighlight.Highlight]::ReadFile($Path.FullName, $theme, $Path.Extension)
        }
        if ($PSCmdlet.ParameterSetName -eq 'String') {
            [PwshSpectreConsole.SyntaxHighlight.Highlight]::Code($String, $theme, $Language)
        }
    }
}
