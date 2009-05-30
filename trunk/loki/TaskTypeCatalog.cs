// Project: Loki Render - A distributed job queue manager.
// Version: 0.5.1
// 
// File Description: Contains different task types and how to handle them	  
// 
// Copyright (C) 2009 Daniel Petersen
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Diagnostics;

namespace loki
{
	class TaskTypeCatalog
	{
		public TaskTypeCatalog()
		{
		}
		
		//items in array are: 0.task, 1.taskType, 2.winExePath, 3.winFilePath, 4.winOutputPath,
        //5.unixExePath, 6.unixFilePath, 7.unixOutputPath, 8.frame
		
		//returns 'ok' if no problems found, otherwise returns the problem path with appropriate prefix:
		// 'exe', 'file', or 'output' with a delimiter '*' between the prefix, and path
		//for example, could return "exe*blender" if it couldn't find blender
		public static string checkPaths(string platform, string[] t)
		{
            string result;
			
			switch (t[1])
			{
			case "blender":
				result = blender_checkPaths(platform, t);
				break;
			default:
				throw new SanityFailureException("TypeCatalog received unknown task type: " + t[1]);
			}
			return result; 
		}
		
		public static string checkStdout(string type, string stdout)
		{
			string result;
			switch(type)
			{
			case "blender":
				result = blender_checkStdout(stdout);
				break;
			default:
				throw new SanityFailureException("TypeCatalog received unknown task type: " + type);
			}
			return result;
		}
		
		public static string generateTaskArgs(string platform, string[] t)
		{
			string args = "";
			
			switch (t[1])
			{
			case "blender":
				args = blender_generateArgs(platform, t);
				break;
			default:
				throw new SanityFailureException("TypeCatalog received unknown task type: " + t[1]);
			}
			
			return args;
		}
		
		static string blender_generateArgs(string platform, string[] t)
		{
			string args = null;
			if(platform == "windows")
			{
				args = "-b " + t[3] + " -o " + t[4] + " -f " + t[8];
				
			}
			else if (platform == "unix")
			{
				args = "-b " + t[6] + " -o " + t[7] + " -f " + t[8];
			}
			else
			{
				throw new SanityFailureException("TypeCatalog received unknown platform: " + platform);
			}
			return args;
		}
		
		static string blender_checkPaths(string platform, string[] t)
		{
            ProcessStartInfo startInfo;
			Process taskProcess = null;
				
			//setup process stuff
			if(platform == "windows")
			{
				string winExe = t[2];
				startInfo = new ProcessStartInfo(winExe);
            }
			else if(platform == "unix")
            {
				string unixExe = t[5];
				startInfo = new ProcessStartInfo(unixExe);
			}
			else
            {
				throw new SanityFailureException("Task Catalog received an unknown platform: " + platform);
            }
					
			startInfo.Arguments = "--help";
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

			try //attempt to launch the task
			{
				taskProcess = Process.Start(startInfo);
                taskProcess.WaitForExit(5000);
					
			}
			catch(Exception ex)
			{
				//eh, who cares.
			}
			
			//check for a substring we know is exclusively in the 'blender --help' output
			if(taskProcess != null)
			{
			    if(!taskProcess.StandardOutput.ReadToEnd().Contains("ender options"))
			    {   //failed!
					if(platform == "windows")
					{
						return "exe*" + t[2];
					}
					else if(platform == "unix")
		            {
						return "exe*" + t[5];
					}
				}
			}
			else    //failed!
			{
			    if(platform == "windows")
					{
						return "exe*" + t[2];
					}
					else if(platform == "unix")
		            {
						return "exe*" + t[5];
					}	
			}
			
			//check if blender file, and output path exists
			if(platform == "windows")
			{
				if(!File.Exists(t[3]))//winFilePath
				{
					return "file*" + t[3];
				}
				else if(!Directory.Exists(t[4]))//winOutputPath
				{
					return "output*" + t[4];
				}
			}
			else if (platform == "unix")
			{
				if(!File.Exists(t[6]))//unixFilePath
				{
					return "file*" + t[6];
				}
				else if(!Directory.Exists(t[7]))//unixOutputPath
				{
					return "output*" + t[7];
				}
			}
			else
			{
				throw new SanityFailureException("TypeCatalog received unknown platform: " + platform);
			}
					
			return "ok"; //all our checks passed; return "ok".
		}
		
		static string blender_checkStdout(string stdout)
		{
			if(stdout.Contains("Saved:"))
			{
				return "ok";	
			}
			else if(stdout.Contains("Render error:"))
			{
				Console.WriteLine(stdout);
				int startIndex = stdout.IndexOf("Render error:");
				int endIndex = stdout.IndexOf("\n", startIndex);
				
				return stdout.Substring(startIndex, (endIndex - startIndex));
			}
			else
			{
				Console.WriteLine(stdout);
				return "Blender quit with an error.";
			}
		}
	}
}
