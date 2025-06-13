@echo off
echo Testing Companion Star Planetary Systems
echo =======================================
echo.
echo This test will find stars with companions and show their planetary systems.
echo.

REM Build the console version first
call compile-console.bat

echo.
echo Running test...
cd ScientificMilkyWayConsole

REM Create a test input file to automate the testing
echo 2 > test-input.txt
echo 260_45_0_100 >> test-input.txt
echo 260_45_0_100_A >> test-input.txt
echo 260_45_0_100_A_1 >> test-input.txt
echo 260_45_0_101 >> test-input.txt
echo 260_45_0_102 >> test-input.txt
echo q >> test-input.txt
echo 7 >> test-input.txt

dotnet run < test-input.txt

cd ..

echo.
echo Test complete! Look for companion stars with planetary systems in the output above.
pause