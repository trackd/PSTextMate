using System.Management.Automation;
using PwshSpectreConsole.TextMate.Core;
using PwshSpectreConsole.TextMate.Extensions;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate.Cmdlets;

/// <summary>
/// Cmdlet for debugging TextMate processing and theme application.
/// Provides detailed diagnostic information for troubleshooting rendering issues.
/// </summary>
[Cmdlet(VerbsDiagnostic.Debug, "TextMate", DefaultParameterSetName = "String")]
[OutputType(typeof(Test.TextMateDebug))]
public sealed class DebugTextMateCmdlet : PSCmdlet {
    private readonly List<string> _inputObjectBuffer = [];

    /// <summary>
    /// String content to debug.
    /// </summary>
    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "String"
    )]
    [AllowEmptyString]
    public string InputObject { get; set; } = null!;

    /// <summary>
    /// Path to file to debug.
    /// </summary>
    [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "Path",
        Position = 0
    )]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string Path { get; set; } = null!;

    /// <summary>
    /// TextMate language ID (default: 'powershell').
    /// </summary>
    [Parameter(
        ParameterSetName = "String"
    )]
    [ValidateSet(typeof(TextMateLanguages))]
    public string Language { get; set; } = "powershell";

    /// <summary>
    /// Color theme for debug output (default: Dark).
    /// </summary>
    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.Dark;

    /// <summary>
    /// Override file extension for language detection.
    /// </summary>
    [Parameter(
        ParameterSetName = "Path"
    )]
    [TextMateExtensionTransform()]
    [ValidateSet(typeof(TextMateExtensions))]
    [Alias("As")]
    public string ExtensionOverride { get; set; } = null!;

    /// <summary>
    /// Processes each input record from the pipeline.
    /// </summary>
    protected override void ProcessRecord() {
        if (ParameterSetName == "String" && InputObject is not null) {
            _inputObjectBuffer.Add(InputObject);
        }
    }

    /// <summary>
    /// Finalizes processing and outputs debug information.
    /// </summary>
    protected override void EndProcessing() {
        try {
            if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0) {
                string[] strings = [.. _inputObjectBuffer];
                if (strings.AllIsNullOrEmpty()) {
                    return;
                }
                Test.TextMateDebug[]? obj = Test.DebugTextMate(strings, Theme, Language);
                WriteObject(obj, true);
            }
            else if (ParameterSetName == "Path" && Path is not null) {
                FileInfo Filepath = new(GetUnresolvedProviderPathFromPSPath(Path));
                if (!Filepath.Exists) {
                    throw new FileNotFoundException("File not found", Filepath.FullName);
                }
                string ext = !string.IsNullOrEmpty(ExtensionOverride)
                    ? ExtensionOverride
                    : Filepath.Extension;
                string[] strings = File.ReadAllLines(Filepath.FullName);
                Test.TextMateDebug[]? obj = Test.DebugTextMate(strings, Theme, ext, true);
                WriteObject(obj, true);
            }
        }
        catch (Exception ex) {
            WriteError(new ErrorRecord(ex, "DebugTextMateError", ErrorCategory.InvalidOperation, null));
        }
    }
}

/// <summary>
/// Cmdlet for debugging individual TextMate tokens and their properties.
/// Provides low-level token analysis for detailed syntax highlighting inspection.
/// </summary>
[OutputType(typeof(TokenDebugInfo))]
[Cmdlet(VerbsDiagnostic.Debug, "TextMateTokens", DefaultParameterSetName = "String")]
public sealed class DebugTextMateTokensCmdlet : PSCmdlet {
    private readonly List<string> _inputObjectBuffer = [];

    /// <summary>
    /// String content to analyze tokens from.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "String")]
    [AllowEmptyString]
    public string InputObject { get; set; } = null!;

    /// <summary>
    /// Path to file to analyze tokens from.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Path", Position = 0)]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string Path { get; set; } = null!;

    /// <summary>
    /// TextMate language ID (default: 'powershell').
    /// </summary>
    [Parameter(ParameterSetName = "String")]
    [ValidateSet(typeof(TextMateLanguages))]
    public string Language { get; set; } = "powershell";

    /// <summary>
    /// Color theme for token analysis (default: DarkPlus).
    /// </summary>
    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.DarkPlus;

    /// <summary>
    /// Override file extension for language detection.
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    [TextMateExtensionTransform()]
    [ValidateSet(typeof(TextMateExtensions))]
    [Alias("As")]
    public string ExtensionOverride { get; set; } = null!;

    /// <summary>
    /// Processes each input record from the pipeline.
    /// </summary>
    protected override void ProcessRecord() {
        if (ParameterSetName == "String" && InputObject is not null) {
            _inputObjectBuffer.Add(InputObject);
        }
    }

    /// <summary>
    /// Finalizes processing and outputs token debug information.
    /// </summary>
    protected override void EndProcessing() {
        try {
            if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0) {
                string[] strings = [.. _inputObjectBuffer];
                if (strings.AllIsNullOrEmpty()) {
                    return;
                }
                TokenDebugInfo[]? obj = Test.DebugTextMateTokens(strings, Theme, Language);
                WriteObject(obj, true);
            }
            else if (ParameterSetName == "Path" && Path is not null) {
                FileInfo Filepath = new(GetUnresolvedProviderPathFromPSPath(Path));
                if (!Filepath.Exists) {
                    throw new FileNotFoundException("File not found", Filepath.FullName);
                }
                string ext = !string.IsNullOrEmpty(ExtensionOverride)
                    ? ExtensionOverride
                    : Filepath.Extension;
                string[] strings = File.ReadAllLines(Filepath.FullName);
                TokenDebugInfo[]? obj = Test.DebugTextMateTokens(strings, Theme, ext, true);
                WriteObject(obj, true);
            }
        }
        catch (Exception ex) {
            WriteError(new ErrorRecord(ex, "DebugTextMateTokensError", ErrorCategory.InvalidOperation, null));
        }
    }
}

/// <summary>
/// Cmdlet for debugging Sixel image support and availability.
/// Provides diagnostic information about Sixel capabilities in the current environment.
/// </summary>
[Cmdlet(VerbsDiagnostic.Debug, "SixelSupport")]
public sealed class DebugSixelSupportCmdlet : PSCmdlet {
    /// <summary>
    /// Processes the cmdlet and outputs Sixel support diagnostic information.
    /// </summary>
    protected override void ProcessRecord() {
        try {
            var result = new {
                SixelImageAvailable = Core.Markdown.Renderers.ImageRenderer.IsSixelImageAvailable(),
                LastSixelError = Core.Markdown.Renderers.ImageRenderer.GetLastSixelError(),
                LoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name?.Contains("Spectre.Console") == true)
                    .Select(a => new {
                        a.GetName().Name,
                        Version = a.GetName().Version?.ToString(),
                        a.Location,
                        SixelTypes = a.GetTypes()
                            .Where(t => t.Name.Contains("Sixel", StringComparison.OrdinalIgnoreCase))
                            .Select(t => t.FullName)
                            .ToArray()
                    })
                    .ToArray()
            };

            WriteObject(result);
        }
        catch (Exception ex) {
            WriteError(new ErrorRecord(ex, "DebugSixelSupportError", ErrorCategory.InvalidOperation, null));
        }
    }
}

/// <summary>
/// Cmdlet for testing image rendering and debugging issues.
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "ImageRendering")]
public sealed class TestImageRenderingCmdlet : PSCmdlet {
    /// <summary>
    /// URL or path to image for rendering test.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string ImageUrl { get; set; } = null!;

    /// <summary>
    /// Alternative text for the image.
    /// </summary>
    [Parameter(Position = 1)]
    public string AltText { get; set; } = "Test Image";

    /// <summary>
    /// Processes the cmdlet and tests image rendering.
    /// </summary>
    protected override void ProcessRecord() {
        try {
            WriteVerbose($"Testing image rendering for: {ImageUrl}");

            IRenderable result = Core.Markdown.Renderers.ImageRenderer.RenderImage(AltText, ImageUrl);

            var debugInfo = new {
                ImageUrl,
                AltText,
                ResultType = result.GetType().FullName,
                SixelAvailable = Core.Markdown.Renderers.ImageRenderer.IsSixelImageAvailable(),
                LastImageError = Core.Markdown.Renderers.ImageRenderer.GetLastImageError(),
                LastSixelError = Core.Markdown.Renderers.ImageRenderer.GetLastSixelError()
            };

            WriteObject(debugInfo);
            WriteObject("Rendered result:");
            WriteObject(result);
        }
        catch (Exception ex) {
            WriteError(new ErrorRecord(ex, "TestImageRenderingError", ErrorCategory.InvalidOperation, ImageUrl));
        }
    }
}
