// Project: Loki Render - A distributed job queue manager.
// Version: 0.5
// 
// File Description: two parts: listener accepts new remote client connections
// and creates a new client object in the clients list. broadcaster sends out
// a broadcast packet on the LAN so that grunts (remote clients) can find master
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Diagnostics; //for debug

namespace loki
{
    class Listener
    {
        Thread lThread, bThread;
		Socket listenerSock;
        int listenerPort, broadcastPort;
        Queue q;
		bool shutdown;
		int backlog = 500; //the amount of pending connections the listener will queue
        int bInterval;  //in seconds
		int bufferSize;
        public AutoResetEvent shutdownEvent;
		int timeout;//how long should the clientThread wait for a shutdown notice
			                  //the shorter it is, the more responsive to cycle loop, but will also take
			                  //more CPU cycles. If 0, it would 100% CPU!

        //normal constructor
        public Listener(Queue queue, int p, int interval, int t, MasterWin win)
        {
            q = queue;
            listenerPort = p;
			broadcastPort = 26278;
            bInterval = interval;
			timeout = t;
			shutdown = false;
			bufferSize = 1024;

            shutdownEvent = new AutoResetEvent(false);
			
			string mCheckResult = check4Master();
			if(mCheckResult != "silence")//if true, we already have a master!
			{
				win.invokeModalMsg("warning", "There is already a Loki master running on this network at '" +
				                   mCheckResult + "'!\r\n\r\n You need to:\r\n\r\n" +
				                   "a) restart Loki as a grunt on '" + mCheckResult + "' \r\n" +
				                   "and then restart Loki as a master on this computer, OR\r\n\r\n" +
				                   "b) restart Loki as a grunt on this computer.\r\n\r\n" +
				                   "In either case, you need to quit and restart Loki on this computer!");
		    }
			else
			{
	            try
	            {
					listenerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenerPort);
					listenerSock.Bind(ep);
					listenerSock.Listen(backlog);
					lThread = new Thread(listenerThread);   //td - error checking for thread stuff?
            		bThread = new Thread(broadcastThread);   //td - error checking for thread stuff?
					bThread.IsBackground = true;    //so that it will just die when all foreground threads are finished
					lThread.Start();
            		bThread.Start();
	            }
	            catch (Exception e)
	            {
	                Console.WriteLine("Exception: {0}", e);
	            }
			}
        }

		//constructor for special case of both master and grunt being local
        public Listener(Queue queue, int p, int interval, int t, MasterWin win, RemoteClient rC)
        {
            q = queue;
            listenerPort = p;
			broadcastPort = 26278;
            bInterval = interval;
			timeout = t;
			shutdown = false;
			bufferSize = 1024;

            shutdownEvent = new AutoResetEvent(false);
			
			string mCheckResult = check4Master();
			if(mCheckResult != "silence")//if true, we already have a master!
			{
				win.invokeModalMsg("warning", "There is already a Loki master running on this network at '" +
				                   mCheckResult + "'!\r\n\r\n You need to:\r\n\r\n" +
				                   "a) restart Loki as a grunt on '" + mCheckResult + "' \r\n" +
				                   "and then restart Loki as a master on this computer, OR\r\n\r\n" +
				                   "b) restart Loki as a grunt on this computer.\r\n\r\n" +
				                   "In either case, you need to quit and restart Loki on this computer!");
		    }
			else
			{
	            try
	            {
					listenerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenerPort);
					listenerSock.Bind(ep);
					listenerSock.Listen(backlog);
					rC.setMasterOk();	//tell the local rC that it can connect to me
					lThread = new Thread(listenerThread);   //td - error checking for thread stuff?
            		bThread = new Thread(broadcastThread);   //td - error checking for thread stuff?
					lThread.Start();
            		bThread.Start();
	            }
	            catch (Exception e)
	            {
	                Console.WriteLine("Exception: {0}", e);
	            }
			}
        }
		
        public void listenerThread()
        {			
			Socket nextSock;
            while (!shutdown)
            {
                if (shutdownEvent.WaitOne(100, false))
                {
                    shutdown = true;
                }
                else  //no shutdown signal, so business as usual...
                {
                    if (listenerSock.Poll(100, SelectMode.SelectRead))   //do we have a new connection?
                    {
                        try
                        {
							nextSock = listenerSock.Accept();
							nextSock.NoDelay = true;
                            CSocket mySock = new CSocket(nextSock, bufferSize);
                            lock (q.clientsLock)
                            {
                                q.clients.Add(new Client(q, mySock, timeout));
                            }
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine("SocketException: {0}", e);
                        }
                    }
                }//end while
            }
			listenerSock.Close();
			
			Debug.WriteLine("lThread is exiting");
        }

        /// <summary>
        /// sends a broadcast so the slaves can find the master.
        /// In it's simplest, it's just an empty packet so they can pull IP
        /// but later I might add other useful info like path info or something...
        /// exit: is passed a signal on shutdown
        /// </summary>
        public void broadcastThread()  //interval in seconds
        {
            Byte[] sendBytes = Encoding.ASCII.GetBytes("test");
            
            //td - setup broadcast socket stuff here
            UdpClient bCastClient = new UdpClient();
            bCastClient.EnableBroadcast = true;
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

            while (!shutdown)
            {
                bCastClient.Send(sendBytes, sendBytes.Length, RemoteIpEndPoint);
                Thread.Sleep(bInterval * 1000);
            }
			Debug.WriteLine("bT exiting");
        }
		
		static string check4Master()
		{
			IPEndPoint iep;
            EndPoint ep;
			Socket uSock;
			IPAddress destAddr;
			byte[] data;
			string masterExists = "silence";
			
            try
            {
                uSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                iep = new IPEndPoint(IPAddress.Any, 26278);
                uSock.Bind(iep);
                ep = (EndPoint)iep;
                data = new byte[1024];
			}
			catch (SocketException ex)
            {
				Console.WriteLine("SocketException: {0}", ex);
                throw new SanityFailureException("failed to bind to port!");
            }
			try
			{
				int result = 0;
				
	            Thread.Sleep(1000);//this is the interval length of master broadcasts
				try
	            {
	                result = uSock.Available;
	            }
	            catch (SocketException ex)
	            {
	                Console.WriteLine("SocketException: {0}", ex);
	                result = -1;
		            uSock.Close();
	            }
				
				if(result > 0)
				{	 //oops, there's already a master on the network!
					uSock.ReceiveFrom(data, ref ep);  //we don't really care about this string
					uSock.Close();
					iep = (IPEndPoint)ep;
            		destAddr = iep.Address;      //we just want the address!
					masterExists = destAddr.ToString();
					Debug.WriteLine(masterExists);
				}

            }
            catch (SocketException ex)
            {
				Console.WriteLine("SocketException: {0}", ex);
				throw new SanityFailureException("failed to bind to bListen port!");				
            }
            return masterExists;	
		}
    }
}
