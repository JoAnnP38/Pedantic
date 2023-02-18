namespace Pedantic.Client
{
    partial class ConvergenceForm
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
            this.plotConverge = new ScottPlot.FormsPlot();
            this.SuspendLayout();
            // 
            // plotConverge
            // 
            this.plotConverge.Location = new System.Drawing.Point(9, 6);
            this.plotConverge.Margin = new System.Windows.Forms.Padding(0);
            this.plotConverge.Name = "plotConverge";
            this.plotConverge.Size = new System.Drawing.Size(467, 300);
            this.plotConverge.TabIndex = 0;
            // 
            // ConvergenceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(585, 318);
            this.Controls.Add(this.plotConverge);
            this.Name = "ConvergenceForm";
            this.Text = "ConvergenceForm";
            this.Load += new System.EventHandler(this.ConvergenceForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private ScottPlot.FormsPlot plotConverge;
    }
}