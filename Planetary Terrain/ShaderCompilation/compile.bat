rem %1: Input
rem %2: Output

set input=%1
set output=%2
set input=%input:"=%
set output=%output:"=%

if not exist %2 mkdir %2

setlocal enabledelayedexpansion

rem break>%output%.manifest

set file=""
set name=""
set vertexshader=""
set pixelshader=""
for /f "usebackq tokens=*" %%i in ("%input%.manifest") do (
	set str=%%i
	if "%%i" == "Shader" (
		set file=""
		set name=""
		set vertexshader=""
		set pixelshader=""
	)
	if "!str:~0,4!" == "Path" (
		set file=!str:~5!
	)
	if "!str:~0,4!" == "Name" (
		set name=!str:~5!
	)
	if "!str:~0,12!" == "VertexShader" (
		"%~dp0fxc" "%input%!file!" /E !str:~13! /Fo %output%!name!_vs.cso /T vs_5_0 /Od /Zpr /nologo
	)
	if "!str:~0,11!" == "PixelShader" (
		"%~dp0fxc" "%input%!file!" /E !str:~12! /Fo %output%!name!_ps.cso /T ps_5_0 /Od /Zpr /nologo
	)
	if "!str:~0,14!" == "GeometryShader" (
		"%~dp0fxc" "%input%!file!" /E !str:~15! /Fo %output%!name!_gs.cso /T gs_5_0 /Od /Zpr /nologo
	)
	if !errorlevel! == 1 exit
)