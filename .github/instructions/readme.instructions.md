---
applyTo: "README.md"
---

# Trainer Demo Deploy README Best Practices

When editing or creating README.md files for Trainer Demo Deploy projects:

## Structure Requirements

1. **Opening Section:**
   - Start with clear title: [Technology] + [Purpose/Scenario]
   - 1-2 sentence description answering: What gets deployed? What scenario? Who's the audience?
   - Include catalog badge: 💪 This template scenario is part of the larger **[Microsoft Trainer Demo Deploy Catalog](https://aka.ms/trainer-demo-deploy)**.

2. **What You'll Deploy Section (📋):**
   - List all Azure resources that will be created
   - Include resource counts and SKU types
   - **SHOULD include estimated cost:** `$X/day` or `$X-Y/day`

3. **Architecture Section (🏗️):**
   - Include ASCII diagram or reference to architecture diagram
   - Explain data/control flow in 2-3 sentences
   - Note security boundaries if relevant

4. **Deployment Time (⏰):**
   - **SHOULD include time estimate:** "Approximately X minutes"
   - This is important for trainers to plan class timing

5. **Prerequisites (⬇️):**
   - List ALL tools (not just azd)
   - Include local development tools if needed (Docker, SDKs, etc.)
   - Specify Azure permissions required
   - Note region-specific or quota requirements

6. **Deploy in 3 Steps (🚀):**
   - Use the standard 3-command pattern:
     1. `azd init -t [owner/repo]`
     2. `azd up`
     3. `azd down --purge --force`
   - Do NOT use 4-step pattern with mkdir/cd

7. **Verify Deployment (✅):**
   - **MUST include verification steps**
   - How to confirm deployment succeeded
   - Where to find resources in Portal
   - What endpoints to test
   - Expected results/behavior

8. **What You'll Demonstrate (🎓):**
   - **MUST include demo talking points**
   - Organize by skill level or topic area
   - Include certification alignment when relevant (e.g., "Aligned with AZ-104")
   - This is critical for trainers to prepare

9. **Demo Guide (📚):**
   - Link to demoguide with full GitHub URL (not relative path)
   - Use format: `https://github.com/[owner]/[repo]/blob/main/Demoguide/[file].md`
   - Indicate status: "Complete guide available" or "Under development"

10. **Troubleshooting (🐛):**
    - Include common issues and solutions
    - How to access logs
    - Where to check for errors

11. **Cost Management (💰):**
    - List SKUs deployed
    - Estimated hourly/daily cost
    - Cost-saving tips (e.g., when to run azd down)
    - Production readiness warning if using minimal SKUs

## Highly Recommended Elements for Trainers

These elements significantly improve documentation quality and trainer readiness:

- ⏰ **Deployment time** - Helps trainers plan class timing
- 💰 **Cost estimate** - Supports budget planning
- ✅ **Verification steps** - Confirms deployment success
- 🎓 **Demo talking points** - Guides what to emphasize
- 🐛 **Troubleshooting** - Helps resolve common issues

## Emoji Consistency

Use these standard emojis for section headers:
- 📋 What You'll Deploy
- 🏗️ Architecture
- ⏰ Deployment Time
- ⬇️ Prerequisites
- 🚀 Deploy in 3 Steps
- ✅ Verify Deployment
- 🎓 What You'll Demonstrate
- 📚 Demo Guide
- 🔧 What's Automatically Configured
- 🐛 Troubleshooting
- 💰 Cost Management
- 🎯 Training Scenarios
- 📖 Additional Resources
- 💭 Feedback and Contributing

## Link Formatting

External tools and catalog aggregators read README files, so use full GitHub URLs instead of relative paths:

### Demo Guide Links
- Use full GitHub blob URLs: `https://github.com/[owner]/[repo]/blob/main/Demoguide/[file].md`
- NOT relative paths like: `Demoguide/file.md` or `./Demoguide/file.md`
- This ensures links work in the Trainer Demo Deploy catalog and other tools

### Image Links
- Use raw GitHub URLs for images: `https://raw.githubusercontent.com/[owner]/[repo]/main/path/to/image.png`
- Or use GitHub blob URLs: `https://github.com/[owner]/[repo]/blob/main/path/to/image.png?raw=true`
- NOT relative paths like: `./images/diagram.png`
- This ensures images render in external tools and documentation aggregators

### Format Examples
```markdown
✅ Good: [Demo Guide](https://github.com/owner/repo/blob/main/Demoguide/demo.md)
❌ Bad: [Demo Guide](Demoguide/demo.md)

✅ Good: ![Architecture](https://raw.githubusercontent.com/owner/repo/main/docs/architecture.png)
❌ Bad: ![Architecture](./docs/architecture.png)
```

## Tone and Style

- Write for technical trainers, not end users
- Focus on teaching moments and demo opportunities
- Include "what to emphasize" guidance
- Note architectural patterns worth highlighting
- Mention opportunities for comparison (IaaS vs PaaS, etc.)

## Anti-Patterns to Avoid

- ❌ Missing time or cost estimates
- ❌ No verification steps after deployment
- ❌ Prerequisites listing only azd where there are other requirements
- ❌ No demo talking points
- ❌ Relative paths for demo guide links
- ❌ 4-step deployment with mkdir/cd
- ❌ "Coming soon" without timeline
- ❌ Missing troubleshooting section

## Production Warnings

When demos intentionally include security gaps or use minimal SKUs for cost reasons, include a warning:

> [!IMPORTANT]  
> This template uses [describe intentional limitations] for training demos. Review and harden [specific areas] before production use.
