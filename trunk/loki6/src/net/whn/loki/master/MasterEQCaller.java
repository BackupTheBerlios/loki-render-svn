/**
 *Project: Loki Render - A distributed job queue manager.
 *Version 0.6.0
 *Copyright (C) 2009 Daniel Petersen
 *Created on Sep 10, 2009
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

package net.whn.loki.master;

import net.whn.loki.common.*;
import java.awt.EventQueue;

/**
 *
 * @author daniel
 */
public class MasterEQCaller extends EQCallerA {

    /**
     * tell UI we're stopping the queue
     * @param mForm
     */
    public static void invokeStop(final MasterForm mForm) {
        EventQueue.invokeLater(new Runnable()
        {
            public void run()
            {
                mForm.stopQueue();
            }
        });
    }

    /**
     * update the totalCore count on the UI
     * @param mForm
     * @param cores
     */
    public static void invokeUpdateCores(final MasterForm mForm,
            final int cores) {
        EventQueue.invokeLater(new Runnable()
        {
            public void run()
            {
                mForm.updateCores(cores);
            }
        });
    }

    public static void invokeViewGruntDetails(final MasterForm mForm,
            final GruntDetails details) {
        EventQueue.invokeLater(new Runnable()
        {
            public void run()
            {
                mForm.viewGruntDetails(details);
            }
        });
    }

    public static void invokeViewJobDetails(final MasterForm mForm,
            final Job job) {
        EventQueue.invokeLater(new Runnable()
        {
            public void run() {
                mForm.viewJobDetails(job);
            }
        });
    }

    /**
     * update the task progress bar
     * @param mForm
     * @param update
     */
    public static void invokeUpdatePBar(final MasterForm mForm,
            final ProgressUpdate update) {

        uPB = new UpdateProgressBar(mForm, update);

        EventQueue.invokeLater(uPB);
    }

    /*BEGIN PRIVATE*/

    private static UpdateProgressBar uPB;

    private static class UpdateProgressBar implements Runnable {
        UpdateProgressBar(MasterForm mF, ProgressUpdate u) {
           mForm = mF;
           update = u;
        }

        public void run()
        {
            mForm.updateProgressBar(update);
        }

        private final MasterForm mForm;
        private final ProgressUpdate update;
    }
}
