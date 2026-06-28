using System;
using System.IO;
using System.Text;
using PangyaAPI.UpdateList.Flags;
using PangyaAPI.UpdateList.Models;
using PangyaAPI.Utilities.Cryptography;

public class UpdateKeyDetector
{ 

    public static UpdateResult DetectAndSetKey(string filePath, out uint[]? detectedKey, out byte[]? decryptedData, out string Document)
    {
        Document = "";
        detectedKey = null;
        decryptedData = null;

        if (IsFileCrypt(filePath) != OperacaoEnum.Decrypt)
        { 
            return UpdateResult.Sucess;
        }

        var data = File.ReadAllBytes(filePath);
        if (data.Length < 8) return UpdateResult.Falied;

        // Mapeia todas as chaves conhecidas do seu UpdateKeys.cs para testar
        var keysToTest = new Dictionary<string, uint[]>
        {
            { "GB", UpdateKeys.GB },
            { "TH", UpdateKeys.TH },
            { "JP", UpdateKeys.JP },
            { "KR", UpdateKeys.KR },
            { "ID", UpdateKeys.ID },
            { "EU", UpdateKeys.EU }
        };

        foreach (var kvp in keysToTest)
        {
            uint[] key = kvp.Value;
            var _data = new byte[data.Length];
            bool currentKeyFailed = false;

            // Loop pulando de 8 em 8 bytes (Tamanho do bloco XTEA clássico)
            for (int i = 0; i < data.Length; i += 8)
            {
                // Proteção para o último bloco caso o arquivo não seja múltiplo de 8
                int bytesToCopy = Math.Min(8, data.Length - i);

                // Se o bloco final for menor que 8 bytes, precisamos de um buffer temporário de 8 bytes acolchoado com zeros
                byte[] blockBytes = new byte[8];
                Buffer.BlockCopy(data, srcOffset: i, dst: blockBytes, dstOffset: 0, count: bytesToCopy);

                // 1. Converte os 8 bytes do bloco direto para um ÚNICO ulong
                ulong blockValue = BitConverter.ToUInt64(blockBytes, 0);
                 
                blockValue = Xtea.Decrypt(key, blockValue); // Se o método retornar o valor descriptografado
                                                            // Xtea.Decrypt(key, ref blockValue);      // Use esta linha se o método usar 'ref'

                // 3. Converte o ulong descriptografado de volta para um array de 8 bytes
                byte[] decryptedBlockBytes = BitConverter.GetBytes(blockValue);

                // 4. Devolve os bytes decodificados para a posição correta do buffer de resultado
                Buffer.BlockCopy(decryptedBlockBytes, srcOffset: 0, dst: _data, dstOffset: i, count: bytesToCopy);

                // VALIDAÇÃO CRÍTICA: Se decodificou o primeiro bloco e falhou, aborta
                if (i == 0 && (_data[0] != '<' || _data[1] != '?'))
                {
                    currentKeyFailed = true;
                    break;
                }
            }

            // Se o loop terminou sem quebrar no primeiro bloco, encontramos a chave correta!
            if (!currentKeyFailed)
            {
                Console.WriteLine($"[Sucesso] Chave detectada com sucesso: Região {kvp.Key}");

                detectedKey = key;

                // Remove possíveis bytes nulos (\0) inseridos pelo padding do XTEA
               Document = Encoding.UTF8.GetString(_data).Replace("\0", "").Trim();
                decryptedData = Encoding.UTF8.GetBytes(Document);

                // Salva o XML descriptografado na raiz para análise

                string outputXmlPath = Path.Combine(Directory.GetCurrentDirectory(), "updatelist.xml");
                File.WriteAllBytes(outputXmlPath, decryptedData);

                return UpdateResult.Sucess;
            }
        }

        Console.WriteLine("[Aviso] Nenhuma das chaves conhecidas conseguiu descriptografar o arquivo.");
        return UpdateResult.Test_New_Key;
    }

    public static OperacaoEnum IsFileCrypt(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return OperacaoEnum.Falied;
        }

        byte[] rawBytes = File.ReadAllBytes(filePath);
        if (rawBytes.Length < 2) return OperacaoEnum.Decrypt;

        // Verifica os cabeçalhos sem converter o arquivo inteiro para char[] (ganho de performance)
        if (rawBytes[0] == '<' && rawBytes[1] == '?')
        {
            // Se o arquivo tiver tamanho suficiente, lê a região do cabeçalho
            if (rawBytes.Length > 76)
            {
                char c75 = (char)rawBytes[75];
                char c76 = (char)rawBytes[76];
                Console.WriteLine($"[Info] Arquivo aberto em texto puro. Pronto para encriptar ({c75}{c76})");
            }
            return OperacaoEnum.Encrypt;
        }

        return OperacaoEnum.Decrypt;
    }
}