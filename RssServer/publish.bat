echo %time%
dotnet publish -r linux-x64 /p:PublishSingleFile=true -c Release -o ../publish-linux-x64
echo %time%
dotnet publish -r win10-x64 /p:PublishSingleFile=true -c Release -o ../publish-win10-x64
echo %time%
dotnet publish -r osx-x64 /p:PublishSingleFile=true -c Release -o ../publish-osx-x64
echo %time%
pause