REM Modifications Copyright Rich Newman 2017
7z.exe x -y edge.7z
7z.exe x -y -oedge node_modules.7z

del /q 7z.dll
del /q 7z.exe
del /q edge.7z
del /q node_modules.7z
del /q prepare.cmd