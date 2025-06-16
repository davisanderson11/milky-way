@echo off
echo Compiling Scientific Milky Way Generator with Visualization...

REM Try .NET SDK first
where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Using .NET SDK...
    
    REM Clean up any existing project
    if exist ScientificMilkyWayVisual (
        echo Cleaning up existing project...
        rmdir /s /q ScientificMilkyWayVisual
        timeout /t 1 /nobreak >nul
    )
    
    REM Create new console project
    dotnet new console -n ScientificMilkyWayVisual -f net8.0 --force
    
    REM Clean up any leftover files that shouldn't exist
    if exist ScientificMilkyWayVisual\GalaxyChunkSystem.cs del /F /Q ScientificMilkyWayVisual\GalaxyChunkSystem.cs
    
    REM Copy source files
    copy /Y ScientificMilkyWayConsole.cs ScientificMilkyWayVisual\Program.cs
    copy /Y ScientificMilkyWayGenerator.cs ScientificMilkyWayVisual\
    copy /Y ScientificGalaxyVisualizer2.cs ScientificMilkyWayVisual\
    copy /Y GalacticAnalytics.cs ScientificMilkyWayVisual\
    copy /Y ChunkBasedGalaxySystem.cs ScientificMilkyWayVisual\
    copy /Y MultipleStarSystems.cs ScientificMilkyWayVisual\
    copy /Y PlanetarySystemGenerator.cs ScientificMilkyWayVisual\
    copy /Y GalaxyGenerator.cs ScientificMilkyWayVisual\
    copy /Y ChunkVisualizer.cs ScientificMilkyWayVisual\
    copy /Y SkyGenerator.cs ScientificMilkyWayVisual\
    
    REM Add SkiaSharp package
    cd ScientificMilkyWayVisual
    dotnet add package SkiaSharp --version 2.88.6
    dotnet add package SkiaSharp.NativeAssets.Linux --version 2.88.6
    dotnet add package SkiaSharp.NativeAssets.Win32 --version 2.88.6
    
    REM Build the project
    dotnet build
    
    echo.
    echo ========================================
    echo Build complete! 
    echo To run the program:
    echo   cd ScientificMilkyWayVisual
    echo   dotnet run
    echo ========================================
    cd ..
    goto end
)

echo No .NET SDK found! Please install from:
echo https://dotnet.microsoft.com/download

:end
pause