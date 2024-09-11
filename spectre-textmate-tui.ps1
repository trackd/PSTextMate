$layout = New-SpectreLayout -Name "root" -Rows @(
    # Row 1
    (
        New-SpectreLayout -Name "header" -MinimumSize 5 -Ratio 1 -Data ("empty")
    ),
    # Row 2
    (
        New-SpectreLayout -Name "content" -Ratio 10 -Columns @(
            (
                New-SpectreLayout -Name "filelist" -Ratio 2 -Data "empty"
            ),
            (
                New-SpectreLayout -Name "preview" -Ratio 4 -Data "empty"
            )
        )
    )
)
# Functions for rendering the content of each panel
function Get-TitlePanel {
    return "File Browser - Spectre Live Demo [gray]$(Get-Date)[/]" | Format-SpectreAligned -HorizontalAlignment Center -VerticalAlignment Middle | Format-SpectrePanel -Expand
}
function EscapeAnsi {
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [String] $String,
        [Switch] $Highlight
    )
    process {
        $String.EnumerateRunes() | ForEach-Object {
            if ($_.Value -lt 32) {
                if ($Highlight) {
                    $PSStyle.Background.Red
                    [Text.Rune]::new($_.Value + 9216)
                    $PSStyle.Reset
                } else {
                    [Text.Rune]::new($_.Value + 9216)
                }
            }
            else {
                $_
            }
        } | Join-String
    }
}
function Get-FileListPanel {
    param (
        $Files,
        $SelectedFile
    )
    $fileList = $Files | ForEach-Object {
        $name = $_.Name
        if ($_.Name -eq $SelectedFile.Name) {
            $name = "[Turquoise2]$($name)[/]"
        }
        return $name
    } | Out-String
    return Format-SpectrePanel -Header "[white]File List[/]" -Data $fileList.Trim() -Expand
}

function Get-PreviewPanel {
    param (
        $SelectedFile
    )
    $item = Get-Item -Path $SelectedFile.FullName
    $result = ""
    if ($item -is [System.IO.DirectoryInfo]) {
        $result = "[grey]$($SelectedFile.Name) is a directory.[/]"
    } else {
        try {
            if ([PwshSpectreConsole.TextMate.TextMateExtensions]::IsSupportedFile($item.FullName)) {
                $result = Show-TextMate -Path $item.FullName -ErrorAction Stop
            }
            else {
                $content = -Join (Get-Content -Path $item.FullName -ErrorAction Stop)
                $result = ($Content | EscapeAnsi | Get-SpectreEscapedText) | Join-String -OutputPrefix "[grey]" -OutputSuffix "[/]"
            }
        } catch {
            $result = "[red]Error reading file content: $($_.Exception.Message | Get-SpectreEscapedText)[/]"
        }
    }
    return $result | Format-SpectrePanel -Header "[white]Preview[/]" -Expand
}

function Get-LastKeyPressed {
    $lastKeyPressed = $null
    while ([Console]::KeyAvailable) {
        $lastKeyPressed = [Console]::ReadKey($true)
    }
    return $lastKeyPressed
}

# Start live rendering the layout
# Type "↓", "↓", "↓" to navigate the file list, and press "Enter" to open a file in Notepad
Invoke-SpectreLive -Data $layout -ScriptBlock {
    param (
        [Spectre.Console.LiveDisplayContext] $Context
    )

    # State
    $fileList = @(@{Name = ".."; Fullname = ".."}) + (Get-ChildItem)
    $selectedFile = $fileList[0]

    while ($true) {
        # Handle input
        $lastKeyPressed = Get-LastKeyPressed
        if ($lastKeyPressed -ne $null) {
            if ($lastKeyPressed.Key -eq "DownArrow") {
                $selectedFile = $fileList[($fileList.IndexOf($selectedFile) + 1) % $fileList.Count]
            } elseif ($lastKeyPressed.Key -eq "UpArrow") {
                $selectedFile = $fileList[($fileList.IndexOf($selectedFile) - 1 + $fileList.Count) % $fileList.Count]
            } elseif ($lastKeyPressed.Key -eq "Enter") {
                if ($selectedFile -is [System.IO.DirectoryInfo] -or $selectedFile.Name -eq "..") {
                    $fileList = @(@{Name = ".."; Fullname = ".."}) + (Get-ChildItem -Path $selectedFile.FullName)
                    $selectedFile = $fileList[0]
                } else {
                    notepad $selectedFile.FullName
                    return
                }
            } elseif ($lastKeyPressed.Key -eq "Escape") {
                return
            }
        }

        # Generate new data
        $titlePanel = Get-TitlePanel
        $fileListPanel = Get-FileListPanel -Files $fileList -SelectedFile $selectedFile
        $previewPanel = Get-PreviewPanel -SelectedFile $selectedFile

        # Update layout
        $layout["header"].Update($titlePanel) | Out-Null
        $layout["filelist"].Update($fileListPanel) | Out-Null
        $layout["preview"].Update($previewPanel) | Out-Null

        # Draw changes
        $Context.Refresh()
        Start-Sleep -Milliseconds 200
    }
}
