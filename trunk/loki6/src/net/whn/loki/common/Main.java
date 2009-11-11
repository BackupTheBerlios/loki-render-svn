/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package net.whn.loki.common;

import net.whn.loki.error.DefaultExceptionHandler;
import net.whn.loki.IO.IOHelper;
import net.whn.loki.CL.CLHelper;
import java.awt.EventQueue;
import java.awt.Point;
import java.io.File;
import net.whn.loki.master.*;
import java.io.IOException;
import java.util.logging.ConsoleHandler;
import java.util.logging.FileHandler;
import java.util.logging.Handler;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.logging.SimpleFormatter;
import net.whn.loki.grunt.*;
import javax.swing.JOptionPane;
import javax.swing.UIManager;
import net.whn.loki.common.ICommon.LokiRole;
import net.whn.loki.error.ErrorHelper;

/**
 *
 * @author daniel
 */
public class Main {

    /**
     * @param args the command line arguments
     */
    public static void main(String[] args) {
        /**
         * TODO -command line args here:
         * -gruntcl
         */
        LokiForm lokiForm = new LokiForm();
        lokiCfgDir = IOHelper.setupLokiCfgDir();
        setNativeLAF();
        defaultHandler = new DefaultExceptionHandler(lokiForm);
        Thread.setDefaultUncaughtExceptionHandler(defaultHandler);

        try {
            boolean launch = true;

            if (IOHelper.setupRunningLock(lokiCfgDir)) {
                Object[] options = {"Start", "Quit"};
                int result = JOptionPane.showOptionDialog(
                        lokiForm,
                        alreadyRunningText,
                        "Already running, or improper shutdown",
                        JOptionPane.YES_NO_OPTION,
                        JOptionPane.WARNING_MESSAGE,
                        null,
                        options,
                        options[0]);

                if (result != 0) {
                    launch = false;
                }
            }
            
            if (launch) {
                if (lokiCfgDir == null) {
                    showMsg(lokiForm,
                            "Please give Loki read/write permissions to the\n" +
                            "directory: '" + System.getProperty("user.home") + "'\n" +
                            "and restart Loki.");
                    log.severe("filesystem is not writable. Loki exiting.");
                } else {  //filesystem is writable so continue
                    setupLogging();
                    try {
                        cfg = Config.readCfgFile(lokiCfgDir);
                        startLoki(lokiForm);

                    } catch (IOException ex) {
                        //fatal error during Announce startup
                        ErrorHelper.outputToLogMsgAndKill(lokiForm, log,
                                "Loki encountered a fatal error.\n" +
                                "Click OK to exit.", ex);
                        System.exit(-1);
                    }
                }
            }
        } catch (IOException ex) {
            JOptionPane.showMessageDialog(lokiForm,
                    "Loki Render is having IO problems with the filesystem:\n" +
                    ex.getMessage() +
                    "\nClick OK to exit.");
        }
        lokiForm.dispose();
    }

    /*BEGIN PRIVATE*/
    //general
    private static DefaultExceptionHandler defaultHandler;
    private static File lokiCfgDir; //base path for '.lokiconf' dir
    private static LokiRole myRole;
    private static int masterMessageQueueSize = 100;
    private static Config cfg = null;
    private static String alreadyRunningText =
            "Loki is already running, or wasn't properly shutdown.\n" +
            "If Loki isn't running, you can safely select 'Start'";
    //master
    private static MasterR master;
    private static Thread masterThread;
    private static MasterForm masterForm;
    //grunt
    private static GruntR grunt;
    private static Thread gruntReceiverThread;
    private static GruntForm gruntForm;
    //logging
    private static final String className = "net.whn.loki.common.Main";
    private static final Logger log = Logger.getLogger(className);

    /**
     *
     * @param hiddenForm
     * @throws IOException
     * -startMaster() *FATAL*
     */
    private static void startLoki(LokiForm hiddenForm) throws IOException {

        if (cfg.getRole() == LokiRole.ASK) {
            int role = getRole(hiddenForm);
            if (role == 0) {
                myRole = LokiRole.GRUNT;
            } else if (role == 1) {
                myRole = LokiRole.MASTER;
            } else if (role == 2) {
                myRole = LokiRole.MASTER_GRUNT;
            } else {
                log.fine("quit dialog; exiting.");
                System.exit(0);
            }
        } else {
            myRole = cfg.getRole();
        }


        if (myRole == LokiRole.GRUNT || myRole == LokiRole.MASTER_GRUNT) {
            if (!CLHelper.determineBlenderBin(cfg)) {
                System.exit(0);
            }
        }

        if (myRole == LokiRole.GRUNT) {
            startGrunt(null, null);
        } else if (myRole == LokiRole.MASTER) {
            startMaster(hiddenForm, false);
        } else if (myRole == LokiRole.MASTER_GRUNT) {
            startMaster(hiddenForm, true);
        } else //window was closed
        {
            log.fine("unexpected response for getRole:" + myRole + ". exiting.");
            System.exit(0);
        }
    }

    /**
     * TODO - this would probably be better w/ a form w/ tooltips
     * @return
     */
    private static int getRole(LokiForm hiddenForm) {
        String[] options = {"Grunt", "Master", "Master and Grunt"};

        int response = JOptionPane.showOptionDialog(hiddenForm,
                "Please select Loki's role on this computer.",
                "Loki role",
                0,
                JOptionPane.QUESTION_MESSAGE,
                null,
                options,
                options[0]);

        return response;
    }

    private static void setNativeLAF() {
        try {
            UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
            log.logp(Level.FINEST, className, "setNativeLAF()",
                    "Successfully set native look and feel for GUI.");
        } catch (Exception ex) {
            //we can live w/ ugly 'metal' theme...moving on
        }
    }

    /**
     *
     * @param hiddenForm
     * @param localGrunt
     * @throws IOException
     * -from AnnounceR() *FATAL*
     */
    private static void startMaster(LokiForm hiddenForm, boolean localGrunt)
            throws IOException {
        AnnouncerR announcer;
        announcer = new AnnouncerR(cfg, hiddenForm);
        master = new MasterR(lokiCfgDir, cfg, announcer,
                masterMessageQueueSize);

        masterThread = new Thread(master);
        masterThread.setName("master");

        masterForm = new MasterForm(master);
        master.setMasterForm(masterForm);

        masterThread.start();
        masterForm.setVisible(true);

        //we'll have a local grunt so we need to start it and tell it we're here
        if (localGrunt) {
            Point p = masterForm.getLocation();
            Point gPoint = new Point(p.x, (p.y + masterForm.getHeight()));
            startGrunt(master, gPoint);
        }
    }

    private static void startGrunt(MasterR master, Point myPoint) {
        grunt = new GruntR(master, lokiCfgDir, cfg);
        gruntReceiverThread = new Thread(grunt, "grunt");

        gruntForm = new GruntForm(grunt);
        grunt.setGruntForm(gruntForm);
        if (myPoint == null) {
            gruntForm.setLocationRelativeTo(null);
        } else {
            gruntForm.setLocation(myPoint);
        }

        gruntReceiverThread.start();
        gruntForm.setVisible(true);
    }

    private static void showMsg(final LokiForm hiddenForm, final String msg) {
        EventQueue.invokeLater(new Runnable() {

            @Override
            public void run() {
                JOptionPane.showMessageDialog(hiddenForm, msg);
            }
        });
    }

    private static void setupLogging() {
        String logAbsolutePath;
        if (System.getProperty("java.util.logging.config.class") ==
                null && System.getProperty("java.util.logging.config.file") == null) {
            logAbsolutePath = lokiCfgDir.getAbsolutePath() +
                    File.separator + "loki.log";
            try {
                //Logger.getLogger("net.whn.loki").setLevel(Level.ALL);
                final int LOG_ROTATION_COUNT = 2;
                final int fileSize = 500000;
                Handler fHandler = new FileHandler(logAbsolutePath, fileSize,
                        LOG_ROTATION_COUNT, true);
                fHandler.setFormatter(new SimpleFormatter());
                Logger.getLogger("").addHandler(fHandler);
            } catch (IOException ex) {
                log.log(Level.SEVERE, "Can't create log file handler", ex);
            }


            /**
             * set default console handler to finest -TODO not for production
             */
            // Handler for console (reuse it if it already exists)
            Handler consoleHandler = null;
            //see if there is already a console handler
            for (Handler handler : Logger.getLogger("").getHandlers()) {
                if (handler instanceof ConsoleHandler) {
                    //found the console handler
                    consoleHandler = handler;
                    break;
                }
            }


            if (consoleHandler == null) {
                //there was no console handler found, create a new one
                consoleHandler = new ConsoleHandler();
                Logger.getLogger("").addHandler(consoleHandler);
            }
            //set the console handler to fine:
            consoleHandler.setLevel(java.util.logging.Level.FINEST);


            //last of all, set this class's log to log all
            //log.setLevel(Level.ALL);

        }
    }
}
