// Project: Loki Render - A distributed job queue manager.
// Version: 0.5
// 
// File Description: Notice object used for inter-messaging between various
// components
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
    public class Notice
    {
		static int noticeIDCounter = 0;
		
		public readonly int noticeID;
		public int jobID, clientID, firstFrame, lastFrame, frame, frameIndex, failureAllowance;
        public string noticeType, taskType, jobName, winExePath, winFilePath, winOutputPath, 
		                   unixExePath, unixFilePath, unixOutputPath, clientMsg, clientFailureType,
		                   clientFailureValue, errorMsg;

		Notice()
		{
			noticeID = ++noticeIDCounter;
		}
		
		//constructor - used by getNextTask() to return a 'noTasks' notice to queue, or to tell client to 'shutdown'
        /// <summary>
        /// constructor
        /// -used by getNextTask() to return a 'noTasks' notice to queue, 
        /// or to tell client to 'shutdown'
        /// </summary>
        /// <param name="nType"></param>
        public Notice(string nType) : this()
        {
			noticeType = nType;
        }

        //constructor - used by GUI to 'remove' a job.
        public Notice(string nType, string generic) : this()
        {
			noticeID = ++noticeIDCounter;
			noticeType = nType;
            if (noticeType == "update")
                clientMsg = generic;
            else
                jobName = generic;
        }

        //constructor - used by client to tell qMT it's 'new'
        public Notice(string nType, string cMsg, int cID) : this()
        {
            noticeType = nType;
            clientMsg = cMsg;
            clientID = cID;
        }
		
		//constructor - used by client to send an update to qMT
		public Notice(string nType, string cMsg, int jID, int cID, int fIndex) : this()
		{
			noticeType = nType;
			clientMsg = cMsg;
			jobID = jID;
			clientID = cID;
			frameIndex = fIndex;
		}

        //constructor - for a new job request to be passed by the GUI to Queue
        public Notice(string nType, string jName, string tType, string wEPath, string wFPath, string wOPath,
		       string uEPath, string uFPath, string uOPath, int fFrame, int lFrame, int aFailures) : this()
        {
            noticeType = nType;
            jobName = jName;
			taskType = tType;
            winExePath = wEPath;
            winFilePath = wFPath;
            winOutputPath = wOPath;
			unixExePath = uEPath;
			unixFilePath = uFPath;
			unixOutputPath = uOPath;
            firstFrame = fFrame;
            lastFrame = lFrame;
            failureAllowance = aFailures;
        }

        //constructor - for notice exchange concerning an already existing job (between Queue and Clients)
        public Notice(string nType, int jID, string tType, string winEPath, string winFPath, string winOPath,
		       string unixEPath, string unixFPath, string unixOPath, int f, int fIndex) : this()
        {
            noticeType = nType;
			taskType = tType;
            jobID = jID;
            winExePath = winEPath;
            winFilePath = winFPath;
            winOutputPath = winOPath;
			unixExePath = unixEPath;
			unixFilePath = unixFPath;
			unixOutputPath = unixOPath;
            frame = f;
            frameIndex = fIndex;

        }
    }
}
