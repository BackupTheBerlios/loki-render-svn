// Project: Loki Render - A distributed job queue manager.
// Version: 0.5
// 
// File Description: simple dialog to determine Loki's role for this session	  
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

namespace loki
{
	public partial class RoleDialog : Gtk.Dialog
	{
		public int role;
		
		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if(rBtnGrunt.Active)
				role = 0;
			else if(rBtnMaster.Active)
				role = 1;
			else if(rBtnBoth.Active)
				role = 2;
		}		
		public RoleDialog()
		{
			this.Build();
		}
	}
}
