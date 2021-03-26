gitmoji-changelog --preset generic

$Path = $PSScriptRoot + '\CHANGELOG.md'
Write-Host $Path
Write-Host (get-content -Path $Path)
Write-Host (get-content -Path $Path | Select-String -Pattern '^((-  .+)|### Remove)' -NotMatch)
Set-Content -Path $Path -Value (Get-Content -Path $Path | Select-String -Pattern '^((-  .+)|### Remove)' -NotMatch)
$rep = (Get-Content -Path $Path)
$regreplace = '(?<msg>- ([^\x00-\x7F]+ [\x00-\x7F]+ )+)(?<link>\[\[[a-zA-Z0-9]+\](.*)\])'
$regreplace2 = '[^\x00-\x7F]+ [\x00-\x7F]+'
$rep = $rep -replace $regreplace, (([Environment]::NewLine), -join('${link}', ([Environment]::NewLine), '${msg}'))
$rep = $rep -replace $regreplace2, (-join('- $0', ([Environment]::NewLine)))
$rep = $rep -replace '- -', '-'
Write-Host $rep
Set-Content -Path $Path -Value (($rep).TrimEnd())