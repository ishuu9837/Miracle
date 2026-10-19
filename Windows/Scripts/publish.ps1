param([ValidateSet('win-arm64','win-x64')][string]$Runtime = 'win-arm64', [string]$Engines)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../Miracle.Windows/Miracle.Windows.csproj'
$output = Join-Path $PSScriptRoot "../artifacts/$Runtime"
dotnet publish $project -c Release -r $Runtime --self-contained true -o $output -p:TreatWarningsAsErrors=true
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item (Join-Path $PSScriptRoot '../README.md') (Join-Path $output 'README.md') -Force
foreach($name in @('FEATURES.md','THIRD_PARTY.md','SECURITY.md')){Copy-Item (Join-Path $PSScriptRoot ("../"+$name)) (Join-Path $output $name) -Force}
if($Engines){
    if($Runtime -ne 'win-arm64'){throw 'Provide and verify x64 engines separately before packaging x64.'}
    Copy-Item -LiteralPath $Engines -Destination (Join-Path $output 'Engines') -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../Notices') -Destination (Join-Path $output 'Notices') -Recurse -Force
$zip = Join-Path $PSScriptRoot "../artifacts/Miracle-Windows-$Runtime-preview.zip"
Compress-Archive -Path (Join-Path $output '*') -DestinationPath $zip -Force
Write-Host "Portable build: $zip"
