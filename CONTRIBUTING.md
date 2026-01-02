# Contributing to Trainer Demo Deploy Catalog

Thank you for your interest in contributing a template to the [Trainer Demo Deploy Catalog](https://aka.ms/trainer-demo-deploy)!

This starter template provides a foundation for creating azd-compatible templates that trainers can use for demos and hands-on labs.

## 🚀 Getting Started

### 1. Clone This Starter

```powershell
azd init -t petender/tdd-azd-starter
```

### 2. Customize Your Template

Update the following files with your scenario details:

#### **Required Updates:**

- **README.md** - Follow the structure defined in .github/instructions/readme.instructions.md
  - Replace all [YOUR-PLACEHOLDER] text
  - Include cost estimates, deployment time, architecture diagram
  - Add verification steps and troubleshooting guidance

- **azure.yaml** - Update project metadata
  - Change name to your scenario name (e.g., 	dd-my-scenario)
  - Update metadata.template version (use semantic versioning: 1.0.0)
  - Add hooks section if you need lifecycle scripts

- **infra/main.bicep** - Define your infrastructure
  - Uncomment and customize the resources module reference
  - Add any additional parameters needed
  - Ensure SecurityControl: Ignore tag is present

- **infra/resources.bicep** - Add your Azure resources
  - Replace the example storage account with your resources
  - Follow patterns in .github/instructions/bicep.instructions.md
  - Add outputs for connection strings and endpoints

- **prereqs.md** - List scenario-specific prerequisites
  - Replace PowerShell example with your actual requirements
  - Follow naming standards in .github/instructions/prereqs.instructions.md
  - Common options: .NET SDK, Node.js, Docker Desktop, Azure CLI

- **demoguide/demoguide.md** - Create demo instructions
  - Replace all [YOUR-PLACEHOLDER] text
  - Add screenshots showing deployed resources
  - Provide step-by-step demo instructions

#### **Optional Updates:**

- **src/** - Add application code if your template includes services
- **scripts/** - Add PowerShell scripts if using hooks in azure.yaml
- **demoguide/screenshots/** - Add screenshots organized in subfolders

### 3. Test Locally

Test your template thoroughly before submitting:

```powershell
# Initialize and provision
azd up

# Verify all resources are created
# Test demo scenarios
# Verify outputs in .env file

# Clean up
azd down
```

**Testing Checklist:**
- [ ] Deployment completes without errors
- [ ] All resources appear in Azure Portal with correct tags
- [ ] Outputs are populated in .env file
- [ ] Demo steps work as documented
- [ ] Cost is reasonable for training scenarios
- [ ] Clean up removes all resources
- [ ] If using GitHub Copilot, verify the /tdd-review prompt

### 4. Publish to GitHub

Create a new **public** repository on GitHub (via web UI or CLI), then push your template:

```powershell
# Set your repository URL
$repoUrl = "https://github.com/your-username/tdd-my-scenario.git"

# Initialize git repository
git init
git add .
git commit -m "Initial commit: TDD template for [your scenario]"

# Add remote and push
git branch -M main
git remote add origin $repoUrl
git push -u origin main
```

Verify all required files are committed:
- [ ] README.md with complete documentation
- [ ] demoguide/demoguide.md with screenshots
- [ ] Architecture diagram (referenced in README)
- [ ] All .bicep files and azure.yaml
- [ ] CONTRIBUTING.md and prereqs.md

### 5. Submit to Catalog

Follow the official contribution process at:  
**https://microsoftlearning.github.io/trainer-demo-deploy/docs/contribute**

## 📋 Quality Standards

Your template should meet these standards:

### **Documentation**
- ✅ README is descriptive (see readme.instructions.md)
- ✅ Includes cost estimates and deployment time
- ✅ Has clear verification steps and troubleshooting
- ✅ Links use raw GitHub URLs (not relative paths)
- ✅ Clear Demo guide with screenshots and steps

### **Infrastructure**
- ✅ Includes SecurityControl: Ignore tag
- ✅ Follows abbreviations + resourceToken naming pattern
- ✅ Has descriptive @description decorators on all parameters
- ✅ Outputs use UPPER_SNAKE_CASE

### **Demo-Readiness**
- ✅ Deploys in 20 minutes or less
- ✅ Clear demo steps with screenshots
- ✅ Cost-effective for training (-20/day typical)
- ✅ Easy to verify successful deployment

### **Security**
- ✅ No hardcoded credentials
- ✅ Uses managed identities where possible
- ✅ Follows least-privilege RBAC patterns
- ✅ No secrets in outputs

## 🎯 Best Practices

Refer to these instruction files for detailed guidance:

- .github/instructions/readme.instructions.md - README structure and content
- .github/instructions/azure-yaml.instructions.md - Azure.yaml configuration
- .github/instructions/bicep.instructions.md - Bicep coding standards
- .github/instructions/prereqs.instructions.md - Prerequisites formatting

## 💡 Tips

- **Keep it simple** - Focus on demonstrating specific Azure capabilities
- **Be cost-conscious** - Use Basic/Standard SKUs, avoid expensive resources
- **Test thoroughly** - Deploy multiple times to ensure reliability
- **Document well** - Trainers need to understand quickly what to demonstrate
- **Use feature flags** - Add bool parameters for optional expensive resources (Bastion, VPN Gateway)

## 🤝 Questions?

- Review existing templates in the [catalog](https://aka.ms/trainer-demo-deploy)
- Check the [documentation](https://microsoftlearning.github.io/trainer-demo-deploy/)
- Review the instruction files in .github/instructions/

Thank you for contributing to the Trainer Demo Deploy ecosystem!
