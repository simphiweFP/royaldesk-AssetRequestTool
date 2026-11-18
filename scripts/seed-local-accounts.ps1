# Local development-only staff and complete demo fixture setup.
# Run in the repository root: powershell -ExecutionPolicy Bypass -File ./scripts/seed-local-accounts.ps1
# The API's existing password hashing and seeding logic stores salted hashes, never plaintext.
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if (-not (Test-Path (Join-Path $root "RoyalDesk.Api/RoyalDesk.Api.csproj"))) {
    throw "RoyalDesk.Api project was not found."
}
$database = Join-Path $root "RoyalDesk.Api/royaldesk-full-demo.db"
# Avoid a second API process using a different database.
foreach ($port in @(7080,5080)) {
    $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($listener) {
        throw "Port $port already has a running process. Stop the existing API before seeding so the browser uses the same database."
    }
}
if (Test-Path $database) {
    Write-Host "Demo database already exists. Existing accounts will not be overwritten." -ForegroundColor Yellow
    Write-Host "To reset, back up and remove ONLY the dedicated demo database after stopping the API." -ForegroundColor Yellow
}
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Data Source=$database"
$env:ROYALDESK_SEED_DEMO_USERS = "true"
$env:ROYALDESK_SEED_FULL_DEMO = "true"
$env:ROYALDESK_RESET_DEMO_PASSWORDS = "true"
# These are intentionally disposable, local development-only sample passwords.
$env:ROYALDESK_DEMO_ADMIN_PASSWORD = "RoyalDesk-Admin-Demo-2026!"
$env:ROYALDESK_DEMO_TECH_PASSWORD = "RoyalDesk-Tech-Demo-2026!"

Write-Host ""
Write-Host "RoyalDesk local-only test users" -ForegroundColor Cyan
Write-Host "  Admin      royaldesk.admin        RoyalDesk-Admin-Demo-2026!"
Write-Host "  Technician royaldesk.technician   RoyalDesk-Tech-Demo-2026!"
Write-Host "  Employee   No sign-in required"
Write-Host "  Staff page http://localhost:4200/portal"
Write-Host ""
Write-Host "These credentials are not suitable for production or a shared network." -ForegroundColor Yellow
Write-Host "Using isolated SQLite file: $database"
Write-Host "Only seeder-created demo accounts can have their passwords reset."
Push-Location $root
try {
    dotnet run --project RoyalDesk.Api
    if ($LASTEXITCODE -ne 0) { throw "RoyalDesk API exited with code $LASTEXITCODE. Check migrations and seeding errors." }
}
finally {
    Pop-Location
    Remove-Item Env:ROYALDESK_SEED_DEMO_USERS -ErrorAction SilentlyContinue
    Remove-Item Env:ROYALDESK_SEED_FULL_DEMO -ErrorAction SilentlyContinue
    Remove-Item Env:ROYALDESK_RESET_DEMO_PASSWORDS -ErrorAction SilentlyContinue
    Remove-Item Env:ROYALDESK_DEMO_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:ROYALDESK_DEMO_TECH_PASSWORD -ErrorAction SilentlyContinue
}
