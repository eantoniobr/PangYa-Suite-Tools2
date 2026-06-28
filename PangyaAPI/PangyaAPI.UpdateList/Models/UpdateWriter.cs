using PangyaAPI.Utilities.Cryptography;
using System.Text;

namespace PangyaAPI.UpdateList.Models
{
    public class UpdateWriter
    {
        private readonly uint[] _cryptoKeys;

        public UpdateWriter(uint[] keys)
        {
            _cryptoKeys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        public void WriteUpdateList(string outputPath, UpdateHeader header, List<UpdateEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                Console.WriteLine("Nenhuma alteração para salvar.");
                return;
            }

            // Define o arquivo XML temporário na mesma pasta do destino final
            string directory = Path.GetDirectoryName(outputPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            string tempXmlPath = Path.Combine(directory, "updatelist_temp.xml");

            // Garante o registro do provider euc-kr se estiver no .NET moderno
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (StreamWriter streamWriter = new StreamWriter(tempXmlPath, append: false, Encoding.GetEncoding("euc-kr")))
            {
                streamWriter.WriteLine("<?xml version=\"1.0\" encoding=\"euc-kr\" standalone=\"yes\" ?>");
                streamWriter.WriteLine("<patchVer value=\"" + header.ClientPatchVersion + "\" />");
                streamWriter.WriteLine("<patchNum value=\"" + header.ClientPatchNum + "\" />");
                streamWriter.WriteLine("<updatelistVer value=\"" + header.UpdateVersion + "\" />");
                streamWriter.WriteLine($"<updatefiles count=\"{entries.Count}\">");

                foreach (var xmlFile in entries)
                {
                    streamWriter.WriteLine($"\t<fileinfo fname=\"{xmlFile.fname}\" fdir=\"{xmlFile.fdir}\" fsize=\"{xmlFile.fsize}\" fcrc=\"{xmlFile.fcrc}\" fdate=\"{xmlFile.fdate}\" ftime=\"{xmlFile.ftime}\" pname=\"{xmlFile.pname}\" psize=\"{xmlFile.psize}\" />");
                }
                streamWriter.Write("</updatefiles>");
            }

            // 1. Lê os bytes do XML temporário gerado
            byte[] rawXmlBytes = File.ReadAllBytes(tempXmlPath);

            // 2. CORREÇÃO: Criptografa os dados e captura o array de bytes resultante
            byte[] encryptedData = XteaEncrypt(rawXmlBytes);

            // 3. CORREÇÃO: Grava o arquivo final criptografado no caminho de destino (outputPath)
            File.WriteAllBytes(outputPath, encryptedData);

            // Limpa o arquivo XML temporário
            if (File.Exists(tempXmlPath))
            {
                File.Delete(tempXmlPath);
            }

            Console.WriteLine($"UpdateList gerada com sucesso em: {outputPath}");
        }

        public byte[] XteaEncrypt(byte[] rawData)
        {
            Xtea.EncipherStreamPadNull(_cryptoKeys, new MemoryStream(rawData), out byte[] _result);
            return _result;
        }
    }
}