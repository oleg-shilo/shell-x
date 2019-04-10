$packageName = 'shell-x'
$url = 'https://github.com/oleg-shilo/shell-x/releases/download/v1.0.0.0/shell-x.7z'


$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$app = Join-Path $installDir "shell-x.exe"
#Write-Host "$app"
$cheksum = '16AE4B3E62EBE6AB46EF2FAB8E20F8EAF6B8653BC50A20D1A7EF648222B30B46'
$checksumType = "sha256"

# Download and unpack a zip file
#Install-ChocolateyZipPackage "$packageName" "$url" "$toolsDir" ["$url64" -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64]
Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -checksum $checksum -checksumType $checksumType

# Need to execute exe twice with special arguments in order configure the host system appropriately.
# Register server and generate initial config

Start-ChocolateyProcessAsAdmin -Statements "-r" -ExeToRun $app
Start-ChocolateyProcessAsAdmin -Statements "-init -noui" -ExeToRun $app
