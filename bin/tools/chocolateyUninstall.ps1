$packageName = 'shell-x'

$installDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$app = Join-Path $installDir "shell-x.exe"

# Need to execute exe to unregister server.
Start-Process -FilePath "$app" -ArgumentList "-u"
