// Project: Loki Render - A distributed job queue manager.
// Version: 0.5.1
//
// File Description: Acts as liason between the Master thread and the remote client.
// An instance is created for each remote client that connects, and is destroyed when
// the remote client disconnects.
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
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics; //for debug

namespace loki
{
    public class Client
    {
        static int clientIDCounter = 0;
        public readonly int clientID;
        public string status, name, cores, os;
        Queue qHandle;
        CSocket sock;
        public AutoResetEvent noticeEvent;
        public Queue<Notice> notices;
        public Object noticeLock;
        Thread cThread;
		int timeout;//the amount of time(ms) client will wait for shutdown signal before continuing loop
		DateTime startTime;	//use these to time how long the task has been running

        public Client(Queue q, CSocket mSock, int t)
        {
            clientID = clientIDCounter++;
            qHandle = q;
            sock = mSock;
			timeout = t;
            status = "";
            notices = new Queue<Notice>();
            noticeEvent = new AutoResetEvent(false);
            noticeLock = new Object();

            cThread = new Thread(clientThread);
            cThread.Start();
        }
		
		public string getRunningTaskTime()
		{
			DateTime now = DateTime.UtcNow;
			double runningTime = (DateTime.UtcNow - startTime).TotalSeconds;
			TimeSpan t = TimeSpan.FromSeconds(runningTime);
			//return t.Hours + ":" + t.Minutes + ":" + t.Seconds + "." + String.Format("{0:F0}", (t.Milliseconds/10));
			return String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D2}", t.Hours, t.Minutes, t.Seconds, t.Milliseconds/10);
		}

		/// <summary>
		/// fetches initial notice info from the remote client right after it's connected,
		/// like 'name', 'cores', and 'os'.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
        public bool getFirstNotice()
        {
            string receiveMsg;
            bool success = true;
            //receive initial notice from remoteClient, update my info, then send a notice to qMT
            receiveMsg = sock.readStream();
			Debug.WriteLine("cThread: just received initial notice");
            if (receiveMsg == "lost")
            {
                //oops, we lost our connection already! tell qMT and quit!
                qHandle.deliverNotice(new Notice("update", "lostClient", clientID));
                success = false;
            }
            else //we're ok, continue...
            {
                string[] toks = sock.convert2Tokens(receiveMsg);
                if (toks.Length > 0)
                {
                    if ((toks[0] == "new"))    //we're ok. update info and tell qMT
                    {
                        name = toks[1];
                        cores = toks[2];
                        os = toks[3];
                        qHandle.deliverNotice(new Notice("update", "new", clientID));
                    }
                }
                else    //we're getting garbage - disconnect from this client
                {
                    qHandle.deliverNotice(new Notice("update", "lostClient", clientID));
                    success = false;
                }
            }
            return success;
        }

		/// <summary>
		/// passes a task to the remote client
		/// </summary>
		/// <param name="n">
		/// A <see cref="Notice"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
        public bool sendTask(Notice n)
        {     
			string sendMsg = null;
			
			//Notice, here for reference:
			//public int jobID, clientID, firstFrame, lastFrame, frame, frameIndex, failureAllowance;
			//public string noticeType, taskType, jobName, exePath, filePath, outputPath, clientMsg;
			if(n.noticeType == "taskToRun")
			{
				//items passed are: "task", taskType, winExePath, winFilePath, winOutputPath,
				//unixExePath, unixFilePath, unixOutputPath, frame
				sendMsg = "task" + "*" + n.taskType + "*" + n.winExePath + "*" + n.winFilePath + "*"
				+ n.winOutputPath + "*" + n.unixExePath + "*" + n.unixFilePath + "*" + n.unixOutputPath
						+ "*" + n.frame;
			}
			else if(n.noticeType == "shutdown")
			{
				if(System.Environment.MachineName != null)
				{
					string localName = System.Environment.MachineName;
					if(localName == name) //looks like this client is running on the master system; tell it to shutdown
					{
						sendMsg = "rCShutdown" + "*";
					}
					else //remote client, so don't tell it to shutdown; just disconnect
						sendMsg = "disconnect" + "*";
				}
				else
					sendMsg = "disconnect" + "*";	
			}
			else
			{
				throw new SanityFailureException("sendTask received an unknown task type!: " + n.noticeType);
			}
			
            if (sock.writeStream(sendMsg))
            {
				Debug.WriteLine("c: just sent msg:" + sendMsg);
                return true;
            }
            else
                return false;
        }

		/// <summary>
		/// handles updates from a running remote client until it's done or fails
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
        public string getUpdate()
        {
            string msg = sock.readStream(); //ex: could receive "update*failed*exe*blender"
			Debug.WriteLine("c: just received update:" + msg);
            if (msg == "lost")
            {
                //we lost connection with rClient
                return "lost";
            }
            else
            {
                string[] tokens = sock.convert2Tokens(msg);
                if (tokens[1] == "running")
                    return msg;
                else if (tokens[1] == "failed")
                    return msg;
                else if (tokens[1] == "done")
                    return msg;
                else
                    throw new SanityFailureException("remote Client is sending an unknown update message!");
            }
        }

        /// <summary>
        /// what: the clients thread function. waits for a notice form qMT of two types: "shutdown",
        /// or "tasktoRun":
        /// shutdown - closes the socket to the client and ends the thread
        /// taskToRun - passes the task to client, receives updates and when done, goes back to waiting
        /// for a notice from qMT. Note that while it's waiting for updates from the client while the task
        /// is running, it will periodically check to see if it's received a shutdown signal. Also, if at
        /// any time it loses it's connection with the client, it will notify qMT with a 'requestFailed', close
        /// the socket, and end.
        /// </summary>
        public void clientThread()
        {
            Notice fetchedN;
			Notice newN;
            bool quit = false;
            bool waiting;
            bool lostConnection = false; //use this to determine if we're shutting down or dying at the end...

            if (!getFirstNotice())   //receive first notice from client, update data, and send update to qMT
            {   //if we lost connection, getFirstNotice() already sent a notice to qMT
                quit = true;    //never enter the main loop: we're done:-( 
            }

            while (!quit)   //we start this loop again each time a task ends (failed or done).
            {
                waiting = true; //initialize for each loop
                fetchedN = fetchOrWait4Notice();   //get our next notice from qMT, or detect lost rCdel
				if(fetchedN.noticeType == "lost")
				{
					quit = true;
					qHandle.deliverNotice(new Notice("update", "lostClient", clientID));
					break;
				}
				newN = new Notice("update", "validateme!", fetchedN.jobID, clientID, fetchedN.frameIndex);
               
                if (fetchedN.noticeType == "shutdown")
				{
					sendTask(new Notice("shutdown"));
                    break;  //break out of !quit loop
				}
                else    //we have a "taskToRun" - if we lose client connection in here, send a 'requestFailed'
                {       //to qMT
                    if (fetchedN.noticeType != "taskToRun")    //it should be 'taskToRun'!
                    {
                        throw new SanityFailureException("qMT sent an unknown notice to a client!");
                    }
                    
                    if (!sendTask(fetchedN))   //send the task to remoteClient
                    {   //lost connection to client -send a notification to qMT since
                        //we're outside of the 'waiting' loop
                        newN.clientMsg = "requestFailed";

                        qHandle.deliverNotice(newN);
                        break;  //break out of '!quit' loop
                    }
                    while (waiting) //now we get updates from rC
                    {
						//we need a new notice every time we send
						newN = new Notice("update", "putvalhere!", fetchedN.jobID, clientID, fetchedN.frameIndex);
						if (check4ShutDownNotice(timeout))  //quick check for shutdown notice
                        {   
							Debug.WriteLine("client: sending shutdown to rC");
							sendTask(new Notice("shutdown"));//tell remoteClient we're shutting down
							quit = true;   
                            break;  //break out of 'waiting' loop
                        }
                        else    //no shutdown signal, continue with waiting for updates from rClient
                        {
                            string receiveMsg;

                            if (sock.check4Message() > 0)   //check if we received an update from rClient
                            {
                                receiveMsg = getUpdate();   //wait for an update from rClient
                                if (receiveMsg == "lost")    //lost client connection
                                {
                                    lostConnection = true;
                                    quit = true;
                                    break;
                                }
                                else    //we're ok, continue
                                {
                                    string[] tokens = sock.convert2Tokens(receiveMsg); 
									//receiveMsg examples: "update*running", "update*failed*exe*blender"
									if (tokens[1] == "running")
                                    {
										startTime = DateTime.UtcNow;
                                        newN.clientMsg = "running";    //we'll get another update for this task,
                                                                    //so stay in 'waiting' loop
                                    }
                                    else if (tokens[1] == "failed")
                                    {
										Debug.WriteLine("client receiveMsg: " + receiveMsg);//test
                                        newN.clientMsg = tokens[1];
										string errorMsg = "";
										
										if(tokens.Length > 2)
											newN.clientFailureType = tokens[2];
										if(tokens.Length > 3)
											newN.clientFailureValue = tokens[3];
                                        waiting = false;    //this was the last msg for this task, 
                                                            //so quit 'waiting' loop
										if(tokens[2] == "exe")
										{
											errorMsg = "failed to launch the program: " + tokens[3];
										}
										else if(tokens[2] == "file")
										{
											errorMsg = "couldn't find the file: " + tokens[3];
										}
										else if(tokens[2] == "output")
										{
											errorMsg = "couldn't find the output dir: " + tokens[3];
										}
										else if(tokens[2] == "programOutputFailure")
										{
											errorMsg = tokens[3];
										}
										else if(tokens[2] == "shutdown")
										{
											errorMsg = "Loki was killed while running a task";	
										}
										
										newN.errorMsg = errorMsg;	
                                    }
                                    else if (tokens[1] == "done")   //must be 'done'
                                    {
                                        newN.clientMsg = "done";
                                        waiting = false;    //this was the last msg for this task, 
                                        //so quit 'waiting' loop
                                    }
                                    else
                                    {
                                        throw new SanityFailureException("client received an unknown message" +
                                            " from rClient!"); 
                                    }
                                    qHandle.deliverNotice(newN);   //pass update to qMT
                                }
                            }
                            else if (sock.check4Message() == -1)
                            {   //lost connection to client
                                lostConnection = true;
                                quit = true;
                                break;  //break out of 'waiting' loop
                            }
							else//no message yet, so let's make sure connection is still good
							{
								if(!sock.checkIfConnected())
								{
									lostConnection = true;//oops, lost connection!
									quit = true;
									break;
								}	
							}
                        }
                    }//end of 'waiting' loop - we exit this loop when we've gotten the last update for the task;
                     //now we go back to waiting for the next notice from qMT
                }
                if (lostConnection)  //tell qMT we lost the connection
                {
                    newN.clientMsg = "requestFailed";

                    qHandle.deliverNotice(newN);
                }
            }   //end of while (!quit) - we exit this loop in two cases:
                //1. we received a shutdown signal from qMT, or 2. we lost the connection to the client.
            //in both cases, we close the socket:
			Thread.Sleep(1000);//TODO - kludge, but oh well. make sure the rC gets our shutdown before we close.
            sock.close();

            //now this thread ends, and qMT should remove my client object from the clients list:-)
        }

        /// <summary>
        /// called by:  qMT
        /// calls:      none
        /// what: delivers a notice for the client and signals it's delivery
        /// locks: noticeLock
        /// test: yes, but not with threads
        /// </summary>
        /// <param name="notice"></param>
        /// <returns></returns>
		public void deliverNotice(Notice n)//qMT calls this function to deliver notices to clientThread
        {
            lock (noticeLock)
            {
				notices.Enqueue(n);
                noticeEvent.Set();   //signal to recipient we delivered a notice
			
            }
        }

        public bool check4ShutDownNotice(int timeout)
        {
            Notice nextNotice;
            bool some = false;
            bool shutdown = false;

            lock (noticeLock)
            {
                if (notices.Count > 0)  //remains true until I take all notices
                    some = true;
            }

            if (!some)  //if the queue is empty, wait for one
            {
                if (noticeEvent.WaitOne(timeout, false))
                {
                    shutdown = true;
                    lock (noticeLock)
                    {
                        nextNotice = notices.Dequeue();
                        noticeEvent.Reset();    //if we don't reset, we might leave a signal on!
                        if (nextNotice.noticeType != "shutdown")
						{
                            throw new SanityFailureException("qMT should NOT be telling this client to run another task!" +
							                                 " received noticeType:" + nextNotice.noticeType);
						}
						sendTask(new Notice("shutdown"));
                    }
                }
            }
            else    //already a notice there, it has to be a 'shutdown' signal!
            {
                shutdown = true;
                lock (noticeLock)
                {
                    nextNotice = notices.Dequeue();
                    noticeEvent.Reset();    //if we don't reset, we might leave a signal on!
                    if (nextNotice.noticeType != "shutdown")
                    {
						throw new SanityFailureException("qMT should NOT be telling this client to run another task!" +
							                                 " received noticeType:" + nextNotice.noticeType);
					}
                }
            }
            return shutdown;
        }

        /// <summary>
        /// called by:  clientThread
        /// calls:      none
        /// what: fetches a Notice from the notices queue
        /// locks: noticeBoxLock
        /// test: yes, but not with threading
        /// td - back to private after testing!
        /// NOTE! - this is a point where cT could be idle for a long time, so we're also checking
        /// the socket connection to make sure we're still alive!
        /// </summary>
        /// <returns>fetched notice: 'run' or 'stop' from GUI, or 'update' from one of the jobs</returns>
        public Notice fetchOrWait4Notice()
        {
            Notice nextNotice;
            bool noticeInQueue = false;

            lock (noticeLock)
            {
                if (notices.Count > 0)  //remains true until I take all notices
                    noticeInQueue = true;
            }

            if (!noticeInQueue)  //if the queue is empty, wait for one
            {
				do
				{   
					if (noticeEvent.WaitOne(100, false))
					{
						noticeInQueue = true;
					}
					else //no notice yet-we'll stay in loop, so let's check if connection is still good
					{						
						if(!sock.checkIfConnected())
						{
							break;//get out of the loop!
						}
					}
				}while(!noticeInQueue);
            }
			
			if(noticeInQueue)
			{
	            lock (noticeLock)
	            {
	                nextNotice = notices.Dequeue();
	                noticeEvent.Reset();    //if we don't reset, we might leave a signal on!
	            }
				return nextNotice;
			}
			else
				return new Notice("lost");
        }
    }
}
