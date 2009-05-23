// Project: Loki Render - A distributed job queue manager.
// Version: 0.5.1
// 
// File Description: This is the center of the master's queue management	  
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
using System.IO;					//for writing/reading jobs to/from file
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization;	//for writing/reading jobs to/from file
using System.Runtime.Serialization.Formatters.Binary;	//for writing/reading jobs to/from file
using System.Diagnostics;	//for debug output

namespace loki
{
    public class Queue
    {
        bool qRunning;
		bool notifyAllFailures; //default: false. TODO - change by config

        List<Job> jobs;
        public List<Client> clients;    //both the queueManageThread and the listenerThread access this
        Queue<Notice> notices;
        public Object clientsLock;

        Listener myListener;
        int port;               //where the remoteClients should connect
        int broadcastInterval;  //how many seconds between each broadcast
		int timeout;//how long should the clientThread wait for a shutdown notice
			                  //the shorter it is, the more responsive to cycle loop, but will also take
			                  //more CPU cycles. If 0, it would 100% CPU!

        public AutoResetEvent noticeEvent;
        public Object noticeLock;

        Thread qThread;
		
		MasterWin win;//store our reference to the MainWindow here

        //default constructor
        public Queue(int connectPort, int bInterval, int t, MasterWin win)
        {
            port = connectPort;
            broadcastInterval = bInterval;
			timeout = t;
            qRunning = false;
			notifyAllFailures = false;
            jobs = new List<Job>();
            clients = new List<Client>();
            notices = new Queue<Notice>();
            noticeEvent = new AutoResetEvent(false);
            noticeLock = new Object();
            clientsLock = new Object();
			
			//setup windows stuff
			this.win = win;	//need this before we start listener
			
			//listener and broadcast threads are launched in constructor!
            myListener = new Listener(this, port, broadcastInterval, timeout, this.win);

			loadJobsFromCfg();	//load the last session's jobs into the queue
			
            qThread = new Thread(queueManagerThread);   //td - error checking for thread stuff?
            qThread.Start();
        }
		
		//constructor - specifically for case where both master and grunt are local
		public Queue(int connectPort, int bInterval, int t, MasterWin win, RemoteClient rC)
        {
            port = connectPort;
            broadcastInterval = bInterval;
			timeout = t;
            qRunning = false;
			notifyAllFailures = false;
            jobs = new List<Job>();
            clients = new List<Client>();
            notices = new Queue<Notice>();
            noticeEvent = new AutoResetEvent(false);
            noticeLock = new Object();
            clientsLock = new Object();
			
			//setup windows stuff
			this.win = win;	//need this before we start listener
			
			//listener and broadcast threads are launched in constructor!
            myListener = new Listener(this, port, broadcastInterval, timeout, this.win, rC);
			
			loadJobsFromCfg();	//load the last session's jobs into the queue

            qThread = new Thread(queueManagerThread);   //td - error checking for thread stuff?
            qThread.Start();
        }
		
		int findFirstFreeJobID()
		{
			int id = -1;
			bool free;
			
			do
			{
				id++;
				free = true;
				foreach(Job j in jobs)
				{
					if(j.jobID == id)	//is this id already taken?
					{
						free = false;	
						break;
					}
				}
			}while(!free);
			
			return id;
		}
		
		//called by MasterWin //TODO - really shouldn't let Gtk thread touch the jobs list!
		public bool checkIfJobDone(string jName)
		{
			if(jobs[findJobIndex(jName)].Status == 'd')
				return true;
			else
				return false;
		}
		
		//pulls the last session's jobs from the lokiRender.cfg file and adds them to the queue.
		bool loadJobsFromCfg()
		{
			if(File.Exists("lokiRender.cfg"))
			{
				IFormatter formatter = new BinaryFormatter();
				Stream stream = new FileStream("lokiRender.cfg", FileMode.Open, FileAccess.Read, FileShare.Read);
				try
				{
					jobs = (List<Job>) formatter.Deserialize(stream);
					
					foreach(Job j in jobs)
					{
						for(int f = 0; f<j.frames.Length; f++)
						{
							if(j.frames[f].status == "running" || j.frames[f].status == "requested")
								j.frames[f].status = "unassigned";
						}
						j.updateMyStatus();
						addJobToGUI(j.jobID);
					}
					updateProgressBarToGUI();
				}
				catch(Exception ex)
				{
					Debug.WriteLine("qMT: exception while trying to load jobs: " + ex.Message);
					return false;
				}
				finally
				{
					stream.Dispose();
					stream.Close();	
				}
			}
			return true;
		}
		
		public bool getQRunning()
		{
			return qRunning;
		}
		
		public bool checkIfAllDone()
		{
			bool allDone = true;
			foreach (Job j in jobs)
			{
				if(j.Status != 'd')
				{
					allDone = false;
					break;
				}
			}
			return allDone;
		}
		
		public bool anyClientsWithPlatform(string platform)
		{
			bool any = false;
			lock(clientsLock)
			{
				foreach(Client c in clients)
				{
					if(c.os == platform)
					{
						any = true;
						break;
					}
				}
			}
			return any;
		}

        /// <summary>
        /// what: receives and handle notices of type: 
        /// -update, 
        /// -start
        /// -stop
        /// -shutdown
        /// -add
        /// -remove
        /// should test: update, start, stop, shutdown, add, remove
        /// tested: shutdown, add, remove, shutdown, stop
        /// </summary>
        public void queueManagerThread()
        {
            bool shutdown = false;
            Notice n;
            while (!shutdown)
            {
                n = fetchOrWait4Notice();    //will take next Notice from queue, or wait for next if empty
				Debug.WriteLine("qMT: received notice: " + n.noticeType);
				
                if (n.noticeType == "update")
                {
                    if (!handleUpdate(n))  //note: handleUpdate() calls manageJobs() if there are any status changes
                    {
                        throw new SanityFailureException("handleUpdate() failed!");
                    }
                }
                else if (n.noticeType == "start")
                {
                    if (!qRunning)
                    {
						if(!checkIfAllDone())
						{
							qRunning = true;
							manageJobs();
						}
                    }
                }
                else if (n.noticeType == "stop")
                {
                    qRunning = false;
					win.invokeStopGUI();
                }
                else if (n.noticeType == "shutdown")
                {
                    qRunning = false;
					win.invokeStopGUI();
                    shutdown = true;
                    myListener.shutdownEvent.Set(); //tell myListener to close listening port and end
                    lock (clientsLock)
                    {
                        foreach (Client c in clients)
                        {
                            c.deliverNotice(new Notice("shutdown"));
                        }
                    }
					
					//let's write our jobs to the lokiRender.cfg file so we can read them next startup
					IFormatter formatter = new BinaryFormatter();
					Stream stream = new FileStream("lokiRender.cfg", FileMode.Create, FileAccess.Write, FileShare.None);
					try
					{
						formatter.Serialize(stream, jobs);	
					}
					catch(SerializationException ex)
					{
						Debug.WriteLine("qMT: failed to write Jobs to lokiRender.cfg: " + ex.Message);
					}
					finally
					{
						stream.Dispose();
						stream.Close();
					}
                }
                else if (n.noticeType == "add")
                {
                    //first make sure the job name doesn't already exist
                    bool unique = true;
                    foreach (Job j in jobs)
                    {
                        if (j.name == n.jobName)
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
						jobs.Add(new Job(n.jobName, n.taskType, n.winExePath, n.winFilePath, n.winOutputPath,
						                 n.unixExePath, n.unixFilePath, n.unixOutputPath, n.firstFrame,
						                 n.lastFrame, n.failureAllowance, findFirstFreeJobID()));
						
						addJobToGUI(jobs[findJobIndex(n.jobName)].jobID);
						updateProgressBarToGUI();
                    }
                    else //not unique
                    {
						win.invokeModalMsg("info", "There is already a job with the name '" + n.jobName +
									"' in the queue. Please choose a unique name.");
                    }
                }
                else if (n.noticeType == "remove")
                {
                    int index = findJobIndex(n.jobName);
                    if (jobs[index].Status == 'a' || jobs[index].Status == 'd')
					{
                        jobs.RemoveAt(index);
						win.invokeRemoveJobTV(n.jobName);
						updateProgressBarToGUI();
					}
                    else
                    {
						win.invokeModalMsg("info", "A job can't be removed while it has tasks running.");
                    }
                }
				else if (n.noticeType == "removeFinished")
				{
					foreach(Job j in jobs)
					{
						if(j.Status == 'd')
						{
							deliverNotice(new Notice("remove", j.name));
						}
					}	
				}
				else if (n.noticeType == "startEdit")
				{
					int index = findJobIndex(n.jobName);
					if (jobs[index].Status == 'a' || jobs[index].Status == 'd')
					{
						Job j = jobs[index];
                        win.invokeStartEditJob(j.taskType, j.name, j.winFilePath, j.winOutputPath, j.unixFilePath,
						                  j.unixOutputPath, j.firstFrame, j.lastFrame);
					}
					else
					{
						win.invokeModalMsg("info", "A job can't be edited while it has tasks running.");	
					}
				}
				else if (n.noticeType == "commitEdit")
				{
					int index = findJobIndex(n.jobName);
					if (jobs[index].Status == 'a' || jobs[index].Status == 'd')
					{
						Job j = jobs[index];
						j.edit(n.winFilePath, n.winOutputPath, n.unixFilePath, n.unixOutputPath);
					}
					else
					{
						win.invokeModalMsg("info", "A job can't be edited while it has tasks running.");	
					}	
				}
                else
                {
                    throw new SanityFailureException("unknown notice type: " + n.noticeType);
                }
            }//end while (!shutdown)
        }//end queueManagerThread
		
		bool addJobToGUI(int jobID)
		{
			Debug.WriteLine("qMT: adding job to queue w/ jobID: " + jobID);
			Job j = jobs[findJobIndex(jobID)];
				
			bool result = true;
			int aborted, done, remaining;
			aborted = 0;
			done = 0;
			remaining = j.frames.Length;
				
			string status;
			if(j.Status == 'a')
				status = "queued";
			else if((j.Status == 'b') || (j.Status == 'c'))
				status = "running";
			else
				status = "finished";
				
				
			for(int a = 0; a<j.frames.Length; a++)
			{
				if(j.frames[a].status == "aborted")
				{
					aborted++;
				}
				else if(j.frames[a].status == "finished")
				{
					done++;
				}
			}
			remaining = (j.frames.Length - (aborted + done));
				
			win.invokeAddJobToTV(j.jobID, j.name, status, remaining.ToString(), aborted.ToString(), 
				                     done.ToString());
			return result;
		}
		
		void updateProgressBarToGUI()
		{
			int grandTotal = 0;
			int grandDone = 0;
			foreach(Job myJob in jobs)
			{
				for(int a = 0; a<myJob.frames.Length; a++)
				{
					if(myJob.frames[a].status == "aborted")
					{
						grandDone++;
					}
					else if(myJob.frames[a].status == "finished")
					{
						grandDone++;
					}
				}
				grandTotal += myJob.frames.Length;
			}
			//now we have values
			win.invokeUpdateProgress(grandDone, grandTotal);
		}
		
		bool jobUpdateToGUI(int jobID)
		{
			Job j = jobs[findJobIndex(jobID)];
				
			bool result = true;
			int aborted, done;
			aborted = 0;
			done = 0;
				
			string status;
			if(j.Status == 'a')
				status = "queued";
			else if((j.Status == 'b') || (j.Status == 'c'))
				status = "running";
			else
				status = "finished";
				
				
			for(int a = 0; a<j.frames.Length; a++)
			{
				if(j.frames[a].status == "aborted")
				{
					aborted++;
				}
				else if(j.frames[a].status == "finished")
				{
					done++;
				}
			}
			j.remainingTasks = (j.frames.Length - (aborted + done));
				  
			win.invokeUpdateJobTV(j.jobID, status, j.remainingTasks.ToString(), aborted.ToString(),
			                      done.ToString());
			
			updateProgressBarToGUI();
			return result;
		}

        public int countAvailableClients()
        {
            int count = 0;
            lock (clientsLock)
            {
                foreach (Client c in clients)
                {
                    if (c.status == "available")
                        count++;
                }
            }
            return count;
        }

        /// <summary>
        /// who: called by queueManagerThread() when we have a notice of type 'update'.
        /// what: handles the update based on the type of clientMsg. the types are:
        /// 'new' - 
        /// 'running' - 
        /// 'requestFailed' - 
        /// 'failed' - 
        /// 'killMe' - 
        /// 'done' - 
        /// tested: yes
        /// </summary>
        /// <param name="n"></param>
        /// <returns>returns false if it gets an unknown client message</returns>
        bool handleUpdate(Notice n)
        {
            int jIndex = findJobIndex(n.jobID); //returns -1 if not found

            if (n.clientMsg == "new")
            {
                lock (clientsLock)
                {
					int i = findClientIndex(n.clientID);
					clients[i].status = "available";
					win.invokeAddGruntToTV(n.clientID, clients[i].name, clients[i].status,
					                       clients[i].os, clients[i].cores);
                }
                if(qRunning)
				{
                    manageJobs();
				}
            }
            else if (n.clientMsg == "running")
            {
                lock (clientsLock)
                {
                    clients[findClientIndex(n.clientID)].status = "running";
                }
                jobs[jIndex].frames[n.frameIndex].status = "running";
                jobs[jIndex].frames[n.frameIndex].clientID = n.clientID;
                jobs[jIndex].updateMyStatus();
				win.invokeUpdateGruntTV(n.clientID, "busy");
				jobUpdateToGUI(jobs[jIndex].jobID);
            }
            else if (n.clientMsg == "requestFailed")//bad client! we lost the rClient, so set task back to unassigned and delete client;
            {                                       //the thread should end itself right after it sends this notice.
                jobs[jIndex].frames[n.frameIndex].status = "unassigned";
                Thread.Sleep(100);  //give the client thread a moment to compose itself and then die:-)
                lock (clientsLock)
                {
                    clients.RemoveAt(findClientIndex(n.clientID));
					win.invokeRemoveGruntTV(n.clientID);
                }
                jobs[jIndex].updateMyStatus();  //this is necessary: going from 'requested' to 'unassigned' could
                                                //change the job status from 'c' to 'b', or 'd' to 'a'
				jobUpdateToGUI(jobs[jIndex].jobID);
            }
            else if (n.clientMsg == "lostClient")   //similar to 'requestFailed', but no task was assigned, so 
            {                                       //just remove the client object...
                Thread.Sleep(100);  //give the client thread a moment to compose itself and then die:-)
                lock (clientsLock)
                {
                    clients.RemoveAt(findClientIndex(n.clientID));
					win.invokeRemoveGruntTV(n.clientID);
                }
            }
            else if (n.clientMsg == "failed")
            {
                string cName;
				lock (clientsLock)
                {
                    clients[findClientIndex(n.clientID)].status = "available";
					cName = clients[findClientIndex(n.clientID)].name;
                }
				win.invokeUpdateGruntTV(n.clientID, "ready", "failed");
                jobs[jIndex].frames[n.frameIndex].failures++;

                if (jobs[jIndex].frames[n.frameIndex].failures >= jobs[jIndex].failureAllowance)
                {	//abort
                    jobs[jIndex].frames[n.frameIndex].status = "aborted";
					int tries = jobs[jIndex].frames[n.frameIndex].failures;
					
					//stop the queue, tell the user to resolve the problem and start queue again
					qRunning = false;
					win.invokeStopGUI();
					foreach(Job j in jobs)
					{
						j.resetStatus();	
					}
					win.invokeAbortMsg(cName, n.errorMsg, tries);
                }
                else
				{	//failed
                    jobs[jIndex].frames[n.frameIndex].status = "failed";
					int remainingTries = jobs[jIndex].failureAllowance - jobs[jIndex].frames[n.frameIndex].failures;
					
					if(notifyAllFailures)
					{
						//tell the user the error message, and that we'll try x more times to run the task
						win.invokeFailureMsg(cName, n.errorMsg, remainingTries);
					}
				}
				
				jobs[jIndex].updateMyStatus();
				jobUpdateToGUI(jobs[jIndex].jobID);
	
				if (qRunning)
                {
					manageJobs();
                }
            }
            else if (n.clientMsg == "done")
            {
				string taskTime;
                lock (clientsLock)
                {
					int clientIndex = findClientIndex(n.clientID);
                    clients[clientIndex].status = "available";
					taskTime = clients[clientIndex].getRunningTaskTime();
                } 
				win.invokeUpdateGruntTV(n.clientID, "ready", taskTime);
				
                jobs[jIndex].frames[n.frameIndex].status = "finished";
                jobs[jIndex].updateMyStatus();
				jobUpdateToGUI(jobs[jIndex].jobID);
                if(qRunning)
				{
                    manageJobs();
				}
            }
            else
            {
                throw new SanityFailureException("qMT received an unknown update type!: " + n.clientMsg);
            }
			if(checkIfAllDone())
			{
				qRunning = false;
				win.invokeStopGUI();
			}
            return true;
        }

        /// <summary>
        /// called by manageJobs()
        /// </summary>
        /// <returns>true if any jobs have a status of 'a' or 'b'</returns>
        bool jobsLeft()
        {
            bool anyJobs = false;
            foreach (Job j in jobs)
            {
                if (j.Status == 'a' || j.Status == 'b')
                    anyJobs = true;
            }
            return anyJobs;
        }

        /// <summary>
        /// called by:  queueManagerThread() when qMT starts, and when qMT gets certain types of updates
        /// calls:      Job.getNextTask()
        /// what: will launch as many tasks in 'a' and 'b' jobs, as there are available clients
        /// will also set qRunning to false if all jobs are 'd'.
        /// note: should only be called if qRunning is true
        /// tested:
        /// td - back to private after testing!
        /// </summary>
        /// <returns></returns>
        public void manageJobs()
        {			
			while ((countAvailableClients() > 0) && (jobsLeft()))    //only one task is assigned per iteration of this while loop
            {
				foreach (Job j in jobs)
                {
                    if (j.Status == 'a' || j.Status == 'b')    //find first job w/ status 'a' or 'b' run a task
                    {
                        Notice n1 = j.getNextTask();
                        if (n1.noticeType == "taskToRun") //getNextTask set task status to 'requested'
                        {
                            lock (clientsLock)
                            {
                                foreach (Client c in clients)
                                {
                                    if (c.status == "available")
                                    {
										c.status = "requested"; 
                                        c.deliverNotice(n1);
                                        j.frames[n1.frameIndex].status = "requested";
                                        j.updateMyStatus(); //we changed a task status, so update
                                        break;  //we delivered to a client, so break foreach client loop
                                    }
                                }
                            }
                            break;  //we delivered task, so break foreach job loop and go back to outer while
                        }
                        else if (n1.noticeType == "noTasks")    //the job has no more tasks to run!
                        {
                            throw new SanityFailureException("manageJobs has no tasks!");
                        }
                    }
                }//end foreach(Job j)
            }//end while
        }

        /// <summary>
        /// called by:  GUI thread, and 0 or more client threads
        /// calls:      none
        /// what: delivers a notice for the queueThread to pickup and act on
        /// locks: noticeBoxLock
        /// noticeTypes: 'start', 'stop', 'shutdown', 'add', 'remove', from the GUI, and
        /// 'update' from a client.
        /// test: yes, but not with threads
        /// </summary>
        /// <param name="notice"></param>
        /// <returns></returns>
        public void deliverNotice(Notice n)
        {
            lock (noticeLock)
            {
                notices.Enqueue(n);
                noticeEvent.Set();   //signal to recipient we delivered a notice
            }
        }

        /// <summary>
        /// called by:  queueManagerThread
        /// calls:      none
        /// what: fetches a Notice from the noticeBox, and signals any possibly waiting delivers that it's empty
        /// locks: noticeBoxLock
        /// test: yes, but not with threading
        /// td - back to private after testing!
        /// </summary>
        /// <returns>fetched notice: 'run' or 'stop' from GUI, or 'update' from one of the jobs</returns>
        Notice fetchOrWait4Notice()
        {
            Notice nextNotice;
            bool some = false;

            lock(noticeLock)
            {
                if (notices.Count > 0)  //remains true until I take all notices
                    some = true;
            }

            if (!some)  //if the queue is empty, wait for one
            {
                noticeEvent.WaitOne();
            }
			
            lock (noticeLock)
            {
                nextNotice = notices.Dequeue();
                noticeEvent.Reset();
            }

            return nextNotice;
        }

        /// <summary>
        /// test: yes, seems to work, but it's strange I had to shift right by one to get the correct index!
        /// </summary>
        /// <param name="jName"></param>
        /// <returns>index if found, -1 if not found</returns>
        public int findJobIndex(int jID)
        {
            return jobs.FindIndex(delegate(Job job) { return job.jobID == jID; });
        }

        /// <summary>
        /// test: yes
        /// </summary>
        /// <param name="jName"></param>
        /// <returns>index if found, -1 if not found</returns>
        public int findJobIndex(string jName)
        {
            return jobs.FindIndex(delegate(Job job) { return job.name == jName; });
        }

        /// <summary>
        /// test: yes
        /// </summary>
        /// <param name="cID"></param>
        /// <returns>index if found, -1 if not found</returns>
        public int findClientIndex(int cID)
        {
            int result;
            lock (clientsLock)
            {
                result = clients.FindIndex(delegate(Client client) { return client.clientID == cID; });
            }
            return result;
        }
    }
}
