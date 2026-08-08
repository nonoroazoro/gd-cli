$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "gd-cli.csproj"

& dotnet build $projectPath --configuration Release

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
