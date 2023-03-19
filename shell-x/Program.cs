using SharpShell;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using ShellX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Environment;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

class App
{
    public static int Main(string[] args)
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

            File.WriteAllText(dir.PathJoin("00.Notepad.cmd"), "notepad.exe %*");
            File.WriteAllText(dir.PathJoin("01.Notepad++.cmd"), "notepad++.exe %*");
            File.WriteAllText(dir.PathJoin("02.separator"), "");
            File.WriteAllText(dir.PathJoin("03.Show Info.c.cmd"), $"dir %*{NewLine}pause");
            File.WriteAllText(dir.PathJoin("03.Show Path.c.ps1"), $"Write-Host \"File: $($args[0])\" \n" +
                                                                      $"Write-Host -NoNewLine 'Press any key to continue...'; " +
                                                                          $"$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');");
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
        else if (args.ContainsAny("-test"))
        {
            return ExplorerStub.Test(args.Where(x => x != "-test").FirstOrDefault());
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
            Console.WriteLine($"Copyright (C) 2019-2020 Oleg Shilo (github.com/oleg-shilo/shell-x)");
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
            Console.WriteLine($"  -test <path>    Creates the context menu for a given path and shows it without the need to " +
                              $"                  interact with the explorer.");
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

public class ExplorerSelectionStub
{
    DynamicContextMenuExtension extension = new DynamicContextMenuExtension();

    public ExplorerSelectionStub(params string[] selection)
    {
        SetItemPaths(selection.ToList());
    }

    public void SetItemPaths(List<string> selection)
    {
        var field = typeof(ShellExtInitServer).GetField("selectedItemPaths", BindingFlags.NonPublic | BindingFlags.Instance);

        var paths = (List<string>)field.GetValue(extension);
        paths.AddRange(selection);
    }

    public bool CanShowMenu()
    {
        return (bool)
            extension.GetType()
                     .GetMethod("CanShowMenu", BindingFlags.NonPublic | BindingFlags.Instance)
                     .Invoke(extension, new object[0]);
    }

    public ContextMenuStrip CreateMenu()
    {
        return (ContextMenuStrip)
            extension.GetType()
                     .GetMethod("CreateMenu", BindingFlags.NonPublic | BindingFlags.Instance)
                     .Invoke(extension, new object[0]);
    }
}

[ComVisible(true)]
// [COMServerAssociation(AssociationType.ClassOfExtension, ".dll", ".txt", ".cs")]
[COMServerAssociation(AssociationType.AllFiles)]
[COMServerAssociation(AssociationType.DirectoryBackground)]
[COMServerAssociation(AssociationType.Folder)]
public class DynamicContextMenuExtension : SharpContextMenu
{
    static int lastPopupTime = 0;

    protected override bool CanShowMenu()
    {
        if ((Environment.TickCount - lastPopupTime) < 1000)
            return false; // the query is executed twice if the clicked item is a folder on the folder tree. so exit to avoid duplication

        lastPopupTime = Environment.TickCount;
#if DEBUG
        Debug.WriteLine("--------------------");
        Debug.WriteLine("this.SelectedItemPaths.Count: " + this.SelectedItemPaths.Count());
        Debug.WriteLine("this.FolderPath: " + this.FolderPath);
        Debug.WriteLine("ConfiguredFileExtensions: " + String.Join(", ", this.ConfiguredFileExtensions));
        Debug.WriteLine("Configured Multi File Extensions: " + String.Join(", ", new DynamicContextMenuExtension().ConfiguredFileExtensions.ParseMultipleExt().Select(x => "group(" + String.Join(", ", x) + ")")));
        Debug.WriteLine("--------------------");
#endif
        if (this.SelectedItemPaths.Count() == 1)
        {
            // Debug.Assert(false);
            var path = this.SelectedItemPaths.First();

            // Debug.Assert(false, "file/folder\n" + path);

            var ext = Path.GetExtension(path).Replace(".", "");

            return
                Directory.Exists(path) || // always allow for folders as it is the only way to invoke shell-x config (single menu item)
                ConfiguredFileExtensions.Any(x => x.Matching(ext)) ||
                ConfiguredFileExtensions.Any(x => Path.GetFileName(path).MatchingAsExpression(x)) ||
                ConfiguredFileExtensions.Any(x => x.Matching("[any]")) ||
                (ConfiguredFileExtensions.Any(x => x.Matching("[file]")) && path.IsFile()) ||
                (ConfiguredFileExtensions.Any(x => x.Matching("[folder]")) && path.IsDir()) ||
                ConfiguredFileExtensions.ParseMultipleExt().Any(x => x != null ? x.Any(e => e.Matching(ext)) : false);
        }
        else
        {
            var extensions = ConfiguredFileExtensions.SelectMany(x =>
            {
                if (x.StartsWith("[") && x.EndsWith("]") && x.Contains(","))
                    return x.Substring(1, x.Length - 2).Split(',');
                else
                    return new[] { x };
            });

            foreach (string item in extensions)
            {
                var ext = "." + item;
                if (this.SelectedItemPaths.All(x => x.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            if (this.SelectedItemPaths.Any())
                return ConfiguredFileExtensions.Any(x => x.Matching("[any]"));
            else
                return ConfiguredFileExtensions.Any(x => x.Matching("[folder]"));
        }
        return false;
    }

    protected override ContextMenuStrip CreateMenu()
    {
        // if we are here then every selected item is guaranteed to:
        // - have an extension
        // - all extensions are the same
        // - extension is configured for having context menu

        var selectedItemPaths = new List<string>(this.SelectedItemPaths);

        if (!selectedItemPaths.Any() && this.FolderPath.Any())
            selectedItemPaths.Add(this.FolderPath);

        var configDir = GetConfigDirFor(selectedItemPaths.First());
        var items = BuildMenuFrom(configDir, selectedItemPaths.ToArgumentsString());

        try
        {
            if (ConfiguredFileExtensions.Any(x => x.Matching("[any]")))
            {
                var extraItems = BuildMenuFrom(GetConfigDirForAny(), selectedItemPaths.ToArgumentsString());
                items = items.Concat(extraItems).ToArray();
            }

            if (ConfiguredFileExtensions.Any(x => x.Matching("[file]")) && selectedItemPaths[0].IsFile())
            {
                var extraItems = BuildMenuFrom(GetConfigDirByPath("[file]"), selectedItemPaths.ToArgumentsString());
                items = items.Concat(extraItems).ToArray();
            }

            var multiItemConfiguredFileExt = ConfiguredFileExtensions.ParseMultipleExt().Where(x => x.Any(xe => selectedItemPaths.All(e => Path.GetExtension(e)?.Matching(xe) != null)));

            if (multiItemConfiguredFileExt.Count() > 0)
            {
                multiItemConfiguredFileExt.ForEach(extList =>
                {
                    if (selectedItemPaths.All(File.Exists)) // all files, not dirs
                    {
                        bool selectedExtensionIsConfigured = selectedItemPaths.Select(Path.GetExtension)        // .ext
                                                                              .Select(x => x.TrimStart('.'))    // ext
                                                                              .All(x => extList.Contains(x));   // present in the configured extensions

                        if (selectedExtensionIsConfigured)
                        {
                            var extraItems = BuildMenuFrom(GetConfigDirByPath("[" + string.Join(",", extList) + "]"), selectedItemPaths.ToArgumentsString());
                            items = items.Concat(extraItems).ToArray();
                        }
                    }
                });
            }
        }
        catch { }

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(items);

        DisposeLastMenu();
        lastOpenedMenu = menu;

        return menu;
    }

    static ContextMenuStrip lastOpenedMenu;

    void DisposeLastMenu()
    {
        if (lastOpenedMenu != null)
        {
            foreach (var item in lastOpenedMenu.Items.OfType<ToolStripMenuItem>())
            {
                try
                {
                    var img = item.Image;
                    item.Image = null;
                    img?.Dispose();
                }
                catch { }
            }
            lastOpenedMenu.Close();
            lastOpenedMenu.Dispose();
        }
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

        var img = File.Exists(imgFile) ? imgFile.ReadImage() : null;
        return img;
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

                    try
                    {
                        var size = parentMenu.ContentRectangle.Height.ToStandardIconSize();
                        parentMenu.Image = parentMenu.Image?.Resize(size, size);
                    }
                    catch { }

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
                    else if (item.EndsWithAny(".cmd", ".bat", ".ps1"))
                    {
                        var menu = new ToolStripMenuItem
                        {
                            Text = item.ToFileMenuText(),
                            Image = LookupImageFor(item)
                        };

                        try
                        {
                            var size = menu.ContentRectangle.Height.ToStandardIconSize();

                            menu.Image = menu.Image?.Resize(size, size);
                        }
                        catch { }

                        menu.Click += (s, e) =>
                        {
                            Execute(item, invokeArguments);
                        };
                        current.AddItem(menu);
                    }
                }
            }
        }

        return menus.ToArray();
    }

    public static void Cleanup()
    {
        try
        {
            var except = Process.GetCurrentProcess().Id.ToString();
            Directory.GetFiles(App.ConfigDir.PathJoin(".run").EnsureDirectory(), "*.*.ps1")
                     .Select(x => new { path = x, pid = x.GetFileName().Split('.').First() })
                     .Where(x => x.pid != except)
                     .ToList()
                     .ForEach(x =>
                     {
                         try
                         {
                             File.Delete(x.path);
                         }
                         catch { }
                     });
        }
        catch { }
    }

    public static string CloneScript(string script)
    {
        var hash = (Path.GetFullPath(script) + File.GetLastWriteTimeUtc(script)).GetHashCode();

        var clone = App.ConfigDir.PathJoin(".run")
                                 .EnsureDirectory()
                                 .PathJoin($"{Process.GetCurrentProcess().Id}.{hash}.ps1");

        if (!File.Exists(clone))
        {
            var content = File.ReadAllBytes(script);
            File.WriteAllBytes(clone, content);
        }

        return clone;
    }

    public static void Execute(string item, string invokeArguments)
    {
        lock (typeof(App))
        {
            bool showConsole = item.Contains(".c.");
            bool handleMultiselect = item.Contains(".ms.");

            var arguments = new List<string>();

            if (handleMultiselect)
                arguments.AddRange(invokeArguments.SplitCommandLine());
            else
                arguments.Add(invokeArguments);

            foreach (var arg in arguments)
            {
                try
                {
                    var p = new Process();
                    p.StartInfo.FileName = item;

                    if (arg.StartsWith("\"") && arg.EndsWith("\""))
                        p.StartInfo.Arguments = arg;
                    else
                        p.StartInfo.Arguments = $"\"{arg}\"";

                    if (item.EndsWithAny(".ps1"))
                    {
                        p.StartInfo.FileName = "powershell.exe";
                        p.StartInfo.Arguments = $"\"{CloneScript(item)}\" {invokeArguments}";
                    }

                    if (showConsole)
                    {
                        // code below works very well and produces less noise
                        // though it unconditionally waits. Thus an orthodox execution as
                        // above is adequate particularly because it lets user to pose (with 'pause')
                        // in the batch file or path through to the exit.

                        // p.StartInfo.FileName = "cmd.exe";
                        // p.StartInfo.Arguments = $"/K \"\"{item}\" {invokeArguments}";
                    }
                    else
                    {
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

                Task.Run(Cleanup);
            }
        }
    }

    class BuildItem
    {
        public Action<ToolStripItem> AddItem;
        public string dir;
    }

#if DEBUG

    public string[] ConfiguredFileExtensions

#else

    string[] ConfiguredFileExtensions

#endif
        => Directory.GetDirectories(App.ConfigDir)
                    .Select(x => x.GetFileName()) // gets dir name only without the rest of the path
                    .ToArray();

    string GetConfigDirFor(string file)
    {
        var dir = (Directory.Exists(file)) ? App.ConfigDir.PathJoin("[folder]") : App.ConfigDir.PathJoin(file.GetExtension().Replace(".", ""));
        if (Directory.Exists(dir))
            return dir;

        var match = Directory
                    .GetDirectories(App.ConfigDir)
                    .Where(x => Path.GetFileName(file).MatchingAsExpression(Path.GetFileName(x)))
                    .FirstOrDefault();

        return match;
    }

    string GetConfigDirForAny() => App.ConfigDir.PathJoin("[any]");

    string GetConfigDirByPath(string dir) => App.ConfigDir.PathJoin(dir);
}

static class Utils
{
    static Dictionary<string, Image> loadedImages = new Dictionary<string, Image>();

    public static Image ReadImage(this string file)
    {
        var imageId = $"{file}{File.GetLastWriteTimeUtc(file).ToFileTimeUtc()}";

        if (loadedImages.ContainsKey(imageId))
            return loadedImages[imageId];

        using (var ms = new MemoryStream(File.ReadAllBytes(file)))
        {
            Image image;
            if (string.Compare(Path.GetExtension(file), ".ico", StringComparison.OrdinalIgnoreCase) == 0)
            {
                using (var temp = new Icon(ms))
                    image = temp.ToBitmap();
            }
            else
            {
                image = Image.FromStream(ms);
            }
            loadedImages[imageId] = image;
            return image;
        }
    }

    public static Bitmap Resize(this Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        return destImage;
    }
}