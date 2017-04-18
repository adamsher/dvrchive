/*
Copyright 2017 Adam Sher adam@shernet.com
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace dvrchive
{
    class CommandBuilder
    {

        public static ProcessStartInfo GenerateEncodingCommand(Show show, Episode episode, Segment segment, int segmentCount)
        {
            string arguments = "-ss " + segment.start.ToString() + " -i \"" + episode.path + "\" -to " + segment.end.ToString() + " -vcodec libx264 -preset fast -crf 23 -vf " + show.GetDeinterlaceString() + "scale=" + show.width + ":-1 -acodec ac3 -ac 6 -ab 384k -copyts -start_at_zero \"" + episode.GetTempLocation() + segmentCount + ".mkv\"";

            //Console.WriteLine(arguments);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            return psi;
        }

        public static ProcessStartInfo GenerateLastCommand(Show show, Episode episode, Segment segment, int segmentCount)
        {
            if (AppConfig.isWindows)
            {
                return GenerateLastCommandWindows(show, episode, segment, segmentCount);
            }
            else
            {
                return GenerateLastCommandNonWindows(show, episode, segment, segmentCount);
            }
        }

        private static ProcessStartInfo GenerateLastCommandWindows(Show show, Episode episode, Segment segment, int segmentCount)
        {
            string arguments = "-ss " + segment.start.ToString() + " -i \"" + episode.path + "\" -vcodec libx264 -preset fast -crf 23 -vf " + show.GetDeinterlaceString() + "scale=" + show.width + ":-1 -acodec ac3 -ac 6 -ab 384k -copyts -start_at_zero " + ApplyNonWindowsSpaces(episode.GetTempLocation() + segmentCount + ".mkv");

            //Console.WriteLine(arguments);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            return psi;
        }

        private static ProcessStartInfo GenerateLastCommandNonWindows(Show show, Episode episode, Segment segment, int segmentCount)
        {
            string arguments = "-ss " + segment.start.ToString() + " -i " + ApplyNonWindowsSpaces(episode.path) + " -vcodec libx264 -preset fast -crf 23 -vf " + show.GetDeinterlaceString() + "scale=" + show.width + ":-1 -acodec ac3 -ac 6 -ab 384k -copyts -start_at_zero \"" + episode.GetTempLocation() + segmentCount + ".mkv\"";

            //Console.WriteLine(arguments);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            return psi;
        }

        public static ProcessStartInfo GenerateMkvmergeCommand(Show show, Episode episode, int segmentCount)
        {
            if (AppConfig.isWindows)
            {
                return GenerateMkvmergeCommandWindows(show, episode, segmentCount);
            }
            else
            {
                return GenerateMkvmergeCommandNonWindows(show, episode, segmentCount);
            }
        }

        private static ProcessStartInfo GenerateMkvmergeCommandWindows(Show show, Episode episode, int segmentCount)
        {
            string episodeName = episode.GetEpisodeNameAndNumber();
            episodeName += ".mkv";

            if (AppConfig.debug)
            {
                Console.WriteLine("Episode will be archived as {0}", episodeName);
            }

            string arguments = "";

            arguments = "-o \"" + episode.archivePath + AppConfig.GetSlash() + episode.GetEpisodeNameAndNumber() + ".mkv\" ";

            for (int i = 1; i < segmentCount; i++)
            {
                arguments += "\"" + episode.GetTempLocation() + i.ToString() + ".mkv\" + ";
            }

            arguments += "\"" + episode.GetTempLocation() + segmentCount.ToString() + ".mkv\"";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "mkvmerge",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            return psi;
        }

        private static ProcessStartInfo GenerateMkvmergeCommandNonWindows(Show show, Episode episode, int segmentCount)
        {
            string episodeName = episode.GetEpisodeNameAndNumber();
            episodeName += ".mkv";

            if (AppConfig.debug)
            {
                Console.WriteLine("Episode will be archived as {0}", episodeName);
            }

            string arguments = "";

            arguments = "-o " + ApplyNonWindowsSpaces(episode.archivePath + AppConfig.GetSlash() + episode.GetEpisodeNameAndNumber() + ".mkv") + " ";

            for (int i = 1; i < segmentCount; i++)
            {
                arguments += ApplyNonWindowsSpaces(episode.GetTempLocation() + i.ToString() + ".mkv") + " ";
            }

            arguments += ApplyNonWindowsSpaces(episode.GetTempLocation() + segmentCount.ToString() + ".mkv");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "mkvmerge",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            return psi;
        }

        public static ProcessStartInfo GenerateEDLCommand(Show show, Episode episode)
        {
            if (AppConfig.isWindows)
            {
                return GenerateEDLCommandWindows(show, episode);
            }
            else
            {
                return GenerateEDLCommandNonWindows(show, episode);
            }
        }

        private static ProcessStartInfo GenerateEDLCommandWindows(Show show, Episode episode)
        {
            string arguments = "\"" + episode.path + "\" --output=\"" + show.path + "\"";            

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "comskip",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            return psi;
        }

        private static ProcessStartInfo GenerateEDLCommandNonWindows(Show show, Episode episode)
        {
            string arguments = AppConfig.pathToComskipNonWindows + "/comskip.exe " + episode.GetFileName();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "wine",
                Arguments = arguments,
                RedirectStandardOutput = false,
                UseShellExecute = false
            };

            return psi;
        }

        public static string GetArchivePath(Episode episode)
        {
            if (AppConfig.isWindows)
            {
                return AppConfig.archivePath + AppConfig.GetSlash() + episode.show.showName + AppConfig.GetSlash() + "Season " + episode.season.ToString();
            }
            else
            {
                return AppConfig.archivePath + AppConfig.GetSlash() + episode.show.showName + AppConfig.GetSlash() + "Season " + episode.season.ToString();
            }
        }

        public static string ApplyNonWindowsSpaces(string s)
        {
            return s.Replace(" ", "\\ ");
        }
    }
}
