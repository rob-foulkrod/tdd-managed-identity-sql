$ErrorActionPreference = 'Stop'

# This script runs after provisioning to:
# 1) Configure the SQL Server Azure AD admin (when possible)
# 2) Create a least-privilege DB role and add Managed Identities to it

$envJson = azd env get-values --output json | Out-String | ConvertFrom-Json

# Optional switch to skip SQL bootstrapping.
# Set with: azd env set SQL_BOOTSTRAP_ENABLED false
$bootstrapEnabledRaw = $envJson.SQL_BOOTSTRAP_ENABLED
if ([string]::IsNullOrWhiteSpace($bootstrapEnabledRaw)) {
  $bootstrapEnabledRaw = $env:SQL_BOOTSTRAP_ENABLED
}



$rg = $envJson.RESOURCE_GROUP_NAME
$sqlServerName = $envJson.SQL_SERVER_NAME
$sqlServerFqdn = $envJson.SQL_SERVER_FQDN
$dbName = $envJson.SQL_DATABASE_NAME
$appServiceName = $envJson.APP_SERVICE_NAME
$uamiName = $envJson.USER_ASSIGNED_MI_NAME
$uamiClientId = $envJson.USER_ASSIGNED_MI_CLIENT_ID
$appEndpoint = $envJson.APP_ENDPOINT
$publicIp = $envJson.PUBLIC_IP

if ([string]::IsNullOrWhiteSpace($rg) -or [string]::IsNullOrWhiteSpace($sqlServerName) -or [string]::IsNullOrWhiteSpace($sqlServerFqdn) -or [string]::IsNullOrWhiteSpace($dbName) -or [string]::IsNullOrWhiteSpace($appServiceName)) {
  throw "Missing required azd outputs. Ensure 'azd provision' completed successfully and outputs are present."
}


# Print the key values instructors commonly need for manual steps.
Write-Host ''
Write-Host '--- Demo values (copy/paste) ---'
if (-not [string]::IsNullOrWhiteSpace($appEndpoint)) { Write-Host "APP_ENDPOINT=$appEndpoint" }
Write-Host "APP_SERVICE_NAME=$appServiceName"
Write-Host "SQL_SERVER_FQDN=$sqlServerFqdn"
Write-Host "SQL_DATABASE_NAME=$dbName"
if (-not [string]::IsNullOrWhiteSpace($uamiName)) { Write-Host "USER_ASSIGNED_MI_NAME=$uamiName" }
if (-not [string]::IsNullOrWhiteSpace($uamiClientId)) { Write-Host "USER_ASSIGNED_MI_CLIENT_ID=$uamiClientId" }
Write-Host '------------------------------'
Write-Host ''


if (-not [string]::IsNullOrWhiteSpace($bootstrapEnabledRaw)) {
  $disabledValues = @('0', 'false', 'no', 'off')
  if ($disabledValues -contains $bootstrapEnabledRaw.ToString().Trim().ToLowerInvariant()) {
    Write-Host 'SQL bootstrap is disabled (SQL_BOOTSTRAP_ENABLED=false). Skipping postprovision SQL setup.'
    exit 0
  }
}


Write-Host "Bootstrapping SQL permissions for database '$dbName' on server '$sqlServerFqdn'";


# Step 0: Ensure the current workstation IP is allowed by SQL firewall.
# We do this here (in addition to any IaC rule) because azd may skip infra updates and still run hooks.
try {
  if ([string]::IsNullOrWhiteSpace($publicIp)) {
    $publicIp = (Invoke-RestMethod -Uri 'https://api.ipify.org').ToString().Trim()
  }

  if (-not [string]::IsNullOrWhiteSpace($publicIp)) {
    $ruleName = 'AzdBootstrapperIp'
    Write-Host "Ensuring SQL firewall allows current IP: $publicIp"

    # Create-or-update behavior
    $existingRule = $null
    try {
      $existingRule = az sql server firewall-rule show -g $rg -s $sqlServerName -n $ruleName -o json 2>$null | ConvertFrom-Json
    }
    catch {
      $existingRule = $null
    }

    if ($existingRule) {
      az sql server firewall-rule update -g $rg -s $sqlServerName -n $ruleName --start-ip-address $publicIp --end-ip-address $publicIp | Out-Null
    }
    else {
      az sql server firewall-rule create -g $rg -s $sqlServerName -n $ruleName --start-ip-address $publicIp --end-ip-address $publicIp | Out-Null
    }
  }
  else {
    Write-Warning 'PUBLIC_IP is empty; cannot create a workstation firewall rule. SQL bootstrap may fail if your IP is blocked.'
  }
}
catch {
  Write-Warning "Unable to ensure SQL firewall rule for workstation IP. You may need to add it manually. Details: $($_.Exception.Message)"
}

# Step 1: Ensure Azure AD admin exists for the SQL Server.
# Without an AAD admin, AAD token connections to Azure SQL will fail.
try {
  $existingAdmin = az sql server ad-admin list -g $rg -s $sqlServerName -o json | ConvertFrom-Json
  if (-not $existingAdmin -or $existingAdmin.Count -eq 0) {
    Write-Host 'No SQL Azure AD admin found. Attempting to set it to the signed-in user...'

    $signedIn = az ad signed-in-user show -o json | ConvertFrom-Json
    $objectId = $signedIn.id
    $displayName = $signedIn.userPrincipalName
    if ([string]::IsNullOrWhiteSpace($displayName)) { $displayName = $signedIn.displayName }

    if ([string]::IsNullOrWhiteSpace($objectId) -or [string]::IsNullOrWhiteSpace($displayName)) {
      throw 'Unable to resolve signed-in user object id/display name.'
    }

    az sql server ad-admin create -g $rg -s $sqlServerName --display-name $displayName --object-id $objectId | Out-Null
    Write-Host "Set SQL Azure AD admin to '$displayName'"
  }
  else {
    Write-Host 'SQL Azure AD admin already configured.'
  }
}
catch {
  Write-Warning "Unable to auto-configure SQL Azure AD admin. You may need to set it manually in Portal before Managed Identity auth will work. Details: $($_.Exception.Message)"
}

# Step 2: Create database role and users using AAD token (AzureCliCredential).
# This requires the SQL Azure AD admin step above to succeed.
$toolProject = Join-Path $PSScriptRoot '..\src\tools\SqlBootstrapper\SqlBootstrapper.csproj'

$dotnetArgs = @(
  'run',
  '--project', $toolProject,
  '--',
  '--server', $sqlServerFqdn,
  '--database', $dbName,
  '--role', 'catalog_reader',
  '--schema', 'SalesLT',
  '--systemAssigned', $appServiceName
)

# Optional: pre-create UAMI database user + role membership so the conversion is only app config.
if (-not [string]::IsNullOrWhiteSpace($uamiName)) {
  $dotnetArgs += @('--userAssigned', $uamiName)
}

Write-Host 'Running SQL bootstrapper...' 

$maxAttempts = 10
$delaySeconds = 20

for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
  $output = & dotnet @dotnetArgs 2>&1
  $exitCode = $LASTEXITCODE

  if ($exitCode -eq 0) {
    break
  }

  $text = ($output | Out-String)

  if ($text -match 'Client with IP address .* is not allowed to access the server') {
    if ($attempt -lt $maxAttempts) {
      Write-Warning "SQL firewall change may still be propagating. Retrying in $delaySeconds seconds... (attempt $attempt/$maxAttempts)"
      Start-Sleep -Seconds $delaySeconds
      continue
    }
  }

  Write-Host $text
  throw "SQL bootstrapper failed with exit code $exitCode"
}

Write-Host 'SQL permissions bootstrapped successfully.'
