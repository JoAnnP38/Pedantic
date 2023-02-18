namespace Pedantic.Client
{
    partial class EvolutionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblEvolutionID = new System.Windows.Forms.Label();
            this.txtEvolutionID = new System.Windows.Forms.TextBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.txtCount = new System.Windows.Forms.TextBox();
            this.txtConvergePct = new System.Windows.Forms.TextBox();
            this.lblOf = new System.Windows.Forms.Label();
            this.txtMaxGen = new System.Windows.Forms.TextBox();
            this.lblAvgGenTime = new System.Windows.Forms.Label();
            this.txtAvgGenTime = new System.Windows.Forms.TextBox();
            this.lblTotalTime = new System.Windows.Forms.Label();
            this.txtRunTime = new System.Windows.Forms.TextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripOutput = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.runTimer = new System.Windows.Forms.Timer(this.components);
            this.lblState = new System.Windows.Forms.Label();
            this.txtEvolutionState = new System.Windows.Forms.TextBox();
            this.lblLastUpdated = new System.Windows.Forms.Label();
            this.txtUpdatedOn = new System.Windows.Forms.TextBox();
            this.bgWorker = new System.ComponentModel.BackgroundWorker();
            this.linkConvergence = new System.Windows.Forms.LinkLabel();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblEvolutionID
            // 
            this.lblEvolutionID.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblEvolutionID.Location = new System.Drawing.Point(12, 9);
            this.lblEvolutionID.Name = "lblEvolutionID";
            this.lblEvolutionID.Size = new System.Drawing.Size(100, 16);
            this.lblEvolutionID.TabIndex = 0;
            this.lblEvolutionID.Text = "Evolution ID:";
            this.lblEvolutionID.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtEvolutionID
            // 
            this.txtEvolutionID.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtEvolutionID.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtEvolutionID.Location = new System.Drawing.Point(118, 10);
            this.txtEvolutionID.Name = "txtEvolutionID";
            this.txtEvolutionID.ReadOnly = true;
            this.txtEvolutionID.Size = new System.Drawing.Size(175, 16);
            this.txtEvolutionID.TabIndex = 1;
            this.txtEvolutionID.Text = "63da147260961e01d917f026";
            // 
            // lblCount
            // 
            this.lblCount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblCount.Location = new System.Drawing.Point(12, 75);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(100, 16);
            this.lblCount.TabIndex = 6;
            this.lblCount.Text = "Generations:";
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtCount
            // 
            this.txtCount.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtCount.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtCount.Location = new System.Drawing.Point(118, 76);
            this.txtCount.Name = "txtCount";
            this.txtCount.ReadOnly = true;
            this.txtCount.Size = new System.Drawing.Size(70, 16);
            this.txtCount.TabIndex = 7;
            this.txtCount.Text = "0";
            // 
            // txtConvergePct
            // 
            this.txtConvergePct.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtConvergePct.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtConvergePct.Location = new System.Drawing.Point(118, 98);
            this.txtConvergePct.Name = "txtConvergePct";
            this.txtConvergePct.ReadOnly = true;
            this.txtConvergePct.Size = new System.Drawing.Size(70, 16);
            this.txtConvergePct.TabIndex = 11;
            this.txtConvergePct.Text = "0%";
            // 
            // lblOf
            // 
            this.lblOf.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblOf.Location = new System.Drawing.Point(194, 75);
            this.lblOf.Name = "lblOf";
            this.lblOf.Size = new System.Drawing.Size(25, 16);
            this.lblOf.TabIndex = 8;
            this.lblOf.Text = "of:";
            this.lblOf.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMaxGen
            // 
            this.txtMaxGen.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtMaxGen.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMaxGen.Location = new System.Drawing.Point(223, 76);
            this.txtMaxGen.Name = "txtMaxGen";
            this.txtMaxGen.ReadOnly = true;
            this.txtMaxGen.Size = new System.Drawing.Size(70, 16);
            this.txtMaxGen.TabIndex = 9;
            this.txtMaxGen.Text = "400";
            // 
            // lblAvgGenTime
            // 
            this.lblAvgGenTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblAvgGenTime.Location = new System.Drawing.Point(12, 119);
            this.lblAvgGenTime.Name = "lblAvgGenTime";
            this.lblAvgGenTime.Size = new System.Drawing.Size(100, 16);
            this.lblAvgGenTime.TabIndex = 12;
            this.lblAvgGenTime.Text = "Avg Gen Time:";
            this.lblAvgGenTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtAvgGenTime
            // 
            this.txtAvgGenTime.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtAvgGenTime.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtAvgGenTime.Location = new System.Drawing.Point(118, 120);
            this.txtAvgGenTime.Name = "txtAvgGenTime";
            this.txtAvgGenTime.ReadOnly = true;
            this.txtAvgGenTime.Size = new System.Drawing.Size(70, 16);
            this.txtAvgGenTime.TabIndex = 13;
            // 
            // lblTotalTime
            // 
            this.lblTotalTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTotalTime.Location = new System.Drawing.Point(12, 141);
            this.lblTotalTime.Name = "lblTotalTime";
            this.lblTotalTime.Size = new System.Drawing.Size(100, 16);
            this.lblTotalTime.TabIndex = 14;
            this.lblTotalTime.Text = "Total Run Time:";
            this.lblTotalTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtRunTime
            // 
            this.txtRunTime.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtRunTime.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtRunTime.Location = new System.Drawing.Point(118, 142);
            this.txtRunTime.Name = "txtRunTime";
            this.txtRunTime.ReadOnly = true;
            this.txtRunTime.Size = new System.Drawing.Size(70, 16);
            this.txtRunTime.TabIndex = 15;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatus,
            this.toolStripOutput});
            this.statusStrip.Location = new System.Drawing.Point(0, 173);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(422, 24);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 12;
            // 
            // toolStripStatus
            // 
            this.toolStripStatus.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.toolStripStatus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripStatus.Name = "toolStripStatus";
            this.toolStripStatus.Size = new System.Drawing.Size(56, 19);
            this.toolStripStatus.Text = "Running";
            this.toolStripStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripOutput
            // 
            this.toolStripOutput.Name = "toolStripOutput";
            this.toolStripOutput.Size = new System.Drawing.Size(351, 19);
            this.toolStripOutput.Spring = true;
            this.toolStripOutput.Text = "yadoyadaydya";
            this.toolStripOutput.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(323, 6);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 16;
            this.btnStart.Text = "&Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(323, 35);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 17;
            this.btnStop.Text = "Sto&p";
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(323, 64);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 18;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // runTimer
            // 
            this.runTimer.Interval = 1000;
            this.runTimer.Tick += new System.EventHandler(this.runTimer_Tick);
            // 
            // lblState
            // 
            this.lblState.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblState.Location = new System.Drawing.Point(12, 31);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(100, 16);
            this.lblState.TabIndex = 2;
            this.lblState.Text = "Evolution State:";
            this.lblState.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtEvolutionState
            // 
            this.txtEvolutionState.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtEvolutionState.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtEvolutionState.Location = new System.Drawing.Point(118, 32);
            this.txtEvolutionState.Name = "txtEvolutionState";
            this.txtEvolutionState.ReadOnly = true;
            this.txtEvolutionState.Size = new System.Drawing.Size(70, 16);
            this.txtEvolutionState.TabIndex = 3;
            // 
            // lblLastUpdated
            // 
            this.lblLastUpdated.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblLastUpdated.Location = new System.Drawing.Point(12, 53);
            this.lblLastUpdated.Name = "lblLastUpdated";
            this.lblLastUpdated.Size = new System.Drawing.Size(100, 16);
            this.lblLastUpdated.TabIndex = 4;
            this.lblLastUpdated.Text = "Last Updated:";
            this.lblLastUpdated.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtUpdatedOn
            // 
            this.txtUpdatedOn.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtUpdatedOn.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUpdatedOn.Location = new System.Drawing.Point(118, 54);
            this.txtUpdatedOn.Name = "txtUpdatedOn";
            this.txtUpdatedOn.ReadOnly = true;
            this.txtUpdatedOn.Size = new System.Drawing.Size(175, 16);
            this.txtUpdatedOn.TabIndex = 5;
            // 
            // bgWorker
            // 
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgWorker_DoWork);
            this.bgWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bgWorker_RunWorkerCompleted);
            // 
            // linkConvergence
            // 
            this.linkConvergence.Location = new System.Drawing.Point(12, 97);
            this.linkConvergence.Name = "linkConvergence";
            this.linkConvergence.Size = new System.Drawing.Size(100, 16);
            this.linkConvergence.TabIndex = 19;
            this.linkConvergence.TabStop = true;
            this.linkConvergence.Text = "Convergence %:";
            this.linkConvergence.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.linkConvergence.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkConvergence_LinkClicked);
            // 
            // EvolutionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(422, 197);
            this.Controls.Add(this.linkConvergence);
            this.Controls.Add(this.txtUpdatedOn);
            this.Controls.Add(this.lblLastUpdated);
            this.Controls.Add(this.txtEvolutionState);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.txtRunTime);
            this.Controls.Add(this.lblTotalTime);
            this.Controls.Add(this.txtAvgGenTime);
            this.Controls.Add(this.lblAvgGenTime);
            this.Controls.Add(this.txtMaxGen);
            this.Controls.Add(this.lblOf);
            this.Controls.Add(this.txtConvergePct);
            this.Controls.Add(this.txtCount);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.txtEvolutionID);
            this.Controls.Add(this.lblEvolutionID);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "EvolutionForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = " Pedantic Evolution";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EvolutionForm_FormClosing);
            this.Load += new System.EventHandler(this.EvolutionForm_Load);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label lblEvolutionID;
        private TextBox txtEvolutionID;
        private Label lblCount;
        private TextBox txtCount;
        private TextBox txtConvergePct;
        private Label lblOf;
        private TextBox txtMaxGen;
        private Label lblAvgGenTime;
        private TextBox txtAvgGenTime;
        private Label lblTotalTime;
        private TextBox txtRunTime;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatus;
        private ToolStripStatusLabel toolStripOutput;
        private Button btnStart;
        private Button btnStop;
        private Button btnCancel;
        private System.Windows.Forms.Timer runTimer;
        private TextBox txtEvolutionState;
        private Label lblState;
        private TextBox txtUpdatedOn;
        private Label lblLastUpdated;
        private System.ComponentModel.BackgroundWorker bgWorker;
        private LinkLabel linkConvergence;
    }
}