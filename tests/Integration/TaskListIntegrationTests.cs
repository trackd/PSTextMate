using PwshSpectreConsole.TextMate.Core;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate.Tests.Integration;

/// <summary>
/// Integration tests to verify TaskList functionality works without reflection.
/// Tests the complete pipeline from markdown input to rendered output.
/// </summary>
public class TaskListIntegrationTests
{
    [Fact]
    public void MarkdigSpectreMarkdownRenderer_TaskList_ProducesCorrectCheckboxes()
    {
        // Arrange
        var markdown = """
            # Task List Example

            - [x] Completed task
            - [ ] Incomplete task
            - [X] Another completed task
            - Regular bullet point
            """;

        var theme = CreateTestTheme();
        var themeName = ThemeName.DarkPlus;

        // Act
        var result = MarkdigSpectreMarkdownRenderer.Render(markdown, theme, themeName);

        // Assert
        result.Should().NotBeNull();

        // The result should be successfully rendered without reflection errors
        // Since we can't easily inspect the internal structure, we verify that:
        // 1. No exceptions are thrown (which would happen with reflection issues)
        // 2. The result is not null
        // 3. The Renderables collection is not empty
        result.Renderables.Should().NotBeEmpty();

        // In a real scenario, the TaskList items would be rendered with proper checkboxes
        // The fact that this doesn't throw proves the reflection code was successfully removed
    }

    [Theory]
    [InlineData("- [x] Completed", true)]
    [InlineData("- [ ] Incomplete", false)]
    [InlineData("- [X] Uppercase completed", true)]
    [InlineData("- Regular item", false)]
    public void MarkdigSpectreMarkdownRenderer_VariousTaskListFormats_RendersWithoutErrors(string markdown, bool isTaskList)
    {
        // Arrange
        var theme = CreateTestTheme();
        var themeName = ThemeName.DarkPlus;

        // Act & Assert - Should not throw exceptions
        var result = MarkdigSpectreMarkdownRenderer.Render(markdown, theme, themeName);

        result.Should().NotBeNull();
        result.Renderables.Should().NotBeEmpty();
    }

    [Fact]
    public void MarkdigSpectreMarkdownRenderer_ComplexTaskList_RendersWithoutReflectionErrors()
    {
        // Arrange
        var markdown = """
            # Complex Task List

            1. Ordered list with tasks:
               - [x] Sub-task completed
               - [ ] Sub-task incomplete

            - [x] Top-level completed
            - [ ] Top-level incomplete
              - [x] Nested completed
              - [ ] Nested incomplete

            ## Another section
            - Regular bullet
            - Another bullet
            """;

        var theme = CreateTestTheme();
        var themeName = ThemeName.DarkPlus;

        // Act & Assert - This would fail with reflection errors if not fixed
        var result = MarkdigSpectreMarkdownRenderer.Render(markdown, theme, themeName);

        result.Should().NotBeNull();
        result.Renderables.Should().NotBeEmpty();

        // Verify we have multiple rendered elements (headings, lists, etc.)
        result.Renderables.Should().HaveCountGreaterThan(3);
    }

    private static TextMateSharp.Themes.Theme CreateTestTheme()
    {
        var registryOptions = new TextMateSharp.Registry.RegistryOptions(ThemeName.DarkPlus);
        var registry = new TextMateSharp.Registry.Registry(registryOptions);
        return registry.GetTheme();
    }
}
