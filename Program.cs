using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace gitls
{

    class Program
    {
        static void Main(string[] args)
        {
            // Set director to current directory
            var dir = Directory.GetCurrentDirectory();

            // If first argument exists and is a directory, set that as directory
            if (args.Length > 0 && !args[0].Equals(null) && Directory.Exists(args[0]))
                dir = args[0];

            // Check directory, if exists, continue, else exit
            if (Directory.Exists(dir))
            {
                var search = System.IO.SearchOption.TopDirectoryOnly;
                if (args.Contains("-r"))
                    search = SearchOption.AllDirectories;

                // Load all directories from the current directory
                var dirs = Directory.GetDirectories(dir, "*", search);

                // Find each directory where a .git subdirectory exists
                Parallel.ForEach(dirs.Select(d => new GitDirectory(d)), subdir =>
                {
                    if (subdir.IsGitDirectory)
                    {
                        // Output the directory name
                        Console.Write(subdir.Path);

                        // Save the console color before we change it
                        var firstcolor = Console.ForegroundColor;

                        // Change the color of the brackets
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(" [");

                        // Write the branch, color determined by status
                        switch (subdir.BranchStatus)
                        {
                            case GitBranchStatus.Ahead:
                                Console.ForegroundColor = ConsoleColor.Green;
                                break;
                            case GitBranchStatus.Behind:
                                Console.ForegroundColor = ConsoleColor.Red;
                                break;
                            default:
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                break;
                        }

                        Console.Write(subdir.BranchName);

                        // If any files in the index are modified
                        if (subdir.AddedIndexCount + subdir.ModifiedIndexCount + subdir.DeletedIndexCount > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.Write(string.Format(" +{0} ~{1} -{2}", subdir.AddedIndexCount, subdir.ModifiedIndexCount, subdir.DeletedIndexCount));
                        }

                        // If files in both the work tree and index are modified, introduce a seporator
                        if (subdir.AddedWorkTreeCount + subdir.ModifiedWorkTreeCount + subdir.DeletedWorkTreeCount + subdir.UntrackedCount > 0 && subdir.AddedIndexCount + subdir.ModifiedIndexCount + subdir.DeletedIndexCount > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write(" |");
                        }

                        // If any files in the work tree are modified
                        if (subdir.AddedWorkTreeCount + subdir.ModifiedWorkTreeCount + subdir.DeletedWorkTreeCount + subdir.UntrackedCount > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write(string.Format(" +{0} ~{1} -{2}", subdir.AddedWorkTreeCount + subdir.UntrackedCount, subdir.ModifiedWorkTreeCount, subdir.DeletedWorkTreeCount));
                        }

                        // End the branches
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("]");

                        // Reset the color for the user
                        Console.ForegroundColor = firstcolor;
                    }
                    else if (args.Contains("-e"))
                    {
                        Console.WriteLine(subdir.Path);
                    }
                });
            }
        }
    }
}
