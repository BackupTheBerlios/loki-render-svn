/**
 *Project: Loki Render - A distributed job queue manager.
 *Version 0.6.0
 *Copyright (C) 2009 Daniel Petersen
 *Created on Sep 2, 2009
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
package net.whn.loki.IO;

import net.whn.loki.common.*;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.math.BigInteger;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Iterator;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.logging.Logger;
import java.util.zip.Deflater;
import java.util.zip.DeflaterOutputStream;
import javax.swing.ProgressMonitorInputStream;

/**
 *
 * @author daniel
 */
public class IOHelper {

    /**
     * creates the _lokiconf dir in the user's home dir if it doesn't already
     * exist, also checks read/write permissions to make sure we're ok to
     * proceed w/ all other filesystem activities.
     * same stuff for child dir 'fileCache' as well
     * @return File of lokiBaseDir, null if failed somewhere
     */
    public static File setupLokiCfgDir() {
        File lokiBaseDir;
        File fileCacheDir;
        File tmpDir;
        String lokiConfDir = ".loki";

        //first let's retrieve the user's home directory
        String userHomeDir = System.getProperty("user.home");

        if (userHomeDir != null) {
            lokiBaseDir = new File(userHomeDir, lokiConfDir);
            log.finest("lokiBaseDir: " + lokiBaseDir.getAbsolutePath());
        } else {
            log.severe("couldn't retrieve user.home path!");
            return null;
        }

        //now check if base dir already exists; if not, create it
        if (!lokiBaseDir.isDirectory()) {
            //doesn't exist; create it
            if (!lokiBaseDir.mkdir()) {
                log.severe("couldn't create directory:" + lokiConfDir);
                return null;
            }
        }

        //now check if it's writable for files and directories
        if (!isDirWritable(lokiBaseDir)) {
            log.severe("couldn't write to directory: " + lokiConfDir);
            return null;
        }

        fileCacheDir = new File(lokiBaseDir, "fileCache");

        //now let's check if fileCache dir already exists; if not create it
        if (!fileCacheDir.isDirectory()) {
            //doesn't exist; create it
            if (!fileCacheDir.mkdir()) {
                log.severe("couldn't create directory:" + fileCacheDir.toString());
                return null;
            }
        }

        if (!isDirWritable(fileCacheDir)) {
            log.severe("couldn't write to directory:" + fileCacheDir.toString());
            return null;
        }

        tmpDir = new File(lokiBaseDir, "tmp");

        //now let's check if tmp dir already exists; if not create it
        if (!tmpDir.isDirectory()) {
            //doesn't exist; create it
            if (!tmpDir.mkdir()) {
                log.severe("couldn't create directory:" + tmpDir.toString());
                return null;
            }
        }

        if (!isDirWritable(tmpDir)) {
            log.severe("couldn't write to directory:" + tmpDir.toString());
            return null;
        }

        //everything checked out, return
        return lokiBaseDir;
    }

    public static void deleteRunningLock(File lokiCfgDir) {
        File runningFile = new File(lokiCfgDir, ".runningLock");
        if(runningFile.exists()) {
            runningFile.delete();
        }
    }

    /**
     * generates MD5 for given file.
     * @param oFile
     * @return md5 as hex string, or null if failed
     * @throws IOException
     */
    public static String generateMD5(File oFile) throws IOException {
        byte[] buffer = new byte[BUFFER_SIZE];
        InputStream inFile = null;
        MessageDigest digest = null;

        try {
            digest = MessageDigest.getInstance("MD5");
            inFile = new FileInputStream(oFile);

            int amountRead;
            while (true) {
                amountRead = inFile.read(buffer);
                if (amountRead == -1) {
                    break;
                }
                digest.update(buffer, 0, amountRead);
            }
            return binToHex(digest.digest());

        } catch (NoSuchAlgorithmException ex) { //TODO what to do?
            log.severe("md5 algorithm not available!");
        } catch (FileNotFoundException ex) {    //TODO what to do?
            log.severe("file not found: " + ex.getMessage());
        } finally {
            if (inFile != null) {
                inFile.close();
            }
        }
        return null;
    }

    public static long getFileCacheSize(
            ConcurrentHashMap<String, ProjFile> fCMap) {
        long total = 0;
        ProjFile currentpFile;
        Iterator it = fCMap.entrySet().iterator();
        Map.Entry pair;
        while (it.hasNext()) {
            pair = (Map.Entry) it.next();
            currentpFile = (ProjFile) pair.getValue();
            total += currentpFile.getSize();
        }
        return total;
    }

    /**
     *
     * @param lokiCfgDir
     * @return false if .runningLock file didn't exist, false otherwise
     * @throws IOException
     */
    public static boolean setupRunningLock(File lokiCfgDir) throws IOException {
        File runningLock = new File(lokiCfgDir, ".runningLock");

        if (runningLock.createNewFile()) {
            runningLock.deleteOnExit();
            return false;
        } else {
            return true;   //oops; loki is already running on this system
        }
    }

    /*PROTECTED*/
    protected static final int BUFFER_SIZE = 8192;
    protected static long start;

    /**
     * converts bytes to a hex string
     * @param bytes
     * @return
     */
    protected static String binToHex(byte[] bytes) {
        BigInteger bi = new BigInteger(1, bytes);
        return String.format("%0" + (bytes.length << 1) + "X", bi);
    }

    /**
     * adds a previously copied tmp file into the cache (if it's unique)
     * @param fileCacheMap
     * @param md5
     * @param lokiCacheDir
     * @param tmpCacheFile
     */
    protected static void addTmpToCache(
            ConcurrentHashMap<String, ProjFile> fileCacheMap,
            String md5, File lokiCacheDir, File tmpCacheFile, Config cfg)
            throws IOException {

        File md5File = null;
        ProjFile pFile = null;

        //check if file already exists in cache
        if (!fileCacheMap.containsKey(md5)) {
            log.finest("unique md5; adding to cache");
            //new file, so rename and add to map:

            //rename file
            md5File = new File(lokiCacheDir, md5);
            if (!tmpCacheFile.renameTo(md5File)) {
                log.severe("failed to rename CacheFile: " +
                        tmpCacheFile.getAbsolutePath() + " to " +
                        md5File.getAbsolutePath());
                throw new IOException("failed to rename CacheFile!");
            }

            //create new ProjFile object
            pFile = new ProjFile(md5File, md5);

            //insert it into the fileCache
            fileCacheMap.put(md5, pFile);

            if (pFile.getSize() > cfg.getCacheSizeLimitBytes()) {
                cfg.setCacheSizeLimitBytes((pFile.getSize() * 4));
                log.info("increasing cache limit to: " + pFile.getSize() * 4);
            }
            manageCacheLimit(fileCacheMap, cfg);
        } else {
            //file is already in cache, so delete tmp file
            tmpCacheFile.delete();
            //that's all we need to do. md5 string will be returned and
            //placed in job
            log.finest("md5 not unique; we already have the file!");
        }
    }

    /*PRIVATE*/
    //logging
    private static final String className =
            "net.whn.loki.common.LokiFileHelper";
    private static final Logger log = Logger.getLogger(className);
    //for file I/O
    private static ProgressMonitorInputStream pmin = null;
    private static DeflaterOutputStream dout = null;
    private final static Deflater fastDeflater = new Deflater(1);

    /**
     * should be called after a new file has been added to the cache.
     * if we're over the limit, should iteratively remove oldest used files
     * until we meet the limit constraint.
     */
    private static void manageCacheLimit(
            ConcurrentHashMap<String, ProjFile> fileCacheMap, Config cfg) {

        //we need to delete files using a "longest ago used" algorithm
        //until we fall under the limit
        if (!fileCacheMap.isEmpty()) {
            while (cfg.getCacheSize() > cfg.getCacheSizeLimitBytes()) {
                ProjFile oldestPFile = null;
                Iterator it = fileCacheMap.entrySet().iterator();
                long lowestTime = System.currentTimeMillis() + 1000000;
                Map.Entry pair;

                while (it.hasNext()) {
                    pair = (Map.Entry) it.next();
                    ProjFile currentPFile = (ProjFile) pair.getValue();
                    if (currentPFile.getTimeLastUsed() < lowestTime) {
                        oldestPFile = currentPFile;
                        lowestTime = oldestPFile.getTimeLastUsed();
                    }
                }
                //we now have our delete candidate, so delete it.

                if (!oldestPFile.isInUse() &&
                        cfg.getJobsModel().isPFileOrphaned(
                        oldestPFile.getMD5())) {
                    if (!oldestPFile.getProjFile().delete()) {
                        log.severe("failed to delete cache file");
                    }
                    fileCacheMap.remove(oldestPFile.getMD5());
                    log.finer("deleting file: " + oldestPFile.getMD5());
                } else {
                    log.fine("manageCacheLimit wanted to delete file in use!");
                    break;
                }
            }
        }
    }

    private static boolean isDirWritable(File bDir) {
        boolean ok = true;

        String tDir = "lokiDir";

        try {
            if (!bDir.isDirectory()) {
                ok = false;
            }
            //can I write a file to current working dir?
            File tempFile = new File(bDir, "loki.tmp");

            if (!tempFile.createNewFile()) {
                ok = false; //couldn't create file
            } else {  //file was created
                if (!tempFile.delete()) {
                    ok = false; //couldn't delete the file
                }
            }

            if (ok) {
                //can I write a dir to current working dir?
                File tempDir = new File(bDir, tDir);
                if (!tempDir.mkdir()) {
                    ok = false; //couldn't create dir
                } else {  //dir was created
                    if (!tempDir.delete()) {
                        ok = false; //couldn't delete dir
                    }
                }
            }
        } catch (IOException ex) {
            log.severe("couldnt write to directory!" +
                    ex.getMessage());
            ok = false;
        }

        return ok;
    }
}
