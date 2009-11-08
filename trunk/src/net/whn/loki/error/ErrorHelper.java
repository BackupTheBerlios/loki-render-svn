/**
 *Project: Loki Render - A distributed job queue manager.
 *Version 0.6.0
 *Copyright (C) 2009 Daniel Petersen
 *Created on Oct 30, 2009
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

package net.whn.loki.error;

import java.util.logging.Logger;
import javax.swing.JOptionPane;
import net.whn.loki.common.LokiForm;

/**
 *
 * @author daniel
 */
public class ErrorHelper {

    public static void outputToLogAndMsg(LokiForm form, Logger log,
            String text, Throwable t) {
        log.severe(text + t.toString() + "\n" + t.getMessage());
        JOptionPane.showMessageDialog(form,
                text + t.toString() + "\n" + t.getMessage(),
                "Fatal Error",
                JOptionPane.ERROR_MESSAGE);

        System.exit(-1);
    }
}
