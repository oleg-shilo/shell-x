$packageName = 'shell-x'
$url = 'https://github.com/oleg-shilo/shell-x/releases/download/v1.0.0.0/shell-x.7z'

try { 
    $installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
    $app = Join-Path $installDir "shell-x.exe"
    # Write-Host ">>>>>>>> $app"
    $cheksum = '5264B0CF0ED647A384D6ADFD46E682BD4E23CDB91EB3932A3AB7CF1D0C30D866'
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
