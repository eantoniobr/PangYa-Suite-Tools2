namespace PangYa_Suite_Tools
{
    public partial class FrmIFFManager : Form
    {
        private Form? _activeEditorForm = null;

        public FrmIFFManager()
        {
            InitializeComponent();
        }

        private void btnBrowseIffDir_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Selecione a pasta extraída contendo os arquivos .iff (ex: Character.iff, Item.iff)"
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtIffDirectory.Text = fbd.SelectedPath;
                LoadIffFiles(fbd.SelectedPath);
            }
        }

        private void LoadIffFiles(string directoryPath)
        {
            lstIffFiles.Items.Clear();
            CloseActiveEditor();

            try
            {
                // Busca por todos os arquivos .iff na pasta selecionada
                string[] files = Directory.GetFiles(directoryPath, "*.iff", SearchOption.TopDirectoryOnly);

                foreach (string file in files)
                {
                    lstIffFiles.Items.Add(Path.GetFileName(file));
                }

                lblStatus.Text = $"Varredura concluída. {lstIffFiles.Items.Count} arquivos .iff encontrados.";

                if (lstIffFiles.Items.Count == 0)
                {
                    MessageBox.Show("Nenhum arquivo com a extensão '.iff' foi localizado nesta pasta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao listar diretório: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lstIffFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstIffFiles.SelectedItem == null) return;

            string selectedFileName = lstIffFiles.SelectedItem.ToString()!;
            string fullPath = Path.Combine(txtIffDirectory.Text, selectedFileName);

            LoadSpecificIffEditor(selectedFileName, fullPath);
        }

        private void LoadSpecificIffEditor(string filename, string fullPath)
        {
            CloseActiveEditor();
            lblNoFileSelected.Visible = false;

            Form? targetForm = null;

            // Roteamento inteligente baseado no nome do arquivo IFF
            switch (filename.ToLower())
            {
                case "character.iff":
                    // targetForm = new FrmIffCharacter(fullPath); 
                    break;

                case "item.iff":
                    // targetForm = new FrmIffItem(fullPath);
                    break;

                // Adicione os novos cases conforme for criando as views de cada struct
                default:
                    break;
            }

            if (targetForm != null)
            {
                _activeEditorForm = targetForm;

                // Configura o Form para se comportar como um controle comum de painel (Injeção de View)
                targetForm.TopLevel = false;
                targetForm.FormBorderStyle = FormBorderStyle.None;
                targetForm.Dock = DockStyle.Fill;

                pnlEditorContainer.Controls.Add(targetForm);
                pnlEditorContainer.Tag = targetForm;
                targetForm.Show();

                lblStatus.Text = $"Editando estrutura de: {filename}";
            }
            else
            {
                lblNoFileSelected.Text = $"⚠️ A estrutura/layout de editor para o arquivo '{filename}'\nainda não foi implementada ou mapeada.";
                lblNoFileSelected.Visible = true;
                lblStatus.Text = $"Editor não disponível para: {filename}";
            }
        }

        private void CloseActiveEditor()
        {
            if (_activeEditorForm != null)
            {
                _activeEditorForm.Close();
                _activeEditorForm.Dispose();
                _activeEditorForm = null;
            }
            lblNoFileSelected.Text = "Selecione um arquivo .iff na lista ao lado para carregar a tabela de edição.";
            lblNoFileSelected.Visible = true;
        }
    }
}

