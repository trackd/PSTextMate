@{
    RootModule        = 'PSTextMate.psm1'
    ModuleVersion     = '0.0.1'
    GUID              = '5ba21f1d-5ca4-49df-9a07-a2ad379feb00'
    Author            = 'trackd'
    CompanyName       = 'trackd'
    Copyright         = '(c) trackd. All rights reserved.'
    FunctionsToExport = 'Show-CodeBlock'
    # VariablesToExport = '*'
    AliasesToExport   = '*'
    RequiredAssemblies = './packages/TextMateSharp.dll', './packages/TextMateSharp.Grammars.dll', './packages/PwshSpectreConsole.SyntaxHighlight.dll', './packages/Onigwrap.dll'
    RequiredModules    = @(
        @{
            ModuleName      = 'PwshSpectreConsole'
            ModuleVersion = '2.1.0'
            MaximumVersion = '2.9.9'
        }
    )
}
