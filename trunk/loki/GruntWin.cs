// Project: Loki Render - A distributed job queue manager.
// Version: 0.5.1
// 
// File Description: simple GUI for the grunt (remote client)  
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
using Gtk;

namespace loki
{
	public partial class GruntWin : Gtk.Window
	{
		RemoteClient rC;
		bool solo;
		
		public GruntWin(bool s) : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			solo = s; //Am I running by myself on this computer, or with Master?
			pb1.PulseStep = 0.05;
		}
		
		protected virtual void OnDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			bool quit = true;
            if(rC.getBusyStatus())
			{
				MessageDialog md = new MessageDialog (this, 
	            DialogFlags.DestroyWithParent, MessageType.Question, 
	            ButtonsType.YesNo, "Quitting now will abort the currently running task. Quit?");
		
				ResponseType result = (ResponseType)md.Run ();

				if (result == ResponseType.Yes)
				{	
					//yes, we're going to quit            
				}
				else
				{
					md.Destroy();
					quit = false;
				}
			}
			
			if(quit)
			{
				this.Destroy();
				rC.signalShutdown();
				if(solo)
					Application.Quit();
			}
			else
			{
				args.RetVal = true;
			}
		}
		
		//increment the progress bar
		public void invokePulse()
		{
			Application.Invoke (delegate {
				pb1.Pulse();	
			});
		}
		
		//set the progress bar blank
		public void invokeBlank()
		{
			Application.Invoke (delegate {
				pb1.Fraction = 0;	
			});
		}
		
		//change the connection label
		public void invokeSetLblConnection(string newText)
		{
			Application.Invoke (delegate { 
				lblConnection.Text = newText;
			});
		}
		
		//change the status label
		public void invokeSetLblStatus(string newText)
		{
			Application.Invoke (delegate { 
				lblStatus.Text = newText;
			});
		}
		
		//need this so the gui can talk to the remoteClient object
		public void setRCHandle(RemoteClient rC)
		{
			this.rC = rC;
		}	
	}
}
