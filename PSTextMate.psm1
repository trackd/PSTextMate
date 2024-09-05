$lookup = [TextMateSharp.Grammars.RegistryOptions]::new('red').GetAvailableGrammarDefinitions().Name
class TextMateLanguages : System.Management.Automation.IValidateSetValuesGenerator {
    [String[]] GetValidValues() {
        return $script:lookup
    }
}
function Show-CodeBlock {
    param(
        [Parameter(Mandatory, ValueFromPipeline, ParameterSetName = 'String')]
        [String]$String,
        [Parameter(Mandatory, ValueFromPipelineByPropertyName, ParameterSetName = 'File')]
        [Alias('FullName')]
        [String]$File,
        [Parameter(ParameterSetName = 'String')]
        [ValidateSet([TextMateLanguages], ErrorMessage = "Value '{0}' is invalid. Try one of: {1}")]
        [String] $Language = 'powershell',
        [TextMateSharp.Grammars.ThemeName] $Theme = 'Dark'
    )
    process {
        if ($PSCmdlet.ParameterSetName -eq 'File') {
            $Path = Get-Item $PSCmdlet.GetUnresolvedProviderPathFromPSPath($File)
            [PwshSpectreConsole.SyntaxHighlight.Highlight]::ReadFile($Path.FullName, $theme, $Path.Extension)
        }
        if ($PSCmdlet.ParameterSetName -eq 'String') {
            [PwshSpectreConsole.SyntaxHighlight.Highlight]::String($String, $theme, $Language)
        }
    }
}
