$oriPath = Get-Location

Set-Location $PSScriptRoot

$now = get-date
$days = (New-TimeSpan -start "2020-4-4" -end $now).days
$minutes =  $now.hour * 60 + $now.minute
$ver = "0.0.$days.$minutes"

$output = ".\bin\$ver"

dotnet pack "TypeScriptGenerator"  -o $output -c Release  /p:Version=$ver

nuget push $output\* -source http://ecm.qiaodan.com:8081/repository/nuget-hosted/

Set-Location $oriPath
