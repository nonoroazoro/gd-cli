$ErrorActionPreference = "Stop"
$projectPath = Join-Path $PSScriptRoot "Tests\GdCli.Tests.csproj"

& dotnet test $projectPath --configuration Release
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
