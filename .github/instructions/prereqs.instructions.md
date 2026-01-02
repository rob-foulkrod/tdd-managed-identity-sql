---
applyTo: "prereqs.md"
---

# Prerequisites File Best Practices

The prereqs.md file lists tools and requirements that users need before deploying the template. This file is parsed by the Trainer Demo Deploy catalog and rendered separately from the README.

## File Structure

Always start with these comment instructions (do not modify):

```markdown
[comment]: <> (list up any scenario-specific prerequisites the user needs to have installed, to guarantee a successful deployment)
[comment]: <> (typical use case could be a specific Dev Language SDK like .NET 6)
[comment]: <> (don't add any other information, as this is rendered as part of a prereqs element on the webpage)
```

## Content Guidelines

### What to Include
- **Development SDKs/Frameworks** (.NET, Node.js, Python)
- **Container platforms** (Docker Desktop)
- **Command-line tools** (Azure CLI, kubectl, Helm, psql)
- **Scripting engines** (PowerShell)
- **Build tools** (npm, Azure Functions Core Tools)

### What NOT to Include
- Azure Developer CLI (azd) - automatically listed in catalog
- GitHub CLI - auto-installed with azd
- Bicep CLI - auto-installed with azd
- Explanatory text or descriptions
- Azure subscription requirements (goes in README)
- Azure permissions (goes in README Prerequisites section)

## Formatting Standards

### Basic Format
```markdown
- [Tool Name with Version](exact-url-to-download-or-install-page)
```

### Naming Conventions

**PowerShell:**
```markdown
- [PowerShell 7+](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
```
- Use capital P and S
- Specify version: "7+" not just "PowerShell"

**.NET SDK:**
```markdown
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
```
- Use official naming: `.NET` not `Dotnet` or `.NET Core`
- Specify SDK version explicitly

**Azure CLI:**
```markdown
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
```
- Use space between "Azure" and "CLI"

**Docker Desktop:**
```markdown
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
```

**Node.js:**
```markdown
- [Node.js 18+](https://nodejs.org/)
```

**Container Tools:**
```markdown
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [Helm](https://helm.sh/docs/intro/install/)
```

### Optional Prerequisites
Mark optional items explicitly:
```markdown
- [PowerShell 7+ (optional)](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
- [Azure CLI (optional)](https://learn.microsoft.com/cli/azure/install-azure-cli)
```

## URL Standards

### Preferred URLs
- **Microsoft tools**: Use Microsoft Learn documentation
  - `https://learn.microsoft.com/...`
- **Third-party tools**: Use official vendor download pages
  - Docker: `https://www.docker.com/products/docker-desktop`
  - Node.js: `https://nodejs.org/`
- **Open source tools**: Use official project sites
  - kubectl: `https://kubernetes.io/docs/tasks/tools/`

## Ordering Recommendations

List prerequisites in this order:
1. Development SDKs (.NET, Node.js, Python)
2. Container platforms (Docker Desktop)
3. Command-line tools (Azure CLI, kubectl, Helm)
4. Scripting engines (PowerShell)
5. Database client tools (psql, sqlcmd)

## Common Patterns

### Minimal File (Infrastructure-only)
```markdown
[comment]: <> (list up any scenario-specific prerequisites the user needs to have installed, to guarantee a successful deployment)
[comment]: <> (typical use case could be a specific Dev Language SDK like .NET 6)
[comment]: <> (don't add any other information, as this is rendered as part of a prereqs element on the webpage)

- [PowerShell 7+](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
```

### Application Deployment
```markdown
[comment]: <> (list up any scenario-specific prerequisites the user needs to have installed, to guarantee a successful deployment)
[comment]: <> (typical use case could be a specific Dev Language SDK like .NET 6)
[comment]: <> (don't add any other information, as this is rendered as part of a prereqs element on the webpage)

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PowerShell 7+](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
```

## Anti-Patterns to Avoid

❌ **Don't write prose**: "For security reasons having Microsoft Defender for Cloud would be helpful"
✅ **Do use bullet list**: `- [Tool Name](url)`

❌ **Don't explain usage**: "PowerShell is needed to run the deployment hooks"
✅ **Do list only**: `- [PowerShell 7+](url)`

❌ **Don't use inconsistent naming**: "Dotnet 8", ".NET Core 8 SDK", "dotnet"
✅ **Do use official naming**: `.NET 8 SDK`

❌ **Don't list azd tools**: "Azure Developer CLI", "GitHub CLI", "Bicep"
✅ **Do list project-specific tools only**

❌ **Don't break formatting**: Paragraphs, headings, or multiple sections
✅ **Do keep it simple**: Comment block + bullet list only

## When to Skip prereqs.md

If your template has NO additional prerequisites beyond azd, you can:
- Omit the prereqs.md file entirely, OR
- Include only the comment block with no bullets

The catalog will show "No additional prerequisites" automatically.