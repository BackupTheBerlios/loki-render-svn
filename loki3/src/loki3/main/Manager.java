/**
 *Project: Loki Render - A distributed job queue manager.
 *Version 0.6.0
 *Copyright (C) 2009 Daniel Petersen
 *Created on Aug 8, 2009, 8:09:39 PM
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
 *This is the central control point for all job and grunt management
 * @author daniel
 */
public class Manager {
    final JobsModel jobsModel;

    /**
     *default constructor: creates a new JobsModel, jobsModel.
     */
    Manager() {
        jobsModel = new JobsModel();
    }

    /**
     * used by managerFrame to get handle on jobsModel
     * @return
     */
    public JobsModel getJobsModel() {
        return jobsModel;
    }

    /**
     * passes new job to jobsModel
     * @param j
     */
    void addJob(Job j) {
        jobsModel.addJob(j);
    }
}
