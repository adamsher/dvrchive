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
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace dvrchive
{
    class Episode
    {
        public Episode(Show show, string path)
        {
            this.path = path;
            this.show = show;
        }

        public string path;
        private string edlPath = "";
        private bool edlLoaded = false;
        private bool edlExists = false;
        private List<string> edlLines = new List<string>();
        private List<double[]> encodeTimes = new List<double[]>();
        private List<ProcessStartInfo> script = new List<ProcessStartInfo>();
        public Show show;
        public double season = 0;
        public string archivePath = "";
        public Guid guid = Guid.NewGuid();


        public void ProcessEpisode()
        {
            if (AppConfig.maxHours == 0)
            {
                Archive();
            }
            else if ((DateTime.Now - File.GetCreationTime(path)).TotalHours < AppConfig.maxHours)
            {
                Archive();
            }
            else
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: Episode {0} more than {1} hours old, skipping.", path, AppConfig.maxHours);
                }                
            }
        }
                
        private void Archive()
        {
            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Archiving {0}", path);
            }

            DeduceSeasonNumber();

            CreateTempLocation();            

            CreateArchivePath();

            CreateEDL();

            edlExists = CheckEDLExists();

            if (edlExists)
            {
                edlLoaded = LoadEDLFile();
            }
            else if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Unable to archive {0}: EDL Missing", path);
            }

            if (edlLoaded)
            {               
                Encode(edlLines);                
            }
            else if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Unable to archive {0}: EDL coudld be read correctly", path);
            }
            
            RemoveTempLocation();
            RemoveEDLFiles();

        }

        private void CreateEDL()
        {
            if (AppConfig.isWindows)
            {

                ProcessStartInfo psi = CommandBuilder.GenerateEDLCommand(show, this);

                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: EDL Creation Script:");
                    Console.WriteLine(psi.FileName + " " + psi.Arguments);
                }
                                
                Process p = Process.Start(psi);
                p.WaitForExit();
            }
            else
            {
                //Remember where this program was run from
                string startDirectory = Directory.GetCurrentDirectory();

                //Go into the show Directory
                Directory.SetCurrentDirectory(show.path);
                Console.WriteLine("dvrchive: Switced to {0}", Directory.GetCurrentDirectory());

                ProcessStartInfo psi = CommandBuilder.GenerateEDLCommand(show, this);

                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: EDL Creation Script:");
                    Console.WriteLine(psi.FileName + " " + psi.Arguments);
                }

                Process p = Process.Start(psi);
                p.WaitForExit();

                //Go back to starting Directory
                Directory.SetCurrentDirectory(startDirectory);
            }
        }

        private bool CheckEDLExists()
        {
            string[] pathArray = path.Split('.');


            //Take all strings except the last (which is the extension)
            for (int i = 0; i < pathArray.Length - 1; i++)
            {
                edlPath += pathArray[i] + ".";
            }

            edlPath += "edl";

            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: EDL file for {0} is {1}", path, edlPath);
            }

            if (File.Exists(edlPath))
            {
                return true;
            }
            else if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: EDL file {0}  does not exist, skipping", path, edlPath);
            }

            return false;
        }

        //EDL lines tell us what parts of the episode to skip
        private bool LoadEDLFile()
        {
                       
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = File.OpenText(edlPath))
                {
                    while (sr.Peek() >= 0)
                    {
                        edlLines.Add(sr.ReadLine());
                    }
                }
            }
            catch (Exception e)
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: ERROR: Unable to open EDL file {0}", edlPath);
                    Console.WriteLine(e.Message);
                }
                
                return false;
            }           

            if (edlLines.Count < 1)
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: ERROR: EDL File {0} contained no lines to process", edlPath);
                }
                return false;
            }

            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: LoadEDLFile Completed Successfully");
                Console.WriteLine("dvrchive: EDL lines are:");
                foreach (string s in edlLines)
                {
                    Console.WriteLine(s);
                }
            }

            return true;

        }

        /*
            Take the times we're told to skip, conver it into times we want to keep
            EDL Exmaple:
            0.00	102.20	0
            975.17	1217.88	0
            1711.31	1954.05	0
            2332.53	2575.31	0
            2981.78	3391.52	0
            3665.36	3868.33	0
        */
        private void Encode(List<string> edlLines)
        {
            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Generating archive commands for {0}", path);
            }

            List<double> starts = new List<double>();
            List<double> ends = new List<double>();

            foreach (string s in edlLines)
            {
                string[] entry = s.Split('\t');

                starts.Add(double.Parse(entry[0]));
                ends.Add(double.Parse(entry[1]));               
            }

            //See if we're not starting with a commercial, get the beginning to the 1st commercial
            if (starts[0] != 0)
            {
                encodeTimes.Add(new double[] { 0, starts[0] });
            }

            for (int i = 0; i < ends.Count; i++)
            {
                
                //If it's the last one, go from last end time to end of episode
                if (i == ends.Count - 1)
                {

                }
                else
                {
                    encodeTimes.Add(new double[] { ends[i], starts[i + 1] });
                }
            }

            int segmentCount = 1;
            foreach (double[] span in encodeTimes)
            {
                Segment segment = new Segment()
                {
                    start = span[0],
                    end = span[1]
                };
                script.Add(CommandBuilder.GenerateEncodingCommand(show, this, segment, segmentCount));


                segmentCount++;
            }



            //Create an entry in case there's OK data at the end: -ss (last end time) with NO -t so goes until end
            Segment lastSegment = new Segment()
            {
                start = ends[ends.Count - 1]
            };
            script.Add(CommandBuilder.GenerateLastCommand(show, this, lastSegment, segmentCount));

            //Add mkvmerge to script
            script.Add(CommandBuilder.GenerateMkvmergeCommand(show, this, segmentCount));

            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Archive Script:");
                foreach (ProcessStartInfo psi in script)
                {
                    if (AppConfig.debug)
                    {
                        Console.WriteLine(psi.FileName + " " + psi.Arguments);
                    }
                }
            }

            foreach (ProcessStartInfo psi in script)
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: running: " + psi.FileName + " " + psi.Arguments);
                }

                try
                {
                    Process p = Process.Start(psi);
                    //p.WaitForExit();
                    string s = p.StandardOutput.ReadToEnd();
                    if (AppConfig.debug)
                    {
                        Console.WriteLine("dvrchive: command output: {0}", s);
                    }                    
                }
                catch(Exception e)
                {
                    Console.WriteLine("dvrchive: ERROR: Unable to run {0}", psi.FileName + " " + psi.Arguments);
                    Console.WriteLine("dvrchive: ERROR: With Exception {0}", e);
                }

                
            }
        }

        //Returns *just* the file name, not the full path
        public string GetFileName()
        {
            return "\"" + path.Split('/').Last() + "\"";
        }

        //Returns *just* the episode name and number
        public string GetEpisodeNameAndNumber()
        {
            //Get everything after the final / or \
            string nameNoPath = path.Split(AppConfig.GetSlash()).Last();

            //Split by period
            string[] nameArray = nameNoPath.Split('.');

            string episodeName = "";

            //Get all parts of the name except the extension and SXXEXX
            for (int i=0; i < nameArray.Length - 2; i++)
            {
                episodeName += nameArray[i] + ".";
            }

            //Adds SXXEXX without a trailing period
            episodeName += nameArray[nameArray.Length - 2];

            return episodeName;
        }

        private void DeduceSeasonNumber()
        {
            //Remove slashes
            string[] nameArray = path.Split(AppConfig.GetSlash());
            string slashlessName = nameArray.Last();

            //Assuming format: Name of Show With.Title.With.Annoying.Periods.S02E125.ts
            string[] slashlessArray = slashlessName.Split('.');
            string seasonEpisode = slashlessArray[slashlessArray.Length - 2];
            //Split on the E
            string justSeason = seasonEpisode.Split('E')[0];
            //Remove the preceeding S
            season = Double.Parse(justSeason.Substring(1));

            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Episode at {0} believed to be in Season {1}", path, season);
            }
        }

        public void CreateArchivePath()
        {
            archivePath = CommandBuilder.GetArchivePath(this);

            if (Directory.Exists(archivePath))
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: Archive path exists {0}", archivePath);
                }
            }
            else
            {
                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: Creating archive path {0}", archivePath);
                }

                Directory.CreateDirectory(archivePath);
            }
        }

        private void CreateTempLocation()
        {
            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Creating temp location at: {0}", GetTempLocation());
            }

            Directory.CreateDirectory(GetTempLocation());
        }
        
        private void RemoveTempLocation()
        {
            try
            {
                Directory.Delete(GetTempLocation(), true);

                if (AppConfig.debug)
                {
                    Console.WriteLine("dvrchive: Deleting temp location at: {0}", GetTempLocation());
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("dvrchive: Unable to remove temp path {0}", GetTempLocation());
                Console.WriteLine("dvrchive: With exception: {0}", e);
            }
            
        }

        public string GetTempLocation()
        {
            return AppConfig.tempPath + AppConfig.GetSlash() + guid + AppConfig.GetSlash();
        }   

        public void RemoveEDLFiles()
        {
            string episodeName = GetEpisodeNameAndNumber();

            List<string> filesToDelete = new List<string>
            {
                episodeName + ".edl",
                episodeName + ".log",
                episodeName + ".logo.txt",
                episodeName + ".txt"
            };
            if (AppConfig.debug)
            {
                Console.WriteLine("dvrchive: Cleaning up archive files:");
                foreach (string s in filesToDelete)
                {
                    Console.WriteLine("dvrchive: {0}", show.path + AppConfig.GetSlash() + s);
                }
            }

            foreach (string s in filesToDelete)
            {
                if (File.Exists(show.path + AppConfig.GetSlash() + s))
                {
                    try
                    {
                        File.Delete(show.path + AppConfig.GetSlash() + s);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("dvrchive: ERROR: Unable to delete {0}", show.path + AppConfig.GetSlash() + s);
                        Console.WriteLine("dvrchive: ERROR: With exception {0}", e);
                    }
                }
            }
                        
        }
    }
}
