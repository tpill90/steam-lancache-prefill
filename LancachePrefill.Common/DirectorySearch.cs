namespace LancachePrefill.Common
{
    public static class DirectorySearch
    {
        /// <summary>
        /// Searches upwards for the first .sln file that is found.  Will likely be the .sln for the current project.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <returns>DirectoryInfo for the .sln containing folder.</returns>
        public static DirectoryInfo TryGetSolutionDirectory(string currentPath = null)
        {
            var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }

        /// <summary>
        /// Searches upwards for the absolute root of the current repository.  Looks for the top level .git folder to detect the repository root.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <returns>DirectoryInfo for the repository root.</returns>
        public static DirectoryInfo TryGetRepoRoot(string currentPath = null)
        {
            var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetDirectories(".git").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }
    }
}