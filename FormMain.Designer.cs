namespace JP.InvestCalc
{
	partial class FormMain
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
			this.pnlTab = new System.Windows.Forms.TableLayoutPanel();
			this.txtReturnAvg = new System.Windows.Forms.TextBox();
			this.lblReturnAvg = new System.Windows.Forms.Label();
			this.table = new System.Windows.Forms.DataGridView();
			this.mnuOperate = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnuBuy = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuSell = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuDiv = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuCost = new System.Windows.Forms.ToolStripMenuItem();
			this.txtTotal = new System.Windows.Forms.TextBox();
			this.lblDate = new System.Windows.Forms.Label();
			this.lblValueTotal = new System.Windows.Forms.Label();
			this.pnl = new System.Windows.Forms.Panel();
			this.colStock = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colShares = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colReturn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.pnlTab.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.table)).BeginInit();
			this.mnuOperate.SuspendLayout();
			this.pnl.SuspendLayout();
			this.SuspendLayout();
			// 
			// pnlTab
			// 
			this.pnlTab.ColumnCount = 2;
			this.pnlTab.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 49.99999F));
			this.pnlTab.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.pnlTab.Controls.Add(this.txtReturnAvg, 1, 3);
			this.pnlTab.Controls.Add(this.lblReturnAvg, 0, 3);
			this.pnlTab.Controls.Add(this.table, 0, 1);
			this.pnlTab.Controls.Add(this.txtTotal, 1, 2);
			this.pnlTab.Controls.Add(this.lblDate, 0, 0);
			this.pnlTab.Controls.Add(this.lblValueTotal, 0, 2);
			this.pnlTab.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnlTab.Location = new System.Drawing.Point(10, 10);
			this.pnlTab.Margin = new System.Windows.Forms.Padding(4);
			this.pnlTab.Name = "pnlTab";
			this.pnlTab.RowCount = 4;
			this.pnlTab.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.pnlTab.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.pnlTab.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.pnlTab.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.pnlTab.Size = new System.Drawing.Size(562, 433);
			this.pnlTab.TabIndex = 0;
			// 
			// txtReturnAvg
			// 
			this.txtReturnAvg.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtReturnAvg.Location = new System.Drawing.Point(283, 403);
			this.txtReturnAvg.Name = "txtReturnAvg";
			this.txtReturnAvg.ReadOnly = true;
			this.txtReturnAvg.Size = new System.Drawing.Size(276, 27);
			this.txtReturnAvg.TabIndex = 5;
			this.txtReturnAvg.TabStop = false;
			this.txtReturnAvg.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// lblReturnAvg
			// 
			this.lblReturnAvg.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.lblReturnAvg.AutoSize = true;
			this.lblReturnAvg.Location = new System.Drawing.Point(153, 406);
			this.lblReturnAvg.Name = "lblReturnAvg";
			this.lblReturnAvg.Size = new System.Drawing.Size(124, 20);
			this.lblReturnAvg.TabIndex = 4;
			this.lblReturnAvg.Text = "Average return:";
			// 
			// table
			// 
			this.table.AllowUserToAddRows = false;
			this.table.AllowUserToDeleteRows = false;
			this.table.AllowUserToResizeRows = false;
			this.table.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.table.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.table.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.table.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colStock,
            this.colShares,
            this.colPrice,
            this.colValue,
            this.colReturn});
			this.pnlTab.SetColumnSpan(this.table, 2);
			this.table.ContextMenuStrip = this.mnuOperate;
			this.table.Dock = System.Windows.Forms.DockStyle.Fill;
			this.table.Location = new System.Drawing.Point(4, 24);
			this.table.Margin = new System.Windows.Forms.Padding(4);
			this.table.Name = "table";
			this.table.RowTemplate.Height = 24;
			this.table.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.table.Size = new System.Drawing.Size(554, 339);
			this.table.TabIndex = 1;
			// 
			// mnuOperate
			// 
			this.mnuOperate.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.mnuOperate.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.mnuOperate.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuBuy,
            this.mnuSell,
            this.mnuDiv,
            this.mnuCost});
			this.mnuOperate.Name = "mnuOperate";
			this.mnuOperate.Size = new System.Drawing.Size(148, 116);
			// 
			// mnuBuy
			// 
			this.mnuBuy.Name = "mnuBuy";
			this.mnuBuy.Size = new System.Drawing.Size(147, 28);
			this.mnuBuy.Text = "Buy";
			// 
			// mnuSell
			// 
			this.mnuSell.Name = "mnuSell";
			this.mnuSell.Size = new System.Drawing.Size(147, 28);
			this.mnuSell.Text = "Sell";
			// 
			// mnuDiv
			// 
			this.mnuDiv.Name = "mnuDiv";
			this.mnuDiv.Size = new System.Drawing.Size(147, 28);
			this.mnuDiv.Text = "Dividend";
			// 
			// mnuCost
			// 
			this.mnuCost.Name = "mnuCost";
			this.mnuCost.Size = new System.Drawing.Size(147, 28);
			this.mnuCost.Text = "Cost";
			// 
			// txtTotal
			// 
			this.txtTotal.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtTotal.Location = new System.Drawing.Point(283, 370);
			this.txtTotal.Name = "txtTotal";
			this.txtTotal.ReadOnly = true;
			this.txtTotal.Size = new System.Drawing.Size(276, 27);
			this.txtTotal.TabIndex = 3;
			this.txtTotal.TabStop = false;
			this.txtTotal.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// lblDate
			// 
			this.lblDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblDate.AutoSize = true;
			this.pnlTab.SetColumnSpan(this.lblDate, 2);
			this.lblDate.Location = new System.Drawing.Point(3, 0);
			this.lblDate.Name = "lblDate";
			this.lblDate.Size = new System.Drawing.Size(91, 20);
			this.lblDate.TabIndex = 0;
			this.lblDate.Text = "## Date ##";
			// 
			// lblValueTotal
			// 
			this.lblValueTotal.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.lblValueTotal.AutoSize = true;
			this.lblValueTotal.Location = new System.Drawing.Point(165, 373);
			this.lblValueTotal.Name = "lblValueTotal";
			this.lblValueTotal.Size = new System.Drawing.Size(112, 20);
			this.lblValueTotal.TabIndex = 2;
			this.lblValueTotal.Text = "TOTAL value:";
			// 
			// pnl
			// 
			this.pnl.AutoSize = true;
			this.pnl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.pnl.Controls.Add(this.pnlTab);
			this.pnl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnl.Location = new System.Drawing.Point(0, 0);
			this.pnl.Name = "pnl";
			this.pnl.Padding = new System.Windows.Forms.Padding(10);
			this.pnl.Size = new System.Drawing.Size(582, 453);
			this.pnl.TabIndex = 0;
			// 
			// colStock
			// 
			this.colStock.HeaderText = "Stock";
			this.colStock.Name = "colStock";
			this.colStock.ReadOnly = true;
			// 
			// colShares
			// 
			this.colShares.HeaderText = "Shares";
			this.colShares.Name = "colShares";
			this.colShares.ReadOnly = true;
			// 
			// colPrice
			// 
			this.colPrice.HeaderText = "Price";
			this.colPrice.Name = "colPrice";
			// 
			// colValue
			// 
			this.colValue.HeaderText = "Value";
			this.colValue.Name = "colValue";
			this.colValue.ReadOnly = true;
			// 
			// colReturn
			// 
			this.colReturn.HeaderText = "Yearly";
			this.colReturn.Name = "colReturn";
			this.colReturn.ReadOnly = true;
			// 
			// FormMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(582, 453);
			this.Controls.Add(this.pnl);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MinimumSize = new System.Drawing.Size(500, 400);
			this.Name = "FormMain";
			this.Text = "Return on investments";
			this.pnlTab.ResumeLayout(false);
			this.pnlTab.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.table)).EndInit();
			this.mnuOperate.ResumeLayout(false);
			this.pnl.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView table;
		private System.Windows.Forms.TextBox txtTotal;
		private System.Windows.Forms.Panel pnl;
		private System.Windows.Forms.Label lblDate;
		private System.Windows.Forms.TextBox txtReturnAvg;
		private System.Windows.Forms.ToolStripMenuItem mnuBuy;
		private System.Windows.Forms.ToolStripMenuItem mnuSell;
		private System.Windows.Forms.ToolStripMenuItem mnuDiv;
		private System.Windows.Forms.ToolStripMenuItem mnuCost;
		private System.Windows.Forms.TableLayoutPanel pnlTab;
		private System.Windows.Forms.Label lblReturnAvg;
		private System.Windows.Forms.ContextMenuStrip mnuOperate;
		private System.Windows.Forms.Label lblValueTotal;
		private System.Windows.Forms.DataGridViewTextBoxColumn colStock;
		private System.Windows.Forms.DataGridViewTextBoxColumn colShares;
		private System.Windows.Forms.DataGridViewTextBoxColumn colPrice;
		private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
		private System.Windows.Forms.DataGridViewTextBoxColumn colReturn;
	}
}

