/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */

package loki3.main;

/**
 *
 * @author daniel
 */
public class Main {

    /**
     * @param args the command line arguments
     */
    public static void main(String[] args) {
        Manager manager = new Manager();
        ManagerJFrame managerFrame = new ManagerJFrame(manager);
        managerFrame.setVisible(true);
    }

}
