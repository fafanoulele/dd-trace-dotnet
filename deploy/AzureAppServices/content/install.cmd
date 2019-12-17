@echo off

REM Create home directory for tracer version
mkdir D:\home\site\wwwroot\datadog\tracer\v1_10_18

REM Copy all dependencies
xcopy /e D:\home\SiteExtensions\Datadog.Trace.AzureAppServices\Tracer D:\home\site\wwwroot\datadog\tracer\v1_10_18

REM Create logging directory for tracer version
mkdir D:\home\LogFiles\Datadog\Tracer\v1_10_18

REM Create directory for agent to live
mkdir D:\home\site\wwwroot\app_data\jobs\continuous\datadog-trace-agent

REM Copy the trace agent
copy /y trace-agent.exe D:\home\site\wwwroot\app_data\jobs\continuous\datadog-trace-agent

REM enable powershell install script (TODO: Will we need this?)
powershell.exe -ExecutionPolicy RemoteSigned -File install.ps1

echo %ERRORLEVEL%
