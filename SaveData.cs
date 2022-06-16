using System;
using System.IO;
using System.Diagnostics;

namespace Atode
{
    public class SaveData
    {   // セーブデータ管理クラス
        private const string COMPANY = "atode.net";
        private String filename = "noname.dat";

        // 以下、ファイルに書き込み、読み取りするデータ
        private const int DATAVERSION = 1;
        public int width = 0;
        public int height = 0;
        
        public SaveData(String title)
        {
            filename = title;
        }

        private String GetFolder()
        {
            // これはWindows依存か？
            String folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            folder += "\\";
            folder += COMPANY;
            return folder;
        }
        private String GetName()
        {
            String name = GetFolder();
            name += "\\";
            name += filename;
            name += ".dat";
            return name;
        }
        public int Save(int width, int height)
        {
            String folder = GetFolder();

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
