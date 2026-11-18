param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$solution = Join-Path $root "DiskFill.sln"
$project = Join-Path $root "DiskFill\DiskFill.csproj"
$publishDirectory = Join-Path $root "publish"
$artifactsDirectory = Join-Path $root "artifacts"

[xml]$projectXml = Get-Content -LiteralPath $project
$version = [string]$projectXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "1.1.0"
}

$resolvedRoot = [System.IO.Path]::GetFullPath($root).TrimEnd('\')
$resolvedPublish = [System.IO.Path]::GetFullPath($publishDirectory)
if (-not $resolvedPublish.StartsWith($resolvedRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean a publish directory outside the repository."
}

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

[System.IO.Directory]::CreateDirectory($artifactsDirectory) | Out-Null

dotnet restore $solution
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed."
}

dotnet build $solution -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
}

dotnet test $solution -c $Configuration --no-build --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed."
}

dotnet publish $project -c $Configuration -f net48 --no-build -o $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$zipPath = Join-Path $artifactsDirectory ("DiskFill-v{0}-win-net48.zip" -f $version)
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath
Write-Host "Release package: $zipPath"
