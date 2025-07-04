# Markdown Test File

This file is for testing all supported markdown features in PSTextMate.  

---

## Headings

# Heading 1

## Heading 2

### Heading 3

---

## Paragraphs and Line Breaks

This is a paragraph with a line break.  
This should be on a new line.  

This is a new paragraph after a blank line.  

---

## Emphasis

*Italic text* and **bold text** and ***bold italic text***.

---

## Links

[GitHub](https://github.com)  
[Blue styled link](https://example.com)  

---

## Lists

- Unordered item 1
- Unordered item 2
  - Nested item
- Unordered item 3

1. Ordered item 1
2. Ordered item 2
   1. Nested ordered item
3. Ordered item 3

---

## Blockquote

> This is a blockquote.
> It can span multiple lines.

---

## Code

Inline code: `Write-Host "Hello, World!"`

```
This is a fenced code block with no language.
```

```powershell
# PowerShell code block
Get-ChildItem $PWD
```

```csharp
// C# code block
public static StringBuilder AppendWithStyle(this StringBuilder builder, Style? style, string? value)
{
    value ??= string.Empty;
    if (style is not null)
    {
        return builder.Append('[')
            .Append(style.ToMarkup())
            .Append(']')
            .Append(value.EscapeMarkup())
            .Append("[/]");
    }
    return builder.Append(value);
}
```

---

## Table

| Name    | Value |
|---------|-------|
| Alpha   | 1     |
| Beta    | 2     |
| Gamma   | 3     |

---

## Images

![Alt text](https://via.placeholder.com/100x50.png)

---

## Horizontal Rule

---

## Task List

- [x] Task 1
- [ ] Task 2
- [ ] Task 3

---

## HTML

<div style="color: red;">This is raw HTML and may not render in all markdown processors.</div>  

---

## Escaped Characters

\*This is not italic\*
\# Not a heading

---

## Second Table

| Name    | Text  |
|---------|-------|
| Foo     | Bar   |
| Hello   | World |

---
End of test file.  
