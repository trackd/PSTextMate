@{
    RootModule        = 'PSTextMate.dll'
    ModuleVersion     = '0.0.2'
    GUID              = '5ba21f1d-5ca4-49df-9a07-a2ad379feb00'
    Author            = 'trackd'
    CompanyName       = 'trackd'
    Copyright         = '(c) trackd. All rights reserved.'
    CmdletsToExport   = 'Show-TextMate'
    AliasesToExport   = '*'
    RequiredAssemblies = './lib/TextMateSharp.dll', './lib/TextMateSharp.Grammars.dll', './lib/Onigwrap.dll'
    RequiredModules    = @(
        @{
            ModuleName      = 'PwshSpectreConsole'
            ModuleVersion = '2.1.0'
            MaximumVersion = '2.9.9'
        }
    )
}
