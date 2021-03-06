﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DuetControlServer
{
    /// <summary>
    /// Shortcut for ToPhysicalAsync() to avoid multiple nested locks
    /// </summary>
    public enum FileDirectory
    {
        /// <summary>
        /// Filaments directory
        /// </summary>
        Filaments,

        /// <summary>
        /// GCodes directory
        /// </summary>
        GCodes,

        /// <summary>
        /// Macros directory
        /// </summary>
        Macros,
        
        /// <summary>
        /// System directory
        /// </summary>
        System,

        /// <summary>
        /// WWW directory
        /// </summary>
        WWW
    }

    /// <summary>
    /// Static class used to provide functions for file path resolution
    /// </summary>
    public static class FilePath
    {
        /// <summary>
        /// Default name of the config file
        /// </summary>
        public const string ConfigFile = "config.g";

        /// <summary>
        /// Fallback file if the config file could not be found
        /// </summary>
        public const string ConfigFileFallback = "config.g.bak";

        /// <summary>
        /// Config override as generated by M500
        /// </summary>
        public const string ConfigOverrideFile = "config-override.g";

        /// <summary>
        /// Default heightmap file
        /// </summary>
        public const string DefaultHeightmapFile = "heightmap.csv";

        /// <summary>
        /// File holding the filaments mapping
        /// </summary>
        public const string FilamentsFile = "filaments.csv";

        /// <summary>
        /// Resolve a RepRapFirmware/FatFs-style file path to a physical file path asynchronously.
        /// The first drive (0:/) is reserved for usage with the base directory as specified in the settings
        /// </summary>
        /// <param name="filePath">File path to resolve</param>
        /// <param name="directory">Directory containing filePath if it is not absolute is specified</param>
        /// <returns>Resolved file path</returns>
        public static async Task<string> ToPhysicalAsync(string filePath, FileDirectory directory)
        {
            Match match = Regex.Match(filePath, "^(\\d+):?/?(.*)");
            if (match.Success)
            {
                int driveNumber = int.Parse(match.Groups[1].Value);
                if (driveNumber == 0)
                {
                    return Path.Combine(Path.GetFullPath(Settings.BaseDirectory), match.Groups[2].Value);
                }

                using (await Model.Provider.AccessReadOnlyAsync())
                {
                    if (driveNumber > 0 && driveNumber < Model.Provider.Get.Storages.Count)
                    {
                        return Path.Combine(Model.Provider.Get.Storages[driveNumber].Path, match.Groups[2].Value);
                    }
                }

                throw new ArgumentException("Invalid drive index");
            }

            if (!filePath.StartsWith('/'))
            {
                string directoryPath;
                using (await Model.Provider.AccessReadOnlyAsync())
                {
                    directoryPath = directory switch
                    {
                        FileDirectory.Filaments => Model.Provider.Get.Directories.Filaments,
                        FileDirectory.GCodes => Model.Provider.Get.Directories.GCodes,
                        FileDirectory.Macros => Model.Provider.Get.Directories.Macros,
                        FileDirectory.WWW => Model.Provider.Get.Directories.WWW,
                        _ => Model.Provider.Get.Directories.System,
                    };

                    match = Regex.Match(directoryPath, "^(\\d+):?/?(.*)");
                    if (match.Success)
                    {
                        int driveNumber = int.Parse(match.Groups[1].Value);
                        if (driveNumber == 0)
                        {
                            directoryPath = Path.Combine(Path.GetFullPath(Settings.BaseDirectory), match.Groups[2].Value);
                        }

                        if (driveNumber > 0 && driveNumber < Model.Provider.Get.Storages.Count)
                        {
                            directoryPath = Path.Combine(Model.Provider.Get.Storages[driveNumber].Path, match.Groups[2].Value);
                        }
                    }
                }
                return Path.Combine(Path.GetFullPath(Settings.BaseDirectory), directoryPath, filePath);
            }
            return Path.Combine(Path.GetFullPath(Settings.BaseDirectory), filePath.StartsWith('/') ? filePath.Substring(1) : filePath);
        }

        /// <summary>
        /// Resolve a RepRapFirmware/FatFs-style file path to a physical file path asynchronously.
        /// The first drive (0:/) is reserved for usage with the base directory as specified in the settings.
        /// </summary>
        /// <param name="filePath">File path to resolve</param>
        /// <param name="directory">Directory containing filePath if it is not absolute is specified</param>
        /// <returns>Resolved file path</returns>
        public static async Task<string> ToPhysicalAsync(string filePath, string directory = null)
        {
            Match match = Regex.Match(filePath, "^(\\d+):?/?(.*)");
            if (match.Success)
            {
                int driveNumber = int.Parse(match.Groups[1].Value);
                if (driveNumber == 0)
                {
                    return Path.Combine(Path.GetFullPath(Settings.BaseDirectory), match.Groups[2].Value);
                }

                using (await Model.Provider.AccessReadOnlyAsync())
                {
                    if (driveNumber > 0 && driveNumber < Model.Provider.Get.Storages.Count)
                    {
                        return Path.Combine(Model.Provider.Get.Storages[driveNumber].Path, match.Groups[2].Value);
                    }
                }

                throw new ArgumentException("Invalid drive index");
            }

            if (directory != null && !filePath.StartsWith('/'))
            {
                match = Regex.Match(directory, "^(\\d+):?/?(.*)");
                if (match.Success)
                {
                    int driveNumber = int.Parse(match.Groups[1].Value);
                    if (driveNumber == 0)
                    {
                        directory = Path.Combine(Path.GetFullPath(Settings.BaseDirectory), match.Groups[2].Value);
                    }

                    using (await Model.Provider.AccessReadOnlyAsync())
                    {
                        if (driveNumber > 0 && driveNumber < Model.Provider.Get.Storages.Count)
                        {
                            directory = Path.Combine(Model.Provider.Get.Storages[driveNumber].Path, match.Groups[2].Value);
                        }
                    }
                }

                return Path.Combine(Path.GetFullPath(Settings.BaseDirectory), directory, filePath);
            }
            return Path.Combine(Path.GetFullPath(Settings.BaseDirectory), filePath.StartsWith('/') ? filePath.Substring(1) : filePath);
        }

        /// <summary>
        /// Convert a physical ile path to a RRF-style file path asynchronously.
        /// The first drive (0:/) is reserved for usage with the base directory as specified in the settings.
        /// </summary>
        /// <param name="filePath">File path to convert</param>
        /// <returns>Resolved file path</returns>
        public static async Task<string> ToVirtualAsync(string filePath)
        {
            if (filePath.StartsWith(Settings.BaseDirectory))
            {
                filePath = filePath.Substring(Settings.BaseDirectory.EndsWith('/') ? Settings.BaseDirectory.Length : (Settings.BaseDirectory.Length + 1));
                return Path.Combine("0:/", filePath);
            }

            using (await Model.Provider.AccessReadOnlyAsync())
            {
                foreach (var storage in Model.Provider.Get.Storages)
                {
                    if (filePath.StartsWith(storage.Path))
                    {
                        return Path.Combine("0:/", filePath.Substring(storage.Path.Length));
                    }
                }
            }

            return Path.Combine("0:/", filePath);
        }
    }
}
