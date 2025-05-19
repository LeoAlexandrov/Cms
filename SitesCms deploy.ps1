& "C:\Program Files\7-Zip\7z.exe" a -ttar "C:\VSBuild\Cms\publish.tar" "C:\VSBuild\Cms\Publish\*"

$prepCommands = @"
mkdir -p -v /home/leo/Cms/publish;
rm -r /home/leo/Cms/publish/*
"@

$commands = @"
cd /home/leo/Cms;
tar -xf publish.tar --directory publish
rm -f publish.tar
chmod -R a+x publish; 
docker rm -f SitesCms;
docker rmi sites-cms:latest;
docker build --tag sites-cms .;

docker run -p 8084:8080 --name SitesCms -h SitesCms --restart=always --network external \
	-e SETTINGS=/etc/SitesCms/settings.json \
	-v /etc/SitesCms:/etc/SitesCms \
	-v /var/www/sites-media:/var/www/sites-media \
	-d sites-cms:latest
"@

<# chown -R leo:leo /home/leo/Cms; #>


Write-Host "============== ContaboVPS ==============\r\n"

ssh ContaboVPS $prepCommands
scp C:\VSBuild\Cms\publish.tar ContaboVPS:/home/leo/Cms
scp C:\OneDrive\Projects\Cms\AleProjects.Cms.Web\Dockerfile ContaboVPS:/home/leo/Cms

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


<#

Write-Host "============== MiniAir11 ===============\r\n"

ssh MiniAir11 $prepCommands
scp C:\VSBuild\Cms\publish.tar MiniAir11:/home/leo/Cms
scp C:\OneDrive\Projects\Cms\AleProjects.Cms.Web\Dockerfile MiniAir11:/home/leo/Cms

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

#>

Write-Host "========================================"

Remove-Item "C:\VSBuild\Cms\publish.tar" -Force -ErrorAction SilentlyContinue

Pause
