using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PangyaAPI.UpdateList.Models
{
    public class UpdateMaker
    {
        private readonly Crc32 _crcCalculator = new Crc32();

        /// <summary>
        /// Varre uma pasta de arquivos de atualização e gera o arquivo updatelist finalizado
        /// </summary>
        public void GenerateFromDirectory(string targetFolder, string outputPath, uint[] regionKeys, string patchVersion, string updateVersion = "20090331", string clientPatchNum = "1")
        {
            if (!Directory.Exists(targetFolder))
                throw new DirectoryNotFoundException($"Diretório alvo não existe: {targetFolder}");

            var entries = new List<UpdateEntry>();

            // Invoca o seu método privado/recursivo de listagem para ignorar extensões indesejadas (.cln, .json)
            var files = ListarArquivos(targetFolder);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);

                // Ignora o próprio arquivo de lista final se ele estiver na mesma pasta de varredura
                if (fileInfo.Name.Equals(Path.GetFileName(outputPath), StringComparison.OrdinalIgnoreCase))
                    continue;

                // CORREÇÃO: Calcula o caminho relativo com base na pasta raiz usando Uri (idêntico ao seu GetXmlFileInfo antigo)
                string basePatchDir = targetFolder.EndsWith("\\") ? targetFolder : (targetFolder + "\\");
                string relativePath = new Uri(basePatchDir).MakeRelativeUri(new Uri(file)).ToString().Replace("/", "\\");
                string finalFdir = "\\" + Path.GetDirectoryName(relativePath);

                var entry = new UpdateEntry
                {
                    fname = fileInfo.Name,
                    fdir = finalFdir, // Garante o formato "\pasta\subpasta" exigido pelo jogo
                    fsize = fileInfo.Length,
                    fcrc = _crcCalculator.CalculateFileCRC(file),
                    fdate = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd"), // Uso do Utc como no legado
                    ftime = fileInfo.LastWriteTimeUtc.ToString("HH:mm:ss"),
                    pname = fileInfo.Name + ".zip", // Mantém o comportamento original (.zip no pname)
                    psize = 717469 // Tamanho fake inicial mantido para sua futura implementação de GUI/Compressão
                };

                entries.Add(entry);
            }

            // Monta o Header utilizando os parâmetros recebidos
            var header = new UpdateHeader
            {
                ClientPatchVersion = patchVersion,
                ClientPatchNum = clientPatchNum,
                UpdateVersion = updateVersion
            };

            // Invoca o Writer passando as chaves da região selecionada
            var writer = new UpdateWriter(regionKeys);
            writer.WriteUpdateList(outputPath, header, entries);
        }
         
        private List<string> ListarArquivos(string pasta)
        {
            List<string> list = new List<string>();

            if (!Directory.Exists(pasta)) return list;

            string[] directories = Directory.GetDirectories(pasta);
            foreach (string text in directories)
            {
                if (!text.Contains(".cln") && !text.Contains(".json"))
                {
                    list.AddRange(ListarArquivos(text));
                }
            }

            string[] files = Directory.GetFiles(pasta);
            foreach (string text2 in files)
            {
                if (!text2.EndsWith(".cln") && !text2.EndsWith(".json"))
                {
                    list.Add(text2);
                }
            }
            return list;
        }
    }
}