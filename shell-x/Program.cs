using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using ShellX;
using static System.Environment;

class App
{
    static int Main(string[] args)
    {
        if (args.Contains("-open"))
        {
            Console.WriteLine($"Opening config directory: '{ConfigDir}'");
            Process.Start("explorer", $"\"{ConfigDir}\"");
        }
        else if (args.ContainsAny("-init"))
        {
            var dir = ConfigDir.PathJoin("txt", "01.=== Shell-X for •.txt file ===").EnsureDirectory();

            File.WriteAllText(dir.PathJoin("..", "00.separator"), "");
            File.WriteAllText(dir.PathJoin("..", "02.separator"), "");

            File.WriteAllText(dir.PathJoin("00.Notepad.cmd"), "notepad.exe \"%*\"");
            File.WriteAllText(dir.PathJoin("01.Notepad++.cmd"), "notepad++.exe \"%*\"");
            File.WriteAllText(dir.PathJoin("02.separator"), "");
            File.WriteAllText(dir.PathJoin("03.Show Info.c.cmd"), $"dir \"%*\"{NewLine}pause");
            File.WriteAllText(dir.PathJoin("04.separator"), "");
            File.WriteAllText(dir.PathJoin("05.Shell-X configure.cmd"), $"explorer \"{ConfigDir}\"");
            Resources.logo.Save(dir.PathJoin("05.Shell-X configure.ico"));

            dir = ConfigDir.PathJoin("[any]").EnsureDirectory();
            File.WriteAllText(dir.PathJoin("01.Shell-X configure.cmd"), $"explorer \"{ConfigDir}\"");
            Resources.logo.Save(dir.PathJoin("01.Shell-X configure.ico"));

            dir = ConfigDir.PathJoin("txt");
            Console.WriteLine($"Configured context menu for '*.*' and '*.txt' files: '{ConfigDir}'");

            if (!args.ContainsAny("-noui"))
                Process.Start("explorer", $"\"{ConfigDir}\"");
        }
        else if (args.ContainsAny("-register", "-r"))
        {
            return Regasm.Register(Assembly.GetExecutingAssembly().Location, Is64BitOperatingSystem) ? 0 : -1;
        }
        else if (args.ContainsAny("-unregister", "-u"))
        {
            return Regasm.Unregister(Assembly.GetExecutingAssembly().Location, Is64BitOperatingSystem) ? 0 : -1;
        }
        else
        {
            var serverType = typeof(DynamicContextMenuExtension);
            string cpuType = Environment.Is64BitProcess ? "x64" : "x32";

            Console.WriteLine($"Dynamic context menu manager. Version {Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine($"Copyright (C) 2019 Oleg Shilo (github.com/oleg-shilo/shell-x)");
            Console.WriteLine();
            Console.WriteLine("==================================================================");
            Console.WriteLine($"  Config directory: {ConfigDir}");
            Console.WriteLine($"  Shell Extension server: {serverType.GUID}");
            Console.WriteLine($"  Registered ({cpuType}): {serverType.IsRegeiteredComServer()}");
            Console.WriteLine("==================================================================");
            // Console.WriteLine("-----------------------");
            Console.WriteLine();
            Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name} [option]");
            Console.WriteLine();
            Console.WriteLine($"Options:");
            Console.WriteLine($"  -open           Opens configuration folder.");
            Console.WriteLine($"                  You can edit the context menus by changing the content of this folder");
            Console.WriteLine($"  -init           Creates the sample configuration (e.g. for *.txt files).");
            Console.WriteLine($"  -register|-r    Registers shell extension server");
            Console.WriteLine($"  -unregister|-u  Unregisters shell extension server");
        }

        return 0;
    }

    static public string Name = Assembly.GetExecutingAssembly().GetTitle();

    static public string ConfigDir => SpecialFolder.ApplicationData
                                          .GetPath()
                                              .PathJoin(Name.ToLower())
                                              .EnsureDirectory();
}

[ComVisible(true)]
// [COMServerAssociation(AssociationType.ClassOfExtension, ".dll", ".txt", ".cs")]
[COMServerAssociation(AssociationType.AllFiles)]
public class DynamicContextMenuExtension : SharpContextMenu
{
    protected override bool CanShowMenu()
    {
        if (this.SelectedItemPaths.Count() == 1)
        {
            var ext = Path.GetExtension(this.SelectedItemPaths.First()).Replace(".", "");
            return ConfiguredFileExtensions.Any(x => x.Matching(ext)) ||
                ConfiguredFileExtensions.Any(x => x.Matching("[any]"));
        }
        else
        {
            foreach (string item in ConfiguredFileExtensions)
            {
                var ext = "." + item;
                if (this.SelectedItemPaths.All(x => x.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    return true;
                else
                    return ConfiguredFileExtensions.Any(x => x.Matching("[any]"));
            }
        }
        return false;
    }

    protected override ContextMenuStrip CreateMenu()
    {
        // if we are here then every selected item is guaranteed to:
        // - have an extension
        // - all extensions are the same
        // - extension is configured for having context menu

        var configDir = GetConfigDirFor(this.SelectedItemPaths.First());
        var items = BuildMenuFrom(configDir, this.SelectedItemPaths.ToArgumentsString());

        if (ConfiguredFileExtensions.Any(x => x.Matching("[any]")))
        {
            var extraItems = BuildMenuFrom(GetConfigDirForAny(), this.SelectedItemPaths.ToArgumentsString());
            items = items.Concat(extraItems).ToArray();
        }

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(items);
        return menu;
    }

    static Image LookupImageFor(string path)
    {
        return LookupImageFor(path, ".ico") ??
               LookupImageFor(path, ".png") ??
               LookupImageFor(path, ".gif");
    }

    static Image LookupImageFor(string path, string imgExtension)
    {
        var imgFile = path.IsDir() ?
                      path + imgExtension :
                      path.ChangeExtensionTo(imgExtension);

        return File.Exists(imgFile) ? Image.FromFile(imgFile) : null;
    }

    internal static ToolStripItem[] BuildMenuFrom(string configDir, string invokeArguments)
    {
        var menus = new List<ToolStripItem>();

        var dirsToProcess = new Queue<BuildItem>();
        dirsToProcess.Enqueue(new BuildItem { AddItem = menus.Add, dir = configDir });

        while (dirsToProcess.Any())
        {
            BuildItem current = dirsToProcess.Dequeue();

            if (!Directory.Exists(current.dir))
                continue;

            var items = Directory.GetFiles(current.dir)
                                 .Concat(Directory.GetDirectories(current.dir))
                                 .OrderBy(Path.GetFileName);

            foreach (var item in items)
            {
                if (item.IsDir())
                {
                    var parentMenu = new ToolStripMenuItem
                    {
                        Text = item.ToDirMenuText(),
                        Image = LookupImageFor(item)
                    };

                    current.AddItem(parentMenu);
                    dirsToProcess.Enqueue(new BuildItem
                    {
                        AddItem = x => parentMenu.DropDownItems.Add(x),
                        dir = item
                    });
                }
                else
                {
                    if (item.EndsWithAny(".separator"))
                    {
                        current.AddItem(new ToolStripSeparator());
                    }
                    else if (item.EndsWithAny(".cmd", ".bat"))
                    {
                        var menu = new ToolStripMenuItem
                        {
                            Text = item.ToFileMenuText(),
                            Image = LookupImageFor(item)
                        };

                        menu.Click += (s, e) =>
                        {
                            bool showConsole = item.EndsWithAny(".c.cmd", ".c.bat");
                            try
                            {
                                var p = new Process();
                                if (showConsole)
                                {
                                    p.StartInfo.FileName = item;
                                    p.StartInfo.Arguments = invokeArguments;

                                    // code below works very well and produces less noise
                                    // though it unconditionally waits. Thus an orthodox execution as
                                    // above is adequate particularly because it lets user to pose (with 'pause')
                                    // in the batch file or path through to the exit.

                                    // p.StartInfo.FileName = "cmd.exe";
                                    // p.StartInfo.Arguments = $"/K \"\"{item}\" {invokeArguments}";
                                }
                                else
                                {
                                    p.StartInfo.FileName = item;
                                    p.StartInfo.Arguments = invokeArguments;
                                    p.StartInfo.UseShellExecute = false;
                                    p.StartInfo.RedirectStandardOutput = true;
                                    p.StartInfo.CreateNoWindow = true;
                                }
                                p.Start();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error: {ex}", App.Name);
                            };
                        };
                        current.AddItem(menu);
                    }
                }
            }
        }

        return menus.ToArray();
    }

    class BuildItem
    {
        public Action<ToolStripItem> AddItem;
        public string dir;
    }

    string[] ConfiguredFileExtensions
        => Directory.GetDirectories(App.ConfigDir)
                    .Select(x => x.GetFileName()) // gets dir name only without the rest of the path
                    .ToArray();

    string GetConfigDirFor(string file) => App.ConfigDir.PathJoin(file.GetExtension().Replace(".", ""));

    string GetConfigDirForAny() => App.ConfigDir.PathJoin("[any]");
}