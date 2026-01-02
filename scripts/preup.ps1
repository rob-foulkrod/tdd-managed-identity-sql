$ErrorActionPreference = 'Stop'

# Capture public IP for optional SQL firewall rule (trainer-friendly for query editor / Azure Data Studio).
try {
  $ip = (Invoke-RestMethod -Uri 'https://api.ipify.org').ToString().Trim()
  if ($ip) {
    Write-Host "Setting PUBLIC_IP=$ip"
    azd env set PUBLIC_IP $ip | Out-Null
  }
}
catch {
  Write-Warning "Unable to resolve public IP. SQL firewall rule for workstation access will be skipped. Details: $($_.Exception.Message)"
}

# Ensure a SQL admin password exists in the azd environment.
# NOTE: We avoid generating passwords in Bicep (newGuid anti-pattern). This keeps infra deterministic and trainer-friendly.
$envJson = azd env get-values --output json | Out-String | ConvertFrom-Json

# Optional: ask whether to run postprovision SQL bootstrapping.
# azd only auto-prompts for *infrastructure parameters*; for environment toggles like this,
# the recommended pattern is to prompt in a hook and store the value in the azd environment.
if (-not $envJson.SQL_BOOTSTRAP_ENABLED -or [string]::IsNullOrWhiteSpace($envJson.SQL_BOOTSTRAP_ENABLED)) {
  $answer = Read-Host "Run automatic SQL bootstrap after provision? (Y/n)"
  $enabled = -not ($answer -and $answer.Trim().ToLowerInvariant() -in @('n', 'no', 'false', '0'))

  $value = $enabled ? 'true' : 'false'
  Write-Host "Setting SQL_BOOTSTRAP_ENABLED=$value"
  azd env set SQL_BOOTSTRAP_ENABLED $value | Out-Null
}

if (-not $envJson.SQL_ADMIN_PASSWORD -or [string]::IsNullOrWhiteSpace($envJson.SQL_ADMIN_PASSWORD)) {
  function New-StrongPassword {
    param(
      [int]$Length = 24
    )

    if ($Length -lt 12) {
      throw 'Password length must be at least 12.'
    }

    $lower = 'abcdefghijkmnopqrstuvwxyz'
    $upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'
    $digits = '23456789'
    $special = '!@#$%^&*-_=+?'
    $all = ($lower + $upper + $digits + $special)

    $chars = New-Object System.Collections.Generic.List[char]

    # Guarantee complexity (at least one from each set)
    $chars.Add($lower[(Get-Random -Minimum 0 -Maximum $lower.Length)])
    $chars.Add($upper[(Get-Random -Minimum 0 -Maximum $upper.Length)])
    $chars.Add($digits[(Get-Random -Minimum 0 -Maximum $digits.Length)])
    $chars.Add($special[(Get-Random -Minimum 0 -Maximum $special.Length)])

    # Fill the rest using crypto-quality randomness
    $bytes = New-Object byte[] ($Length - $chars.Count)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    foreach ($b in $bytes) {
      $chars.Add($all[$b % $all.Length])
    }

    # Shuffle to avoid predictable prefix
    $bytes2 = New-Object byte[] ($chars.Count)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes2)
    $shuffled = for ($i = 0; $i -lt $chars.Count; $i++) { $chars[$bytes2[$i] % $chars.Count] }
    return (-join $shuffled)
  }

  $password = New-StrongPassword -Length 24

  Write-Host 'Generating SQL_ADMIN_PASSWORD and storing it in the azd environment'
  azd env set SQL_ADMIN_PASSWORD $password | Out-Null
}
