@{
    RootModule           = 'PSTextMate.dll'
    ModuleVersion        = '0.0.3'
    GUID                 = '5ba21f1d-5ca4-49df-9a07-a2ad379feb00'
    Author               = 'trackd'
    CompanyName          = 'trackd'
    Copyright            = '(c) trackd. All rights reserved.'
    PowerShellVersion    = '7.4'
    CompatiblePSEditions = 'Core'
    CmdletsToExport      = 'Show-TextMate', 'Test-SupportedTextMate', 'Get-SupportedTextMate', 'Debug-TextMate', 'Debug-TextMateTokens'
    AliasesToExport      = '*'
    RequiredAssemblies   = './lib/TextMateSharp.dll', './lib/TextMateSharp.Grammars.dll', './lib/Onigwrap.dll'
    FormatsToProcess     = 'PSTextMate.format.ps1xml'
    RequiredModules      = @(
        @{
            ModuleName     = 'PwshSpectreConsole'
            ModuleVersion  = '2.1.0'
            MaximumVersion = '2.9.9'
        }
    )
}
