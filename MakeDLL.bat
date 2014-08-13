@echo off
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /out:eccm.dll /t:library /recurse:*.cs /r:D:\web\C#\ASPX_HOME\eccm\bin\EcmRegex.dll;Microsoft.JScript.dll;D:\web\C#\ASPX_HOME\eccm\bin\NMatrix.Schematron.dll

IF ERRORLEVEL 1 GOTO END

copy .\eccm.dll ..\..\..\bin\eccm.dll
copy .\eccm.dll D:\Inetpub\wwwroot\bin\eccm.dll
:END
pause
