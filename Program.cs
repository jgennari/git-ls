using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace gitls
{
    public enum GitStatus
    {
        None,
        Added,
        Deleted,
        Updated,
        Copied,
        Renamed,
        Modified,
        Unmodified,
        Untracked,
        Comment
    }

    class GitStatusEntry
    {
        public GitStatusEntry(string line)
        {
            if (line.Length > 3)
            {
                this.IndexStatus = ParseGitStatus(line.Substring(0, 1));
                this.WorkTreeStatus = ParseGitStatus(line.Substring(1, 2));
                this.Value = line.Substring(3);
            }
        }

        public GitStatus IndexStatus { get; set; }
        public GitStatus WorkTreeStatus { get; set; }

        public string Value { get; set; }
        
        public static GitStatus ParseGitStatus(string status)
        {
            if (status.Length < 1)
                return GitStatus.None;

            if (status.Substring(0, 1) == "#")
                return GitStatus.Comment;

            switch (status.Substring(0, 1))
            {
                case "M":
                    return GitStatus.Modified;
                case "D":
                    return GitStatus.Deleted;
                case "U":
                    return GitStatus.Updated;
                case "C":
                    return GitStatus.Copied;
                case "A":
                    return GitStatus.Added;
                case "R":
                    return GitStatus.Renamed;
                case "?":
                    return GitStatus.Untracked;                    
                case " ":
                    return GitStatus.Unmodified;
                default:
                    return GitStatus.None;
            }
        }
    }

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
                foreach (var subDirectory in dirs)
                {
                    if (Directory.Exists(Path.Combine(subDirectory, ".git")))
                    {
                        // Fire a proccess to get the git status
                        Process pStatus = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                WorkingDirectory = subDirectory,
                                FileName = "git.exe",
                                Arguments = "status -s -b" // -s = short, -b = branch
                            }
                        };

                        pStatus.Start();
                        var outputStatus = pStatus.StandardOutput.ReadToEnd().Split(Environment.NewLine.ToCharArray()).Select(l => new GitStatusEntry(l));
                        pStatus.WaitForExit();

                        // Branch name and status starts with ##
                        var branch = outputStatus.Single(s => s.IndexStatus == GitStatus.Comment).Value;
                    
                        // Save the console color before we change it
                        var firstcolor = Console.ForegroundColor;

                        var branchcolor = ConsoleColor.Cyan;

                        // Look for the ahead/behind keywords and set the color, also eliminate the trailing text
                        if (branch.Contains("Initial commit on"))
                        {
                            branch = branch.Replace("Initial commit on ", "");
                        }
                        else if (branch.Contains("...") && branch.Contains("[ahead"))
                        {
                            branch = branch.Substring(0, branch.IndexOf("..."));
                            branchcolor = ConsoleColor.Green;
                        }
                        else if (branch.Contains("...") && branch.Contains("[behind"))
                        {
                            branch = branch.Substring(0, branch.IndexOf("..."));
                            branchcolor = ConsoleColor.Red;
                        }

                        var added = new[] { GitStatus.Added };
                        var modified = new[] { GitStatus.Modified, GitStatus.Renamed, GitStatus.Updated, GitStatus.Copied };
                        var deleted = new[] { GitStatus.Deleted };

                        // Staged changes
                        var addedindexcount = outputStatus.Where(s => added.Contains(s.IndexStatus)).Count();
                        var modifiedindexcount = outputStatus.Where(s => modified.Contains(s.IndexStatus)).Count();
                        var deletedindexcount = outputStatus.Where(s => deleted.Contains(s.IndexStatus)).Count();

                        // Work tree changes
                        var addedworkcount = outputStatus.Where(s => added.Contains(s.WorkTreeStatus)).Count();
                        var modifiedworkcount = outputStatus.Where(s => modified.Contains(s.WorkTreeStatus)).Count();
                        var deletedworkcount = outputStatus.Where(s => deleted.Contains(s.WorkTreeStatus)).Count();

                        var untrackedcount = outputStatus.Where(s => s.IndexStatus == GitStatus.Untracked).Count();
                    
                        // Output the directory name
                        Console.Write(subDirectory);

                        // Change the color of the brackets
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(" [");

                        // Write the branch based on the color decided above
                        Console.ForegroundColor = branchcolor;
                        Console.Write(branch);

                        // If any files in the index are modified
                        if (addedindexcount + modifiedindexcount + deletedworkcount > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.Write(string.Format(" +{0} ~{1} -{2}", addedindexcount, modifiedindexcount, deletedworkcount));
                        }

                        // If files in both the work tree and index are modified, introduce a seporator
                        if (addedworkcount + modifiedworkcount + deletedworkcount + untrackedcount > 0 && addedindexcount + modifiedindexcount + deletedworkcount > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write(" |");
                        }

                        // If any files in the work tree are modified
                        if (addedworkcount + modifiedworkcount + deletedworkcount + untrackedcount > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write(string.Format(" +{0} ~{1} -{2}", addedworkcount + untrackedcount, modifiedworkcount, deletedworkcount));
                        }

                        // End the branches
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("]");

                        // Reset the color for the user
                        Console.ForegroundColor = firstcolor;
                    }                    
                    else if (args.Contains("-e"))
                    {
                        Console.WriteLine(subDirectory);
                    }
                }
            }
        }
    }
}
