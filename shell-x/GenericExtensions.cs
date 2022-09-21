using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ShellX;

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

    public static string[][] ParseMultipleExt(this string[] items)
    {
        return items.Select((x) => x.Contains(",") && x.StartsWith("[") && x.EndsWith("]") ? x.Substring(1, x.Length - 2).Split(',') : null).Where(x => x != null).ToArray();
    }

    public static bool Matching(this string text, string pattern, bool ignoreCase = true)
        => string.Compare(text, pattern, ignoreCase) == 0;

    public static bool MatchingAsExpression(this string text, string rawPattern, bool ignoreCase = true)
    {
        string safeQuestionMark = "？"; // The unicode characters that look like ? and * but still allowed in dir and file names
        string safeAsterisk = "⁎";

        if (rawPattern.IndexOfAny((safeQuestionMark + safeAsterisk).ToArray()) != -1)
        {
            var pattern = rawPattern.Replace(safeQuestionMark, "?").Replace(safeAsterisk, "*");
            var wildcard = new Regex(pattern.ConvertSimpleExpToRegExp(), ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            return wildcard.IsMatch(text);
        }
        return false;
    }

    internal static bool EndsWithAny(this string text, params string[] patterns)
        => patterns.Any(x => text.EndsWith(x, StringComparison.OrdinalIgnoreCase));

    public static string GetPath(this Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);

    public static string PathJoin(this string path, params string[] paths)
        => Path.Combine(new[] { path }.Concat(paths).ToArray());

    public static string GetTitle(this Assembly assembly) => assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;

    public static bool IsDir(this string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

    public static bool IsFile(this string path) => !path.IsDir();

    public static string ToArgumentsString(this IEnumerable<string> args) => string.Join(" ", args.Select(x => $"\"{x}\"").ToArray());

    public static string GetDirName(this string path) => Path.GetDirectoryName(path);

    public static string GetFileName(this string path) => Path.GetFileName(path);

    public static string GetExtension(this string path) => Path.GetExtension(path);

    public static void Save(this Icon icon, string file)
    {
        using (var fs = new FileStream(file, FileMode.Create))
            icon.Save(fs);
    }

    public static string ChangeExtensionTo(this string path, string newExtension) => Path.ChangeExtension(path, newExtension);

    public static string ToFileMenuText(this string path) => path.GetFileName()
                                                                 .Split(new[] { '.' }, 2)
                                                                 .Last()
                                                                 .Replace(".c.cmd", "")
                                                                 .Replace(".c.bat", "")
                                                                 .Replace(".c.ps1", "")
                                                                 .Replace(".cmd", "")
                                                                 .Replace(".bat", "")
                                                                 .Replace(".ps1", "");

    public static string ToDirMenuText(this string path) => path.GetFileName().Split(new[] { '.' }, 2).Last();

    public static string GetFileNameWithoutExtension(this string path) => Path.GetFileNameWithoutExtension(path);

    //Credit to MDbg team: https://github.com/SymbolSource/Microsoft.Samples.Debugging/blob/master/src/debugger/mdbg/mdbgCommands.cs
    public static string ConvertSimpleExpToRegExp(this string simpleExp)
    {
        //
        // string pattern = ConvertSimpleExpToRegExp();
        // var wildcard = new Regex(pattern, RegexOptions.IgnoreCase);
        // if (wildcard.IsMatch(dir))
        // //

        var sb = new StringBuilder();
        sb.Append("^");
        foreach (char c in simpleExp)
        {
            switch (c)
            {
                case '\\':
                case '{':
                case '|':
                case '+':
                case '[':
                case '(':
                case ')':
                case '^':
                case '$':
                case '.':
                case '#':
                case ' ':
                    sb.Append('\\').Append(c);
                    break;

                case '*':
                    sb.Append(".*");
                    break;

                case '?':
                    sb.Append(".");
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        sb.Append("$");
        return sb.ToString();
    }
}

class ExplorerStub
{
    public static int Test(string path)
    {
        TestForm.Show(path);
        return 0;
    }
}