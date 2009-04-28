// Project: Loki Render - A distributed job queue manager.
// Version: 0.5
// 
// File Description: 'Grunt' program for the remote client	  
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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace loki
{
    public class RemoteClient
    {
        string receiveMsg, machineName, os, cores;
        CSocket sock;
        Thread rThread;
        int broadcastPort, connectPort;
        int bufferSize;
        IPAddress destAddr;
		bool shutdown;
		public bool busy;
		GruntWin gWin;
		AutoResetEvent localShutdownEvent;
		AutoResetEvent ok2ConnectMaster;
		public Object busyLock;
		bool solo;	//if true, then I'm NOT running with a master on this computer!

        public RemoteClient(int bPort, int cPort, int bSize, bool background, GruntWin g, bool solo)
        {
			this.solo = solo;	//if true, then I'm NOT running with a master on this machine!
			shutdown = false;
			busy = false;
			broadcastPort = bPort;
            connectPort = cPort;  
            bufferSize = bSize;
			gWin = g;
			localShutdownEvent = new AutoResetEvent(false);
			ok2ConnectMaster = new AutoResetEvent(false);
			busyLock = new Object();
			
            rThread = new Thread(rClientThread);
			if(background)
				rThread.IsBackground = true;
            rThread.Start();
        }
		
		public void setMasterOk()
		{
			ok2ConnectMaster.Set();
		}
		
		public bool getBusyStatus()
		{
			bool result;
			lock(busyLock)
			{
				result = busy;
			}
			return result;
		}
		
		//called by GruntWindow when user exits
		public void signalShutdown()
		{
			localShutdownEvent.Set();
		}
		
		bool check4LocalShutdown(int timeout)
		{
			bool s = false;//shutdown
			if (localShutdownEvent.WaitOne(timeout, false))
                {
				    s = true;
                }
			return s;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns>
		/// -1 if we lost connection
		/// 0 if we have no shutdown message
		/// 1 if we have a disconnect message (which means disconnect and stop task, but don't shutdown)
		/// 2 if we have a rCShutdown message (disconnect, stop task, and shutdown)
		/// A <see cref="System.Int32"/>
		/// </returns>
		int check4RemoteShutdown()
		{
			string msg;
			int result = sock.check4Message();
			int val;
			
			if(result > 0)//we have a message - it ought to be a shutdown
			{
				msg = sock.readStream();
				string[] t = sock.convert2Tokens(msg);
				if(msg == "lost")
				{	//oops, lost our connection
					val = -1;	
				}
				else if(t[0] == "disconnect")
				{
					val = 1;
				}
				else if (t[0] == "rCShutdown")
				{
					val = 2;
				}
				else
				{
					throw new SanityFailureException("rC: check4RemoteShutdown received unknown msg: " + t[0]);	
				}
			}
			else if(result < 0)
			{ //socket error, we'll assume lost connection
				val = -1;
			}
			else//result is 0, so connection is ok, but no message
				val = 0;
			
			return val;
		}

		//returns true if everything went ok, false if we got a shutdown
        public bool discoverMaster()
        {
			IPEndPoint iep;
            EndPoint ep;
			Socket uSock;
			byte[] data;
			if(solo)	//if solo, we need to discover master's IP address
			{
	            try
	            {
	                uSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
	                iep = new IPEndPoint(IPAddress.Any, broadcastPort);
	                uSock.Bind(iep);
	                ep = (EndPoint)iep;
	                data = new byte[1024];
				}
				catch (SocketException ex)
	            {
					Console.WriteLine("discoverMaster() socket start failed");
					Console.WriteLine("SocketException: {0}", ex);
	                return false;
	            }
				try
				{
					bool keepChecking = true;
					int result = 0;
					do
					{
			            try
			            {
			                result = uSock.Available;
			            }
			            catch (SocketException ex)
			            {
							Console.WriteLine("discoverMaster() uSock.Available failed");
			                Console.WriteLine("SocketException: {0}", ex);
			                result = -1;
				            uSock.Close();
			            }
						
						if(result > 0)//contact with master! grab the packet...
						{						
							uSock.ReceiveFrom(data, ref ep);  //we don't really care about this string
							uSock.Close();
							keepChecking = false;
						}
						if(check4LocalShutdown(100))
						{
							shutdown = true;
							keepChecking = false; //get me out of this loop so I can shutdown!
						}
					}while(keepChecking);
	            }
	            catch (SocketException ex)
	            {
					Console.WriteLine("discoverMaster() pickup master broadcast failed");
					Console.WriteLine("SocketException: {0}", ex);
	                return false;
	            }
				iep = (IPEndPoint)ep;
            	destAddr = iep.Address;      //we just want the address!
			}//end if(solo)
			else	//we're running w/ the master, so just connect to 127.0.0.1
			{
				if(ok2ConnectMaster.WaitOne(5000, false))//wait for a signal from master so we know it's ready
				{
					destAddr = IPAddress.Loopback;
				}
				else
					shutdown = true;
			}
            return !shutdown;//inverse since discover true = found master.
        }

        public string prepareInitialNotice()
        {
            //TODO - need to make sure these work on linux and mac, as well as windows
            
			if(System.Environment.MachineName != null)
				machineName = System.Environment.MachineName;
			else
				machineName = "unknown";//TODO - make more robust
			
			if(System.Environment.OSVersion.Platform == PlatformID.Unix)
				os = "Unix";
			else
				os = "Windows";

			if(System.Environment.ProcessorCount.ToString() != null)
				cores = System.Environment.ProcessorCount.ToString();
			else
				cores = "unknown";
			
            //TODO - pull specific data on memory, etc to pass here
            return "new*" + machineName + "*" + cores + "*" + os;
        }

        public void rClientThread()
        {
			bool connected;
			
            while (!shutdown)
			{
				gWin.invokeBlank();	//blank the progress bar
				if (!discoverMaster())
                {
					shutdown = true;//quit - nothing to do if we can't bind to listen for master
			    }
                else
				{
                    //now create a CSocket and connect to the master
                    sock = new CSocket(bufferSize);	
                     
					if(!sock.connect(destAddr, connectPort))
                    {   //oops, failed to connect to server...wait a bit and try again. 
					    Thread.Sleep(1000);
                    }
                    else    //we're connected to the master, so continue:-)
                    {	
						connected = true;
                        gWin.invokeSetLblConnection("online with master at '" + destAddr.ToString() + "'.");
                        //send our initial node info
                        if (!sock.writeStream(prepareInitialNotice()))
                        {
                            //oops, failed!
                            connected = false;
                        }
						Debug.WriteLine("rC: just sent initial notice");

                        do //connected && !shutdown
                        {
							if(sock.check4Message() > 0)
                            {
	                            receiveMsg = sock.readStream(); //we should receive a 'taskToRun' here
								Debug.WriteLine("rC just got receiveMsg:" + receiveMsg);
		                        if (receiveMsg == "lost")
		                        {
		                            //oops, we lost the connection!
		                            connected = false;
		                        }
		                        else    //we're ok, continue...
		                        {    
		                            if (!handleTask(receiveMsg))    //handleTask runs task and sends appropriate updates
		                            {
		                                //we lost connection or were told that the master is shutting down
		                                connected = false;
		                            }
								}
						    }
                            else //no task from master, so let's check for local shutdown, or lost connection
							{	
								if(check4LocalShutdown(100))
								{
									shutdown = true;
								}
								else
								{
									if(!sock.checkIfConnected())
										connected = false;
								}
							}
						        
					    }while(connected && !shutdown);
                        gWin.invokeSetLblConnection("lost connection! Trying to reconnect...");
                    }
				    sock.close();
                }
		    }//end while (!shutdown)
        }

        /// <summary>
        /// current implementation doesn't listen for message from master while we're busy!
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>true if we're still connected to client, false if not</returns>
        public bool handleTask(string msg)
		{
            bool connected = true; //this indicates true if we're still connected to client, false if not
			    //note that it doesn't indicate if the job succeeded or failed!
			bool success = true;//indicates if our task succeeded or failed
			bool solo = false;//if we lose our connection, indicates we'll attempt to finish task w/ out telling master
			bool remoteShutdown = false;//master is shutting down: told us to kill task
			bool taskOutputShowsFailure = false;
            string pathCheck, platform;;
			string[] t = sock.convert2Tokens(msg);		
			
			//items in array are: 0.task, 1.taskType, 2.winExePath, 3.winFilePath, 4.winOutputPath,
			//5.unixExePath, 6.unixFilePath, 7.unixOutputPath, 8.frame
			if(check4LocalShutdown(10))
			{
				Debug.WriteLine("rC: caught localShutdown in handletask()");
				shutdown = true;
			}
            else if (t[0] == "task")
			{
				lock(busyLock)
				{
				    busy = true;
			    }
                gWin.invokeSetLblStatus("busy"); 
          
				//identify platform here
				if(System.Environment.OSVersion.Platform == PlatformID.Unix)
				{
				    platform = "unix";
				}
			    else
				{
			        platform = "windows";
			    }
				
                //let's do most typeCatalog stuff here: checkPaths and generateArgs. We'll checkReturn after task 
				pathCheck = TaskTypeCatalog.checkPaths(platform, t);
				string args = TaskTypeCatalog.generateTaskArgs(platform, t);      
               
				ProcessStartInfo startInfo;
				
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
                    throw new SanityFailureException("rC received an unknown platform: " + platform);
                }
                Process taskProcess = null;

				startInfo.Arguments = args;
				startInfo.UseShellExecute = false;
				startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

				if(pathCheck == "ok")
                {
					try //attempt to launch the task
					{
						taskProcess = Process.Start(startInfo);
						bool taskFinished = false;
						
	                   	if (!sock.writeStream("update*running"))//tell master we're running
	                   	{
	                    	//oops, we failed to write!
							connected = false;
						}
						else //successfully sent 'running' update to master
					    {
							Debug.WriteLine("rC: sent update running");
		                    do//stay in this loop until: a) task finishes, b) receive local shutdown, c) remoteShutdown  
							{
								gWin.invokePulse();
								taskFinished = taskProcess.WaitForExit(0);//check if task has finished
								if(!taskFinished)
	                            {
									if(check4LocalShutdown(100))
	                            	{
										taskProcess.Kill();
	                                    taskProcess.WaitForExit(2000);
										shutdown = true;
	                                    break; //we have to shutdown now, can't wait for task to finish!    
									}
									else //check if master sent disconnect or shutdown
									{
										int result = check4RemoteShutdown(); //if 0, do nothing
										if(result == -1)//lost connection - let's attempt to finish the task quietly, but we'll 
										{				//skip all the reporting stuff later
											solo = true;
											connected = false;
											break;//get out of the loop
										}
										else if(result == 1)//we should disconnect and kill task, but not shutdown
										{
											Debug.WriteLine("rC: received rShutdown");
											remoteShutdown = true;
											taskProcess.Kill();
		                                    taskProcess.WaitForExit(2000);
		                                    break; //still running a task, but he's the boss!
										}
										else if(result == 2)//disconnect, kill task, and shutdown
										{
											taskProcess.Kill();
		                                    taskProcess.WaitForExit(2000);
											shutdown = true;
		                                    break; //still running a task, but he's the boss!
										}
										else if(result == 0)
										{
											//do nothing
										}
										else
										{
											throw new SanityFailureException("received unknown return from"
														+ "check4RemoteShutdown:"
											            + result);
										}
									
									}//end of disconnect shutdown block
	                            }
		                    }while(!taskFinished);
							
							if(taskFinished)
	                        {   
								success = TaskTypeCatalog.checkStdout(t[1], taskProcess.StandardOutput.ReadToEnd());
								taskOutputShowsFailure = true;
						    }
						    else //if we come here, we have a shutdown signal (we broke out of the taskFinished loop)
	                            success = false;    
	                    }
						
						if(solo)
							taskProcess.WaitForExit();//we broke out earlier, so need to wait now...
					}
					catch(Exception ex)
					{
					    success = false;
				    }
                }//end of if(pathCheck == "ok")

				if(!solo)//we're not disconnected; report as usual...
				{
					if((taskProcess != null) && success)
					{
						if (!sock.writeStream("update*done")) //td - should pass stdout here as well
	                    {
	                        //oops, lost our connection!
						    connected = false;
					    }
						Debug.WriteLine("rC: just sent update done");
				    }
	                else //either: pathbad, task failed to launch, deduced failure from output, or we have a shutdown
					{
						if (shutdown) //we're shutting down
						{
						    if (!sock.writeStream("update*failed*shutdown"))
	                        {
	                            //oops, lost our connection!
	                            connected = false;
	                        }
							Debug.WriteLine("rC: just sent update failed");
					    }
						else if(remoteShutdown)
						{
							connected = false;	
						}
						else if (pathCheck != "ok") //path problem
						{
							if(!sock.writeStream("update*failed*" + pathCheck))//pathCheck ex: "exe*blender"
	                        {
	                            //oops, lost our connection!
	                            connected = false;
						    }
							Debug.WriteLine("rC: just sent update failed");
					    }
						else if(taskOutputShowsFailure)
						{
							if(!sock.writeStream("update*failed*programOutputFailure"))
	                        {
	                            //oops, lost our connection!
	                            connected = false;
						    }	
						}
						else//process must have failed to launch for unknown reason
					    {
							if(!sock.writeStream("update*failed*unknown"))
	                        {
	                            //oops, lost our connection!
	                            connected = false;
						    }
	                    }
            		}
				}//end if(!solo)
				
				lock(busyLock)
			    {
		            busy = false;
				} 
	            gWin.invokeSetLblStatus("idle");
				gWin.invokeBlank();
			}//end if(t[0] = "task")
			else if (t[0] == "disconnect")    //master is shutting down, so disconnect
	        {
	            Debug.WriteLine("rC got a disconnect");
				Thread.Sleep(2000); //give the master a moment to shutdown...
	            connected = false; // set this so we close our connection, then try to connect again...
	        }
			else if (t[0] == "rCShutdown") //this rC should disconnect and shutdown
	        {
				shutdown = true;
			    connected = false;
	        }
			else
			{
	            //td - error, should never get here!
	            throw new SanityFailureException("client sent us an unkown task!: " + t[0]);
	        }	
        	return connected;
		}
		
	    //td - test	
	    public static bool randomBool()
		{
	            Random randomSeed = new Random();
	
				return (randomSeed.NextDouble() > 0.5);
		}
    }
}