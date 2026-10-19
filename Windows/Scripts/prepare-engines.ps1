param([Parameter(Mandatory)][string]$Output, [ValidateSet('win-arm64','win-x64')][string]$Runtime='win-x64', [string]$Cache=(Join-Path $env:TEMP 'MiracleEngineCache'))
$ErrorActionPreference='Stop'
$Output=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $Output,$Cache | Out-Null
function Fetch([string]$Url,[string]$Name,[string]$Hash) {
    $path=Join-Path $Cache $Name
    if(!(Test-Path -LiteralPath $path)){Invoke-WebRequest -Uri $Url -OutFile $path}
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Hash){throw "Checksum mismatch: $Name"}
    return $path
}
$ffmpeg=Fetch 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip' 'ffmpeg-9.0.2.zip' '60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba'
if($Runtime -eq 'win-x64'){
    $lo=Fetch 'https://download.documentfoundation.org/libreoffice/stable/26.8.1/win/x86_64/LibreOffice_26.8.1_Win_x86-64.msi' 'LibreOffice-26.8.1-x64.msi' 'c6298b10cfa1748bcbedad94c62ce771e240e5021bfd6d7a967cd571b04b5312'
}else{
    $lo=Fetch 'https://download.documentfoundation.org/libreoffice/stable/26.8.1/win/aarch64/LibreOffice_26.8.1_Win_aarch64.msi' 'LibreOffice-26.8.1-arm64.msi' '97c2173752325af1da3d8731f41c8e09a7c4ad491cf55d9b1069fadb5668e6d7'
}
$media=Join-Path $Output 'Media'; New-Item -ItemType Directory -Force $media|Out-Null
$expanded=Join-Path $Cache 'ffmpeg-expanded';Expand-Archive -LiteralPath $ffmpeg -DestinationPath $expanded -Force
foreach($name in @('ffmpeg.exe','ffprobe.exe','LICENSE','README.txt')){Get-ChildItem $expanded -Filter $name -Recurse|Select-Object -First 1|Copy-Item -Destination $media -Force}
$office=Join-Path $Output 'LibreOffice'
if(!(Test-Path (Join-Path $office 'program/soffice.com'))){
    $msiArguments='/a "'+$lo+'" /qn TARGETDIR="'+$office+'" /L*v "'+(Join-Path $Cache 'office-extract.log')+'"'
    $p=Start-Process msiexec.exe -ArgumentList $msiArguments -PassThru -Wait
    if($p.ExitCode -ne 0){throw "Office extraction failed: $($p.ExitCode)"}
}
# Windows 11 tar.exe can extract the MSYS2 Zstandard package archives.
$heif=Join-Path $Output 'Heif';New-Item -ItemType Directory -Force $heif|Out-Null
$lockPath=Join-Path $PSScriptRoot '../Engines/heif-ucrt64.lock.json'
foreach($package in (Get-Content $lockPath -Raw|ConvertFrom-Json)){
    $name=[IO.Path]::GetFileName(([Uri]$package.url).AbsolutePath)
    $archive=Fetch $package.url $name $package.sha256
    $temporary=Join-Path $Cache ('extract-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory $temporary|Out-Null
    try{
        & tar.exe -xf $archive -C $temporary
        if($LASTEXITCODE -ne 0){throw "HEIC extraction failed: $name"}
        $bin=Join-Path $heif 'bin';New-Item -ItemType Directory -Force $bin|Out-Null
        Get-ChildItem (Join-Path $temporary 'ucrt64/bin') -File -ErrorAction SilentlyContinue|Where-Object {$_.Extension -eq '.dll' -or $_.Name -in @('heif-enc.exe','heif-dec.exe')}|Copy-Item -Destination $bin -Force
        $licenses=Join-Path $temporary 'ucrt64/share/licenses'
        if(Test-Path $licenses){$target=Join-Path $heif 'share/licenses';New-Item -ItemType Directory -Force $target|Out-Null;Copy-Item (Join-Path $licenses '*') $target -Recurse -Force}
    }finally{Remove-Item -LiteralPath $temporary -Recurse -Force}
}
Copy-Item $lockPath (Join-Path $heif 'packages.lock.json') -Force
Write-Host "Offline $Runtime engine folder ready. Install the matching Microsoft Visual C++ runtime if required."
Write-Host 'Preserve all upstream licenses and corresponding-source information when redistributing.'
