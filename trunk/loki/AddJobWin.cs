// Project: Loki Render - A distributed job queue manager.
// Version: 0.5.1
//
// File Description: This window validates, and creates a new job from user input	  
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
using System.Diagnostics;	//for debug

namespace loki
{
	public partial class AddJobWin : Gtk.Window
	{
		string name, type, winExe, winFile, winOutputDir, unixExe, unixFile, unixOutputDir;
		int firstFrame, lastFrame, allowedFailures;
		bool edit;
		Queue q;
		
		public AddJobWin(Queue q, bool e) : base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			this.q = q;
			edit = e;
		}
		
		/// <summary>
		/// show's a modal message box where the user can only click ok.
		/// </summary>
		/// <param name="type">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="msg">
		/// A <see cref="System.String"/>
		/// </param>
		void showModalMsg(string type, string msg)
		{
			MessageDialog md;
			
			switch (type)
			{
			case "info":
				md = new MessageDialog (this, DialogFlags.DestroyWithParent, MessageType.Info,
					       ButtonsType.Ok, msg);
				break;
			case "warning":
				md = new MessageDialog (this, DialogFlags.DestroyWithParent, MessageType.Warning,
					       ButtonsType.Ok, msg);
				break;
			case "error":
				md = new MessageDialog (this, DialogFlags.DestroyWithParent, MessageType.Error,
					       ButtonsType.Ok, msg);
				break;
			default:
				throw new SanityFailureException("unknown ModalMsg type requested: " + type);
			}
			
			md.Run ();
			md.Destroy();
				
		}
		
		/// <summary>
		/// modifies the addJobWin so we can use it as an edit window instead
		/// </summary>
		/// <param name="tType">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="winFP">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="winOP">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="unixFP">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="unixOP">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="firstF">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="lastF">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void setupEdit(string tType, string name, string winFP, string winOP,
		                      string unixFP, string unixOP, int firstF, int lastF)
		{
			eName.Text = name;
			eWinFile.Text = winFP;
			eWinOutputDir.Text = winOP;
			eUnixFile.Text = unixFP;
			eUnixOutputDir.Text = unixOP;
			eFirstFrame.Text = firstF.ToString();
			eLastFrame.Text = lastF.ToString();
			
			//td we're disabling these because we can't yet cope with these changes internally
			cBoxType.Sensitive = false;
			eName.Sensitive = false;
			eFirstFrame.Sensitive = false;
			eLastFrame.Sensitive = false;
			
		}

		/// <summary>
		/// check with the user before closing window and losing changes
		/// </summary>
		/// <param name="o">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="args">
		/// A <see cref="Gtk.DeleteEventArgs"/>
		/// </param>
		protected virtual void OnDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			MessageDialog md = new MessageDialog (this, 
            DialogFlags.DestroyWithParent, MessageType.Question, 
            ButtonsType.YesNo, "Close without saving Job?");
	
			ResponseType result = (ResponseType)md.Run ();

			if (result == ResponseType.Yes)
			{	
				            
			}
			else
			{
				md.Destroy();
				args.RetVal = true;
			}
		}

		/// <summary>
		/// performs various types of checks to validate user input
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		protected bool validateEntries()
		{
			bool valid = true;
			//input validation: 1. empty fields? 2. valid entries?
			//3. fields for relevant OSs filled in?
			
			////1. empty fields?
			//check if name field is empty
			if(eName.Text == "")
			{
				valid = false;
				showModalMsg("info", "Please fill in the 'Name' field.");
			}
			
			if(valid)
			{
				if(eWinFile.Text == "" || eWinOutputDir.Text == "")
				{
					if(q.anyClientsWithPlatform("Windows")){
						valid = false;
						
						showModalMsg("info", "Loki has detected at least one connected grunt with a "
						       + "Windows platform. Please fill in the relevant fields.");
					}	
				}
			}
			
			if(valid)
			{
				//if either unix entries aren't filled, check that we don't have any unix clients
				if(eUnixFile.Text == "" || eUnixOutputDir.Text == "")
				{
					if(q.anyClientsWithPlatform("Unix")){
						valid = false;
						showModalMsg("info", "Loki has detected at least one connected grunt with a "
						       + "Unix based platform. Please fill in the relevant fields.");
					}		
				}
			}
			
			if(valid)//make sure we have both file and output dir for either windows or unix
			{
				if(eWinFile.Text == "" || eWinOutputDir.Text == "")
				{
					if(eUnixFile.Text == "" || eUnixOutputDir.Text == "")
					{
						valid = false;
						showModalMsg("info", "Please fill in the 'Windows'"
	                    		+ " and/or 'Unix' fields, based on which platform(s) your grunts use.");
					}
				}
			}
			
			if(valid)//bunch more tests
			{
				//2. valid entries? 
				//make sure that firstFrame and lastFrame have valid integers, (and are not empty)
				try
				{
					firstFrame = System.Convert.ToInt32(eFirstFrame.Text);
					lastFrame = System.Convert.ToInt32(eLastFrame.Text);
				}
				catch(FormatException formatEx)
				{
					valid = false;
					showModalMsg("info", "Please enter valid integers in the 'Frame Range' fields");
				}
				if(valid)
				{
					if(!(lastFrame > firstFrame))
					{
						valid = false;
						showModalMsg("info", "'Last frame' must be greater than 'First frame'.");
					}
				}
			}
			return valid;
		}

		/// <summary>
		/// if validation succeeds, create a new job based on values
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="e">
		/// A <see cref="System.EventArgs"/>
		/// </param>
		protected virtual void OnBtnSaveClicked (object sender, System.EventArgs e)
		{
			if(validateEntries()) //we're ok, so let's grab all the entries
			{
				name = eName.Text;
				type = "blender";//td - shouldn't be hardcoded!
				winExe = "blender.exe";//td - shouldn't be hardcoded!
				unixExe = "blender";//td - shouldn't be hardcoded!
				allowedFailures = 3;//td - shouldn't be hardcoded!
				winFile = eWinFile.Text;
				winOutputDir = eWinOutputDir.Text;
				unixFile = eUnixFile.Text;
				unixOutputDir = eUnixOutputDir.Text;
				
				if(!edit)//this is an add job
				{
					q.deliverNotice(new Notice("add", name, type, winExe, winFile, winOutputDir, unixExe,
			                  unixFile, unixOutputDir, firstFrame, lastFrame, allowedFailures));
				}
				else//this is an edit job
				{
					q.deliverNotice(new Notice("commitEdit", name, type, winExe, winFile, winOutputDir, unixExe,
			                  unixFile, unixOutputDir, firstFrame, lastFrame, allowedFailures));	
				}
				
				this.Destroy();
			}
		}

		/// <summary>
		/// close the window without saving values
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="e">
		/// A <see cref="System.EventArgs"/>
		/// </param>
		protected virtual void OnBtnCancelClicked (object sender, System.EventArgs e)
		{
			this.Destroy();
		}
	}
}
