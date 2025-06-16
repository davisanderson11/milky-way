@echo off
echo Compiling Scientific Milky Way Generator (Console Version)...

REM Try .NET SDK first
where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Using .NET SDK...
    
    REM Clean up any existing project
    if exist ScientificMilkyWayConsole rmdir /s /q ScientificMilkyWayConsole
    
    REM Create new console project
    dotnet new console -n ScientificMilkyWayConsole -f net8.0 --force
    
    REM Copy source files
    copy /Y ScientificMilkyWayConsole.cs ScientificMilkyWayConsole\Program.cs
    copy /Y ScientificMilkyWayGenerator.cs ScientificMilkyWayConsole\
    copy /Y GalacticAnalytics.cs ScientificMilkyWayConsole\
    copy /Y ChunkBasedGalaxySystem.cs ScientificMilkyWayConsole\
    copy /Y MultipleStarSystems.cs ScientificMilkyWayConsole\
    copy /Y PlanetarySystemGenerator.cs ScientificMilkyWayConsole\
    
    REM Build the project
    cd ScientificMilkyWayConsole
    dotnet build
    
    echo.
    echo ========================================
    echo Build complete! 
    echo To run the program:
    echo   cd ScientificMilkyWayConsole
    echo   dotnet run
    echo ========================================
    cd ..
    goto end
)

REM Try .NET Framework csc
set "DOTNET_PATH=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if exist "%DOTNET_PATH%\csc.exe" (
    echo Using .NET Framework compiler...
    "%DOTNET_PATH%\csc.exe" /out:ScientificMilkyWayConsole.exe ScientificMilkyWayConsole.cs ScientificMilkyWayGenerator.cs GalacticAnalytics.cs ChunkBasedGalaxySystem.cs MultipleStarSystems.cs PlanetarySystemGenerator.cs
    echo.
    echo Build complete! Run with: ScientificMilkyWayConsole.exe
    goto end
)

echo No C# compiler found! Please install .NET SDK from:
echo https://dotnet.microsoft.com/download

:end
pause