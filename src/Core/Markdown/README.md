# Markdown Renderer Architecture

This document describes the refactored markdown rendering architecture that replaced the monolithic `MarkdigSpectreMarkdownRenderer` class.  

## Overview

The markdown rendering functionality has been split into focused, single-responsibility components organized in the `Core/Markdown` folder structure for better maintainability and testing.  

## Folder Structure

```
src/Core/Markdown/
├── MarkdownRenderer.cs           # Main orchestrator
├── InlineProcessor.cs            # Inline element processing
└── Renderers/                    # Block-specific renderers
    ├── BlockRenderer.cs          # Main dispatcher
    ├── HeadingRenderer.cs        # Heading blocks
    ├── ParagraphRenderer.cs      # Paragraph blocks
    ├── ListRenderer.cs           # List and task list blocks
    ├── CodeBlockRenderer.cs      # Fenced/indented code blocks
    ├── TableRenderer.cs          # Table blocks
    ├── QuoteRenderer.cs          # Quote blocks
    ├── HtmlBlockRenderer.cs      # HTML blocks
    └── HorizontalRuleRenderer.cs # Horizontal rules
```

## Component Responsibilities

### MarkdownRenderer
- **Purpose**: Main entry point for markdown rendering
- **Responsibilities**: 
  - Creates Markdig pipeline with extensions
  - Parses markdown document
  - Orchestrates block rendering
  - Manages spacing between elements

### InlineProcessor
- **Purpose**: Handles all inline markdown elements
- **Responsibilities**:
  - Processes inline text extraction
  - Handles emphasis (bold/italic)
  - Processes links and images
  - Manages inline code styling
  - Applies theme-based styling

### BlockRenderer
- **Purpose**: Dispatches block elements to specific renderers
- **Responsibilities**:
  - Pattern matches block types
  - Routes to appropriate specialized renderer
  - Maintains clean separation of concerns

### Specialized Renderers

Each renderer handles a specific block type with focused responsibilities:

- **HeadingRenderer**: H1-H6 headings with theme-aware styling
- **ParagraphRenderer**: Text paragraphs with inline processing
- **ListRenderer**: Ordered/unordered lists and task lists with checkbox support
- **CodeBlockRenderer**: Syntax-highlighted code blocks (fenced and indented)
- **TableRenderer**: Complex table rendering with headers and data rows
- **QuoteRenderer**: Blockquotes with bordered panels
- **HtmlBlockRenderer**: Raw HTML blocks with syntax highlighting
- **HorizontalRuleRenderer**: Thematic breaks and horizontal rules

## Key Features

### Task List Support
- Detects `[x]`, `[X]`, and `[ ]` checkbox syntax
- Renders with Unicode checkbox characters (☑️, ☐)
- Automatically strips checkbox markup from displayed text

### Theme Integration
- Full TextMate theme support across all elements
- Consistent color and styling application
- Fallback styling for unsupported elements

### Performance Optimizations
- StringBuilder usage for efficient text building
- Batch processing where possible
- Minimal object allocation
- Escape markup handling optimized per context

### Image Handling
- Special image link rendering with emoji indicators
- Styled image descriptions
- URL display for accessibility

### Code Highlighting
- TextMateProcessor integration for syntax highlighting
- Language-specific panels with headers
- Fallback rendering for unsupported languages
- Proper markup escaping in code blocks

## Migration Notes

### Backward Compatibility
The original `MarkdigSpectreMarkdownRenderer` class remains as a legacy wrapper that delegates to the new implementation, ensuring existing code continues to work without changes.  

### Usage
```csharp
// New way (recommended)
var result = MarkdownRenderer.Render(markdown, theme, themeName);

// Old way (still works via delegation)
var result = MarkdigSpectreMarkdownRenderer.Render(markdown, theme, themeName);
```

## Benefits of Refactoring

1. **Maintainability**: Each component has a single responsibility
2. **Testability**: Individual renderers can be unit tested in isolation
3. **Extensibility**: New block types can be added without modifying existing code
4. **Readability**: Clear separation of concerns makes code easier to understand
5. **Performance**: Optimized processing paths for different element types
6. **Debugging**: Issues can be isolated to specific renderer components

## Future Enhancements

The modular architecture makes it easy to add:
- Custom block renderers
- Additional inline element processors
- Enhanced theme customization
- Performance monitoring per renderer
- Caching strategies per component type
