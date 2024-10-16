@echo off
set dest=M:\Backup\Cms\%date:~10,4%-%date:~4,2%-%date:~7,2%#%time:~0,2%-%time:~3,2%
set src=C:\OneDrive\Projects\Cms

"C:\Program Files\7-Zip\7z.exe" a -tzip "%dest%.zip" "%src%\*" -mx0 -xr!bin -xr!obj -xr!.vs

pause