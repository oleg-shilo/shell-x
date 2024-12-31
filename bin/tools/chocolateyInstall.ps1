$packageName = 'shell-x'
$url = 'https://github.com/oleg-shilo/shell-x/releases/download/v1.5.7.0/shell-x.v1.5.7.0.7z'


$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$app = Join-Path $installDir "shell-x.exe"
#Write-Host "$app"
$checksum = 'AD775CC5872D5CD4A475C5D77530E1713B32C0DDEA5C6DACDED34329DDCC873E'
$checksumType = "sha256"

# Download and unpack a zip file
Install-ChocolateyZipPackage "$packageName" -Url $url -UnzipLocation "$installDir" -checksum $checksum -ChecksumType $checksumType

# Need to execute exe twice with special arguments in order configure the host system appropriately.
# Register server and generate initial config


Start-ChocolateyProcessAsAdmin -Statements "-r" -ExeToRun $app
Start-ChocolateyProcessAsAdmin -Statements "-init -nogui" -ExeToRun $app

