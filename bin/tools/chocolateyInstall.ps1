$packageName = 'shell-x'
$url = 'https://github.com/oleg-shilo/shell-x/releases/download/v1.0.1.0/shell-x.7z'


$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$app = Join-Path $installDir "shell-x.exe"
#Write-Host "$app"
$cheksum = 'E53EBAAB8FDA245BCEEE7761D6117648CB0BA806FF080189C7BCC2FCC4C14C60'
$checksumType = "sha256"

# Download and unpack a zip file
#Install-ChocolateyZipPackage "$packageName" "$url" "$toolsDir" ["$url64" -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64]
Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -checksum $checksum -checksumType $checksumType

# Need to execute exe twice with special arguments in order configure the host system appropriately.
# Register server and generate initial config


Start-ChocolateyProcessAsAdmin -Statements "-r" -ExeToRun $app
Start-ChocolateyProcessAsAdmin -Statements "-init -nogui" -ExeToRun $app

