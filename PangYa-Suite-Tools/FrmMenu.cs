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
        public FrmMenu()
        {
            InitializeComponent();
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