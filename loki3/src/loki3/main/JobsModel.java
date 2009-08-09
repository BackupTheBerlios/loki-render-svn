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

import java.util.ArrayList;
import javax.swing.table.AbstractTableModel;

/**holds an array of job objects and serves as the model
 * for the jobs queue table
 *
 * @author daniel
 */
public class JobsModel extends AbstractTableModel {
    ArrayList<Job> jobsList = new ArrayList<Job>();
    String[] columnHeaders;

    /**
     * initializes columnHeaders, from which is derived columnCount
     * NOTE: make sure to update Job.getValue() if you change headers
     */
    JobsModel() {
        columnHeaders = new String[] {
            "Name",
            "Aborted",
            "Remain",
            "Done",
            "Status"
        };
    }

    /**
     * adds given job to the end of jobsList, then calls fireTableRowsInserted()
     * @param j
     */
    void addJob(Job j) {
        //TODO IndexOutOfBoundsException here
        jobsList.add(j);
        int newRow = jobsList.size() - 1;
        fireTableRowsInserted(newRow, newRow); //tell AWT.EventQueue
    }

    /**
     * returns the current column count of the model
     * @return
     */
    public int getColumnCount() {
        return columnHeaders.length;
    }

    /**
     * returns the current row count of the model
     * @return
     */
    public int getRowCount() {
        return jobsList.size();
    }

    /**
     * fetches the column value on specified row (job)
     * @param row
     * @param column
     * @return string value
     */
    public String getValueAt(int row, int column) {
        return jobsList.get(row).getValue(column);
    }

    /**
     *
     * @param c
     * @return
     */
    @Override
    public String getColumnName(int c) {
        return columnHeaders[c];
    }

}
