//criado por LUISMK -> github.com/luismk
using PangyaAPI.PAK.Flags;
using PangyaAPI.Utilities.Cryptography; 
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PangyaAPI.PAK.Models
{ 
    public class PakReader : IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly object _ioLock = new();
        private bool _disposed;
        public uint xorKey = 0x71;
        public PakHeader Header { get; private set; } = new();
        public List<PakFileEntry> Entries { get; } = new();
        public uint[]? LocationKeys { get; private set; }

        public PakReader(string path)
        {
            _reader = new BinaryReader(File.OpenRead(path), Encoding.ASCII, leaveOpen: false);
        }

        // ── Parsing ─────────────────────────────────────────────────────────

        public void Parse(uint[]? forceKeys = null)
        {
            var br = _reader;
            long fileLen = br.BaseStream.Length;

            if (fileLen < PakHeader.BinarySize)
                throw new InvalidDataException($"Arquivo muito pequeno: 0x{fileLen:X}");

            // Cabeçalho fica no final do arquivo
            br.BaseStream.Seek(-PakHeader.BinarySize, SeekOrigin.End);

            Header = new PakHeader
            {
                OffsetFileEntry = br.ReadUInt32(),
                NumFileEntry = br.ReadUInt32(),
                Version = br.ReadByte(),
            };

            Console.WriteLine($"  Versão  : 0x{Header.Version:X2}");
            Console.WriteLine($"  Entradas: {Header.NumFileEntry}");

            const byte kVersion = 0x12;
            if (Header.Version != kVersion)
                Console.WriteLine($"[Aviso] Versão LZPak: 0x{Header.Version:X2} != 0x{kVersion:X2}");

            // ── Entries ──────────────────────────────────────────────────────
            br.BaseStream.Seek(Header.OffsetFileEntry, SeekOrigin.Begin);
            LocationKeys = forceKeys;

            for (uint i = 0; i < Header.NumFileEntry; i++)
            {
                byte nameLen = br.ReadByte();
                byte typever = br.ReadByte();

                var version = (PakFileEntryVersion)(typever >> 4);
                var type = (PakFileEntryType)(typever & 0xF);

                uint offset = br.ReadUInt32();
                uint compSz = br.ReadUInt32();
                uint sz = br.ReadUInt32();

                bool hasNull = version < PakFileEntryVersion.V3 || version == PakFileEntryVersion.Raw;
                int rawLen = nameLen + (hasNull ? 1 : 0);
                byte[] nameRaw = br.ReadBytes(rawLen);

                // ── Descriptografia ────────────────────────────────────────── 
                if (version != PakFileEntryVersion.Raw && version < PakFileEntryVersion.V3)
                {
                    xorKey = 0x71;
                    sz ^= xorKey; 
                    DecryptXor(nameRaw, nameLen);
                }
                else if (version == PakFileEntryVersion.V3)
                {
                    if (LocationKeys == null)
                        LocationKeys = AutoDetectKeys(offset, sz) ?? PromptKeys();

                    ulong packed = ((ulong)sz << 32) | offset;
                    if (LocationKeys.Count() > 0)
                    {
                        packed = Xtea.Decrypt(LocationKeys!, packed);
                        sz = (uint)(packed >> 32);
                        offset = (uint)(packed & 0xFFFFFFFF);

                        for (int k = 0; k < nameLen; k += 8)
                        {
                            ulong block = BitConverter.ToUInt64(nameRaw, k);
                            block = Xtea.Decrypt(LocationKeys!, block);
                            var bytes = BitConverter.GetBytes(block);
                            Array.Copy(bytes, 0, nameRaw, k, Math.Min(8, nameRaw.Length - k));
                        }
                    }
                    else
                    {
                        DecryptXor(nameRaw, nameLen);
                    }
                }
              

                Entries.Add(new PakFileEntry
                {
                    NameLength = nameLen,
                    Type = type,
                    Version = version,
                    Offset = offset,
                    CompressSize = compSz,
                    Size = sz,
                    NameRaw = nameRaw,
                });
            }

            // ── Autor ────────────────────────────────────────────────────────
            Header.Author = ReadAuthor();
        }

        private void DecryptXor(byte[] nameRaw, int nameLen)
        {
            for (int k = 0; k < nameLen; k++)
                nameRaw[k] ^= Convert.ToByte(xorKey);
        }

        private string ReadAuthor()
        {
            if (Entries.Count == 0) return "Desconhecido";

            var last = Entries[^1];
            int restAuthor = (int)(Header.OffsetFileEntry - (last.Offset + last.CompressSize));

            if (restAuthor <= 2) return "Desconhecido";

            _reader.BaseStream.Seek(Header.OffsetFileEntry - 2, SeekOrigin.Begin);
            short authorLenLE = _reader.ReadInt16();
            // big endian → inverte bytes
            short authorLen = (short)(((authorLenLE >> 8) & 0xFF) | ((authorLenLE << 8) & 0xFF00));

            if (authorLen == 0) return "Desconhecido";
            if (authorLen < 0 || authorLen > (restAuthor - 2)) return "Desconhecido";

            _reader.BaseStream.Seek(Header.OffsetFileEntry - 2 - authorLen, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(_reader.ReadBytes(authorLen));
        }

        // ── Extração ─────────────────────────────────────────────────────────

        /// <summary>
        /// Extrai todas as entradas cujo nome combine com <paramref name="pattern"/> (suporta '*').
        /// </summary>
        /// <param name="onProgress">Callback opcional invocado como (arquivosProcessados, totalDeArquivos).</param>
        public void Extract(string pattern, string outputDir = "./",
                            Action<string>? log = null,
                            Action<int, int>? onProgress = null)
        {
            // Converte wildcard '*' para regex '.*'
            var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            var matches = Entries
                .Where(e => e.Type != PakFileEntryType.Directory && regex.IsMatch(e.Name))
                .ToList();

            int total = matches.Count;
            int done = 0;

            foreach (var entry in matches)
            {
                string name = entry.Name;
                log?.Invoke($"Encontrado: {name}");

                string outDir = outputDir.TrimEnd('/', '\\') + "/";
                string? subDir = Path.GetDirectoryName(name);
                if (!string.IsNullOrEmpty(subDir))
                    outDir = Path.Combine(outDir, subDir) + "/";

                Directory.CreateDirectory(outDir);

                string outPath = Path.Combine(outDir, Path.GetFileName(name));

                byte[]? data = ExtractEntryToBytes(entry);

                if (data == null)
                {
                    if (entry.Size == 0)
                    {
                        data = Array.Empty<byte>();
                        log?.Invoke($"[Warning] Arquivo Vazio: {name}");
                    }
                    else
                    {
                        log?.Invoke($"[Erro] Falha ao extrair: {name}");
                        done++;
                        onProgress?.Invoke(done, total);
                        continue;
                    }
                }

                File.WriteAllBytes(outPath, data);
                log?.Invoke($"Extraído: {name} → {outPath.Replace("\\", "/")}");

                done++;
                onProgress?.Invoke(done, total);
            }
        }

        /// <summary>
        /// Lê e descomprime os bytes de uma única entry, reaproveitando o stream já
        /// aberto deste PakReader (thread-safe via lock interno). Não toca em disco.
        /// </summary>
        public byte[]? ExtractEntryToBytes(PakFileEntry entry)
        {
            lock (_ioLock)
            {
                _reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);

                if (entry.Type == PakFileEntryType.Raw)
                    return _reader.ReadBytes((int)entry.Size);

                if (entry.Type == PakFileEntryType.LZ77)
                {
                    byte[] compressed = _reader.ReadBytes((int)entry.CompressSize);
                    return Lz77.Decompress(compressed, entry.Size, entry.CompressSize);
                }

                if (entry.Type == PakFileEntryType.LZ772)
                {
                    byte[] compressed = _reader.ReadBytes((int)entry.CompressSize);
                    return Lz772.Decompress(compressed, entry.Size, entry.CompressSize);
                }

                return null;
            }
        }

        /// <summary>
        /// Extrai UMA única entry (já conhecida via <see cref="Entries"/>) direto para
        /// <paramref name="outputPath"/>, sem precisar reabrir/reparsear o arquivo .pak.
        /// Use esse método em vez de instanciar um novo PakReader só para um item —
        /// é a forma rápida de implementar "extrair item selecionado" na GUI.
        /// </summary>
        public void ExtractEntry(PakFileEntry entry, string outputPath)
        {
            if (entry.Type == PakFileEntryType.Directory)
                throw new InvalidOperationException("Não é possível extrair uma entrada de diretório como arquivo.");

            byte[]? data = ExtractEntryToBytes(entry);

            if (data == null && entry.Size == 0)
                data = Array.Empty<byte>();

            if (data == null)
                throw new InvalidDataException($"Falha ao extrair/descomprimir: {entry.Name}");

            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(outputPath, data);
        }

        // ── Auto-detecção de chave ───────────────────────────────────────────

        private static uint[]? AutoDetectKeys(uint offset, uint size)
        {
            foreach (var (_, keys) in PakKeys.All)
            {
                ulong packed = ((ulong)size << 32) | offset;
                ulong result = Xtea.Decrypt(keys, packed);
                if ((result & 0xFFFFFFFF) == 0u)
                    return keys;
            }
            return new uint[0];
        }

        private static uint[] PromptKeys()
        {
            // Solicita ao utilizador via console
            Console.WriteLine("Não foi possível detectar a chave automaticamente.");
            Console.WriteLine("Escolha a localidade:");
            for (int i = 0; i < PakKeys.All.Count; i++)
                Console.WriteLine($"  {i}) {PakKeys.All[i].Label}");
            Console.WriteLine($"  {PakKeys.All.Count}) Chave personalizada");

            int choice = -1;
            while (choice < 0 || choice > PakKeys.All.Count)
            {
                Console.Write("Opção: ");
                _ = int.TryParse(Console.ReadLine(), out choice);
            }

            if (choice < PakKeys.All.Count)
                return PakKeys.All[choice].Keys;

            // Chave personalizada
            uint[] custom = new uint[4];
            while (true)
            {
                Console.Write("Chave (ex: 4ffff,3ddd,4444,2222): ");
                string? line = Console.ReadLine();
                if (line == null) continue;
                var parts = line.Split(',');
                if (parts.Length == 4 &&
                    uint.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.HexNumber, null, out custom[0]) &&
                    uint.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.HexNumber, null, out custom[1]) &&
                    uint.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.HexNumber, null, out custom[2]) &&
                    uint.TryParse(parts[3].Trim(), System.Globalization.NumberStyles.HexNumber, null, out custom[3]))
                    break;
            }
            return custom;
        }

        public void Dispose()
        {
            lock (_ioLock)
            {
                if (!_disposed) { _reader.Dispose(); _disposed = true; }
            }
        }
    }

}
