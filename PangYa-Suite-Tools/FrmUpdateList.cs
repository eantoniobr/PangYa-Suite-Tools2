
using System.Text; 
using PangyaAPI.UpdateList.Flags; 
using PangyaAPI.UpdateList.Models;
namespace PangYa_Suite_Tools
{
    public partial class FrmUpdateList : Form
    {
        private readonly Dictionary<string, FileStateApp> _fileCache = new(StringComparer.OrdinalIgnoreCase); 
        private UpdateMaker? _updateMaker;
        private UpdateHeader? _updateHeader;
        private List<UpdateEntry> _updateEntries = new();

        private FileSystemWatcher? _watcher;
        private readonly Lock _generatorLock = new();
        private bool _isMonitoring = false;

        public FrmUpdateList()
        {
            InitializeComponent();
            SetupComponents();
        }

        private void SetupComponents()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Ativa o suporte de Drag-and-Drop visual na Aba 1
            pnlCryptoDrop.AllowDrop = true;
            pnlCryptoDrop.DragEnter += PnlCryptoDrop_DragEnter;
            pnlCryptoDrop.DragDrop += pnlCryptoDrop_DragDrop;

            // Alinha as chaves predefinidas do ComboBox de Região
            cboFileKey.Items.Clear();
            cboFileKey.Items.AddRange(new string[] { "JP", "TH", "US", "KR", "ID", "EU" });
            cboFileKey.SelectedIndex = 0; // "JP" como padrão estável

            // Valores de inicialização padrão sugeridos para os novos inputs da Aba 2
            txtPatchVersion.Text = "JP.R7.983.00";
            txtUpdateListVer.Text = DateTime.Now.ToString("yyyyMMdd01");
            txtClientPatchNum.Text = "1";

            Log("Interface inicializada no formato multi-abas (Estilo PakMaker). Pronta para uso.");
        }

        #region ABA 1: VISUALIZADOR / DECRYPT DE XML

        private void PnlCryptoDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private async void pnlCryptoDrop_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (files.Length == 0) return;

            string targetFile = files[0];

            txtXmlViewer.Clear();
            lblDropHint.Text = $"Processando: {Path.GetFileName(targetFile)}...";

            string selectedKeyName = string.Empty;
            this.Invoke(() => selectedKeyName = cboFileKey.SelectedItem!.ToString()!);

            await Task.Run(() =>
            {
                try
                {
                    var operacao = UpdateKeyDetector.IsFileCrypt(targetFile);

                    if (operacao == OperacaoEnum.Decrypt)
                    {
                        this.Invoke(() => Log($"🔒 Arquivo protegido detectado. Testando chave [{selectedKeyName}]..."));

                        uint[] selectedKey = GetKeysByName(selectedKeyName);
                        var reader = new UpdateReader(selectedKey);

                        try
                        {
                            var (header, entries) = reader.ReadUpdateList(targetFile);

                            if (entries != null && entries.Count > 0)
                            {
                                byte[] rawDoc = reader.XteaDecrypt(targetFile);
                                int num = Array.IndexOf(rawDoc, (byte)0);
                                string xmlText = Encoding.GetEncoding("euc-kr").GetString(rawDoc, 0, num == -1 ? rawDoc.Length : num);

                                // Embeleza o XML antes de mandar para a tela
                                string formattedXml = FormatXml(xmlText);

                                this.Invoke(() => {
                                    txtXmlViewer.Text = formattedXml;
                                    lblDropHint.Text = "🪂 Arraste e solte um arquivo 'updatelist' criptografado aqui para visualizar o XML decodificado em tempo real.";
                                    Log($"✅ [SUCESSO] Descriptografado com a chave {selectedKeyName}!");
                                });
                                return;
                            }
                        }
                        catch
                        {
                            this.Invoke(() => Log($"⚠️ Falha com a chave [{selectedKeyName}]. Iniciando scanner de força-bruta automático..."));
                        }

                        var result = UpdateKeyDetector.DetectAndSetKey(targetFile, out uint[]? autoDetectedKey, out byte[]? decryptedData, out string document);

                        if (result == UpdateResult.Sucess && decryptedData != null)
                        {
                            int num = Array.IndexOf(decryptedData, (byte)0);
                            string xmlText = Encoding.GetEncoding("euc-kr").GetString(decryptedData, 0, num == -1 ? decryptedData.Length : num);

                            string formattedXml = FormatXml(xmlText);

                            this.Invoke(() => {
                                txtXmlViewer.Text = formattedXml;
                                lblDropHint.Text = "🪂 Arraste e solte um arquivo 'updatelist' criptografado aqui para visualizar o XML decodificado em tempo real.";
                                Log($"✅ [BRUTE-FORCE SUCESSO] Chave identificada com sucesso!");
                            });
                        }
                        else
                        {
                            this.Invoke(() => {
                                lblDropHint.Text = "❌ Erro: Nenhuma chave decodificou a estrutura.";
                                Log("❌ [FALHA TOTAL] Nenhuma chave do banco de dados conhecido conseguiu abrir a estrutura deste arquivo.");
                            });
                        }
                    }
                    else if (operacao == OperacaoEnum.Encrypt)
                    {
                        string xmlText = File.ReadAllText(targetFile, Encoding.GetEncoding("euc-kr"));
                        string formattedXml = FormatXml(xmlText);

                        this.Invoke(() => {
                            txtXmlViewer.Text = formattedXml;
                            lblDropHint.Text = "🪂 Arraste e solte um arquivo 'updatelist' criptografado aqui para visualizar o XML decodificado em tempo real.";
                            Log("📋 O arquivo dropado já está em modo texto/XML puro. Exibido formatado no painel.");
                        });
                    }
                    else
                    {
                        this.Invoke(() => {
                            lblDropHint.Text = "⚠️ Arquivo inválido ou corrompido.";
                            Log("⚠️ Arquivo inválido ou corrompido.");
                        });
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(() => {
                        lblDropHint.Text = "❌ Falha crítica ao analisar arquivo.";
                        Log($"❌ [ERRO DE ANÁLISE] {ex.Message}");
                    });
                }
            });
        }
        #endregion

        #region ABA 2: GERADOR & MONITOR DE UPDATELIST

        private void btnBrowsePangya_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog { Description = "Selecione a pasta raiz do Pangya (onde ficam os arquivos executáveis e .pak)" };
            if (fbd.ShowDialog() == DialogResult.OK) txtPangyaPath.Text = fbd.SelectedPath;
        }

        private void btnBrowseUpdate_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog { Description = "Selecione a pasta do WebServer de destino do Update" };
            if (fbd.ShowDialog() == DialogResult.OK) txtUpdatePath.Text = fbd.SelectedPath;
        }

        private async void btnToggleWatch_Click(object sender, EventArgs e)
        {
            if (_isMonitoring) StopMonitoring();
            else await StartMonitoringAsync();
        }

        private async Task StartMonitoringAsync()
        {
            string pangyaPath = txtPangyaPath.Text;
            string destPath = txtUpdatePath.Text;

            if (!Directory.Exists(pangyaPath) || !Directory.Exists(destPath))
            {
                MessageBox.Show("Verifique se as pastas de Origem e de Destino do WebServer são caminhos de diretórios válidos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnToggleWatch.Enabled = false;
            lblWatchStatus.Text = "Inicializando...";

            try
            {
                string selectedKeyName = cboFileKey.SelectedItem!.ToString()!;
                string patchVersion = txtPatchVersion.Text;
                string updateVersion = txtUpdateListVer.Text;
                string patchNum = txtClientPatchNum.Text;

                uint[] regionKeys = GetKeysByName(selectedKeyName);

                await Task.Run(() =>
                {
                    _updateMaker = new UpdateMaker();
                    this.Invoke(() => Log("Varrendo a árvore de diretórios e gerando mapeamento inicial do cliente..."));

                    string finalOutputPath = Path.Combine(destPath, "updatelist");

                    // Injeção de todos os novos parâmetros capturados direto dos inputs dinâmicos do formulário
                    _updateMaker.GenerateFromDirectory(pangyaPath, finalOutputPath, regionKeys, patchVersion, updateVersion, patchNum);
                });

                _watcher = new FileSystemWatcher(pangyaPath)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };
                _watcher.Changed += OnFileChanged;
                _watcher.Created += OnFileChanged;
                _watcher.EnableRaisingEvents = true;

                _isMonitoring = true;
                btnToggleWatch.Text = "🛑 Parar Monitoramento";
                btnToggleWatch.BackColor = Color.Tomato;
                lblWatchStatus.Text = "MONITORANDO ATIVAMENTE";
                lblWatchStatus.ForeColor = Color.Green;
                Log($"[SERVIÇO] FileSystemWatcher ativo na pasta: {pangyaPath}");
            }
            catch (Exception ex)
            {
                Log($"[ERRO INICIALIZAÇÃO] {ex.Message}");
                StopMonitoring();
            }
            finally
            {
                btnToggleWatch.Enabled = true;
            }
        }

        private void StopMonitoring()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _isMonitoring = false;
            btnToggleWatch.Text = "▶️ Iniciar Monitoramento";
            btnToggleWatch.BackColor = Color.LightGreen;
            lblWatchStatus.Text = "INATIVO";
            lblWatchStatus.ForeColor = Color.DimGray;
            Log("Monitoramento em background foi encerrado.");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath).ToLower();
            if (ext != ".pak" && ext != ".exe" && ext != ".dll") return;

            lock (_generatorLock)
            {
                Thread.Sleep(1000); // Buffer de segurança física do Windows para liberação do lock do arquivo

                if (!File.Exists(e.FullPath)) return;

                var info = new FileInfo(e.FullPath);
                var currentState = new FileStateApp { Length = info.Length, LastWriteTime = info.LastWriteTime };

                if (_fileCache.TryGetValue(e.FullPath, out var last) && last.Length == currentState.Length) return;
                _fileCache[e.FullPath] = currentState;

                this.Invoke(() => Log($"[DETECTADO] Modificação no arquivo: {e.Name}"));

                string pangyaPath = string.Empty;
                string destPath = string.Empty;
                string selectedKeyName = string.Empty;
                string patchVersion = string.Empty;
                string updateVersion = string.Empty;
                string patchNum = string.Empty;

                this.Invoke(() => {
                    pangyaPath = txtPangyaPath.Text;
                    destPath = txtUpdatePath.Text;
                    selectedKeyName = cboFileKey.SelectedItem!.ToString()!;
                    patchVersion = txtPatchVersion.Text;
                    updateVersion = txtUpdateListVer.Text;
                    patchNum = txtClientPatchNum.Text;
                });

                try
                {
                    string destFile = Path.Combine(destPath, e.Name!);
                    string destFileDir = Path.GetDirectoryName(destFile)!;

                    if (!Directory.Exists(destFileDir)) Directory.CreateDirectory(destFileDir);
                    File.Copy(e.FullPath, destFile, true);

                    uint[] regionKeys = GetKeysByName(selectedKeyName);
                    string finalOutputPath = Path.Combine(destPath, "updatelist");

                    // Gera o patch mantendo a paridade de versões modificadas em tempo real
                    _updateMaker?.GenerateFromDirectory(pangyaPath, finalOutputPath, regionKeys, patchVersion, updateVersion, patchNum);

                    this.Invoke(() => Log($"✨ [COMPILADO] updatelist assinado com sucesso! Gatilho: {e.Name}"));
                }
                catch (Exception ex)
                {
                    this.Invoke(() => Log($"[ERRO E/S] Não foi possível gerenciar o arquivo {e.Name}: {ex.Message}"));
                }
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES

        private uint[] GetKeysByName(string name)
        {
            return name.ToUpper() switch
            {
                "TH" => UpdateKeys.TH,
                "JP" => UpdateKeys.JP,
                "US" => UpdateKeys.GB,
                "GB" => UpdateKeys.GB,
                "KR" => UpdateKeys.KR,
                "ID" => UpdateKeys.ID,
                "EU" => UpdateKeys.EU,
                _ => UpdateKeys.JP
            };
        }

        private void Log(string text)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private static string FormatXml(string rawXml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawXml)) return string.Empty;

                // Trata possíveis quebras incorretas ou espaços no início/fim antes do parse
                rawXml = rawXml.Trim();

                var doc = System.Xml.Linq.XDocument.Parse(rawXml);
                var settings = new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    ", // 4 Espaços para manter legível
                    NewLineOnAttributes = false, // Atributos na mesma linha
                    OmitXmlDeclaration = false,
                    NewLineHandling = System.Xml.NewLineHandling.Replace, // Força o tratamento de novas linhas
                    NewLineChars = "\r\n" // Garante o padrão do Windows (CRLF) exigido pelo TextBox
                };

                using var stringWriter = new StringWriter();
                using (var xmlWriter = System.Xml.XmlWriter.Create(stringWriter, settings))
                {
                    doc.Save(xmlWriter);
                }

                string result = stringWriter.ToString();

                // Garantia extra: se o XmlWriter ainda deixar passar algum '\n' isolado, 
                // normaliza tudo para o padrão que o TextBox do WinForms aceita
                return result.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            }
            catch
            {
                // Fallback de segurança: Caso falhe, tenta pelo menos normalizar as quebras brutas
                return rawXml.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            }
        }
        #endregion
    }

    public class FileStateApp
    {
        public long Length { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}