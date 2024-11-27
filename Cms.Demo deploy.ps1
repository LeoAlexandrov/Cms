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
docker rm -f HeadlessCmsDemo;
docker rmi headlesscmsdemo:latest;
docker build --tag headlesscmsdemo .;
docker run -p 8083:8080 --name HeadlessCmsDemo -h HeadlessCmsDemo --restart=always --network external -v /etc/HeadlessCms.Demo:/etc/HeadlessCms.Demo -d headlesscmsdemo:latest
"@

<# chown -R leo:leo /home/leo/Cms; #>

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


<# #>

Write-Host "============== MiniAir11 ===============\r\n"

ssh MiniAir11 $prepCommands
scp C:\VSBuild\Cms\publish.tar MiniAir11:/home/leo/Cms.Demo
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


Write-Host "========================================"

Remove-Item "C:\VSBuild\Cms.Demo\publish.tar" -Force -ErrorAction SilentlyContinue

Pause
