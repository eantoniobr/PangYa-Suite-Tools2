using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace PangyaAPI.UpdateList.Models
{
    internal class UpdateXMLParser
    {
        public string FilePath { get; set; }
        public Updatelist item { get; set; }

        public UpdateXMLParser(string _file)
        {
            FilePath = _file;
            item = new Updatelist();
        }
        /// <summary>
        /// obtem os arquivos contidos no "updatelist"
        /// </summary>
        /// <param name="path">local do arquivo</param>
        /// <returns></returns>
        public Updatelist getFiles(string path)
        {
            try
            {
                List<string> xmlList = File.ReadAllLines(path).ToList();
                xmlList.Insert(1, "<root>");
                xmlList.Insert(xmlList.Count, "</root>");
                File.WriteAllLines(path, xmlList);
                XElement xDoc = XElement.Load(path);
                IEnumerable<XElement> files = xDoc.Element("updatefiles").Elements("fileinfo");
                foreach (XElement fileName in files)
                {
                    Dictionary<string, string> _tempDict = new Dictionary<string, string>();
                    string fginalStringNilename;

                    if (fileName.Attribute("fdir").Value != "")
                    {
                        string FixedFilename = fileName.Attribute("fdir").Value.Replace("\\", "/");
                        fginalStringNilename = FixedFilename.ReplaceFirst("/", "") + "/" + fileName.Attribute("fname").Value;
                    }
                    else
                    {
                        fginalStringNilename = fileName.Attribute("fname").Value;
                    }

                    _tempDict.Add("fname", fileName.Attribute("fname").Value);
                    _tempDict.Add("fdir", fileName.Attribute("fdir").Value);
                    _tempDict.Add("fsize", fileName.Attribute("fsize").Value);
                    _tempDict.Add("fcrc", fileName.Attribute("fcrc").Value);
                    _tempDict.Add("fdate", fileName.Attribute("fdate").Value);
                    _tempDict.Add("ftime", fileName.Attribute("ftime").Value);
                    _tempDict.Add("pname", fginalStringNilename);
                    _tempDict.Add("psize", fileName.Attribute("psize").Value);
                    item.Add(fginalStringNilename, new FileItem
                    {
                        Name = fileName.Attribute("fname").Value,
                        dir = fileName.Attribute("fdir").Value,
                        Size = long.Parse(fileName.Attribute("fsize").Value),
                        crc = int.Parse(fileName.Attribute("fcrc").Value),
                        date = DateTime.Parse(fileName.Attribute("fdate").Value),
                        time = DateTime.Parse(fileName.Attribute("ftime").Value),
                        pName = fileName.Attribute("pname").Value,//compression
                        pSize = long.Parse(fileName.Attribute("psize").Value)//compression
                    });
                }
                item.patchVer = xDoc.Element("patchVer").Attribute("value").Value;
                item.patchNum = xDoc.Element("patchNum").Attribute("value").Value;
                return item;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("XMLParse.getFiles: " + ex.Message);
                return item;
            }
        }

        public Updatelist addFiles(string path, Dictionary<string, FileItem> UploadListUpdated)
        {
            try
            {
                Updatelist item = new Updatelist();

                Dictionary<string, Dictionary<string, string>> fileNameList = new Dictionary<string, Dictionary<string, string>>();
                List<string> xmlList = File.ReadAllLines(path).ToList();
                XElement xDoc = XElement.Load(path);
                IEnumerable<XElement> files = xDoc.Element("updatefiles").Elements("fileinfo");
                File.Delete(path);
                foreach (XElement fileName in files)
                {
                    Dictionary<string, string> _tempDict = new Dictionary<string, string>();
                    string fginalStringNilename;

                    if (fileName.Attribute("fdir").Value != "")
                    {
                        string FixedFilename = fileName.Attribute("fdir").Value.Replace("\\", "/");
                        fginalStringNilename = FixedFilename.ReplaceFirst("/", "") + "/" + fileName.Attribute("fname").Value;
                    }
                    else
                    {
                        fginalStringNilename = fileName.Attribute("fname").Value;
                    }




                }
                xDoc.Element("updatefiles").Attribute("count").Value = (int.Parse(xDoc.Element("updatefiles").Attribute("count").Value) + UploadListUpdated.Count).ToString();

                item.patchVer = xDoc.Element("patchVer").Attribute("value").Value;
                item.patchNum = xDoc.Element("patchNum").Attribute("value").Value;
                xDoc.Save(path);
                string conteudoXml = File.ReadAllText(path, Encoding.UTF8);

                // Alterar o encoding para euc-kr
                conteudoXml = conteudoXml.Replace("encoding=\"utf-8\"", "encoding=\"euc-kr\"");

                // Salvar o arquivo com o novo encoding
                File.WriteAllText(path, conteudoXml, Encoding.GetEncoding("euc-kr"));
                return item;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("XMLParse.addFiles: " + ex.Message);
                return null;
            }
        }

        public bool createNewEmptyXML(Dictionary<string, FileItem> localClientfilesInfos, string fileOutput, string _patchVer, string _patchNum)
        {
            string scontent = "<?xml version=\"1.0\" encoding=\"euc-kr\" standalone=\"yes\" ?>\n";
            scontent += $"<patchVer value=\"{_patchVer}\"  />\n";
            scontent += $"<patchNum value=\"{_patchNum}\"  />\n";
            scontent += $"<updatelistVer value=\"20090331\" />\n";
            scontent += $"<updatefiles count=\"{localClientfilesInfos.Count}\">\n";
            foreach (var i in localClientfilesInfos.Keys)
            {
                scontent += $"\t<fileinfo fname=\"{localClientfilesInfos[i].Name}\" fdir=\"{localClientfilesInfos[i].dir}\"" +
                    $" fsize=\"{localClientfilesInfos[i].Size}\" fcrc=\"{localClientfilesInfos[i].crc}\" fdate=\"{localClientfilesInfos[i].date.ToString("yyyy-MM-dd")}\"" +
                    $" ftime=\"{localClientfilesInfos[i].time.ToString("HH:mm:ss")}\" pname=\"{localClientfilesInfos[i].pName}\" " +
                    $" psize=\"{localClientfilesInfos[i].pSize}\" />\n";
            }
            scontent += "</updatefiles>\n";
            File.WriteAllText(fileOutput, scontent);
            return true;
        }

        public bool rewriteXMLFiles(string fileOutput, Dictionary<string, FileItem> localClientfilesInfos, string sKey, string _patchVer, string _patchNum)
        {
            try
            {
                string scontent = "<?xml version=\"1.0\" encoding=\"euc-kr\" standalone=\"yes\" ?>\n";
                scontent += $"<patchVer value=\"{_patchVer}\" />\n";
                scontent += $"<patchNum value=\"{_patchNum}\" />\n";
                scontent += $"<updatelistVer value=\"20090331\" />\n";
                scontent += $"<updatefiles count=\"{localClientfilesInfos.Count}\">\n";

                foreach (var i in localClientfilesInfos.Keys)
                {
                    scontent += $"\t<fileinfo fname=\"{localClientfilesInfos[i].Name}\" fdir=\"{localClientfilesInfos[i].dir}\"" +
                                $" fsize=\"{localClientfilesInfos[i].Size}\" fcrc=\"{localClientfilesInfos[i].crc}\" fdate=\"{localClientfilesInfos[i].date.ToString("yyyy-MM-dd")}\"" +
                                $" ftime=\"{localClientfilesInfos[i].time.ToString("HH:mm:ss")}\" pname=\"{localClientfilesInfos[i].pName}\" " +
                                $" psize=\"{localClientfilesInfos[i].pSize}\" />\n";
                }

                scontent += "</updatefiles>\n";
                File.WriteAllText(fileOutput, scontent);
                return new FileCrypt(fileOutput).EncryptFile(sKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("XMLParse.rewriteXMLFiles: " + ex.Message);
                return false;
            }
        }
        public bool EncryptFile(string skey = "US")
        {
            var PangyaCrypt = new FileCrypt(FilePath);
            return PangyaCrypt.EncryptFile(skey);
        }
        public bool DecryptFile(string skey = "US")
        {
            var PangyaCrypt = new FileCrypt(FilePath);
            return PangyaCrypt.DecryptFile(skey);
        }
    }
}
