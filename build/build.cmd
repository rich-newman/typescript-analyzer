@echo off

echo Running build.cmd to create folder WebLinterVsix\Node
if exist "%~dp0..\src\WebLinterVsix\Node\log.txt" echo Nothing to do - WebLinterVsix\Node\log.txt already exists & goto:done

echo Deleting and recreating folder WebLinterVsix\Node... 
if exist "%~dp0..\src\WebLinterVsix\Node" rmdir /s /q "%~dp0..\src\WebLinterVsix\Node"
mkdir "%~dp0..\src\WebLinterVsix\Node"

echo Copying and unzipping core files (node.exe, server.js)...
copy /y "%~dp0..\src\WebLinter\Node\*.*" "%~dp0..\src\WebLinterVsix\Node"
pushd "%~dp0..\src\WebLinterVsix\Node"
7z.exe x -y node.7z
del /q 7z.dll
del /q 7z.exe
del /q node.7z

echo Installing packages...
call npm install ^
     --no-optional --quiet > nul

echo Deleting unneeded files and folders...
del /s /q *.html > nul
del /s /q *.markdown > nul
del /s /q *.md > nul
del /s /q *.npmignore > nul
del /s /q *.txt > nul
del /s /q *.yml > nul
del /s /q .gitattributes > nul
del /s /q CHANGELOG > nul
del /s /q CHANGES > nul
del /s /q CNAME > nul
del /s /q README > nul
del /s /q package-lock.json > nul
del /q package.json > nul

for /d /r . %%d in (benchmark)  do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (bench)      do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (doc)        do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (docs)       do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (example)    do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (examples)   do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (images)     do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (man)        do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (media)      do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (scripts)    do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (tests)      do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (testing)    do @if exist "%%d" rd /s /q "%%d" > nul
for /d /r . %%d in (tst)        do @if exist "%%d" rd /s /q "%%d" > nul

echo Creating log.txt success file...
type nul > log.txt
:done
echo build.cmd completed
pushd "%~dp0"