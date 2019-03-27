$packageName = 'shell-x'

try { 
    $installDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $app = Join-Path $installDir "shell-x.exe"
    #Write-Host "$app"
    Start-Process -FilePath "$app" -ArgumentList "-u"
}
catch {
    throw $_.Exception
}
