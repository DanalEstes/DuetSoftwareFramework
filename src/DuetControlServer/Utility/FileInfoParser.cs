﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DuetAPI;
using DuetAPI.Commands;
using Code = DuetControlServer.Commands.Code;

namespace DuetControlServer
{
    /// <summary>
    /// Static class used to retrieve information from G-code jobs
    /// </summary>
    public static class FileInfoParser
    {
        /// <summary>
        /// Parse a G-code file
        /// </summary>
        /// <param name="fileName">File to analyze</param>
        /// <returns>Information about the file</returns>
        public static async Task<ParsedFileInfo> Parse(string fileName)
        {
            using FileStream fileStream = new FileStream(fileName, FileMode.Open);
            using StreamReader reader = new StreamReader(fileStream, null, true, Settings.FileInfoReadBufferSize);
            ParsedFileInfo result = new ParsedFileInfo
            {
                FileName = await FilePath.ToVirtualAsync(fileName),
                Size = fileStream.Length,
                LastModified = File.GetLastWriteTime(fileName)
            };

            if (fileStream.Length > 0)
            {
                await ParseHeader(reader, result);
                await ParseFooter(reader, result);
                if (result.FirstLayerHeight + result.LayerHeight > 0F && result.Height > 0F)
                {
                    result.NumLayers = (int?)(Math.Round((result.Height - result.FirstLayerHeight) / result.LayerHeight) + 1);
                }
            }
            return result;
        }

        /// <summary>
        /// Parse the header of a G-code file
        /// </summary>
        /// <param name="reader">Stream reader</param>
        /// <param name="partialFileInfo">G-code file information</param>
        /// <returns>Asynchronous task</returns>
        private static async Task ParseHeader(StreamReader reader, ParsedFileInfo partialFileInfo)
        {
            // Every time CTS.Token is accessed a copy is generated. Hence we cache one until this method completes
            CancellationToken token = Program.CancelSource.Token;

            Code code = new Code();
            bool inRelativeMode = false, lastLineHadInfo = false, enforcingAbsolutePosition = false;
            do
            {
                token.ThrowIfCancellationRequested();

                // Read another line
                string line = await reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                // See what codes to deal with
                bool gotNewInfo = false;
                using (StringReader stringReader = new StringReader(line))
                {
                    while (Code.Parse(stringReader, code, ref enforcingAbsolutePosition))
                    {
                        if (code.Type == CodeType.GCode && partialFileInfo.FirstLayerHeight == 0)
                        {
                            if (code.MajorNumber == 91)
                            {
                                // G91 code (relative positioning)
                                inRelativeMode = true;
                                gotNewInfo = true;
                            }
                            else if (inRelativeMode)
                            {
                                // G90 (absolute positioning)
                                inRelativeMode = (code.MajorNumber != 90);
                                gotNewInfo = true;
                            }
                            else if (code.MajorNumber == 0 || code.MajorNumber == 1)
                            {
                                // G0/G1 is a move, see if there is a Z parameter present
                                CodeParameter zParam = code.Parameter('Z');
                                if (zParam != null)
                                {
                                    float z = zParam;
                                    if (z <= Settings.MaxLayerHeight)
                                    {
                                        partialFileInfo.FirstLayerHeight = z;
                                        gotNewInfo = true;
                                    }
                                }
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(code.Comment))
                        {
                            gotNewInfo |= partialFileInfo.LayerHeight == 0 && FindLayerHeight(line, ref partialFileInfo);
                            gotNewInfo |= FindFilamentUsed(line, ref partialFileInfo);
                            gotNewInfo |= string.IsNullOrEmpty(partialFileInfo.GeneratedBy) && FindGeneratedBy(line, ref partialFileInfo);
                            gotNewInfo |= partialFileInfo.PrintTime == 0 && FindPrintTime(line, ref partialFileInfo);
                            gotNewInfo |= partialFileInfo.SimulatedTime == 0 && FindSimulatedTime(line, ref partialFileInfo);
                        }
                        code.Reset();
                    }
                }

                // Is the file info complete?
                if (!gotNewInfo && !lastLineHadInfo && IsFileInfoComplete(partialFileInfo))
                {
                    break;
                }
                lastLineHadInfo = gotNewInfo;
            }
            while (reader.BaseStream.Position < Settings.FileInfoReadLimitHeader + Settings.FileInfoReadBufferSize);
        }

        /// <summary>
        /// Parse the footer of a G-code file
        /// </summary>
        /// <param name="reader">Stream reader</param>
        /// <param name="partialFileInfo">G-code file information</param>
        /// <returns>Asynchronous task</returns>
        private static async Task ParseFooter(StreamReader reader, ParsedFileInfo partialFileInfo)
        {
            CancellationToken token = Program.CancelSource.Token;
            reader.BaseStream.Seek(0, SeekOrigin.End);

            Code code = new Code();
            bool inRelativeMode = false, lastLineHadInfo = false, hadFilament = partialFileInfo.Filament.Count > 0, enforcingAbsolutePosition = false;

            char[] buffer = new char[Settings.FileInfoReadBufferSize];
            int bufferPointer = 0;
            do
            {
                token.ThrowIfCancellationRequested();

                // Read another line
                ReadLineFromEndResult readResult = await ReadLineFromEndAsync(reader, buffer, bufferPointer);
                if (readResult == null)
                {
                    break;
                }
                bufferPointer = readResult.BufferPointer;

                // See what codes to deal with
                bool gotNewInfo = false;
                using (StringReader stringReader = new StringReader(readResult.Line))
                {
                    while (Code.Parse(stringReader, code, ref enforcingAbsolutePosition))
                    {
                        if (code.Type == CodeType.GCode && partialFileInfo.Height == 0)
                        {
                            if (code.MajorNumber == 90)
                            {
                                // G90 code (absolute positioning) implies we were in relative mode
                                inRelativeMode = true;
                                gotNewInfo = true;
                            }
                            else if (inRelativeMode)
                            {
                                // G91 code (relative positioning) implies we were in absolute mode
                                inRelativeMode = (code.MajorNumber != 91);
                                gotNewInfo = true;
                            }
                            else if (code.MajorNumber == 0 || code.MajorNumber == 1)
                            {
                                // G0/G1 is an absolute move, see if there is a Z parameter present
                                CodeParameter zParam = code.Parameter('Z');
                                if (zParam != null && (code.Comment == null || !code.Comment.TrimStart().StartsWith("E")))
                                {
                                    gotNewInfo = true;
                                    partialFileInfo.Height = zParam;
                                }
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(code.Comment))
                        {
                            gotNewInfo |= partialFileInfo.LayerHeight == 0 && FindLayerHeight(readResult.Line, ref partialFileInfo);
                            gotNewInfo |= !hadFilament && FindFilamentUsed(readResult.Line, ref partialFileInfo);
                            gotNewInfo |= string.IsNullOrEmpty(partialFileInfo.GeneratedBy) && FindGeneratedBy(readResult.Line, ref partialFileInfo);
                            gotNewInfo |= partialFileInfo.PrintTime == 0 && FindPrintTime(readResult.Line, ref partialFileInfo);
                            gotNewInfo |= partialFileInfo.SimulatedTime == 0 && FindSimulatedTime(readResult.Line, ref partialFileInfo);
                        }
                        code.Reset();
                    }
                }

                // Is the file info complete?
                if (!gotNewInfo && !lastLineHadInfo && IsFileInfoComplete(partialFileInfo))
                {
                    break;
                }
                lastLineHadInfo = gotNewInfo;
            }
            while (reader.BaseStream.Length - reader.BaseStream.Position < Settings.FileInfoReadLimitFooter + buffer.Length);
        }

        /// <summary>
        /// Result for wrapping the buffer pointer because ref parameters are not supported for async functions
        /// </summary>
        private class ReadLineFromEndResult
        {
            /// <summary>
            /// Read line
            /// </summary>
            public string Line;

            /// <summary>
            /// New pointer in the buffer
            /// </summary>
            public int BufferPointer;
        }

        /// <summary>
        /// Read another line from the end of a file
        /// </summary>
        /// <param name="reader">Stream reader</param>
        /// <param name="buffer">Internal buffer</param>
        /// <param name="bufferPointer">Pointer to the next byte in the buffer</param>
        /// <returns>Read result</returns>
        private static async Task<ReadLineFromEndResult> ReadLineFromEndAsync(StreamReader reader, char[] buffer, int bufferPointer)
        {
            string line = string.Empty;
            do
            {
                if (bufferPointer == 0)
                {
                    if (reader.BaseStream.Position == 0)
                    {
                        return null;
                    }

                    reader.DiscardBufferedData();
                    if (reader.BaseStream.Position < buffer.Length)
                    {
                        int prevPosition = (int)reader.BaseStream.Position;
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        await reader.ReadBlockAsync(buffer);
                        bufferPointer = prevPosition;
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    }
                    else
                    {
                        long position = reader.BaseStream.Position - buffer.Length;
                        reader.BaseStream.Seek(position, SeekOrigin.Begin);
                        bufferPointer = await reader.ReadBlockAsync(buffer);
                        reader.BaseStream.Seek(position, SeekOrigin.Begin);
                    }
                }

                char c = buffer[--bufferPointer];
                if (c == '\n')
                {
                    return new ReadLineFromEndResult
                    {
                        Line = line,
                        BufferPointer = bufferPointer
                    };
                }
                if (c != '\r')
                {
                    line = c + line;
                }
            }
            while (true);
        }

        /// <summary>
        /// Checks if the given file info is complete
        /// </summary>
        /// <param name="result">File information</param>
        /// <returns>Whether the file info is complete</returns>
        private static bool IsFileInfoComplete(ParsedFileInfo result)
        {
            return (result.Height != 0) &&
                    (result.FirstLayerHeight != 0) &&
                    (result.LayerHeight != 0) &&
                    (result.Filament.Count > 0) &&
                    (!string.IsNullOrEmpty(result.GeneratedBy));
        }

        /// <summary>
        /// Try to find the layer height
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="fileInfo">File information</param>
        /// <returns>Whether layer height could be found</returns>
        private static bool FindLayerHeight(string line, ref ParsedFileInfo fileInfo)
        {
            foreach (Regex item in Settings.LayerHeightFilters)
            {
                Match match = item.Match(line);
                if (match.Success)
                {
                    foreach (Group grp in match.Groups)
                    {
                        if (grp.Name == "mm")
                        {
                            fileInfo.LayerHeight = float.Parse(grp.Value);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Try to find the filament usage
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="fileInfo">File information</param>
        /// <returns>Whether filament consumption could be found</returns>
        private static bool FindFilamentUsed(string line, ref ParsedFileInfo fileInfo)
        {
            bool hadMatch = false;
            foreach (Regex item in Settings.FilamentFilters)
            {
                Match match = item.Match(line);
                if (match.Success)
                {
                    foreach (Group grp in match.Groups)
                    {
                        if (grp.Name == "mm")
                        {
                            foreach (Capture c in grp.Captures)
                            {
                                fileInfo.Filament.Add(float.Parse(c.Value));
                            }
                            hadMatch = true;
                        }
                        else if (grp.Name == "m")
                        {
                            foreach (Capture c in grp.Captures)
                            {
                                fileInfo.Filament.Add(float.Parse(c.Value) * 1000F);
                            }
                            hadMatch = true;
                        }
                    }
                }
            }
            return hadMatch;
        }

        /// <summary>
        /// Find the toolchain that generated the file
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="fileInfo">File information</param>
        /// <returns>Whether the slicer could be found</returns>
        private static bool FindGeneratedBy(string line, ref ParsedFileInfo fileInfo)
        {
            foreach (Regex item in Settings.GeneratedByFilters)
            {
                Match match = item.Match(line);
                if (match.Success && match.Groups.Count > 1)
                {
                    fileInfo.GeneratedBy = match.Groups[1].Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Find the total print time
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="fileInfo">File information</param>
        /// <returns>Whether the print time could be found</returns>
        private static bool FindPrintTime(string line, ref ParsedFileInfo fileInfo)
        {
            foreach (Regex item in Settings.PrintTimeFilters)
            {
                Match match = item.Match(line);
                if (match.Success)
                {
                    long seconds = 0;
                    foreach (Group grp in match.Groups)
                    {
                        if (!string.IsNullOrEmpty(grp.Value))
                        {
                            switch (grp.Name)
                            {
                                case "h":
                                    seconds += long.Parse(grp.Value) * 3600;
                                    break;
                                case "m":
                                    seconds += long.Parse(grp.Value) * 60;
                                    break;
                                case "s":
                                    seconds += long.Parse(grp.Value);
                                    break;
                            }
                        }
                    }
                    fileInfo.PrintTime = seconds;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Find the simulated time
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="fileInfo">File information</param>
        /// <returns>Whether the simulated time could be found</returns>
        private static bool FindSimulatedTime(string line, ref ParsedFileInfo fileInfo)
        {
            foreach (Regex item in Settings.SimulatedTimeFilters)
            {
                Match match = item.Match(line);
                if (match.Success)
                {
                    long seconds = 0;
                    foreach (Group grp in match.Groups)
                    {
                        if (!string.IsNullOrEmpty(grp.Value))
                        {
                            switch (grp.Name)
                            {
                                case "h":
                                    seconds += long.Parse(grp.Value) * 3600;
                                    break;
                                case "m":
                                    seconds += long.Parse(grp.Value) * 60;
                                    break;
                                case "s":
                                    seconds += long.Parse(grp.Value);
                                    break;
                            }
                        }
                    }
                    fileInfo.SimulatedTime = seconds;
                    return true;
                }
            }
            return false;
        }
    }
}
