//css_args /ac
//css_inc %csscript_inc%\cmd.cs
using System.IO;
using System.Net;
using System;

void main()
{
    ServicePointManager.Expect100Continue = true;
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

    var url = "https://github.com/oleg-shilo/shell-x/releases/download/v1.1.0.0/shell-x.7z";

    var installScript = @"tools\chocolateyInstall.ps1";

    var cheksum = calcChecksum(url);
    // var cheksum = "E1809AD6433A91B2FF4803E7F4B15AE0FA88905A28949EAC5590F7D9FD9BE9C3";
    Console.WriteLine(cheksum);

    var code = File.ReadAllText(installScript + ".template")
                   .Replace("$url = ???", "$url = '" + url + "'")
                   .Replace("$checksum = ???", "$cheksum = '" + cheksum + "'");

    File.WriteAllText(installScript, code);
    Console.WriteLine("--------------");
    Console.WriteLine(code);
    Console.WriteLine("--------------");
    Console.WriteLine();
    Console.WriteLine("Done...");
}

string calcChecksum(string url)
{
    var file = "shell-x.7z";
    cmd.DownloadBinary(url, file, (step, total) => Console.Write("\r{0}%\r", (int)(step * 100.0 / total)));
    Console.WriteLine();

    var cheksum = cmd.run(@"C:\ProgramData\chocolatey\tools\checksum.exe", "-t sha256 -f \"" + file + "\"", echo: false).Trim();
    return cheksum;
}