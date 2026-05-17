$packageName = 'shell-x'
$url = 'https://github.com/oleg-shilo/shell-x/releases/download/v1.6.0.0/shell-x.v1.6.0.0.7z'


$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$app = Join-Path $installDir "shell-x.exe"
#Write-Host "$app"
$checksum = '7495A2372CD0B89865CDFD52346A2F125A92798682FE6F1D1E350C8329613D81'
$checksumType = "sha256"

# Download and unpack a zip file
Install-ChocolateyZipPackage "$packageName" -Url $url -UnzipLocation "$installDir" -checksum $checksum -ChecksumType $checksumType

# Need to execute exe twice with special arguments in order configure the host system appropriately.
# Register server and generate initial config


Start-ChocolateyProcessAsAdmin -Statements "-r" -ExeToRun $app
Start-ChocolateyProcessAsAdmin -Statements "-init -nogui" -ExeToRun $app

