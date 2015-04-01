using StyleCop;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TortoiseSVNStyleCop
{
    public static class Ix
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
    }

    internal static class PathUtility
    {
        public static string GetRootPath(IEnumerable<string> filePaths)
        {
            if (filePaths.Any())
            {
                string[] testAgainst = filePaths.First().Split('/');
                int noOfLevels = filePaths.Select(path => EqualLength(testAgainst, path.Split('/'))).Min();
                return (testAgainst.Take(noOfLevels).Aggregate((m, n) => m + "/" + n));
            }
            return string.Empty;
        }

        public static int EqualLength(string[] s0, string[] s1)
        {
            return s0.Zip(s1, (x, y) => x == y).TakeWhile(x => x).Count();
        }

        public static string FindFileFromAncestors(string fileName, string startPath)
        {
            while (startPath != "" && Path.GetPathRoot(startPath) != startPath)
            {
                var path = Path.Combine(startPath, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
                startPath = Path.GetDirectoryName(startPath);
            }
            return null;
        }

        public static string FindFileFromTargetPathsAncestors(string fileName, IEnumerable<string> targetPaths)
        {
            var deepestPath = targetPaths
                .Select(path => new { Path = path, Depth = path.Count(c => c == '/') })
                .OrderByDescending(x => x.Depth)
                .Select(x => x.Path)
                .FirstOrDefault();

            string rootPath = (deepestPath != null) ? FindFileFromAncestors(fileName, Path.GetDirectoryName(deepestPath)) : null;
            // TortoiseSVN から渡されるパスの区切り文字は '/' なのでそちらに合わせる
            return rootPath != null ? rootPath.Replace('\\', '/') : null;
        }
    }

    internal class Context
    {
        public static readonly string IgnoreListFileName = "StyleCopIgnore.txt";
        public static readonly string SettingsFileName = "Settings.StyleCop";

        public string ProjectPath { get; private set; }
        public string SettingsPath { get; private set; }
        public IEnumerable<string> TargetFiles { get; private set; }
        public HashSet<string> IgnoredFiles { get; private set; }

        public Context(IEnumerable<string> filePaths)
        {
            this.ProjectPath = PathUtility.GetRootPath(filePaths);
            var exeDir = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.SettingsPath = Path.Combine(exeDir, SettingsFileName);
            if (!File.Exists(this.SettingsPath))
            {
                this.SettingsPath = null;
            }

            var ignoreListFile = PathUtility.FindFileFromTargetPathsAncestors(IgnoreListFileName, filePaths);
            if (ignoreListFile != null)
            {
                this.IgnoredFiles = File.ReadAllLines(ignoreListFile).ToHashSet();
                this.TargetFiles = filePaths.Where(path => !IgnoredFiles.Contains(path));
            }
            else
            {
                this.TargetFiles = filePaths;
            }
        }
    }

    internal class Program
    {
        public static void Usage(int exitCode)
        {
            var name = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Console.WriteLine(@"
Usage:
  Check style.
    {0} FILELIST

  Check style of files in DIRECTORY.
    {0} -r DIRECTORY

  Create ignored file list.
    {0} -l DIRECTORY > StyleCopIgnore.txt
", name);
            Environment.Exit(exitCode);
        }

        public static void Main(string[] args)
        {
            int foundViolatons = 0;

            if (args.Length == 0 || args.Length > 2)
                Usage(1);

            if (args[0] == "-l")
            {
                if (args.Length == 2)
                {
                    ListAllSourceFilesInSubdirectories(args[1]);
                    Environment.Exit(0);
                }
                Usage(1);
            }

            IEnumerable<string> filePaths;
            if (args[0] == "-r")
            {
                if (args.Length != 2)
                    Usage(1);
                filePaths = AllSourceFilesInSubdirectories(args[1]);
            }
            else
            {
                if (Path.GetExtension(args[0]) == ".cs")
                    filePaths = new string[] { args[0] };
                else
                    filePaths = File.ReadAllLines(args[0]);
            }

            var config = new Context(filePaths);
            List<Violation> violations = Analyze(config);

            foreach (string file in config.TargetFiles)
            {
                List<Violation> fileViolations = violations.FindAll(viol => viol.SourceCode.Path == file);

                if (fileViolations.Count > 0)
                {
                    foundViolatons = 1;
                    Console.Error.WriteLine("{0} - {1} violations.", fileViolations[0].SourceCode.Name, fileViolations.Count);
                    foreach (Violation violation in fileViolations)
                    {
                        Console.Error.WriteLine("      {0}: Line {1}-{2}", violation.Rule.CheckId, violation.Line, violation.Message);
                    }
                }
            }
            Environment.Exit(foundViolatons);
        }

        private static IEnumerable<string> AllSourceFilesInSubdirectories(string path)
        {
            return Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Select( f => f.Replace( '\\', '/') );
        }

        private static void ListAllSourceFilesInSubdirectories(string path)
        {
            foreach (var f in AllSourceFilesInSubdirectories(path))
            {
                Console.WriteLine(f.Replace('\\', '/'));
            }
        }

        private static List<Violation> Analyze(Context config)
        {
            StyleCopConsole styleCopConsole = new StyleCopConsole(config.SettingsPath, false, null, null, true);

            Configuration configuration = new Configuration(null);

            CodeProject project = new CodeProject(0, config.ProjectPath, configuration);

            foreach (string file in config.TargetFiles)
            {
                var loaded = styleCopConsole.Core.Environment.AddSourceCode(project, file, null);
            }

            List<Violation> violations = new List<Violation>();
            styleCopConsole.ViolationEncountered += ((sender, arguments) => violations.Add(arguments.Violation));

            List<string> output = new List<string>();
            styleCopConsole.OutputGenerated += ((sender, arguments) => output.Add(arguments.Output));

            styleCopConsole.Start(new[] { project }, true);
            return violations;
        }

    }
}