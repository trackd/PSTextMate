using PwshSpectreConsole.TextMate.Core.Markdown.Renderers;
using Markdig;
using Markdig.Syntax;
using Spectre.Console;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Tests.Core.Markdown.Renderers;

public class ListRendererTests
{
    [Fact]
    public void Render_TaskList_ProducesCorrectCheckboxes()
    {
        // Arrange
        var markdown = """
            - [x] Completed task
            - [ ] Incomplete task
            - [X] Another completed task
            """;

        var pipeline = new MarkdownPipelineBuilder()
            .UseTaskLists()
            .Build();

        var document = Markdig.Markdown.Parse(markdown, pipeline);
        var listBlock = document.OfType<ListBlock>().FirstOrDefault();
        var theme = CreateTestTheme();

        // Act
        var result = ListRenderer.Render(listBlock!, theme);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Markup>();

        var markup = (Markup)result;
        var text = markup.ToString();

        // Should contain checkboxes for completed and incomplete tasks
        text.Should().Contain("✅"); // Completed tasks
        text.Should().Contain("☐"); // Incomplete tasks
    }

    [Fact]
    public void Render_RegularList_ProducesBulletPoints()
    {
        // Arrange
        var markdown = """
            - First item
            - Second item
            - Third item
            """;

        var pipeline = new MarkdownPipelineBuilder()
            .UseTaskLists()
            .Build();

        var document = Markdig.Markdown.Parse(markdown, pipeline);
        var listBlock = document.OfType<ListBlock>().FirstOrDefault();
        var theme = CreateTestTheme();

        // Act
        var result = ListRenderer.Render(listBlock!, theme);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Markup>();

        var markup = (Markup)result;
        var text = markup.ToString();

        // Should contain bullet points
        text.Should().Contain("•");
        text.Should().NotContain("✅");
        text.Should().NotContain("☐");
    }

    [Fact]
    public void Render_OrderedList_ProducesNumbers()
    {
        // Arrange
        var markdown = """
            1. First item
            2. Second item
            3. Third item
            """;

        var pipeline = new MarkdownPipelineBuilder()
            .UseTaskLists()
            .Build();

        var document = Markdig.Markdown.Parse(markdown, pipeline);
        var listBlock = document.OfType<ListBlock>().FirstOrDefault();
        var theme = CreateTestTheme();

        // Act
        var result = ListRenderer.Render(listBlock!, theme);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Markup>();

        var markup = (Markup)result;
        var text = markup.ToString();

        // Should contain numbers
        text.Should().Contain("1.");
        text.Should().Contain("2.");
        text.Should().Contain("3.");
    }

    [Fact]
    public void Render_MixedTaskList_HandlesVariousCheckboxFormats()
    {
        // Arrange
        var markdown = """
            - [x] Lowercase x (checked)
            - [X] Uppercase X (checked)
            - [ ] Empty (unchecked)
            - Regular bullet point
            """;

        var pipeline = new MarkdownPipelineBuilder()
            .UseTaskLists()
            .Build();

        var document = Markdig.Markdown.Parse(markdown, pipeline);
        var listBlock = document.OfType<ListBlock>().FirstOrDefault();
        var theme = CreateTestTheme();

        // Act
        var result = ListRenderer.Render(listBlock!, theme);

        // Assert
        result.Should().NotBeNull();
        var markup = (Markup)result;
        var text = markup.ToString();

        // Should have 2 completed tasks and 1 incomplete task
        var completedCount = text.Count(c => c == '✅');
        var incompleteCount = text.Count(c => c == '☐');
        var bulletCount = text.Count(c => c == '•');

        completedCount.Should().Be(2, "Should have 2 completed tasks");
        incompleteCount.Should().Be(1, "Should have 1 incomplete task");
        bulletCount.Should().Be(1, "Should have 1 regular bullet point");
    }

    private static Theme CreateTestTheme()
    {
        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var registry = new Registry(registryOptions);
        return registry.GetTheme();
    }
}
