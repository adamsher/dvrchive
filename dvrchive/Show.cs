﻿/*
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
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace dvrchive
{
    class Show
    {
        public Show(string path)
        {
            this.path = path;
        }

        public string path;
        private string resolution; //Valid options: 1080p, 720p, 480p
        public double width = 1280; //Archive encoding width
        private bool deinterlace = true; //Should the recordings be de-interlaced when archived
        private bool archive = false; //Should this show be archived
        private bool configLoaded = false;
        public string showName = "";

        public static IConfigurationRoot config { get; private set; }

        public void Process()
        {
            LoadConfig();

            if (configLoaded && archive)
            {
                DeduceShowName();

                Console.WriteLine("dvrchive: Checking for episodes in {0}", path);

                foreach (string episodePath in Directory.GetFiles(path, "*." + AppConfig.recordingExtension))
                {
                    Episode episode = new Episode(this, episodePath);
                    episode.ProcessEpisode();
                }
            }            
        }

        /*
         * Load a config file for the show
         * If it doesn't exist, we'll ignore this show
         * 
         */
        private void LoadConfig()
        {
            try
            {
                config = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("dvrchive.json")
                .Build();
            }
            catch
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: Cannot process dvrchive.json in {0}, skipping show.", path);
                }
                return;
            }


            try
            {
                archive = bool.Parse(config["archive"]);
            }
            catch
            {
                Console.WriteLine("dvrchive: dvrchive.json does not specify archive, defaulting to false, skipping show.");
            }


            try
            {
                resolution = config["resolution"];
                if (resolution == "1080p")
                {
                    width = 1920;
                }
                else if (resolution == "480p")
                {
                    width = 852;
                }
                else
                {
                    width = 1280;
                }
            }
            catch
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: dvrchive.json does not specify resolution, defaulting to 720p");
                }
            }

            try
            {
                deinterlace = bool.Parse(config["deinterlace"]);
            }
            catch
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: dvrchive.json does not specify deinterlace, defaulting to true");
                }
            }

            configLoaded = true;
        }

        private void DeduceShowName()
        {
            //Remove slashes
            string[] nameArray = path.Split(AppConfig.GetSlash());
            string slashlessName = nameArray.Last();

            //Assuming format: Name of Show.S02E125.ts
            showName = slashlessName.Split('.').First();

            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Show in {0} believed to be called {1}", path, showName);
            }
        }

        public string GetDeinterlaceString()
        {
            if (deinterlace)
            {
                return "yadif=1,";
            }
            else
            {
                return "";
            }
        }

        public string GetNonWindowsPath()
        {
            return path.Replace(" ", "\\ ");
        }
    }
}