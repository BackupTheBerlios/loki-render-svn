// Project: Loki Render - A distributed job queue manager.
// Version: 0.5
// 
// File Description: holds all info related to a job, including it's progress
// and all functions needed to interact with the job
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

namespace loki
{
    public class Job
    {
        public struct FrameStatus
        {
            public int frame, clientID, failures;
            public string status;  //can be 'unassigned', 'running', 'failed', 'aborted', 
            //'finished', or 'requested'; where 'aborted' means (failed >= failureAllowance)
        }

        static int jobIDCounter = 0;
        
        /// <summary>
        /// a - remaining, stopped
        /// b - remining, tasks running
        /// c - all assigned, running
        /// d - all tasks finished or aborted
        /// </summary>
        char status;
		public int remainingTasks;
		

        public readonly int firstFrame, lastFrame, totalFrames, jobID, failureAllowance;
        public string name, taskType, winExePath, winFilePath, winOutputPath,
		                unixExePath, unixFilePath, unixOutputPath;
        public FrameStatus[] frames;

        //constructor
        public Job(string iName, string tType, string winEPath, string winFPath, string winOPath,
            string unixEPath, string unixFPath, string unixOPath, int iFirstFrame, int iLastFrame,
            int iFailureAllowance)
        {
            name = iName;
			taskType = tType;
            winExePath = winEPath;
            winFilePath = winFPath;
            winOutputPath = winOPath;
			unixExePath = unixEPath;
            unixFilePath = unixFPath;
            unixOutputPath = unixOPath;
            firstFrame = iFirstFrame;
            lastFrame = iLastFrame;
            totalFrames = (lastFrame - firstFrame) + 1;
            failureAllowance = iFailureAllowance;
            frames = new FrameStatus[totalFrames];

            jobID = jobIDCounter++;
            status = 'a'; // can later be set to 'b', 'c', 'd'

            //initialize framesStatus
            for (int f = 0; f < totalFrames; f++)
            {
                frames[f].frame = firstFrame + f;
                frames[f].failures = 0;
                frames[f].status = "unassigned";
                frames[f].clientID = -1;
            }
        }

		//called by queue thread to edit the job
		public void edit(string wFP, string wOP, string uFP, string uOP)
		{
			winFilePath = wFP;
			winOutputPath = wOP;
			unixFilePath = uFP;
			unixOutputPath = uOP;
		}
		
        /// <summary>
        /// who: called by manageJobs() in Queue
        /// what: returns the next 'unassigned' or 'failed' task to manageJobs in a Notice object
        /// tested: yes
        /// </summary>
        /// <returns></returns>
        public Notice getNextTask()
        {
            for (int f = 0; f < totalFrames; f++)
            {
                if ((frames[f].status == "unassigned") || (frames[f].status == "failed"))
                {
					return new Notice("taskToRun", jobID, taskType, winExePath, winFilePath, winOutputPath,
					                  unixExePath, unixFilePath, unixOutputPath, firstFrame + f, f);
                }
            }
            status = 'd';
            return new Notice("noTasks");
        }
		
		/// <summary>
		/// qMT calls this function if it stopped the queue because of an abort
		/// </summary>
		public void resetStatus()
		{
			//initialize framesStatus
            for (int f = 0; f < totalFrames; f++)
            {
				//only reset status if it was failed or aborted!
                if(frames[f].status == "failed" || frames[f].status == "aborted")
				{
					frames[f].failures = 0;
	                frames[f].status = "unassigned";
	                frames[f].clientID = -1;
				}
            }	
		}

        /// <summary>
        /// called to update the job's status. this is important for manageJobs()
        /// frame status types:
        /// 0 - unassigned
        /// 1 - requested
        /// 2 - running
        /// 3 - failed
        /// 4 - aborted
        /// 5 - finished
        /// note: we treat 'requested' like 'running' for categorization.
        /// tested: yes
        /// </summary>
        public void updateMyStatus()
        {
            //first let's tally all types of states for each task
            int unassigned = 0;
            int requested = 0;
            int running = 0;
            int failed = 0;
            int aborted = 0;
            int finished = 0;

            for (int f = 0; f < totalFrames; f++)
            {
                if (frames[f].status == "unassigned")
                    unassigned++;
                else if (frames[f].status == "requested")
                    requested++;
                else if (frames[f].status == "running")
                    running++;
                else if (frames[f].status == "failed")
                    failed++;
                else if (frames[f].status == "aborted")
                    aborted++;
                else if (frames[f].status == "finished")
                    finished++;
                else
                {
                    throw new SanityFailureException("updateMyStatus found an unknown status tag!: " + frames[f].status);
                }
            }

            //if we have any unassigned, requested, or failed
            if (unassigned > 0 || failed > 0)
            {
             	if (running < 1 && requested < 1)
                	status = 'a';
                else
                	status = 'b';
            }
            else if (unassigned < 1 && failed < 1)
            {
             	if (running > 0 || requested > 0)
                	status = 'c';
                else
                    status = 'd';
            }
        }

        public char Status
        {
            get { return status; }
            set { status = value; }
        }

    }
}
