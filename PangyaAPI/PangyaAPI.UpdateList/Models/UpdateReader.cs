using PangyaAPI.Utilities.Cryptography;
using System.Text;
using System.Xml;

namespace PangyaAPI.UpdateList.Models
{
    public class UpdateReader
    {
        private readonly uint[] _cryptoKeys;

        public UpdateReader(uint[] keys)
        {
            _cryptoKeys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        public UpdateReader()
        {
            _cryptoKeys = Array.Empty<uint>();
        }

        public (UpdateHeader Header, List<UpdateEntry> Entries) ReadUpdateList(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Arquivo de update não encontrado: {filePath}");

            var entries = new List<UpdateEntry>();
            var header = new UpdateHeader();
            var Document = XteaDecrypt(filePath);

            if (Document == null || Document.Length == 0)
            {
                return (header, entries);
            }

            int num = Array.IndexOf(Document, (byte)0);
            if (num == -1)
            {
                num = Document.Length;
            }

            // Garante o registro do provider euc-kr se estiver rodando em .NET Core / .NET 10
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string text = Encoding.GetEncoding("euc-kr").GetString(Document, 0, num);

            int num2 = text.IndexOf("<patchVer");
            int num3 = text.LastIndexOf("</updatefiles>") + "</updatefiles>".Length;
            if (num2 != -1 && num3 > num2)
            {
                text = text.Substring(num2, num3 - num2);
            }

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>" + text + "</root>");

            // --- AJUSTE: Populando o Header com os dados reais do XML ---
            var patchVerNode = xmlDocument.SelectSingleNode("//patchVer");
            var patchNumNode = xmlDocument.SelectSingleNode("//patchNum");
            var updateListVerNode = xmlDocument.SelectSingleNode("//updatelistVer");

            header.ClientPatchVersion = patchVerNode?.Attributes["value"]?.Value ?? "";
            header.ClientPatchNum = patchNumNode?.Attributes["value"]?.Value ?? "";
            header.UpdateVersion = updateListVerNode?.Attributes["value"]?.Value ?? "";
            // -------------------------------------------------------------

            entries.Clear();
            foreach (XmlNode item in xmlDocument.SelectNodes("//fileinfo"))
            {
                entries.Add(new UpdateEntry
                {
                    fname = (item.Attributes["fname"]?.Value ?? ""),
                    fdir = (item.Attributes["fdir"]?.Value ?? ""),
                    fsize = long.Parse(item.Attributes["fsize"]?.Value ?? "0"),
                    fcrc = int.Parse(item.Attributes["fcrc"]?.Value ?? "0"),
                    fdate = (item.Attributes["fdate"]?.Value ?? ""),
                    ftime = (item.Attributes["ftime"]?.Value ?? ""),
                    pname = (item.Attributes["pname"]?.Value ?? ""),
                    psize = int.Parse(item.Attributes["psize"]?.Value ?? "0")
                });
            }

            return (header, entries);
        }
         
        public byte[] XteaDecrypt(string _file = "")
        {
            if (!File.Exists(_file)) return Array.Empty<byte>();

            using (FileStream r = File.OpenRead(_file))
            {
                Xtea.DecipherStreamTrimNull(_cryptoKeys, r, out byte[] _result);
                return _result;
            }
        }
    }
}