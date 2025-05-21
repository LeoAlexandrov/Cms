& "C:\Program Files\7-Zip\7z.exe" a -ttar "C:\VSBuild\Cms.Demo\publish.tar" "C:\VSBuild\Cms.Demo\Publish\*"

$prepCommands = @"
mkdir -p -v /home/leo/Cms.Demo/publish;
rm -r /home/leo/Cms.Demo/publish/*
"@

$commands = @"
cd /home/leo/Cms.Demo;
tar -xf publish.tar --directory publish
rm -f publish.tar
chmod -R a+x publish; 
docker rm -f H-Cms-Demo;
docker rmi h-cms-demo:latest;
docker build --tag h-cms-demo .;

docker run -p 8083:8080 --name H-Cms-Demo -h H-Cms-Demo --restart=always --network external \
	-e SETTINGS=/etc/HCms.Demo/settings.json \
	-v /etc/HCms.Demo:/etc/HCms.Demo \
	-d h-cms-demo:latest
"@

<# chown -R leo:leo /home/leo/Cms.Demo; #>

<##>

Write-Host "============== ContaboVPS ==============\r\n"

ssh ContaboVPS $prepCommands
scp C:\VSBuild\Cms.Demo\publish.tar ContaboVPS:/home/leo/Cms.Demo
scp C:\OneDrive\Projects\Cms\DemoSite\Dockerfile ContaboVPS:/home/leo/Cms.Demo

ssh ContaboVPS $commands 

$winscpResult = $LastExitCode

if ($winscpResult -eq 0)
{
  Write-Host "Success"
}
else
{
  Write-Host "Error"
}


Write-Host "============== MiniPC ===============\r\n"

ssh MiniPC $prepCommands
scp C:\VSBuild\Cms.Demo\publish.tar MiniPC:/home/leo/Cms.Demo
scp C:\OneDrive\Projects\Cms\DemoSite\Dockerfile MiniPC:/home/leo/Cms.Demo

ssh MiniPC $commands 

$winscpResult = $LastExitCode

if ($winscpResult -eq 0)
{
  Write-Host "Success"
}
else
{
  Write-Host "Error"
}

Write-Host "============== MiniAir11 ===============\r\n"

ssh MiniAir11 $prepCommands
scp C:\VSBuild\Cms.Demo\publish.tar MiniAir11:/home/leo/Cms.Demo
scp C:\OneDrive\Projects\Cms\DemoSite\Dockerfile MiniAir11:/home/leo/Cms.Demo

ssh MiniAir11 $commands 

$winscpResult = $LastExitCode

if ($winscpResult -eq 0)
{
  Write-Host "Success"
}
else
{
  Write-Host "Error"
}

<#
#>


Write-Host "========================================"

Remove-Item "C:\VSBuild\Cms.Demo\publish.tar" -Force -ErrorAction SilentlyContinue

Pause
