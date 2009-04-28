// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace loki {
    
    
    public partial class RoleDialog {
        
        private Gtk.Alignment alignment6;
        
        private Gtk.VBox vbox4;
        
        private Gtk.Alignment alignment8;
        
        private Gtk.Label label5;
        
        private Gtk.Alignment alignmentButtons;
        
        private Gtk.VBox vbox5;
        
        private Gtk.RadioButton rBtnGrunt;
        
        private Gtk.RadioButton rBtnMaster;
        
        private Gtk.RadioButton rBtnBoth;
        
        private Gtk.Button buttonOk;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget loki.RoleDialog
            this.Name = "loki.RoleDialog";
            this.Title = "Loki Role";
            this.Icon = Stetic.IconLoader.LoadIcon(this, "gtk-yes", Gtk.IconSize.Menu, 16);
            this.WindowPosition = ((Gtk.WindowPosition)(4));
            this.HasSeparator = false;
            // Internal child loki.RoleDialog.VBox
            Gtk.VBox w1 = this.VBox;
            w1.Name = "dialog1_VBox";
            w1.BorderWidth = ((uint)(2));
            // Container child dialog1_VBox.Gtk.Box+BoxChild
            this.alignment6 = new Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
            this.alignment6.Name = "alignment6";
            // Container child alignment6.Gtk.Container+ContainerChild
            this.vbox4 = new Gtk.VBox();
            this.vbox4.Name = "vbox4";
            this.vbox4.Spacing = 6;
            // Container child vbox4.Gtk.Box+BoxChild
            this.alignment8 = new Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
            this.alignment8.Name = "alignment8";
            this.alignment8.TopPadding = ((uint)(25));
            // Container child alignment8.Gtk.Container+ContainerChild
            this.label5 = new Gtk.Label();
            this.label5.Name = "label5";
            this.label5.LabelProp = "Select the role(s) that Loki will have on this computer:";
            this.alignment8.Add(this.label5);
            this.vbox4.Add(this.alignment8);
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.vbox4[this.alignment8]));
            w3.Position = 0;
            w3.Expand = false;
            w3.Fill = false;
            // Container child vbox4.Gtk.Box+BoxChild
            this.alignmentButtons = new Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
            this.alignmentButtons.Name = "alignmentButtons";
            this.alignmentButtons.LeftPadding = ((uint)(50));
            this.alignmentButtons.TopPadding = ((uint)(5));
            this.alignmentButtons.BorderWidth = ((uint)(10));
            // Container child alignmentButtons.Gtk.Container+ContainerChild
            this.vbox5 = new Gtk.VBox();
            this.vbox5.Name = "vbox5";
            this.vbox5.Spacing = 6;
            // Container child vbox5.Gtk.Box+BoxChild
            this.rBtnGrunt = new Gtk.RadioButton("Grunt");
            this.rBtnGrunt.TooltipMarkup = "A worker in the farm. Runs tasks from the job queue.";
            this.rBtnGrunt.CanFocus = true;
            this.rBtnGrunt.Name = "rBtnGrunt";
            this.rBtnGrunt.DrawIndicator = true;
            this.rBtnGrunt.UseUnderline = true;
            this.rBtnGrunt.Group = new GLib.SList(System.IntPtr.Zero);
            this.vbox5.Add(this.rBtnGrunt);
            Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(this.vbox5[this.rBtnGrunt]));
            w4.Position = 0;
            w4.Expand = false;
            w4.Fill = false;
            // Container child vbox5.Gtk.Box+BoxChild
            this.rBtnMaster = new Gtk.RadioButton("Master");
            this.rBtnMaster.TooltipMarkup = "Master of the farm and job queue!";
            this.rBtnMaster.CanFocus = true;
            this.rBtnMaster.Name = "rBtnMaster";
            this.rBtnMaster.DrawIndicator = true;
            this.rBtnMaster.UseUnderline = true;
            this.rBtnMaster.Group = this.rBtnGrunt.Group;
            this.vbox5.Add(this.rBtnMaster);
            Gtk.Box.BoxChild w5 = ((Gtk.Box.BoxChild)(this.vbox5[this.rBtnMaster]));
            w5.Position = 1;
            w5.Expand = false;
            w5.Fill = false;
            // Container child vbox5.Gtk.Box+BoxChild
            this.rBtnBoth = new Gtk.RadioButton("Master and Grunt");
            this.rBtnBoth.TooltipMarkup = "This computer will be the master and a grunt. The CPU will be very busy when the queue is running!";
            this.rBtnBoth.CanFocus = true;
            this.rBtnBoth.Name = "rBtnBoth";
            this.rBtnBoth.DrawIndicator = true;
            this.rBtnBoth.UseUnderline = true;
            this.rBtnBoth.Group = this.rBtnGrunt.Group;
            this.vbox5.Add(this.rBtnBoth);
            Gtk.Box.BoxChild w6 = ((Gtk.Box.BoxChild)(this.vbox5[this.rBtnBoth]));
            w6.Position = 2;
            w6.Expand = false;
            w6.Fill = false;
            this.alignmentButtons.Add(this.vbox5);
            this.vbox4.Add(this.alignmentButtons);
            Gtk.Box.BoxChild w8 = ((Gtk.Box.BoxChild)(this.vbox4[this.alignmentButtons]));
            w8.Position = 1;
            w8.Expand = false;
            w8.Fill = false;
            this.alignment6.Add(this.vbox4);
            w1.Add(this.alignment6);
            Gtk.Box.BoxChild w10 = ((Gtk.Box.BoxChild)(w1[this.alignment6]));
            w10.Position = 0;
            w10.Expand = false;
            w10.Fill = false;
            // Internal child loki.RoleDialog.ActionArea
            Gtk.HButtonBox w11 = this.ActionArea;
            w11.Name = "dialog1_ActionArea";
            w11.Spacing = 6;
            w11.BorderWidth = ((uint)(5));
            w11.LayoutStyle = ((Gtk.ButtonBoxStyle)(4));
            // Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.buttonOk = new Gtk.Button();
            this.buttonOk.CanDefault = true;
            this.buttonOk.CanFocus = true;
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.UseStock = true;
            this.buttonOk.UseUnderline = true;
            this.buttonOk.BorderWidth = ((uint)(3));
            this.buttonOk.Label = "gtk-ok";
            this.AddActionWidget(this.buttonOk, -5);
            Gtk.ButtonBox.ButtonBoxChild w12 = ((Gtk.ButtonBox.ButtonBoxChild)(w11[this.buttonOk]));
            w12.Expand = false;
            w12.Fill = false;
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.DefaultWidth = 400;
            this.DefaultHeight = 203;
            this.Show();
            this.buttonOk.Clicked += new System.EventHandler(this.OnButtonOkClicked);
        }
    }
}
