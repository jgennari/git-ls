using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace gitls
{
    public static class DirectoryTools
    {

        public static IEnumerable<string> GetDirectories(string path, SearchOption option)
        {         
            var dirs = new List<string>();

            try
            {
                dirs = Directory.GetDirectories(path).ToList();
            }
            catch (UnauthorizedAccessException)
            {
            }

            foreach (var dir in dirs)
            {
                if (option == SearchOption.AllDirectories)
                {
                    foreach (var x in GetDirectories(dir, option))
                    {
                        yield return x;
                    }
                }

                yield return dir;
            }
        }
    }
}
