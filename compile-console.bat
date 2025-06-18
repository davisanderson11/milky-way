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
    copy /Y GalacticAnalytics.cs ScientificMilkyWayConsole\
    copy /Y ChunkBasedGalaxySystem.cs ScientificMilkyWayConsole\
    copy /Y UnifiedSystemGenerator.cs ScientificMilkyWayConsole\
    copy /Y GalaxyGenerator.cs ScientificMilkyWayConsole\
    copy /Y RoguePlanet.cs ScientificMilkyWayConsole\
    copy /Y ScientificGalaxyVisualizer2.cs ScientificMilkyWayConsole\
    copy /Y ChunkVisualizer.cs ScientificMilkyWayConsole\
    copy /Y StellarTypeGenerator.cs ScientificMilkyWayConsole\
    copy /Y Star.cs ScientificMilkyWayConsole\
    copy /Y RealStellarData.cs ScientificMilkyWayConsole\
    
    REM Copy stellar data directory
    if exist stellar_data (
        echo Copying stellar data files...
        if not exist ScientificMilkyWayConsole\stellar_data mkdir ScientificMilkyWayConsole\stellar_data
        copy /Y stellar_data\*.csv ScientificMilkyWayConsole\stellar_data\
    )
    
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
    "%DOTNET_PATH%\csc.exe" /out:ScientificMilkyWayConsole.exe ScientificMilkyWayConsole.cs GalacticAnalytics.cs ChunkBasedGalaxySystem.cs UnifiedSystemGenerator.cs GalaxyGenerator.cs RoguePlanet.cs ChunkVisualizer.cs StellarTypeGenerator.cs Star.cs
    echo.
    echo Build complete! Run with: ScientificMilkyWayConsole.exe
    goto end
)

echo No C# compiler found! Please install .NET SDK from:
echo https://dotnet.microsoft.com/download

:end
pause