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
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;

namespace dvrchive
{
    class AppConfig
    {
        public static string path;
        public static double maxHours = 0; //Oldest a show can be in hours and still be archived. If 0, archive all
        public static string recordingExtension = "ts"; //Extension of files for archiving. (for .ts files, use ts)
        public static bool debug = true; //Should the program log debug messages
        public static string archivePath = ""; //Where are they episodes archived to (your Plex/Kodi/etc. TV folder)
        public static bool isWindows = false; //Is the OS Windows
        public static bool deleteEDLFiles = true; //Delete generated EDL files after the archiving
        public static string pathToComskipNonWindows = ""; //For non-Windows systems, location of comskip.exe
        public static string tempPath = Path.GetTempPath(); //Can be overrident from config.json

        public static IConfigurationRoot Config { get; private set; }

        public static void Load()
        {
            CheckOS();
            path = AppContext.BaseDirectory + GetSlash() + "config.json";

            Config = new ConfigurationBuilder()
                .AddJsonFile(path)
                .Build();

            try
            {
                debug = bool.Parse(Config["debug"]);
            }
            catch
            {
                Console.WriteLine("config.json does not specify debugging, defaulting to true.");
            }

            try
            {
                maxHours = double.Parse(Config["maxHours"]);
            }
            catch
            {
                if (debug)
                {
                    Console.WriteLine("config.json does not specify maxhours, defaulting to 0: archive all.");
                }
            }

            try
            {
                recordingExtension = Config["recordingExtension"];
            }
            catch
            {
                if (debug)
                {
                    Console.WriteLine("config.json does not specify recording extension, defaulting to ts");
                }
            }

            try
            {
                archivePath = Config["archivePath"];
            }
            catch
            {
                archivePath = Directory.GetCurrentDirectory();
                if (debug)
                {
                    Console.WriteLine("config.json does not specify archivePath, defaulting to current directory: {0}", Directory.GetCurrentDirectory());
                }
            }

            try
            {
                pathToComskipNonWindows = Config["pathToComskipNonWindows"];
            }
            catch
            {
                pathToComskipNonWindows = Directory.GetCurrentDirectory();
                if (debug)
                {
                    Console.WriteLine("config.json does not specify comskip.exe folder, defaulting to current directory: {0}", Directory.GetCurrentDirectory());
                }
            }

            try
            {
                string configTemp = Config["tempPath"];
                if (configTemp != "")
                {
                    tempPath = configTemp;
                }
            }
            catch
            {
                if (debug)
                {
                    Console.WriteLine("config.json does not specify temp path, defaulting to current system default: {0}", tempPath);
                }
            }
        }

        public static bool CheckExists()
        {            
            return File.Exists(path);
        }

        private static void CheckOS()
        {
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (debug)
            {
                Console.WriteLine("dvrchive: Running on Windows: {0}", isWindows);
            }
        }

        //Because comskip will not be in the PATH for non-Windows systems, make sure it exists as specified.
        public static bool CheckWineComskipExists()
        {
            if (isWindows)
            {
                return true;
            }
            else
            {
                if (debug)
                {
                    Console.WriteLine("dvrchive: Checking if {0} exists", pathToComskipNonWindows + "/comskip.exe");
                }

                return File.Exists(pathToComskipNonWindows + "/comskip.exe");
            }           
        }

        //One place to grab the correct slash from
        public static char GetSlash()
        {
            if (isWindows)
            {
                return '\\';
            }
            else
            {
                return '/';
            }
        }

        public static bool RunSanityChecks()
        {
            if (!CheckExists())
            {
                Console.WriteLine("dvrchive: ERROR: config.json not found at: {0}", path);
                return false;
            }

            if (!DVR.CheckExists())
            {
                Console.WriteLine("dvrchive: ERROR: DVR location does not exist: {0}", DVR.path);
                return false;
            }

            if (!Directory.Exists(archivePath))
            {
                Console.WriteLine("dvrchive: ERROR: archive location does not exist: {0}", archivePath);
                return false;
            }

            if (!CheckWineComskipExists())
            {
                Console.WriteLine("dvrchive: ERROR: using non-Windows OS and comskip.exe not specified correctly in config.json");
                return false;
            }

            return true;
        }
    }
}
