using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using SharpShell.Extensions;
using SharpShell.ServerRegistration;

static class Regasm
{
    public static Action<string> OnError = Console.WriteLine;
    public static Action<string> OnOut;

    static bool Run(string exe, string arguments)
    {
        var regasm = new Process
        {
            StartInfo =
            {
                FileName = exe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        regasm.Start();
        regasm.WaitForExit();

        if (regasm.ExitCode == 0)
            OnOut?.Invoke(regasm.StandardOutput.ReadToEnd());
        else
            OnError?.Invoke(regasm.StandardError.ReadToEnd());

        return regasm.ExitCode == 0;
    }

    public static bool Register(string assemblyPath, bool is64)
        => Run(GetRegasm(is64), $"/codebase \"{assemblyPath}\"");

    public static bool Unregister(string assemblyPath, bool is64)
        => Run(GetRegasm(is64), $"/u \"{assemblyPath}\"");

    static string GetRegasm(bool is64)
    {
        //  C:\WINDOWS\Microsoft.Net\Framework\v1.1.4322\regasm.exe
        //  C:\WINDOWS\Microsoft.Net\Framework\v2.0.50727\regasm.exe
        //  C:\WINDOWS\Microsoft.Net\Framework\v4.0.30319\regasm.exe
        var frameworkFolder = is64 ? "Framework64" : "Framework";
        var searchRoot = Path.Combine("%WINDIR%", "Microsoft.Net", frameworkFolder).ExpandEnvars();

        var path = Directory.GetDirectories(searchRoot, "v*")
                            .OrderByDescending(s => s)
                            .Select(c => c.PathJoin("regasm.exe"))
                            .FirstOrDefault(File.Exists);

        if (path == null)
            throw new InvalidOperationException($@"Failed to find regasm in '{searchRoot}\v*\regasm.exe'.");

        return path;
    }
}

public class ServerRegistration
{
    public static bool IsExtensionApproved(Guid serverClsid, bool is64)
    {
        //  Open the approved extensions key.
        using (var approvedKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
            is64 ? RegistryView.Registry64 : RegistryView.Registry32)
            .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", RegistryKeyPermissionCheck.ReadSubTree))
        {
            //  If we can't open the key, we're going to have problems.
            if (approvedKey == null)
                throw new InvalidOperationException("Failed to open the Approved Extensions key.");

            return approvedKey.GetValueNames().Any(vn => vn.Equals(serverClsid.ToRegistryString(), StringComparison.OrdinalIgnoreCase));
        }
    }

    // public static ShellExtensionRegistrationInfo GetServerRegistrationInfo(Guid serverCLSID, RegistrationType registrationType)
    // {
    //     //  We can very quickly check to see if the server is approved.
    //     bool serverApproved = IsExtensionApproved(serverCLSID, registrationType);

    //     //  Open the classes.
    //     using (var classesKey = OpenClassesKey(registrationType, RegistryKeyPermissionCheck.ReadSubTree))
    //     {
    //         //  Do we have a subkey for the server?
    //         using (var serverClassKey = classesKey.OpenSubKey(serverCLSID.ToRegistryString()))
    //         {
    //             //  If there's no subkey, the server isn't registered.
    //             if (serverClassKey == null)
    //                 return null;

    //             //  Do we have an InProc32 server?
    //             using (var inproc32ServerKey = serverClassKey.OpenSubKey(KeyName_InProc32))
    //             {
    //                 //  If we do, we can return the server info for an inproc 32 server.
    //                 if (inproc32ServerKey != null)
    //                 {
    //                     //  Get the default value.
    //                     var defaultValue = GetValueOrEmpty(inproc32ServerKey, null);

    //                     //  If we default value is null or empty, we've got a partially registered server.
    //                     if (string.IsNullOrEmpty(defaultValue))
    //                         return new ShellExtensionRegistrationInfo(ServerRegistationType.PartiallyRegistered, serverCLSID);

    //                     //  Get the threading model.
    //                     var threadingModel = GetValueOrEmpty(inproc32ServerKey, KeyName_ThreadingModel);

    //                     //  Is it a .NET server?
    //                     if (defaultValue == KeyValue_NetFrameworkServer)
    //                     {
    //                         //  We've got a .NET server. We should have one subkey, with the assembly version.
    //                         var subkeyName = inproc32ServerKey.GetSubKeyNames().FirstOrDefault();

    //                         //  If we have no subkey name, we've got a partially registered server.
    //                         if (subkeyName == null)
    //                             return new ShellExtensionRegistrationInfo(ServerRegistationType.PartiallyRegistered, serverCLSID);

    //                         //  Otherwise we now have the assembly version.
    //                         var assemblyVersion = subkeyName;

    //                         //  Open the assembly subkey.
    //                         using (var assemblySubkey = inproc32ServerKey.OpenSubKey(assemblyVersion))
    //                         {
    //                             //  If we can't open the key, we've got a problem.
    //                             if (assemblySubkey == null)
    //                                 throw new InvalidOperationException("Can't open the details of the server.");

    //                             //  Read the managed server details.
    //                             var assembly = GetValueOrEmpty(assemblySubkey, KeyName_Assembly);
    //                             var @class = GetValueOrEmpty(assemblySubkey, KeyName_Class);
    //                             var runtimeVersion = GetValueOrEmpty(assemblySubkey, KeyName_RuntimeVersion);
    //                             var codeBase = assemblySubkey.GetValue(KeyName_CodeBase, null);

    //                             //  Return the server info.
    //                             return new ShellExtensionRegistrationInfo(ServerRegistationType.ManagedInProc32, serverCLSID)
    //                             {
    //                                 ThreadingModel = threadingModel,
    //                                 Assembly = assembly,
    //                                 AssemblyVersion = assemblyVersion,
    //                                 Class = @class,
    //                                 RuntimeVersion = runtimeVersion,
    //                                 CodeBase = codeBase != null ? codeBase.ToString() : null,
    //                                 IsApproved = serverApproved
    //                             };
    //                         }
    //                     }

    //                     //  We've got a native COM server.

    //                     //  Return the server info.
    //                     return new ShellExtensionRegistrationInfo(ServerRegistationType.NativeInProc32, serverCLSID)
    //                     {
    //                         ThreadingModel = threadingModel,
    //                         ServerPath = defaultValue,
    //                         IsApproved = serverApproved
    //                     };
    //                 }
    //             }

    //             //  If by this point we haven't return server info, we've got a partially registered server.
    //             return new ShellExtensionRegistrationInfo(ServerRegistationType.PartiallyRegistered, serverCLSID);
    //         }
}