using StyleCop;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TortoiseSVNStyleCop
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            int foundViolatons = 0;

            string[] filePaths = File.ReadAllLines(args[0]);
            filePaths = filePaths.Where(path => Path.GetExtension(path).ToLower() == ".cs").ToArray();
            string projectPath = GetRootPath(filePaths);
            string settingsPath = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, @"Settings.StyleCop");
            if (File.Exists(settingsPath))
            {
                settingsPath = null;
            }
            Console.Error.WriteLine("DEBUG: {0}", settingsPath);
            List<Violation> violations = Analyze(filePaths, projectPath, settingsPath);

            foreach (string file in filePaths)
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

        private static List<Violation> Analyze(string[] filePaths, string projectPath, string settingsPath)
        {
            StyleCopConsole styleCopConsole = new StyleCopConsole(settingsPath, false, null, null, true);

            Configuration configuration = new Configuration(null);

            CodeProject project = new CodeProject(0, projectPath, configuration);

            foreach (string file in filePaths)
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

        private static string GetRootPath(string[] filePaths)
        {
            if (filePaths.Length > 0)
            {
                string[] testAgainst = filePaths[0].Split('/');
                int noOfLevels = filePaths.Select(path => EqualLength(testAgainst, path.Split('/'))).Min();
                return (testAgainst.Take(noOfLevels).Aggregate((m, n) => m + "/" + n));
            }
            return string.Empty;
        }

        private static int EqualLength(string[] s0, string[] s1)
        {
            return s0.Zip(s1, (x, y) => x == y).TakeWhile(x => x).Count();
        }
    }
}