param([ValidateSet('win-arm64','win-x64')][string]$Runtime = 'win-arm64')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../Miracle.Windows/Miracle.Windows.csproj'
$output = Join-Path $PSScriptRoot "../artifacts/$Runtime"
dotnet publish $project -c Release -r $Runtime --self-contained true -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item (Join-Path $PSScriptRoot '../README.md') (Join-Path $output 'README.md') -Force
$zip = Join-Path $PSScriptRoot "../artifacts/Miracle-Windows-$Runtime-preview.zip"
Compress-Archive -Path (Join-Path $output '*') -DestinationPath $zip -Force
Write-Host "Portable build: $zip"
