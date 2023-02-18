namespace Pedantic.Client
{
    #region TT Chess Merida Font Mapping
    /*
        B = white bishop on dark square
        K = white king on dark square
        L = black king on dark square
        M = black knight on dark square
        N = white knight on dark square
        O = black pawn on dark square
        P = white pawn on dark square
        Q = white queen on dark square
        R = white rook on dark square
        T = black rook on dark square
        V = black bishop on dark square
        W = black queen on dark square

        b = white bishop on light square
        k = white king on light square
        l = black king on light square
        m = black knight on light square
        n = white knight on light square
        o = black pawn on light square
        p = white pawn on light square
        q = white queen on light square
        r = white rook on light square
        t = black rook on light square
        v = black bishop on light square
        w = black queen on light square

        + = empty dark sqaure
        * = empty light square

        ! = upper left corner
        " = top border
        # = upper right corner
        $ = left border
                 % = right border
        ( = bottom border
        ) = lower right corner
        0x002F = lower left corner
        0x00E0 = 1 left border
        0x00E1 = 2 left border
        0x00E2 = 3 left border
        0x00E3 = 4 left border
        0x00E4 = 5 left border
        0x00E5 = 6 left border
        0x00E6 = 7 left border
        0x00E7 = 8 left border
        0x00E8 = a bottom border
        0x00E9 = b bottom border
        0x00EA = c bottom border
        0x00EB = d bottom border
        0x00EC = e bottom border
        0x00ED = f bottom border
        0x00EE = g bottom border
        0x00EF = h bottom border
    */
    #endregion

    partial class GameForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbChessBoard = new System.Windows.Forms.RichTextBox();
            this.lblCurrentMoveLine = new System.Windows.Forms.Label();
            this.txtMoveList = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtbChessBoard
            // 
            this.rtbChessBoard.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.rtbChessBoard.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbChessBoard.CausesValidation = false;
            this.rtbChessBoard.Font = new System.Drawing.Font("Chess Merida Arena", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.rtbChessBoard.Location = new System.Drawing.Point(6, 71);
            this.rtbChessBoard.Margin = new System.Windows.Forms.Padding(0);
            this.rtbChessBoard.Name = "rtbChessBoard";
            this.rtbChessBoard.ReadOnly = true;
            this.rtbChessBoard.Size = new System.Drawing.Size(250, 250);
            this.rtbChessBoard.TabIndex = 0;
            this.rtbChessBoard.TabStop = false;
            this.rtbChessBoard.Text = "!\"\"\"\"\"\"\"\"#\nç*+*+*+*+%\næ+*+*+*+*%\nå*+*+*+*+%\nä+*+*+*+*%\nã*+*+*+*+%\nâ+*+*+*+*%\ná*+*" +
    "+*+*+%\nà+*+*+*+*%\n/èéêëìíîï)";
            this.rtbChessBoard.WordWrap = false;
            this.rtbChessBoard.Enter += new System.EventHandler(this.rtbChessBoard_Enter);
            // 
            // lblCurrentMoveLine
            // 
            this.lblCurrentMoveLine.AutoEllipsis = true;
            this.lblCurrentMoveLine.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblCurrentMoveLine.Location = new System.Drawing.Point(6, 52);
            this.lblCurrentMoveLine.Name = "lblCurrentMoveLine";
            this.lblCurrentMoveLine.Size = new System.Drawing.Size(480, 23);
            this.lblCurrentMoveLine.TabIndex = 1;
            this.lblCurrentMoveLine.Text = "current move and principal variation";
            // 
            // txtMoveList
            // 
            this.txtMoveList.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.txtMoveList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMoveList.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtMoveList.Location = new System.Drawing.Point(250, 86);
            this.txtMoveList.Margin = new System.Windows.Forms.Padding(0);
            this.txtMoveList.Multiline = true;
            this.txtMoveList.Name = "txtMoveList";
            this.txtMoveList.ReadOnly = true;
            this.txtMoveList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMoveList.Size = new System.Drawing.Size(236, 235);
            this.txtMoveList.TabIndex = 0;
            this.txtMoveList.TabStop = false;
            this.txtMoveList.Text = "1. e4 e5 2. d4 xd4";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.textBox2);
            this.panel1.Controls.Add(this.textBox1);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(492, 50);
            this.panel1.TabIndex = 2;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label6.Location = new System.Drawing.Point(389, 26);
            this.label6.Margin = new System.Windows.Forms.Padding(0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(90, 14);
            this.label6.TabIndex = 7;
            this.label6.Text = "00.000";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label5.Location = new System.Drawing.Point(146, 26);
            this.label5.Margin = new System.Windows.Forms.Padding(0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 14);
            this.label5.TabIndex = 6;
            this.label5.Text = "00.000";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label4.Location = new System.Drawing.Point(299, 26);
            this.label4.Margin = new System.Windows.Forms.Padding(0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 14);
            this.label4.TabIndex = 5;
            this.label4.Text = "00:00.000";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label3.Location = new System.Drawing.Point(56, 26);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 14);
            this.label3.TabIndex = 4;
            this.label3.Text = "00:00.000";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.Control;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBox2.Location = new System.Drawing.Point(299, 11);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(190, 12);
            this.textBox2.TabIndex = 3;
            this.textBox2.Text = "XXXXXXXXXXXXXXXXXXXXXXXX";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Control;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBox1.Location = new System.Drawing.Point(56, 11);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(190, 12);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "XXXXXXXXXXXXXXXXXXXXXXXX";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(246, 10);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 14);
            this.label2.TabIndex = 1;
            this.label2.Text = "Black:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(6, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "White:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // GameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(492, 326);
            this.Controls.Add(this.txtMoveList);
            this.Controls.Add(this.lblCurrentMoveLine);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.rtbChessBoard);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "GameForm";
            this.Text = "XXXXXXXXXXXX - Round 1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GameForm_FormClosed);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private RichTextBox rtbChessBoard;
        private Label lblCurrentMoveLine;
        private TextBox txtMoveList;
        private Panel panel1;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private TextBox textBox2;
        private TextBox textBox1;
        private Label label2;
        private Label label1;
    }
}