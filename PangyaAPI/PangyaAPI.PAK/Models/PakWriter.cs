//criado por LUISMK -> github.com/luismk
using PangyaAPI.PAK.Flags;
using PangyaAPI.Utilities.Cryptography; 
using System.Text;

namespace PangyaAPI.PAK.Models
{
    public class PakWriter
    {
        public byte PakVersion = 0x12;// 0x12 para versão 0.1, 0x13 para versão 0.2, 0x14 para versão 0.3
        public uint kXorKey = 0x71u;//version 0x71, 0x80 para versão 0 
        public string Author { get; set; } = "PangyaAPI.PAK";
        public PakFileEntryVersion EntryVersion { get; set; } = PakFileEntryVersion.V3;
        public PakFileEntryType EntryType { get; set; } = PakFileEntryType.LZ772;
        public byte CompressLevel { get; set; } = 5;
        public uint[] LocationKeys { get; set; } = PakKeys.JP;

        public void CreateFromDirectory(string sourceDir, string outputPath,
                                        Action<string>? log = null)
        {
            var entries = new List<(bool isDir, string path)>();

            string dir = sourceDir.TrimEnd('/', '\\');
            entries.Add((true, dir));

            foreach (string entry in Directory.EnumerateFileSystemEntries(dir, "*", SearchOption.AllDirectories))
                entries.Add((Directory.Exists(entry), entry));

            string baseDir = Path.GetDirectoryName(dir) ?? dir;
            if (!baseDir.EndsWith('/') && !baseDir.EndsWith('\\'))
                baseDir += "/";

            CreateFromEntries(entries, baseDir, outputPath, log);
        }

        /// <summary>
        /// Igual a <see cref="CreateFromDirectory"/>, mas empacota apenas o CONTEÚDO de
        /// <paramref name="sourceDir"/>: o nome da própria pasta NÃO entra nos caminhos
        /// internos. Use isso ao reconstruir um PAK a partir de uma pasta temporária
        /// (ex.: nomes como "PakTemp_xxxxx") — caso contrário esse nome aleatório acaba
        /// virando o primeiro segmento de todo caminho interno do PAK.
        /// </summary>
        public void CreateFromDirectoryContents(string sourceDir, string outputPath,
                                                 Action<string>? log = null)
        {
            string dir = sourceDir.TrimEnd('/', '\\');

            var entries = new List<(bool isDir, string path)>();
            foreach (string entry in Directory.EnumerateFileSystemEntries(dir, "*", SearchOption.AllDirectories))
                entries.Add((Directory.Exists(entry), entry));

            string baseDir = dir;
            if (!baseDir.EndsWith('/') && !baseDir.EndsWith('\\'))
                baseDir += "/";

            CreateFromEntries(entries, baseDir, outputPath, log);
        }

        public void CreateFromFile(string filePath, string outputPath,
                                   Action<string>? log = null)
        {
            string baseDir = Path.GetDirectoryName(filePath) ?? "";
            if (!string.IsNullOrEmpty(baseDir) && !baseDir.EndsWith('/') && !baseDir.EndsWith('\\'))
                baseDir += "/";

            var entries = new List<(bool isDir, string path)> { (false, filePath) };
            CreateFromEntries(entries, baseDir, outputPath, log);
        }

        private void CreateFromEntries(List<(bool isDir, string path)> entries,
                         string baseDir,
                         string outputPath,
                         Action<string>? log,
                         Action<int, int>? onProgress = null)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

            var fileEntries = new List<PakFileEntry>();

            // Estrutura intermediária pra guardar o resultado da compressão de cada item,
            // calculado em paralelo, ANTES de escrever sequencialmente no stream.
            var compressedResults = new (bool isDir, string relativeName, byte[] nameField, byte type,
                                          byte[]? compressedData, uint size)[entries.Count];

            int totalFiles = entries.Count(e => !e.isDir);
            int done = 0;
            object progressLock = new();

            // ── FASE 1: leitura + compressão em PARALELO (CPU-bound, usa todos os núcleos) ──
            Parallel.For(0, entries.Count, i =>
            {
                var (isDir, path) = entries[i];

                string relativeName = path.Length == baseDir.Length
                    ? Path.GetFileName(path)
                    : path.Substring(baseDir.Length);
                relativeName = relativeName.Replace('\\', '/');

                byte[] nameBytes = Encoding.ASCII.GetBytes(relativeName);
                int nameLen = nameBytes.Length;
                int nameAligned = (EntryVersion == PakFileEntryVersion.V3)
                    ? ((nameLen / 8 + (nameLen % 8 != 0 ? 1 : 0)) * 8)
                    : (nameLen + 1);

                byte[] nameField = new byte[nameAligned];
                Array.Copy(nameBytes, nameField, nameLen);

                var type = isDir ? PakFileEntryType.Directory : EntryType;
                byte[]? compressed = null;
                uint size = 0u;

                if (!isDir)
                {
                    byte[] fileData = File.ReadAllBytes(path);
                    size = (uint)fileData.Length;

                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext == ".wav" || ext == ".mp3") type = PakFileEntryType.Raw;

                    if (type == PakFileEntryType.Raw)
                        compressed = fileData;
                    else if (type == PakFileEntryType.LZ77)
                        compressed = Lz77.Compress(fileData, CompressLevel, null);
                    else if (type == PakFileEntryType.LZ772)
                        compressed = Lz772.Compress(fileData, CompressLevel, null);

                    if (compressed == null)
                        throw new InvalidOperationException($"Falha ao comprimir: {path}");

                    int progressDone = Interlocked.Increment(ref done);
                    lock (progressLock) { onProgress?.Invoke(progressDone, totalFiles); }
                }

                compressedResults[i] = (isDir, relativeName, nameField, (byte)type, compressed, size);
            });

            // ── FASE 2: escrita SEQUENCIAL no stream (mantém offsets corretos) ──
            foreach (var (isDir, relativeName, nameField, typeRaw, compressed, size) in compressedResults)
            {
                var type = (PakFileEntryType)typeRaw;
                uint currentOffset = (uint)bw.BaseStream.Position;
                uint compSz = 0u;

                if (!isDir && compressed != null)
                {
                    compSz = (uint)compressed.Length;
                    bw.Write(compressed);
                }

                var entry = new PakFileEntry
                {
                    NameLength = (byte)(EntryVersion == PakFileEntryVersion.V3 ? nameField.Length : Encoding.ASCII.GetByteCount(relativeName)),
                    Type = type,
                    Version = EntryVersion,
                    Offset = currentOffset,
                    CompressSize = compSz,
                    Size = size,
                };
                entry.SetRawNameForWrite(nameField);
                fileEntries.Add(entry);
            }

            // ── resto do método permanece IGUAL (escrita do autor + tabela de entries + footer) ──
            byte[] authorBytes = Encoding.ASCII.GetBytes(Author);
            bw.Write(authorBytes);
            ushort authorLenBE = (ushort)((authorBytes.Length >> 8) | (authorBytes.Length << 8));
            bw.Write(authorLenBE);

            uint offsetFileEntry = (uint)bw.BaseStream.Position;

            foreach (var fe in fileEntries)
            {
                bw.Write(fe.NameLength);
                bw.Write((byte)(((byte)fe.Version << 4) | (byte)fe.Type));

                uint encOffset = fe.Offset;
                uint encSize = fe.Size;
                byte[] encName = (byte[])fe.NameRaw.Clone();

                if (EntryVersion < PakFileEntryVersion.V3 || EntryVersion == PakFileEntryVersion.Raw)
                {
                    encSize ^= kXorKey;
                    for (int k = 0; k < encName.Length; k++) encName[k] ^= (byte)kXorKey;
                }
                else if (EntryVersion == PakFileEntryVersion.V3)
                {
                    ulong packed = ((ulong)encSize << 32) | encOffset;
                    packed = Xtea.Encrypt(LocationKeys, packed);
                    encSize = (uint)(packed >> 32);
                    encOffset = (uint)(packed & 0xFFFFFFFF);

                    for (int k = 0; k < encName.Length; k += 8)
                    {
                        ulong block = BitConverter.ToUInt64(encName, k);
                        block = Xtea.Encrypt(LocationKeys, block);
                        var bytes = BitConverter.GetBytes(block);
                        Array.Copy(bytes, 0, encName, k, 8);
                    }
                }

                bw.Write(encOffset);
                bw.Write(fe.CompressSize);
                bw.Write(encSize);
                bw.Write(encName);
            }

            bw.Write(offsetFileEntry);
            bw.Write((uint)fileEntries.Count);
            bw.Write(PakVersion);

            ms.Position = 0;
            using var fs = File.Create(outputPath);
            ms.CopyTo(fs);

            log?.Invoke($"Pak criado com sucesso: {outputPath} ({fs.Length} bytes)");
        }
    }
}
