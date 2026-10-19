param(
    [ValidateSet('win-arm64','win-x64')][string]$Runtime='win-x64',
    [Parameter(Mandatory)][string]$Engines,
    [Parameter(Mandatory)][string]$Prerequisite,
    [string]$SevenZip=(Join-Path $env:ProgramFiles '7-Zip/7z.exe')
)
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $SevenZip)){throw 'Install official 7-Zip for build-time compression; it is not needed by the running app.'}
$project=Join-Path $PSScriptRoot '../Miracle.Windows/Miracle.Windows.csproj'
$stage=Join-Path $PSScriptRoot "../artifacts/$Runtime-compact"
if(Test-Path $stage){throw 'Use a fresh artifacts staging directory to avoid stale files.'}
$output=Join-Path $stage 'Miracle'
dotnet publish $project -c Release -r $Runtime --self-contained true -o $output -p:TreatWarningsAsErrors=true
if($LASTEXITCODE -ne 0){throw 'Publish failed.'}
Get-ChildItem $output -Filter '*.pdb'|Remove-Item
foreach($name in @('README.md','FEATURES.md','THIRD_PARTY.md','SECURITY.md')){Copy-Item (Join-Path $PSScriptRoot ("../"+$name)) (Join-Path $output $name)}
Copy-Item -LiteralPath $Engines -Destination (Join-Path $output 'Engines') -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../Notices') -Destination (Join-Path $output 'Notices') -Recurse
$signature=Get-AuthenticodeSignature -LiteralPath $Prerequisite
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notlike '*Microsoft Corporation*'){throw 'C++ runtime must be an official, validly signed Microsoft redistributable matching the selected architecture.'}
$prereqFolder=Join-Path $output 'Prerequisites';New-Item -ItemType Directory $prereqFolder|Out-Null
$prereqName=if($Runtime -eq 'win-x64'){'vc_redist.x64.exe'}else{'vc_redist.arm64.exe'}
Copy-Item $Prerequisite (Join-Path $prereqFolder $prereqName)
# Keep conversion filters, binaries, Unicode data, fonts, and all license notices.
$office=Join-Path $output 'Engines/LibreOffice'
if(!(Test-Path (Join-Path $office 'program/soffice.com'))){throw 'The selected Office engine is missing.'}
Get-ChildItem (Join-Path $office 'share/extensions') -Directory|Where-Object Name -Like 'dict-*'|Remove-Item -Recurse
Get-ChildItem (Join-Path $office 'program/resource') -Directory|Where-Object Name -NotIn @('en_GB','en_ZA')|Remove-Item -Recurse
foreach($name in @('help','LibreOffice.msi')){$item=Join-Path $office $name;if(Test-Path $item){Remove-Item $item -Recurse}}
Get-ChildItem (Join-Path $office 'share/config') -Filter 'images_*.zip'|Where-Object Name -NE 'images_colibre.zip'|Remove-Item
foreach($locale in @('cs','de','es','fr','it','ja','ko','pl','pt-BR','ru','tr','zh-Hans','zh-Hant')){$item=Join-Path $output $locale;if(Test-Path $item){Remove-Item $item -Recurse}}
# Reject mismatched app/Office CPU architectures before packaging.
function Machine([string]$Path){$bytes=[IO.File]::ReadAllBytes($Path);[BitConverter]::ToUInt16($bytes,[BitConverter]::ToInt32($bytes,0x3c)+4)}
$expected=if($Runtime -eq 'win-x64'){0x8664}else{0xaa64}
foreach($binary in @((Join-Path $output 'Miracle.exe'),(Join-Path $office 'program/soffice.bin'))){if((Machine $binary) -ne $expected){throw "Wrong architecture: $binary"}}
$version=([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version|Where-Object {$_}|Select-Object -First 1
$architectureName=if($Runtime -eq 'win-x64'){'x64'}else{'ARM64'}
$archive=Join-Path $PSScriptRoot "../artifacts/Miracle-Windows-$architectureName-v$version-compact.7z"
Push-Location $stage
try{& $SevenZip a -t7z -mx=9 -md=128m -mmt=2 -ms=on $archive Miracle;if($LASTEXITCODE -ne 0){throw 'Compression failed.'}}finally{Pop-Location}
if((Get-Item $archive).Length -ge 300000000){throw 'Download exceeds the strict 300 MB limit.'}
& $SevenZip t $archive
if($LASTEXITCODE -ne 0){throw 'Archive integrity failed.'}
((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($archive))|Set-Content ($archive+'.sha256') -Encoding ascii
Write-Host "Compact offline package: $archive"
