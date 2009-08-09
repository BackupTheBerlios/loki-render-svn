/**
 *Project: Loki Render - A distributed job queue manager.
 *Version 0.6.0
 *Copyright (C) 2009 Daniel Petersen
 */

/**
 *This program is free software: you can redistribute it and/or modify
 *it under the terms of the GNU General Public License as published by
 *the Free Software Foundation, either version 3 of the License, or
 *(at your option) any later version.
 *
 *This program is distributed in the hope that it will be useful,
 *but WITHOUT ANY WARRANTY; without even the implied warranty of
 *MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *GNU General Public License for more details.
 *
 *You should have received a copy of the GNU General Public License
 *along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

package loki3.main;

/**
 *
 * 
 * @author daniel
 */
public class Job {
    static int jobIDCounter = 0;
    final int jobID, firstFrame, lastFrame;
    final String taskType, name, projFile;

    int aborted, remain, done;

    /// <summary>
    /// a - remaining, stopped
    /// b - remaining, tasks running
    /// c - all assigned, running
    /// d - all tasks finished or aborted
    /// </summary>
    String status;

    Job(String tType, String n, String pFile, String fFrame, String lFrame,
            String maxFailures) {
        jobID = jobIDCounter++;
        taskType = tType;
        name = n;
        projFile = pFile;
        firstFrame = Integer.parseInt(fFrame);
        lastFrame = Integer.parseInt(lFrame);

        //TODO - these are bogus until we get the methods up
        aborted = 5;
        remain = 10;
        done = 40;
        status = "b";
    }
    void calcFrameStatus() {
        //TODO - calculates aborted, remain, done, and job status
    }
    /**
     * this method grabs value specified by column
     * called by jobsModel for the GUI table
     * @param column
     * @return
     */
    public String getValue(int column) {
        if(column == 0) {
            return name;
        }
        else if(column == 1) {
            return Integer.toString(aborted);
        }
        else if(column == 2) {
            return Integer.toString(remain);
        }
        else if(column == 3) {
            return Integer.toString(done);
        }
        else if(column == 4) {
            if(status.equals("a"))
                return "ready";
            else if(status.equals("b") || status.equals("c"))
                return "running";
            else if(status.equals("d"))
                return "done";
            else
                throw new IllegalArgumentException(status);
        }
        else
            throw new IllegalArgumentException(Integer.toString(column));
    }
}
