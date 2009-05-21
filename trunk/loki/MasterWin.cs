// Project: Loki Render - A distributed job queue manager.
// Version: 0.5.1
// 
// File Description: Main window for master mode	  
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
using System.Threading;

namespace loki
{
	public partial class MasterWin: Gtk.Window
	{	
		ListStore gruntList;
		ListStore jobList;
		Queue q;
		int totalCores;
		static string lastSelectedJobName;
		
		public MasterWin (int r): base (Gtk.WindowType.Toplevel)
		{
			Build ();
			initializeGruntsTreeView();
			initializeJobsTreeView();
			jobsView.Selection.Changed += OnJobsSelectionChanged;
			lastSelectedJobName = null;
			totalCores = 0;
		}
		
		public void setQHandle(Queue q)
		{
			this.q = q;	
		}
		
		static void OnJobsSelectionChanged (object o, EventArgs args)
	    {
	        TreeIter iter;
	        TreeModel model;
	
	        if (((TreeSelection)o).GetSelected (out model, out iter))
	        {
				lastSelectedJobName = (string) model.GetValue (iter, 1);
	        }
	    }
		
		public void invokeAddJobToTV(int jobID, string name, string status, string remaining, 
			                             string aborted, string done)
		{
			Application.Invoke (delegate {
				jobList.AppendValues(jobID, name, aborted, remaining, done, status); 
			});
		}
		
		public void invokeUpdateJobTV(int jobID, string status, string remaining, string failed, string done)
		{  
			Application.Invoke (delegate {
				int result = -1;
				int r = 0;//store the row we find
				foreach(object[] row in jobList)
				{
					if(jobID == (int) row[0])
					{
						result = r;
						break;
					}
				    r++;
				}
				
				if(result != -1)
				{
					TreeIter myIter = new TreeIter();
					TreePath tPath = new TreePath(r.ToString());
					jobList.GetIter(out myIter, tPath);
					jobList.SetValue(myIter, 2, failed);
					jobList.SetValue(myIter, 3, remaining);
					jobList.SetValue(myIter, 4, done);
					jobList.SetValue(myIter, 5, status);
				}
				
			});
		}
		
		public void invokeUpdateProgress(int done, int total)
		{
			double frac;
			if(total > 0)
			{
				frac = (double) done / (double) total;
			}
			else
				frac = 0;
			
			string text = done + "/" + total;
			Application.Invoke (delegate {
				pbComplete.Fraction = frac;
				pbComplete.Text = text;
			});
		}
		
		public void invokeAddGruntToTV(int clientID, string name, string status, string os, string cores)
		{
			Application.Invoke (delegate {
				gruntList.AppendValues(clientID, name, os, cores, "", "ready");
				totalCores += Convert.ToInt32(cores, 10);
				lblTotalCores.Text = totalCores.ToString();
			});
		}
		
		public void invokeRemoveGruntTV(int clientID)
		{
			Application.Invoke (delegate {
				int result = -1;
				int r = 0;//store the row we find
				foreach(object[] row in gruntList)
				{      
					if(clientID == (int) row[0])
					{
						result = r;
						break;
					}
				    r++;
				}
				if(result != -1)
				{
					TreeIter myIter = new TreeIter();
					TreePath tPath = new TreePath(r.ToString());
					gruntList.GetIter(out myIter, tPath);
					string gruntsCores = gruntList.GetValue(myIter, 3).ToString();
					totalCores -= Convert.ToInt32(gruntsCores, 10);
					lblTotalCores.Text = totalCores.ToString();
					gruntList.Remove(ref myIter);
				}
			});
		}
		
		//for updating status and taskTime
		public void invokeUpdateGruntTV(int clientID, string status, string taskTime)
		{
			Application.Invoke (delegate
			{
				int result = -1;
				int r = 0;//store the row we find
				foreach(object[] row in gruntList)
				{
					if(clientID == (int) row[0])
					{
						result = r;
						break;
					}
				    r++;
				}
				if(result != -1)
				{
					TreeIter myIter = new TreeIter();
					TreePath tPath = new TreePath(r.ToString());
					gruntList.GetIter(out myIter, tPath);
					
					gruntList.SetValue(myIter, 4, taskTime);
					gruntList.SetValue(myIter, 5, status);
					//jobList.EmitRowChanged(tPath, myIter);
				}
			});		
		}
		
		//for updating the status
		public void invokeUpdateGruntTV(int clientID, string status)
		{  
			Application.Invoke (delegate
			{
				int result = -1;
				int r = 0;//store the row we find
				foreach(object[] row in gruntList)
				{
					if(clientID == (int) row[0])
					{
						result = r;
						break;
					}
				    r++;
				}
				if(result != -1)
				{
					TreeIter myIter = new TreeIter();
					TreePath tPath = new TreePath(r.ToString());
					gruntList.GetIter(out myIter, tPath);
					gruntList.SetValue(myIter, 5, status);
					//jobList.EmitRowChanged(tPath, myIter);
				}
			});
		}
		
		void initializeGruntsTreeView()
		{
			gruntsView.AppendColumn("Name", new CellRendererText(), "text", 1);
			gruntsView.AppendColumn("OS", new CellRendererText(), "text", 2);
			gruntsView.AppendColumn("Cores", new CellRendererText(), "text", 3);
			gruntsView.AppendColumn("Last Task", new CellRendererText(), "text", 4);
			gruntsView.AppendColumn("Status", new CellRendererText(), "text", 5);
			gruntList = new Gtk.ListStore(typeof(int), typeof(string), typeof(string), typeof(string),
			                              typeof(string), typeof(String));
			gruntsView.Model = gruntList;
		}
			
		void initializeJobsTreeView()
		{		
			jobsView.AppendColumn("Name", new CellRendererText(), "text", 1);
			jobsView.AppendColumn("Aborted", new CellRendererText(), "text", 2);
			jobsView.AppendColumn("Remain", new CellRendererText(), "text", 3);
			jobsView.AppendColumn("Done", new CellRendererText(), "text", 4);
			jobsView.AppendColumn("Status", new CellRendererText(), "text", 5);
			jobList = new Gtk.ListStore(typeof(int), typeof(string), typeof(string),
				                                          typeof(string), typeof(string), typeof(string));
			jobsView.Model = jobList;
		}
		
		public void invokeRemoveJobTV(string jobName)
		{
			Application.Invoke (delegate {
				int result = -1;
				int r = 0;//store the row we find
				foreach(object[] row in jobList)
				{
					if(jobName == (string) row[1])
					{
						result = r;
						break;
					}
				    r++;
				}
				if(result != -1)
				{
					TreeIter myIter = new TreeIter();
					TreePath tPath = new TreePath(r.ToString());
					jobList.GetIter(out myIter, tPath);
					jobList.Remove(ref myIter);
				}
			});
		}
		
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			if(q.getQRunning())
			{
				MessageDialog md = new MessageDialog (this, 
		        		DialogFlags.DestroyWithParent, MessageType.Question, 
		                ButtonsType.YesNo, "Quitting now will abort all running tasks" +
				                                 		". Are you sure?");
			
				ResponseType result = (ResponseType)md.Run ();
		
				if (result == ResponseType.Yes)
				{	
					q.deliverNotice(new Notice("shutdown"));    //tells qMT, lT, bT, cThreads, and local RCT to shutdown
					Application.Quit(); //if we have a local grunt, this will kill it's gui too!
				}
				else
				{
					md.Destroy();
					a.RetVal = true;
				}
			}
			else//queue is not running, so just close.
			{
				q.deliverNotice(new Notice("shutdown"));    //tells qMT, lT, bT, cThreads, and local RCT to shutdown
				Application.Quit(); //if we have a local grunt, this will kill it's gui too!
			}
		}
	
		protected virtual void OnBtnStartClicked (object sender, System.EventArgs e)
		{
			if(!q.getQRunning() && !q.checkIfAllDone())
			{
				q.deliverNotice(new Notice("start"));
				btnStart.Label = "Stop";
				
				StateType st = new StateType();
				Gdk.Color gray_color = new Gdk.Color(103, 230, 103);
				
				pbComplete.ModifyBg(st, gray_color);
			}
			else
			{
				q.deliverNotice(new Notice("stop"));
				StateType st = new StateType();
				pbComplete.ModifyBg(st);
			}
		}
		
		public void invokeModalMsg(string type, string msg)
		{
			Application.Invoke (delegate {
				showModalMsg(type, msg);	
			});
		}
		
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
		
		//we don't stop the queue in this case, just report the problem to the user
		public void invokeFailureMsg(string cName, string eMsg, int remaining)
		{
			Application.Invoke (delegate {
				string m = "'" + cName + "' failed with the following error message:" + Environment.NewLine +
						Environment.NewLine + "\"" + eMsg + "\"." + Environment.NewLine + Environment.NewLine +
						"Loki will try to run this task " + remaining + " more times before giving up"
						+ " and stopping the queue.";
				
				showModalMsg("warning", m);
			});
		}
		
		//we report the error, and stop the queue
		public void invokeAbortMsg(string cName, string eMsg, int tries)
		{
			Application.Invoke (delegate {
				string m = "'" + cName + "' failed with the following error message: " + Environment.NewLine +
				           Environment.NewLine + "\"" + eMsg + "\".\r\n\r\nUnfortunately, all " + tries
						   + " attempts to run this task have failed, so the queue has been stopped." +
						   " Please resolve the problem and then start the job queue again." +
							" For more detailed info, check stdout on " + cName + ".";
				
				showModalMsg("warning", m);
			});
		}
		
		//this function is responsible for stopping the GUI and sending a stop notice to qMT. it's called by qMT
		public void invokeStopGUI ()
		{
			Application.Invoke (delegate {
				btnStart.Label = "Start";
				StateType st = new StateType();
				pbComplete.ModifyBg(st);
			});	
		}
	
		protected virtual void OnAddActionActivated (object sender, System.EventArgs e)
		{
			AddJobWin jWin = new AddJobWin(q, false);
			jWin.KeepAbove = true;
			jWin.Modal = true;
		}
		
		public void invokeStartEditJob(string tType, string name, string winFP, string winOP,
		                     string unixFP, string unixOP, int firstF, int lastF)
		{
			Application.Invoke (delegate {
				AddJobWin jWin = new AddJobWin(q, true);
				jWin.setupEdit(tType, name, winFP, winOP, unixFP, unixOP, firstF, lastF);
				jWin.KeepAbove = true;
				jWin.Modal = true;
			});
		}
	
		protected virtual void OnRemoveActionActivated (object sender, System.EventArgs e)
		{
			if(lastSelectedJobName != null)
			{
				MessageDialog md = new MessageDialog (this, 
						DialogFlags.DestroyWithParent, MessageType.Question, 
					    ButtonsType.YesNo, "Remove job '" + lastSelectedJobName + "' from the queue?");
		
				ResponseType result = (ResponseType)md.Run ();
	
				if (result == ResponseType.Yes)
				{	
	                q.deliverNotice(new Notice("remove", lastSelectedJobName));
				}
				md.Destroy();
				lastSelectedJobName = null;	//otherwise we'll get index out of range if we remove again without another select
			}
			else
			{
				showModalMsg("info", "Please select a Job first, then select 'Remove'.");
			}
		}

		protected virtual void OnEditActionActivated (object sender, System.EventArgs e)
		{
			if(lastSelectedJobName != null)
			{
				q.deliverNotice(new Notice("startEdit", lastSelectedJobName));
			}
			else
			{
				showModalMsg("info", "Please select a Job first, then select 'Edit'.");
			}
		}

		protected virtual void OnAboutActionActivated (object sender, System.EventArgs e)
		{
			showModalMsg("info", "Loki Render\r\nVersion 0.5\r\n\r\nBuilt on Mono and Gtk#\r\n\r\n" +
			             "(C) 2009, Daniel Petersen\r\n\r\nGNU General Public License Version 3" );
		}

		protected virtual void OnHelpAction1Activated (object sender, System.EventArgs e)
		{
			System.Diagnostics.Process.Start("http://loki-render.berlios.de/index.php/help");
		}
	}	
}