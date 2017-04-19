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

//TODO: Not working for programs with periods in name, ie: Agents of S.H.I.E.L.D.

namespace dvrchive
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                DVR.path = args[0];

                AppConfig.Load();

                if (
                    !AppConfig.RunSanityChecks()
                    )
                {
                    ShowUsage();
                }
                else
                {
                    DVR.Archive();
                }
            }
            else
            {
                ShowUsage();
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("dvrchive help");
            Console.WriteLine("");
            Console.WriteLine("dvrchive currently takes exactly 1 argument: the path where your DVR files live");
            Console.WriteLine("");
            Console.WriteLine("Example Usage Linux/macOS/POSIX:");
            Console.WriteLine("dotnet dvrchive.dll /mnt/dvr/");
            Console.WriteLine("");
            Console.WriteLine("Example Usage Windows:");
            Console.WriteLine("dotnet dvrchive.dll d:\\dvr\\");
            Console.WriteLine("");
            Console.WriteLine("dvrchive expects that each show is stored in its own folder (dvrchive doesn't support season folders on the DVR, but does in the archive location).");
            Console.WriteLine("Each folder that contains a dvrchive.json file is considered a show and the options for that show will be read from the file.");
            Console.WriteLine("Any show folder without a dvarchive.json file will be ignored.");
            Console.WriteLine("");
            Console.WriteLine("Episodes should be in the format:");
            Console.WriteLine("ShowName.S01E01.ts");
            Console.WriteLine("");
            Console.WriteLine("3rd Party Requirements:");
            Console.WriteLine("The following programs must be installed on your system and in your PATH");
            Console.WriteLine("");
            Console.WriteLine("Linux/macOS:");
            Console.WriteLine("ffmpeg");
            Console.WriteLine("comskip http://www.kaashoek.com/comskip/");
            Console.WriteLine("mkvtoolnix");
            Console.WriteLine("wine (to run comskip)");
            Console.WriteLine("");
            Console.WriteLine("Windows:");
            Console.WriteLine("ffmpeg https://ffmpeg.zeranoe.com/builds/");
            Console.WriteLine("comskip http://www.kaashoek.com/comskip/");
            Console.WriteLine("mkvtoolnix https://mkvtoolnix.download/");        
        }
    }
}