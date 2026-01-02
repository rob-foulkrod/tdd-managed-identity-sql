---
applyTo: "azure.yaml"
---

# Azure.yaml Configuration Best Practices

The azure.yaml file is the core configuration for Azure Developer CLI (azd) templates. It defines the project structure, infrastructure provider, services, and lifecycle hooks.

## Required Schema Header

Always include this as the first line:
```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json
```

This enables IDE validation and IntelliSense in VS Code and other editors.

## Required Fields

### 1. Project Name
```yaml
name: tdd-[technology-or-purpose]
```

**Naming conventions:**
- Use lowercase with hyphens as separators
- Start with `tdd-` prefix (Trainer Demo Deploy)
- Be descriptive but concise
- Examples:
  - `tdd-azd-firewall`
  - `tdd-azd-aks-demo`
  - `tdd-functions-http-eventhubs`

**Avoid:**
- CamelCase or PascalCase
- Underscores
- Spaces
- Version numbers in name

### 2. Metadata
```yaml
metadata:
  template: tdd-[same-as-name]@[version]
```

**Version format:**
- Use semantic versioning: `major.minor.patch`
- Examples:
  - `@1.0.0` - First stable release
  - `@1.2.3` - Subsequent updates
  - `@0.0.1-beta` - Pre-release/testing
  - `@0.1.0` - Initial development

**Avoid:**
- `v` prefix (use `1.0.0` not `v1.0.0`)
- Non-semantic versions (use `1.0.0` not `latest`)

### 3. Infrastructure Provider
```yaml
infra:
  provider: "bicep"
```

**Standard format:**
- Quote the provider name
- Use `"bicep"` for Bicep templates (99% of cases)
- Use `"terraform"` only if using Terraform

## Optional Infrastructure Settings

### Custom Infrastructure Path
```yaml
infra:
  provider: "bicep"
  path: "./infra"
  module: "main"
```

**When to use:**
- `path`: Only if infra is NOT in default `./infra` directory
- `module`: Only if entry point is NOT `main.bicep`

### Infrastructure Parameters
```yaml
infra:
  provider: "bicep"
  parameters:
    environmentName: ${AZURE_ENV_NAME}
    location: ${AZURE_LOCATION}
    whitelistPublicIp: ${PUBLIC_IP}
```

**Use environment variables for:**
- Dynamic values (IP addresses, locations)
- User-specific settings
- Values set by azd automatically
- Values set in hooks

**Common environment variables:**
- `${AZURE_ENV_NAME}` - Environment name
- `${AZURE_LOCATION}` - Azure region
- `${AZURE_SUBSCRIPTION_ID}` - Subscription ID
- `${AZURE_PRINCIPAL_ID}` - User principal ID

## Services Section (Optional)

Include services section ONLY when deploying applications (not infrastructure-only templates).

### Basic Service Definition
```yaml
services:
  web:
    project: ./src/web
    language: dotnet
    host: appservice
```

### Service Fields

**project:** (Required)
- Path to application code
- Examples: `./`, `./src/api`, `./app`

**language:** (Required)
- Use full names: `dotnet`, `python`, `javascript`
- Avoid abbreviations: Use `python` not `py`

**host:** (Required)
- `appservice` - Azure App Service
- `function` - Azure Functions
- `containerapp` - Azure Container Apps
- `aks` - Azure Kubernetes Service

### Container Service Example
```yaml
services:
  api:
    project: ./src/api
    language: python
    host: containerapp
    docker:
      path: ./Dockerfile
      context: ./
      remoteBuild: true
      registry: ${AZURE_CONTAINER_REGISTRY_ENDPOINT}
```

### Multiple Services
```yaml
services:
  web:
    project: ./src/web
    language: dotnet
    host: appservice
  
  api:
    project: ./src/api
    language: python
    host: function
```

## Hooks Section (Optional)

Hooks automate tasks during the azd lifecycle.

### Available Hooks

**Provisioning:**
- `preup` - Before entire up workflow
- `preprovision` - Before infrastructure provisioning
- `postprovision` - After infrastructure provisioning

**Deployment:**
- `prepackage` - Before packaging application
- `predeploy` - Before deploying application
- `postdeploy` - After deploying application

**Cleanup:**
- `predown` - Before deleting resources
- `postdown` - After deleting resources

### Simple Hook Example
```yaml
hooks:
  postprovision:
    shell: pwsh
    run: ./scripts/configure.ps1
    interactive: true
```

### Cross-Platform Hook Example
```yaml
hooks:
  postdeploy:
    windows:
      shell: pwsh
      run: ./scripts/setup.ps1
      interactive: true
    posix:
      shell: sh
      run: ./scripts/setup.sh
      interactive: true
```

### Inline Script Example
```yaml
hooks:
  preup:
    shell: pwsh
    run: |
      $ip = (Invoke-WebRequest -Uri "http://ifconfig.me/ip").Content.Trim()
      azd env set PUBLIC_IP $ip
    interactive: true
```

### Multiple Hooks Example
```yaml
hooks:
  postprovision:
    - shell: pwsh
      run: ./scripts/rbac.ps1
      continueOnError: false
    - shell: pwsh
      run: ./scripts/data.ps1
      continueOnError: true
```

### Hook Fields

**shell:** (Required)
- `pwsh` - PowerShell Core (cross-platform, recommended)
- `sh` - Bash/shell
- `cmd` - Windows Command Prompt

**run:** (Required)
- Path to script file, OR
- Inline script using `|` multiline syntax

**interactive:** (Optional)
- `true` - Allow user interaction (prompts, confirmations)
- `false` - Run non-interactively

**continueOnError:** (Optional)
- `true` - Continue workflow if hook fails
- `false` - Stop workflow on failure (default)

### Common Hook Use Cases

**1. Detect Public IP for Firewall Rules**
```yaml
hooks:
  preup:
    shell: pwsh
    run: |
      $ip = (Invoke-WebRequest -Uri "http://ifconfig.me/ip").Content.Trim()
      azd env set PUBLIC_IP $ip
    interactive: true
```

**2. Grant RBAC Permissions**
```yaml
hooks:
  postprovision:
    shell: pwsh
    run: ./scripts/assign-roles.ps1
    interactive: true
```

**3. Build and Push Container Images**
```yaml
hooks:
  postprovision:
    shell: pwsh
    run: ./scripts/build-containers.ps1
    interactive: false
```

**4. Configure Application Settings**
```yaml
hooks:
  postdeploy:
    shell: pwsh
    run: ./scripts/configure-appsettings.ps1
    interactive: true
```

**5. Clean Up Resources**
```yaml
hooks:
  predown:
    shell: pwsh
    run: ./scripts/cleanup-app-registrations.ps1
    interactive: true
```

## Required Versions (Rare)

Only include when using azd features that require specific versions:

```yaml
requiredVersions:
  azd: ">= 1.17.0"
```

**When to use:**
- New azd CLI features (e.g., Microsoft.Graph extension support)
- Specific bug fixes required
- Breaking changes in azd behavior

**Default:** If omitted, any azd version works

## Complete Examples

### Infrastructure-Only Template
```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

name: tdd-azd-firewall
metadata:
  template: tdd-azd-firewall@1.0.0
infra:
  provider: "bicep"
```

### Application with Services
```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

name: tdd-azd-webapp
metadata:
  template: tdd-azd-webapp@1.2.0
infra:
  provider: "bicep"
  parameters:
    environmentName: ${AZURE_ENV_NAME}
    location: ${AZURE_LOCATION}
    principalId: ${AZURE_PRINCIPAL_ID}

services:
  web:
    project: ./src
    language: dotnet
    host: appservice
```

### Complex with Hooks
```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

name: tdd-azd-secure-webapp
metadata:
  template: tdd-azd-secure-webapp@1.5.0
infra:
  provider: "bicep"
  parameters:
    environmentName: ${AZURE_ENV_NAME}
    location: ${AZURE_LOCATION}
    publicIp: ${PUBLIC_IP}

services:
  web:
    project: ./src/web
    language: python
    host: containerapp
    docker:
      path: ./Dockerfile
      remoteBuild: true

hooks:
  preup:
    shell: pwsh
    run: |
      $ip = (Invoke-WebRequest -Uri "http://ifconfig.me/ip").Content.Trim()
      azd env set PUBLIC_IP $ip
    interactive: true
  
  postprovision:
    windows:
      shell: pwsh
      run: ./scripts/setup-rbac.ps1
      interactive: true
    posix:
      shell: sh
      run: ./scripts/setup-rbac.sh
      interactive: true
  
  postdeploy:
    shell: pwsh
    run: ./scripts/configure-app.ps1
    interactive: false
```

## Anti-Patterns to Avoid

❌ **Don't skip schema header**: Always include yaml-language-server comment
❌ **Don't use inconsistent naming**: Use `tdd-` prefix consistently
❌ **Don't hardcode values**: Use environment variables in parameters
❌ **Don't include services for infra-only**: Only add services when deploying code
❌ **Don't use abbreviated languages**: Use `python` not `py`
❌ **Don't use unquoted provider**: Prefer `"bicep"` over `bicep`
❌ **Don't create Windows-only hooks**: Support both Windows and POSIX when possible
❌ **Don't use version prefixes**: Use `1.0.0` not `v1.0.0`
❌ **Don't mix hook formats**: Be consistent with script paths vs inline