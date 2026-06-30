using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PangYa_Suite_Tools
{
    public partial class FrmMenu : Form
    {
        private bool isInitializingLanguages = true;

        public FrmMenu()
        {
            InitializeComponent();
            InitializeLanguageComboBox();
        }

        private void InitializeLanguageComboBox()
        {
            cboLanguage.ComboBox.DisplayMember = "Key";
            cboLanguage.ComboBox.ValueMember = "Value";

            cboLanguage.Items.Add(new KeyValuePair<string, string>("Português (BR)", "br"));
            cboLanguage.Items.Add(new KeyValuePair<string, string>("English (US)", "en"));
            cboLanguage.SelectedIndex = 1;

            isInitializingLanguages = false;
            ApplyLocalization("en");
        }

        private void cboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializingLanguages) return;

            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                ApplyLocalization(selectedItem.Value);
            }
        }

        private void ApplyLocalization(string lang)
        {
            ComponentResourceManager res = new ComponentResourceManager(typeof(FrmMenu));
            string suffix = (lang == "en") ? "_en" : "_br";

            this.Text = res.GetString($"FrmMenu{suffix}") ?? this.Text;
            lblTitle.Text = res.GetString($"lblTitle{suffix}") ?? lblTitle.Text;
            btnOpenPakMaker.Text = res.GetString($"btnOpenPakMaker{suffix}") ?? btnOpenPakMaker.Text;
            btnOpenUpdateList.Text = res.GetString($"btnOpenUpdateList{suffix}") ?? btnOpenUpdateList.Text;
            btnOpenIffManager.Text = res.GetString($"btnOpenIffManager{suffix}") ?? btnOpenIffManager.Text;
            lblLanguage.Text = res.GetString($"lblLanguage{suffix}") ?? lblLanguage.Text;
        }

        private void btnOpenPakMaker_Click(object sender, EventArgs e)
        {
            var pakMaker = new FrmPakMaker();
            this.Hide();
            pakMaker.ShowDialog();
            this.Show();
        }

        private void btnOpenUpdateList_Click(object sender, EventArgs e)
        {
            var updateList = new FrmUpdateList();
            this.Hide();
            updateList.ShowDialog();
            this.Show();
        }

        private void btnOpenIffManager_Click(object sender, EventArgs e)
        {
            //vai demorar muito para mim fazer-lo, pois o codigo precisa ser bem organizado
            //eu poderia fazer-lo 1 dia, mas eu tenho outras tarefas.
            //base sera bem fraca no inicio, mas depois que toma forma, fica algo gigantesco.
            var iffManager = new FrmIFFManager();
            this.Hide();
            iffManager.ShowDialog();
            this.Show();
        }
    }
}