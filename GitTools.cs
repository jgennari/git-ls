using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace gitls
{
    public enum GitFileStatus
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

    public enum GitBranchStatus
    {
        Initial,
        Ahead,
        Behind,
        UpToDate
    }

    public class GitDirectory
    {
        public string Path { get; set; }
        public IEnumerable<GitFileStatus> Statuses { get; set; }
        public bool IsGitDirectory { get; set; }
        public GitBranchStatus BranchStatus { get; private set; }
        public string BranchName { get; private set; }

        public int AddedIndexCount { get; private set; }
        public int ModifiedIndexCount { get; private set; }
        public int DeletedIndexCount { get; private set; }
        public int AddedWorkTreeCount { get; private set; }
        public int ModifiedWorkTreeCount { get; private set; }
        public int DeletedWorkTreeCount { get; private set; }
        public int UntrackedCount { get; private set; }

        public GitDirectory(string path)
        {
            this.Path = path;
            if (Directory.Exists(System.IO.Path.Combine(path, ".git")))
            {
                this.IsGitDirectory = true;
                // Fire a proccess to get the git status
                Process pStatus = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        WorkingDirectory = path,
                        FileName = "git.exe",
                        Arguments = "status -s -b" // -s = short, -b = branch
                    }
                };

                pStatus.Start();
                var outputStatus = pStatus.StandardOutput.ReadToEnd().Split(Environment.NewLine.ToCharArray()).Select(l => new GitStatusEntry(l));
                pStatus.WaitForExit();

                // Branch name and status starts with ##
                var branch = outputStatus.Single(s => s.IndexStatus == GitFileStatus.Comment).Value;

                // Look for the ahead/behind keywords and set the color, also eliminate the trailing text
                if (branch.Contains("Initial commit on"))
                {
                    branch = branch.Replace("Initial commit on ", "");
                    this.BranchStatus = GitBranchStatus.Initial;
                }
                else if (branch.Contains("...") && branch.Contains("[ahead"))
                {
                    branch = branch.Substring(0, branch.IndexOf("..."));
                    this.BranchStatus = GitBranchStatus.Ahead;
                }
                else if (branch.Contains("...") && branch.Contains("[behind"))
                {
                    branch = branch.Substring(0, branch.IndexOf("..."));
                    this.BranchStatus = GitBranchStatus.Behind;
                }
                else
                {
                    this.BranchStatus = GitBranchStatus.UpToDate;
                }

                this.BranchName = branch;

                var added = new[] { GitFileStatus.Added };
                var modified = new[] { GitFileStatus.Modified, GitFileStatus.Renamed, GitFileStatus.Updated, GitFileStatus.Copied };
                var deleted = new[] { GitFileStatus.Deleted };

                // Staged changes
                this.AddedIndexCount = outputStatus.Where(s => added.Contains(s.IndexStatus)).Count();
                this.ModifiedIndexCount = outputStatus.Where(s => modified.Contains(s.IndexStatus)).Count();
                this.DeletedIndexCount = outputStatus.Where(s => deleted.Contains(s.IndexStatus)).Count();

                // Work tree changes
                this.AddedWorkTreeCount = outputStatus.Where(s => added.Contains(s.WorkTreeStatus)).Count();
                this.ModifiedWorkTreeCount = outputStatus.Where(s => modified.Contains(s.WorkTreeStatus)).Count();
                this.DeletedWorkTreeCount = outputStatus.Where(s => deleted.Contains(s.WorkTreeStatus)).Count();

                this.UntrackedCount = outputStatus.Where(s => s.IndexStatus == GitFileStatus.Untracked).Count();
            }
            else
                this.IsGitDirectory = false;
        }
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

        public GitFileStatus IndexStatus { get; set; }
        public GitFileStatus WorkTreeStatus { get; set; }

        public string Value { get; set; }

        public static GitFileStatus ParseGitStatus(string status)
        {
            if (status.Length < 1)
                return GitFileStatus.None;

            if (status.Substring(0, 1) == "#")
                return GitFileStatus.Comment;

            switch (status.Substring(0, 1))
            {
                case "M":
                    return GitFileStatus.Modified;
                case "D":
                    return GitFileStatus.Deleted;
                case "U":
                    return GitFileStatus.Updated;
                case "C":
                    return GitFileStatus.Copied;
                case "A":
                    return GitFileStatus.Added;
                case "R":
                    return GitFileStatus.Renamed;
                case "?":
                    return GitFileStatus.Untracked;
                case " ":
                    return GitFileStatus.Unmodified;
                default:
                    return GitFileStatus.None;
            }
        }
    }
}
