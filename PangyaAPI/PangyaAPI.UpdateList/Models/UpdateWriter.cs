using PangyaAPI.Utilities.Cryptography;
using System.Text;
using System.Xml;

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

            string directory = Path.GetDirectoryName(outputPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            string tempXmlPath = Path.Combine(directory, "updatelist_temp.xml");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (StreamWriter streamWriter = new StreamWriter(tempXmlPath, append: false, Encoding.GetEncoding("euc-kr")))
            {
                streamWriter.WriteLine("<?xml version=\"1.0\" encoding=\"euc-kr\" standalone=\"yes\" ?>");
                streamWriter.WriteLine("<patchVer value=\"" + XmlEscape(header.ClientPatchVersion) + "\" />");
                streamWriter.WriteLine("<patchNum value=\"" + XmlEscape(header.ClientPatchNum) + "\" />");
                streamWriter.WriteLine("<updatelistVer value=\"" + XmlEscape(header.UpdateVersion) + "\" />");
                streamWriter.WriteLine($"<updatefiles count=\"{entries.Count}\">");

                foreach (var entry in entries)
                {
                    streamWriter.WriteLine("\t" + BuildFileInfoElement(entry));
                }
                streamWriter.Write("</updatefiles>");
            }

            byte[] rawXmlBytes = File.ReadAllBytes(tempXmlPath);
            byte[] encryptedData = XteaEncrypt(rawXmlBytes);
            File.WriteAllBytes(outputPath, encryptedData);

            if (File.Exists(tempXmlPath))
            {
                File.Delete(tempXmlPath);
            }

            Console.WriteLine($"UpdateList gerada com sucesso em: {outputPath}");
        }

        /// <summary>
        /// Monta o elemento &lt;fileinfo .../&gt; iterando sobre UpdateEntryFieldMap.Fields,
        /// em vez de uma interpolação manual com os 8 nomes de atributo hardcoded — assim
        /// qualquer campo adicionado ao mapa aparece aqui automaticamente.
        /// </summary>
        private static string BuildFileInfoElement(UpdateEntry entry)
        {
            var sb = new StringBuilder("<fileinfo");
            foreach (var field in UpdateEntryFieldMap.Fields)
            {
                sb.Append(' ')
                  .Append(field.XmlAttributeName)
                  .Append("=\"")
                  .Append(XmlEscape(field.Get(entry)))
                  .Append('"');
            }
            sb.Append(" />");
            return sb.ToString();
        }

        /// <summary>Escapa caracteres especiais de XML em valores de atributo (nomes de arquivo podem conter & " etc.).</summary>
        private static string XmlEscape(string? value) => SecurityElementEscape(value ?? "");

        private static string SecurityElementEscape(string value) =>
            value.Replace("&", "&amp;")
                 .Replace("\"", "&quot;")
                 .Replace("'", "&apos;")
                 .Replace("<", "&lt;")
                 .Replace(">", "&gt;");

        public byte[] XteaEncrypt(byte[] rawData)
        {
            Xtea.EncipherStreamPadNull(_cryptoKeys, new MemoryStream(rawData), out byte[] _result);
            return _result;
        }
    }
}
