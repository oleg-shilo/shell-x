using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

static class DSLExtensions
{
    public static string ExpandEnvars(this string text) => Environment.ExpandEnvironmentVariables(text);

    public static string EnsureDirectory(this string dir)
    {
        Directory.CreateDirectory(dir); return dir;
    }

    public static bool ContainsAny<T>(this IEnumerable<T> items, params T[] patterns)
        => patterns.Any(x => items.Contains(x));

    public static bool IsRegeiteredComServer(this Type type)
    {
        var registered = false;
        try
        {
            Type comType = Type.GetTypeFromCLSID(type.GUID);
            object instance = Activator.CreateInstance(comType);
            registered = true;
        }
        catch { }
        return registered;
    }

    public static bool Matching(this string text, string pattern, bool ignoreCase = true)
        => string.Compare(text, pattern, ignoreCase) == 0;

    internal static bool EndsWithAny(this string text, params string[] patterns)
        => patterns.Any(x => text.EndsWith(x, StringComparison.OrdinalIgnoreCase));

    public static string GetPath(this Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);

    public static string PathJoin(this string path, params string[] paths)
        => Path.Combine(new[] { path }.Concat(paths).ToArray());

    public static string GetTitle(this Assembly assembly) => assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;

    public static bool IsDir(this string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

    public static string ToArgumentsString(this IEnumerable<string> args) => string.Join(" ", args.Select(x => $"\"{x}\"").ToArray());

    public static string GetDirName(this string path) => Path.GetDirectoryName(path);

    public static string GetFileName(this string path) => Path.GetFileName(path);

    public static string GetExtension(this string path) => Path.GetExtension(path);

    public static string ToFileMenuText(this string path) => path.GetFileName()
                                                                 .Split(new[] { '.' }, 2)
                                                                 .Last()
                                                                 .Replace(".c.cmd", "")
                                                                 .Replace(".c.bat", "")
                                                                 .Replace(".cmd", "")
                                                                 .Replace(".bat", "");

    public static string ToDirMenuText(this string path) => path.GetFileName().Split(new[] { '.' }, 2).Last();

    public static string GetFileNameWithoutExtension(this string path) => Path.GetFileNameWithoutExtension(path);
}