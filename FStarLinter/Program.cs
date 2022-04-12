// See https://aka.ms/new-console-template for more information
using System.Text.RegularExpressions;
using static System.Console;

if (args.Length == 0)
{
    Console.WriteLine("No folder with F* sources provided");
    return;
}

var sourcesFolder = args[0];
var files = Directory.EnumerateFiles(sourcesFolder, "*.fst?", SearchOption.AllDirectories);
foreach (var file in files)
{
    if (file.Contains("examples") || file.Contains("tests"))
    {
        continue;
    }

    ProcessFile(file);
}


void ProcessFile(string file)
{
    //WriteLine($"Processing file {file}");
    var lines = File.ReadAllLines(file);
    file = Path.GetRelativePath(sourcesFolder, file);
    var map = new Dictionary<string, ModuleInclusitonReport>();
    string? moduleName = null;
    foreach (var line in lines)
    {
        var moduleMatch = Regex.Match(line, @"^module (?<name>[\w\.]+)\s*$");
        if (moduleMatch.Success)
        {
            var capturedModuleName = moduleMatch.Groups["name"].Value;
            if (moduleName == null)
            { 
                moduleName = capturedModuleName;
                if (string.Compare(capturedModuleName, Path.GetFileNameWithoutExtension(file), true) != 0)
                {
                    WriteLine($"Module name {capturedModuleName} does not match with filename {file}");
                }
            }
            else
            {
                WriteLine($"Non unique module in the file {file}. Additional module {capturedModuleName}");
            }

            // Console.WriteLine($"Processing module {moduleName}");
        }

        var openMatch = Regex.Match(line, @"^open (?<name>[\w\.]+)\s*$");
        if (openMatch.Success)
        {
            var openedModuleName = openMatch.Groups["name"].Value;
            if (map.TryGetValue(openedModuleName, out var openedModule))
            {
                openedModule.FullNameInclusionCount++;
            }
            else
            {
                map.Add(openedModuleName, new ModuleInclusitonReport() { FullNameInclusionCount = 1 });
            }
        }

        var aliasOpenMatch = Regex.Match(line, @"^module\s+(?<alias>[\w\.]+)\s+=\s+(?<name>[\w\.]+)\s*$");
        if (aliasOpenMatch.Success)
        {
            var aliasOpenedModuleName = aliasOpenMatch.Groups["name"].Value;
            if (map.TryGetValue(aliasOpenedModuleName, out var aliasOpenedModule))
            {
                aliasOpenedModule.UsingAliasInclusionCount++;
            }
            else
            {
                map.Add(aliasOpenedModuleName, new ModuleInclusitonReport() { UsingAliasInclusionCount = 1 });
            }
        }
    }

    foreach (var (name, moduleInclusionReport) in map)
    {
        if (!moduleInclusionReport.IsOk)
        {
            if (moduleInclusionReport.FullNameInclusionCount > 1)
            {
                WriteLine($"Module {name} included {moduleInclusionReport.FullNameInclusionCount} times in the {file}");
            }

            if (moduleInclusionReport.UsingAliasInclusionCount > 1)
            {
                WriteLine($"Module {name} included using alias {moduleInclusionReport.UsingAliasInclusionCount} times in the {file}");
            }

            if (moduleInclusionReport.FullNameInclusionCount >= 1 && moduleInclusionReport.UsingAliasInclusionCount >= 1)
            {
                WriteLine($"Module {name} included using using both alias and globally in the {file}");
            }
        }
    }
}

class ModuleInclusitonReport
{
    public int FullNameInclusionCount;
    public int UsingAliasInclusionCount;

    public bool IsOk => FullNameInclusionCount + UsingAliasInclusionCount == 1;
}