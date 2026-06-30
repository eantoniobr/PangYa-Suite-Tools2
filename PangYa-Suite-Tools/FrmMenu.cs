using System;
using System.ComponentModel;
using System.Windows.Forms;
namespace PangYa_Suite_Tools
{
    public partial class FrmMenu : Form
    {
        // Variável de controle para evitar loops visuais durante a inicialização
        private bool _isInitializing = true;
        string CurrentLanguage = "";
        public FrmMenu()
        {
            InitializeComponent();
            cbLanguage.Items.Clear();
            cbLanguage.Items.Add("Português (BR)");
            cbLanguage.Items.Add("English (US)"); 

            // Vincula o evento de mudança de índice do ComboBox programaticamente
            cbLanguage.SelectedIndexChanged += cbLanguage_SelectedIndexChanged;

            // Configura os itens visualmente limpos e define o padrão baseado na classe global
            cbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLanguage.SelectedIndex = 1; 
            _isInitializing = false;

            // Aplica a localização inicial
            ApplyLocalization("en");
        }

        private void cbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializing) return;

            // Atualiza o estado global com base na seleção do ComboBox
            CurrentLanguage = (cbLanguage.SelectedIndex == 1) ? "en" : "br";

            // Aplica imediatamente no Menu atual
            ApplyLocalization(CurrentLanguage);
        }

        private void btnOpenPakMaker_Click(object sender, EventArgs e)
        {
            var pakMaker = new FrmPakMaker();
            this.Hide();
            pakMaker.ShowDialog();

            // Quando voltar do PakMaker, sincroniza o ComboBox e a tradução local
            // caso o usuário tenha trocado o idioma na barra de status de lá!
            SynchronizeLanguage();
            this.Show();
        }

        private void btnOpenUpdateList_Click(object sender, EventArgs e)
        {
            var updateList = new FrmUpdateList();
            this.Hide();
            updateList.ShowDialog();

            SynchronizeLanguage();
            this.Show();
        }

        private void btnOpenIffManager_Click(object sender, EventArgs e)
        {
            var iffManager = new FrmIFFManager();
            this.Hide();
            iffManager.ShowDialog();

            SynchronizeLanguage();
            this.Show();
        }

        /// <summary>
        /// Sincroniza o índice do ComboBox com a configuração global atualizada
        /// </summary>
        private void SynchronizeLanguage()
        {
            _isInitializing = true;
            cbLanguage.SelectedIndex = (CurrentLanguage == "en") ? 1 : 0;
            _isInitializing = false;

            ApplyLocalization(CurrentLanguage);
        }

        /// <summary>
        /// Aplica as strings traduzidas vindas do arquivo .resx
        /// </summary>
        private void ApplyLocalization(string lang)
        {
            ComponentResourceManager res = new ComponentResourceManager(typeof(FrmMenu));
            string suffix = (lang == "en") ? "_en" : "_br";

            this.Text = res.GetString($"FrmMenu{suffix}") ?? this.Text;
            lblTitle.Text = res.GetString($"lblTitle{suffix}") ?? lblTitle.Text;
            label1.Text = res.GetString($"label1{suffix}") ?? label1.Text;

            btnOpenPakMaker.Text = res.GetString($"btnOpenPakMaker{suffix}") ?? btnOpenPakMaker.Text;
            btnOpenUpdateList.Text = res.GetString($"btnOpenUpdateList{suffix}") ?? btnOpenUpdateList.Text;
            btnOpenIffManager.Text = res.GetString($"btnOpenIffManager{suffix}") ?? btnOpenIffManager.Text;
        }
    }
}