using System;
using System.IO;
using System.Diagnostics;

namespace Atode
{
    public class RecordData
    {   // セーブデータ管理クラス
        private const string COMPANY = "atode.net";
        private string filename = "noname.dat";

        // 以下、ファイルに書き込み、読み取りするデータ
        private const int DATAVERSION = 1;
        public int width = 0;
        public int height = 0;
        public bool fullscreen = false;
        
        public RecordData(string title)
        {
            filename = title;
        }

        private string GetFolder()
        {
            // これはWindows依存か？
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            folder += "\\";
            folder += COMPANY;
            return folder;
        }
        private string GetName()
        {
            string name = GetFolder();
            name += "\\";
            name += filename;
            name += ".dat";
            return name;
        }
        public int Save(int width, int height,bool fullscreen)
        {
            string folder = GetFolder();

            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch
                { // ディレクトリ作成失敗（セーブ失敗）
                    return 1;
                }
            }

            try
            {
                using (var stream = File.Open(GetName(), FileMode.Create))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write((Int32)DATAVERSION);
                        writer.Write((Int32)width);
                        writer.Write((Int32)height);
                        writer.Write(fullscreen?1:0);
                    }
                }
            }
            catch
            { // ファイル書き込み失敗
                return 2;
            }
            return 0; // 0はOK
        }
        public void Load()
        { // セーブデータを読み込む
            // ファイル読み込みに失敗しても気にしない
            try
            {
                using (var stream = File.Open(GetName(), FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        int version;
                        version = reader.ReadInt32();
                        // 未来のバージョンであれば読めないのでスキップ
                        if (version <= DATAVERSION)
                        { 
                            width = reader.ReadInt32();
                            height = reader.ReadInt32();
                            Int32 flag = reader.ReadInt32();
                            if (flag == 1)
                            {
                                fullscreen = true;
                            }
                            else
                            {
                                fullscreen = false;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 読み込み失敗
            }

        }
    }
}
