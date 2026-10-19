param([Parameter(Mandatory)][string]$AppFolder,[Parameter(Mandatory)][string]$ReportFolder)
$ErrorActionPreference='Stop'
$AppFolder=[IO.Path]::GetFullPath($AppFolder)
$exe=Join-Path $AppFolder 'Miracle.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Miracle.exe is missing.'}
New-Item -ItemType Directory -Force -Path $ReportFolder|Out-Null
$report=[ordered]@{collectedUtc=(Get-Date).ToUniversalTime().ToString('o');environment='Existing development VM; not a clean-machine result';publicScannerUpload=$false}
$os=Get-CimInstance Win32_OperatingSystem
$report.os=@{caption=$os.Caption;version=$os.Version;build=$os.BuildNumber;architecture=$os.OSArchitecture}
$signature=Get-AuthenticodeSignature -LiteralPath $exe
$report.authenticode=@{status=$signature.Status.ToString();publisher=if($signature.SignerCertificate){$signature.SignerCertificate.Subject}else{$null}}
$report.sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
$report.manifest='Verify embedded RT_MANIFEST execution level using the binary resource inspection; source manifest alone is insufficient.'
try{$status=Get-MpComputerStatus;$report.defender=$status|Select-Object AntivirusEnabled,AMServiceEnabled,RealTimeProtectionEnabled,AntivirusSignatureVersion,AntivirusSignatureLastUpdated,SmartAppControlState}catch{$report.defenderError=$_.Exception.Message}
try{$report.smartAppControlRegistry=(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name VerifiedAndReputablePolicyState -ErrorAction Stop).VerifiedAndReputablePolicyState}catch{$report.smartAppControlRegistry=$null}
$report.smartScreen='Not yet tested from a public download on a clean machine. No verified publisher can be shown for this unsigned build.'
$report.smartAppControl='Configuration read only; this is not an enforced clean-machine compatibility pass.'
$started=Get-Date
try{
    $scanner=Join-Path $env:ProgramFiles 'Windows Defender/MpCmdRun.exe'
    $platform=Join-Path $env:ProgramData 'Microsoft/Windows Defender/Platform'
    if(Test-Path $platform){$candidate=Get-ChildItem $platform -Directory|Sort-Object Name -Descending|Select-Object -First 1;if($candidate -and (Test-Path (Join-Path $candidate.FullName 'MpCmdRun.exe'))){$scanner=Join-Path $candidate.FullName 'MpCmdRun.exe'}}
    $scanLog=Join-Path $ReportFolder 'defender-scan.txt'
    & $scanner -Scan -ScanType 3 -File $AppFolder -DisableRemediation 2>&1|Out-File $scanLog -Encoding utf8
    $report.defenderScan=@{exitCode=$LASTEXITCODE;startedUtc=$started.ToUniversalTime().ToString('o');endedUtc=(Get-Date).ToUniversalTime().ToString('o');log='defender-scan.txt'}
    $report.defenderScan.threats=@(Get-MpThreatDetection -ErrorAction SilentlyContinue|Where-Object {$_.InitialDetectionTime -ge $started -and ($_.Resources -join ' ') -like ('*'+$AppFolder+'*')}|Select-Object ThreatID,ActionSuccess,InitialDetectionTime)
}catch{$report.defenderScanError=$_.Exception.Message}
$report|ConvertTo-Json -Depth 8|Set-Content (Join-Path $ReportFolder 'release-security.json') -Encoding utf8
Write-Output ($report|ConvertTo-Json -Depth 8)
