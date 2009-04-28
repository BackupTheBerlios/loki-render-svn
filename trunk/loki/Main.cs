// Project: Loki Render - A distributed job queue manager.
// Version: 0.5
//
// File Description: queries user about role, then starts up appropriate
// parts of Loki. 
// TODO - Later I'd like to add command line support for the grunt
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
using Gtk;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;	//for debug

namespace loki
{
	class MainClass
	{
		static MasterWin masterWin; //main window for the 'master' role
		static RoleDialog roleDialog; //query at start of loki to determine the role loki will fill
		static GruntWin gWin; //window for grunt mode
		static int role;
		static int broadcastPort, connectPort, bufferSize, broadcastInterval, clientShutdownWait;
		static Queue q;
		static RemoteClient rC;
		
		public static void Main (string[] args)
		{	
			//setup debug
			Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
			Debug.AutoFlush = true;
			Debug.Indent();
			
			//TODO - take from command line or supply these defaults
			broadcastPort = 26278;
			connectPort = 26279;
			bufferSize = 1024;
			broadcastInterval = 1;
			clientShutdownWait = 1;
			
			normalLokiStart();//call will not return from here till GTK quits!
		}
		
		static void normalLokiStart()
		{
			Application.Init ();
			roleDialog = new RoleDialog();
			roleDialog.Run();
			role = roleDialog.role;
			roleDialog.Destroy();
			
			if(role == 0) //launch a grunt
			{	
				gWin = new GruntWin(true);
				rC = new RemoteClient(broadcastPort, connectPort, bufferSize, false, gWin, true);
				gWin.setRCHandle(rC);
			}
			else if(role == 1) //launch the master
			{
				masterWin = new MasterWin (role);
				q = new Queue(connectPort, broadcastInterval, clientShutdownWait, masterWin);
				masterWin.setQHandle(q);
				
				//test
				Thread tThread = new Thread(test);
				tThread.Start();
			}
			else if(role == 2)//Launch the master...and a grunt, mua ha ha!
			{	
				//grunt part
				gWin = new GruntWin(false);
				rC = new RemoteClient(broadcastPort, connectPort, bufferSize, false, gWin, false);
				gWin.setRCHandle(rC);
				
				//master part
				masterWin = new MasterWin (role);
				q = new Queue(connectPort, broadcastInterval, clientShutdownWait, masterWin, rC);
				masterWin.setQHandle(q);
				
				//TEST
				//Thread tThread = new Thread(test);
				//tThread.Start();
			}
			else if(role < 0 || role > 2)
			{
				throw new SanityFailureException("received an unknown role from RoleDialog!");
			}
			
			Application.Run ();	//tally-ho!
			
		}//end normalLokiStart()
		
		//test quick way to inject jobs into the queue for testing purposes
		static void test()
		{
			int howManyJobs = 0;
			int howManyFrames = 1000;
			int howManyClients = 0;
			int allowedFailures = 3;
			string noticeType = "add";
			string taskType = "blender";
			string winExe = "blender";
			string winFile = @"X:\c.blend";
			string winOutput = @"X:\output\";
			string unixExe = "blender";
			string unixFile = "/mnt/loki/c.blend";
			string unixOutput = "/mnt/loki/output/";

			for(int j = 0; j<howManyJobs; j++)
			{
				q.deliverNotice(new Notice(noticeType, j.ToString(), taskType, winExe, winFile, winOutput,
				                           unixExe, unixFile, unixOutput, 1, howManyFrames,
				                           allowedFailures));
			}
			
			for(int c = 0; c<howManyClients; c++)
			{
				rC = new RemoteClient(broadcastPort, connectPort, bufferSize, true, gWin, false);
		    }	
		}
	}
}//end namespace loki