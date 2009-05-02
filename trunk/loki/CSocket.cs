// Project: Loki Render - A distributed job queue manager.
// Version: 0.5
// 
// File Description: abstracts socket use, checks for errors, and provides
// some other useful functions for the socket, like 'checkIfConnected..'
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
// ﻿
	
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics; //for debug

namespace loki
{
    public class CSocket
    {
        //TcpClient client;
		Socket client;
        NetworkStream stream;
        Byte[] sendBuffer;
        Byte[] receiveBuffer;
        int bufferSize, bytes;

        //this constructor is for Listener - it passes an already connected TcpClient object
        //public CSocket(TcpClient tClient, int bSize)
		public CSocket(Socket tClient, int bSize)
		{
            bufferSize = bSize;
            client = tClient;
            client.NoDelay = true;
			stream = new NetworkStream(client);
            //stream = client.GetStream();
            stream.WriteTimeout = 5000; //only set a timeout for the write
        }

        //this constructor is for RemoteClient - we connect later.
        public CSocket(int bSize)
        {
            bufferSize = bSize;
        }

        /// <summary>
        /// connect to the specified remote node
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="port"></param>
        /// <returns>true if no errors, false if failed!</returns>
        public bool connect(IPAddress addr, int port)
        {
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(addr, port);
                client.NoDelay = true;
				stream = new NetworkStream(client);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine("IOException: {0}", ex);
                return false;
            }
            return true;   
        }

		//returns the amount of bytes received in the stream; zero if none, or -1 if error (probably lost connection)
        public int check4Message()
        {
            int result = 0;
            try
            {
                result = client.Available;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
                result = -1;
                close();
            }
            return result;
        }
		
		//this will only detect a closed connection if rC closed it gracefully! not unplugged cable, etc.
		public bool checkIfConnected()
		{
			try
			{
				if(client.Poll(1, SelectMode.SelectRead))
				{ //if true, then either we have data, or connection was closed by client
					if(client.Available < 1)
					{ //no data, so connection was closed by client!
						close();
						return false;
					}
				}
			}
			catch (SocketException ex)
            {
                close();
                return false;
            }
			return true; //connection with rC is ok
		}

		//If there is no message, this method will check for a lost connection every 100 milliseconds.
		//if connection is lost, it returns the string "lost", else, the received string, when it comes
        public string readStream()
        {
            byte[] msgSize = new Byte[1];
            receiveBuffer = new Byte[bufferSize];
			//Debug.WriteLine("cSock: entering readStream...");
			bool lost = false;
            try
            {
				bool gotMsg = false;
				do
				{
					if(client.Poll(1, SelectMode.SelectRead))
					{ //if true, then either we have data, or connection was closed by client
						if(client.Available < 1)
						{ //no data, so connection was closed by client!
							close();
							lost = true;
							break;
						}
						else//we have a message, so grab it.
						{
						stream.Read(msgSize, 0, 1);//this first byte tells us how many bytes in the message
						bytes = stream.Read(receiveBuffer, 0, (int)msgSize[0]);//grab the specified bytes
						gotMsg = true;
		                if (bytes != (int)msgSize[0])
		                    throw new IOException();
						}
					}
					else//no data, but connection is good, so wait 100 ms, then check again
						Thread.Sleep(100);
					
				}while(!gotMsg);
            }
            catch (IOException ex)
            {
                close();
                lost = true;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
				close();
				lost = true;
            }
			//Debug.WriteLine("cSock: done w/ readStream");
			
			if(lost)
				return "lost";
			else
				return Encoding.ASCII.GetString(receiveBuffer, 0, bytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sMsg"></param>
        /// <returns>true if send succeeded, false if failed</returns>
        public bool writeStream(string sMsg)
        {
			//Debug.WriteLine("cSock: entering writeStream...");
            byte[] msgSize = new Byte[1];
            msgSize[0] = (byte)sMsg.Length;
            sendBuffer = new Byte[bufferSize];
            sendBuffer = System.Text.Encoding.ASCII.GetBytes(sMsg);
            try
            {
                stream.Write(msgSize, 0, 1);
                stream.Write(sendBuffer, 0, sMsg.Length);
            }
            catch (IOException ex)
            {
                close();
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
            }
            
			//Debug.WriteLine("cSock: done w/ writeStream");
            return true;    
        }

        public void close() //td - need more logic in here to take certain steps if certain conditions
        {
            if (client.Connected)
            {
                stream.Close();
                client.Close();
            }
        }

        public string[] convert2Tokens(string msg)
        {
            string[] tokens = msg.Split('*');
            return tokens;
        }
    }
}

