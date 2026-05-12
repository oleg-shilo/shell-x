public static void Execute(string item, string invokeArguments)
{
    lock (typeof(App))
    {
        // Debug.Assert(false);
        bool showConsole = item.Contains(".c.");
        bool handleMultiselect = item.Contains(".ms.");
        bool isPS = item.EndsWithAny(".ps1");

        var arguments = new List<string>();
        arguments.AddRange(invokeArguments.SplitCommandLine());

        arguments = arguments.Select(x => $"\"{x}\"").ToList();

        if (!handleMultiselect)
            arguments = new[] { string.Join(" ", arguments.ToArray()) }.ToList();

        foreach (var arg in arguments)
        {
            try
            {
                var p = new Process();
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(item);
                p.StartInfo.FileName = item;
                p.StartInfo.Arguments = arg;

                if (isPS)
                {
                    p.StartInfo.FileName = "powershell.exe";
                    p.StartInfo.Arguments = $"-File \"{item}\" {arg}";
                }

                // save arguments to temp file
                var tempDir = Path.Combine(Path.GetTempPath(), "shell-x");
                Directory.CreateDirectory(tempDir);

                var tempFile = Path.Combine(tempDir, Path.GetRandomFileName() + ".args.txt");
                File.WriteAllText(tempFile, p.StartInfo.Arguments ?? string.Empty);

                p.StartInfo.Arguments = (p.StartInfo.Arguments ?? string.Empty)
                    .Replace("%ARGS_FILE%", $"\"{tempFile\"")
                    .Replace(" % ARGS_FILE % ", $"\"{tempFile\"");

                if (showConsole)
                {
                    // code below works very well though it unconditionally waits. Thus an orthodox execution as
                    // above is adequate particularly because it lets user to pause (with `pause` or with `$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')`)
                    // in the batch file or path through to the exit.
                    //
                    // if (item.EndsWithAny(".ps1"))
                    // {
                    //     p.StartInfo.FileName = "cmd.exe";
                    //     p.StartInfo.Arguments = $"/K powershell.exe -File \"\"{item}\"\" {arg}";
                    // }
                    // else
                    // {
                    //     p.StartInfo.FileName = "cmd.exe";
                    //     p.StartInfo.Arguments = $"/K \"\"{item}\"\" {arg}";
                    // }
                }
                else
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.EnvironmentVariables["SHELLX.SCRIPTDIR"] = Path.GetDirectoryName(item);
                }

                if (!p.StartInfo.UseShellExecute)
                    p.StartInfo.EnvironmentVariables["ARGS_FILE"] = tempFile;

                p.EnableRaisingEvents = true;
                p.Exited += (s, e) =>
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        try { File.Delete(tempFile); } catch { }
                    });
                };

                Debug.WriteLine($"Run: {p.StartInfo.FileName} {p.StartInfo.Arguments}");
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex}", App.Name);
            }
            ;

            Task.Run(Cleanup);
        }
    }
}