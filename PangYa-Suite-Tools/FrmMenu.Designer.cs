namespace PangYa_Suite_Tools
{
    partial class FrmMenu
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnOpenPakMaker;
        private System.Windows.Forms.Button btnOpenUpdateList;
        private System.Windows.Forms.Button btnOpenIffManager;
        private System.Windows.Forms.Label lblTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnOpenPakMaker = new Button();
            btnOpenUpdateList = new Button();
            btnOpenIffManager = new Button();
            lblTitle = new Label();
            cbLanguage = new ComboBox();
            label1 = new Label();
            SuspendLayout();
            // 
            // btnOpenPakMaker
            // 
            btnOpenPakMaker.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenPakMaker.Location = new Point(42, 70);
            btnOpenPakMaker.Name = "btnOpenPakMaker";
            btnOpenPakMaker.Size = new Size(300, 50);
            btnOpenPakMaker.TabIndex = 1;
            btnOpenPakMaker.Text = "📦 Gerenciador de Arquivos PAK";
            btnOpenPakMaker.UseVisualStyleBackColor = true;
            btnOpenPakMaker.Click += btnOpenPakMaker_Click;
            // 
            // btnOpenUpdateList
            // 
            btnOpenUpdateList.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenUpdateList.Location = new Point(42, 135);
            btnOpenUpdateList.Name = "btnOpenUpdateList";
            btnOpenUpdateList.Size = new Size(300, 50);
            btnOpenUpdateList.TabIndex = 2;
            btnOpenUpdateList.Text = "🌐 Gerenciador de Patch / UpdateList";
            btnOpenUpdateList.UseVisualStyleBackColor = true;
            btnOpenUpdateList.Click += btnOpenUpdateList_Click;
            // 
            // btnOpenIffManager
            // 
            btnOpenIffManager.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenIffManager.Location = new Point(42, 200);
            btnOpenIffManager.Name = "btnOpenIffManager";
            btnOpenIffManager.Size = new Size(300, 50);
            btnOpenIffManager.TabIndex = 3;
            btnOpenIffManager.Text = "📝 Editor / Manager IFF";
            btnOpenIffManager.UseVisualStyleBackColor = true;
            btnOpenIffManager.Click += btnOpenIffManager_Click;
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(12, 19);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(360, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Pangya Studio Suite - Developer Tool";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cbLanguage
            // 
            cbLanguage.FormattingEnabled = true;
            cbLanguage.Items.AddRange(new object[] { "Portugues", "English" });
            cbLanguage.Location = new Point(131, 282);
            cbLanguage.Name = "cbLanguage";
            cbLanguage.Size = new Size(121, 23);
            cbLanguage.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(84, 286);
            label1.Name = "label1";
            label1.Size = new Size(47, 15);
            label1.TabIndex = 5;
            label1.Text = "Idioma:";
            // 
            // FrmMenu
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(384, 332);
            Controls.Add(label1);
            Controls.Add(cbLanguage);
            Controls.Add(btnOpenIffManager);
            Controls.Add(btnOpenUpdateList);
            Controls.Add(btnOpenPakMaker);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmMenu";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Pangya Studio - Menu Principal";
            ResumeLayout(false);
            PerformLayout();
        }

        private ComboBox cbLanguage;
        private Label label1;
    }
}