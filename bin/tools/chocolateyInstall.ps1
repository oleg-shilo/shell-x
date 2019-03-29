$packageName = 'shell-x'
$url = 'https://github.com/oleg-shilo/shell-x/releases/download/v1.0.0.0/shell-x.7z'

try { 
    $installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
    $app = Join-Path $installDir "shell-x\shell-x.exe"
    #Write-Host "$app"
    $cheksum = '3571F3C148D207A4707FC9260AB5D4DD89C42A907340331CF327EC6FFB19F17A'
    $checksumType = "sha256"
  
    # Download and unpack a zip file
    #Install-ChocolateyZipPackage "$packageName" "$url" "$toolsDir" ["$url64" -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64]
    Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -checksum $checksum -checksumType $checksumType
  
    Start-Process -FilePath "$app" -ArgumentList "-r"
    Start-Process -FilePath "$app" -ArgumentList "-i -noui"
}
catch {
    throw $_.Exception
}
