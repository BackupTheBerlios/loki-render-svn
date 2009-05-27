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
    
    
    public partial class MasterWin {
        
        private Gtk.UIManager UIManager;
        
        private Gtk.Action JobsAction;
        
        private Gtk.Action NewAction;
        
        private Gtk.Action RemoveSelectedAction;
        
        private Gtk.Action EditAction;
        
        private Gtk.Action HelpAction;
        
        private Gtk.Action OnlineHelpAction;
        
        private Gtk.Action AboutAction;
        
        private Gtk.Action RemoveAllFinishedAction;
        
        private Gtk.VBox vbox1;
        
        private Gtk.MenuBar menubar1;
        
        private Gtk.Alignment alignment1;
        
        private Gtk.HBox hbox3;
        
        private Gtk.Label label2;
        
        private Gtk.Label lblTotalCores;
        
        private Gtk.HBox hbox1;
        
        private Gtk.Alignment alignment3;
        
        private Gtk.Frame frame1;
        
        private Gtk.Alignment GtkAlignment;
        
        private Gtk.ScrolledWindow GtkScrolledWindow;
        
        private Gtk.TreeView jobsView;
        
        private Gtk.Label GtkLabel3;
        
        private Gtk.Alignment alignment2;
        
        private Gtk.Frame frame2;
        
        private Gtk.Alignment GtkAlignment1;
        
        private Gtk.ScrolledWindow GtkScrolledWindow1;
        
        private Gtk.TreeView gruntsView;
        
        private Gtk.Label GtkLabel2;
        
        private Gtk.HBox hbox2;
        
        private Gtk.Alignment alignmentStartButton;
        
        private Gtk.Button btnStart;
        
        private Gtk.Alignment alignment4;
        
        private Gtk.ProgressBar pbComplete;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget loki.MasterWin
            this.UIManager = new Gtk.UIManager();
            Gtk.ActionGroup w1 = new Gtk.ActionGroup("Default");
            this.JobsAction = new Gtk.Action("JobsAction", "Jobs", null, null);
            this.JobsAction.ShortLabel = "Jobs";
            w1.Add(this.JobsAction, null);
            this.NewAction = new Gtk.Action("NewAction", "New", null, "gtk-new");
            this.NewAction.ShortLabel = "Add";
            w1.Add(this.NewAction, "<Control><Mod2>n");
            this.RemoveSelectedAction = new Gtk.Action("RemoveSelectedAction", "Remove selected", null, "gtk-delete");
            this.RemoveSelectedAction.ShortLabel = "Remove";
            w1.Add(this.RemoveSelectedAction, "<Control><Mod2>r");
            this.EditAction = new Gtk.Action("EditAction", "Edit", null, "gtk-open");
            this.EditAction.ShortLabel = "Edit";
            w1.Add(this.EditAction, "<Control><Mod2>e");
            this.HelpAction = new Gtk.Action("HelpAction", "Help", null, null);
            this.HelpAction.ShortLabel = "Help";
            w1.Add(this.HelpAction, null);
            this.OnlineHelpAction = new Gtk.Action("OnlineHelpAction", "Online Help", null, "gtk-help");
            this.OnlineHelpAction.ShortLabel = "Help";
            w1.Add(this.OnlineHelpAction, null);
            this.AboutAction = new Gtk.Action("AboutAction", "About", null, "gtk-about");
            this.AboutAction.ShortLabel = "About";
            w1.Add(this.AboutAction, null);
            this.RemoveAllFinishedAction = new Gtk.Action("RemoveAllFinishedAction", "Remove all finished", null, "gtk-clear");
            this.RemoveAllFinishedAction.ShortLabel = "Remove all finished";
            w1.Add(this.RemoveAllFinishedAction, "<Control>f");
            this.UIManager.InsertActionGroup(w1, 0);
            this.AddAccelGroup(this.UIManager.AccelGroup);
            this.Name = "loki.MasterWin";
            this.Title = "Loki master";
            this.Icon = Gdk.Pixbuf.LoadFromResource("16x16.png");
            this.WindowPosition = ((Gtk.WindowPosition)(1));
            // Container child loki.MasterWin.Gtk.Container+ContainerChild
            this.vbox1 = new Gtk.VBox();
            this.vbox1.Name = "vbox1";
            this.vbox1.Spacing = 6;
            // Container child vbox1.Gtk.Box+BoxChild
            this.UIManager.AddUiFromString("<ui><menubar name='menubar1'><menu name='JobsAction' action='JobsAction'><menuitem name='NewAction' action='NewAction'/><menuitem name='EditAction' action='EditAction'/><menuitem name='RemoveSelectedAction' action='RemoveSelectedAction'/><menuitem name='RemoveAllFinishedAction' action='RemoveAllFinishedAction'/></menu><menu name='HelpAction' action='HelpAction'><menuitem name='OnlineHelpAction' action='OnlineHelpAction'/><menuitem name='AboutAction' action='AboutAction'/></menu></menubar></ui>");
            this.menubar1 = ((Gtk.MenuBar)(this.UIManager.GetWidget("/menubar1")));
            this.menubar1.Name = "menubar1";
            this.vbox1.Add(this.menubar1);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.vbox1[this.menubar1]));
            w2.Position = 0;
            w2.Expand = false;
            w2.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.alignment1 = new Gtk.Alignment(1F, 0.5F, 0F, 1F);
            this.alignment1.Name = "alignment1";
            this.alignment1.RightPadding = ((uint)(15));
            // Container child alignment1.Gtk.Container+ContainerChild
            this.hbox3 = new Gtk.HBox();
            this.hbox3.Name = "hbox3";
            this.hbox3.Spacing = 6;
            // Container child hbox3.Gtk.Box+BoxChild
            this.label2 = new Gtk.Label();
            this.label2.Name = "label2";
            this.label2.LabelProp = "Total cores:";
            this.hbox3.Add(this.label2);
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.hbox3[this.label2]));
            w3.Position = 0;
            w3.Expand = false;
            w3.Fill = false;
            // Container child hbox3.Gtk.Box+BoxChild
            this.lblTotalCores = new Gtk.Label();
            this.lblTotalCores.Name = "lblTotalCores";
            this.lblTotalCores.Xalign = 0F;
            this.lblTotalCores.LabelProp = "0";
            this.hbox3.Add(this.lblTotalCores);
            Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(this.hbox3[this.lblTotalCores]));
            w4.Position = 1;
            w4.Expand = false;
            w4.Fill = false;
            this.alignment1.Add(this.hbox3);
            this.vbox1.Add(this.alignment1);
            Gtk.Box.BoxChild w6 = ((Gtk.Box.BoxChild)(this.vbox1[this.alignment1]));
            w6.Position = 1;
            w6.Expand = false;
            w6.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.hbox1 = new Gtk.HBox();
            this.hbox1.Name = "hbox1";
            this.hbox1.Spacing = 6;
            // Container child hbox1.Gtk.Box+BoxChild
            this.alignment3 = new Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
            this.alignment3.Name = "alignment3";
            this.alignment3.TopPadding = ((uint)(5));
            // Container child alignment3.Gtk.Container+ContainerChild
            this.frame1 = new Gtk.Frame();
            this.frame1.TooltipMarkup = "List of jobs in the queue and their respective info.";
            this.frame1.Name = "frame1";
            this.frame1.ShadowType = ((Gtk.ShadowType)(0));
            this.frame1.LabelXalign = 0.5F;
            // Container child frame1.Gtk.Container+ContainerChild
            this.GtkAlignment = new Gtk.Alignment(0F, 0F, 1F, 1F);
            this.GtkAlignment.Name = "GtkAlignment";
            this.GtkAlignment.LeftPadding = ((uint)(12));
            // Container child GtkAlignment.Gtk.Container+ContainerChild
            this.GtkScrolledWindow = new Gtk.ScrolledWindow();
            this.GtkScrolledWindow.Name = "GtkScrolledWindow";
            this.GtkScrolledWindow.ShadowType = ((Gtk.ShadowType)(1));
            // Container child GtkScrolledWindow.Gtk.Container+ContainerChild
            this.jobsView = new Gtk.TreeView();
            this.jobsView.CanFocus = true;
            this.jobsView.Name = "jobsView";
            this.GtkScrolledWindow.Add(this.jobsView);
            this.GtkAlignment.Add(this.GtkScrolledWindow);
            this.frame1.Add(this.GtkAlignment);
            this.GtkLabel3 = new Gtk.Label();
            this.GtkLabel3.Name = "GtkLabel3";
            this.GtkLabel3.LabelProp = "<b>Job Queue</b>";
            this.GtkLabel3.UseMarkup = true;
            this.GtkLabel3.Justify = ((Gtk.Justification)(2));
            this.frame1.LabelWidget = this.GtkLabel3;
            this.alignment3.Add(this.frame1);
            this.hbox1.Add(this.alignment3);
            Gtk.Box.BoxChild w11 = ((Gtk.Box.BoxChild)(this.hbox1[this.alignment3]));
            w11.Position = 0;
            // Container child hbox1.Gtk.Box+BoxChild
            this.alignment2 = new Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
            this.alignment2.Name = "alignment2";
            this.alignment2.TopPadding = ((uint)(5));
            this.alignment2.RightPadding = ((uint)(12));
            // Container child alignment2.Gtk.Container+ContainerChild
            this.frame2 = new Gtk.Frame();
            this.frame2.TooltipMarkup = "List of grunts in the farm and their respective info.";
            this.frame2.Name = "frame2";
            this.frame2.ShadowType = ((Gtk.ShadowType)(0));
            this.frame2.LabelXalign = 0.5F;
            // Container child frame2.Gtk.Container+ContainerChild
            this.GtkAlignment1 = new Gtk.Alignment(0F, 0F, 1F, 1F);
            this.GtkAlignment1.Name = "GtkAlignment1";
            this.GtkAlignment1.LeftPadding = ((uint)(12));
            // Container child GtkAlignment1.Gtk.Container+ContainerChild
            this.GtkScrolledWindow1 = new Gtk.ScrolledWindow();
            this.GtkScrolledWindow1.Name = "GtkScrolledWindow1";
            this.GtkScrolledWindow1.ShadowType = ((Gtk.ShadowType)(1));
            // Container child GtkScrolledWindow1.Gtk.Container+ContainerChild
            this.gruntsView = new Gtk.TreeView();
            this.gruntsView.CanFocus = true;
            this.gruntsView.Name = "gruntsView";
            this.GtkScrolledWindow1.Add(this.gruntsView);
            this.GtkAlignment1.Add(this.GtkScrolledWindow1);
            this.frame2.Add(this.GtkAlignment1);
            this.GtkLabel2 = new Gtk.Label();
            this.GtkLabel2.Name = "GtkLabel2";
            this.GtkLabel2.LabelProp = "<b>Grunts</b>";
            this.GtkLabel2.UseMarkup = true;
            this.frame2.LabelWidget = this.GtkLabel2;
            this.alignment2.Add(this.frame2);
            this.hbox1.Add(this.alignment2);
            Gtk.Box.BoxChild w16 = ((Gtk.Box.BoxChild)(this.hbox1[this.alignment2]));
            w16.Position = 1;
            this.vbox1.Add(this.hbox1);
            Gtk.Box.BoxChild w17 = ((Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
            w17.Position = 2;
            // Container child vbox1.Gtk.Box+BoxChild
            this.hbox2 = new Gtk.HBox();
            this.hbox2.Name = "hbox2";
            this.hbox2.Spacing = 6;
            // Container child hbox2.Gtk.Box+BoxChild
            this.alignmentStartButton = new Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
            this.alignmentStartButton.Name = "alignmentStartButton";
            this.alignmentStartButton.LeftPadding = ((uint)(8));
            this.alignmentStartButton.BottomPadding = ((uint)(5));
            this.alignmentStartButton.BorderWidth = ((uint)(5));
            // Container child alignmentStartButton.Gtk.Container+ContainerChild
            this.btnStart = new Gtk.Button();
            this.btnStart.TooltipMarkup = "Start and stop the job queue.";
            this.btnStart.WidthRequest = 100;
            this.btnStart.HeightRequest = 30;
            this.btnStart.CanDefault = true;
            this.btnStart.CanFocus = true;
            this.btnStart.Events = ((Gdk.EventMask)(1024));
            this.btnStart.Name = "btnStart";
            this.btnStart.UseUnderline = true;
            this.btnStart.Label = "Start";
            this.alignmentStartButton.Add(this.btnStart);
            this.hbox2.Add(this.alignmentStartButton);
            Gtk.Box.BoxChild w19 = ((Gtk.Box.BoxChild)(this.hbox2[this.alignmentStartButton]));
            w19.Position = 0;
            w19.Expand = false;
            w19.Fill = false;
            // Container child hbox2.Gtk.Box+BoxChild
            this.alignment4 = new Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
            this.alignment4.Name = "alignment4";
            this.alignment4.RightPadding = ((uint)(9));
            this.alignment4.BottomPadding = ((uint)(5));
            this.alignment4.BorderWidth = ((uint)(5));
            // Container child alignment4.Gtk.Container+ContainerChild
            this.pbComplete = new Gtk.ProgressBar();
            this.pbComplete.TooltipMarkup = "Overall progress for all tasks of all jobs in the queue.";
            this.pbComplete.HeightRequest = 30;
            this.pbComplete.Name = "pbComplete";
            this.pbComplete.Text = "";
            this.alignment4.Add(this.pbComplete);
            this.hbox2.Add(this.alignment4);
            Gtk.Box.BoxChild w21 = ((Gtk.Box.BoxChild)(this.hbox2[this.alignment4]));
            w21.Position = 1;
            this.vbox1.Add(this.hbox2);
            Gtk.Box.BoxChild w22 = ((Gtk.Box.BoxChild)(this.vbox1[this.hbox2]));
            w22.Position = 3;
            w22.Expand = false;
            w22.Fill = false;
            this.Add(this.vbox1);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.DefaultWidth = 736;
            this.DefaultHeight = 485;
            this.Show();
            this.DeleteEvent += new Gtk.DeleteEventHandler(this.OnDeleteEvent);
            this.NewAction.Activated += new System.EventHandler(this.OnAddActionActivated);
            this.RemoveSelectedAction.Activated += new System.EventHandler(this.OnRemoveActionActivated);
            this.EditAction.Activated += new System.EventHandler(this.OnEditActionActivated);
            this.OnlineHelpAction.Activated += new System.EventHandler(this.OnHelpAction1Activated);
            this.AboutAction.Activated += new System.EventHandler(this.OnAboutActionActivated);
            this.RemoveAllFinishedAction.Activated += new System.EventHandler(this.OnRemoveAllFinishedActionActivated);
            this.jobsView.ButtonPressEvent += new Gtk.ButtonPressEventHandler(this.OnJobsViewButtonPressEvent);
            this.btnStart.Clicked += new System.EventHandler(this.OnBtnStartClicked);
        }
    }
}
