Describe 'Show-TextMate -Stream' {
    It 'emits multiple RenderableBatch objects with correct coverage' {
        # Arrange
        $lines = 0..2499 | ForEach-Object { if ($_ % 5 -eq 0) { '// comment line' } else { 'var x = 1; // code' } }
        $temp = [System.IO.Path]::GetTempFileName()
        Set-Content -Path $temp -Value $lines -NoNewline

        try {
            # The build pipeline (build.ps1) should have published the module into the Output folder
            $moduleManifest = Join-Path -Path (Resolve-Path "$PSScriptRoot\..\..\Output") -ChildPath 'PSTextMate.psd1'
            if (-Not (Test-Path $moduleManifest)) {
                throw "Module manifest not found at $moduleManifest. Ensure build.ps1 was run prior to tests."
            }

            Import-Module -Name $moduleManifest -Force -ErrorAction Stop

            # Act
            $batches = Show-TextMate -Path $temp -Stream -BatchSize 1000 | Where-Object { $_ -is [PwshSpectreConsole.TextMate.Core.RenderableBatch] } | Select-Object -Property BatchIndex, LineCount

            # Assert
            $batches | Should -Not -BeNullOrEmpty
            $batches.Count | Should -BeGreaterThan 1

            $covered = ($batches | Measure-Object -Property LineCount -Sum).Sum
            $covered | Should -BeGreaterOrEqualTo $lines.Count

            # Ensure batch indexes are sequential starting from zero
            $expected = 0..($batches.Count - 1)
            ($batches | ForEach-Object { $_.BatchIndex }) | Should -Be $expected
        }
        finally {
            Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue
        }
    }
}
