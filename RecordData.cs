using System;
using System.IO;
using System.Collections.Generic;

using Snake82;

namespace Atode
{
    public struct PlayRecord
    {
        public int speed;
        public int size;
        public int stage;
        public bool autopilot;
        public DateTime playdate;

        public PlayRecord(int speed, int size, int stage, DateTime playdate, bool autopilot)
        {
            this.speed = speed;
            this.size = size;
            this.stage = stage;
            this.playdate = playdate;
            this.autopilot = autopilot;
        }

        // チェックサムみたいなもの ファイル破損、改ざんチェック用
        public int CheckCode()
        {
            return size * 1559 + stage * 199 + speed * 3 + (autopilot?1:0);
        }
    }
    public class RecordData
    {   // セーブデータ管理クラス
        private const string COMPANY = "atode.net";
        private string filename = "noname.dat";     // コンストラクタで上書きされる
        public const int MAX_RECORDSPEED = 50;
        public const int MAX_RANK = 100;
        private List<PlayRecord>[] playRoot;

        // セーブデータ ファイルに書き込み、読み取りする
        private const int HEADER_VERSION = 3;
        public int width = 0;
        public int height = 0;
        public bool fullscreen = false;
        public int speedRate = Game1.SPEED_DEFAULT;
        public bool autopilotUnlock = false;
        public bool autopilotEnable = false;
        public int totalPlay = 0;
        private const int PLAYRECORD_VERSION = 2;
        // セーブデータ 終わり

        public bool saveupdate = false; // セーブ後にアップデートされたか
        
        public RecordData(string title)
        {
            filename = title;
            // speed0は欠番なので個数は最大スピード+1、さらにオートパイロット有無でx2
            playRoot = new List<PlayRecord>[(MAX_RECORDSPEED+1)*2];
        }

        // チェックサムみたいなもの ファイル破損、改ざんチェック用
        public int CheckCode()
        {
            return width * 1559 + height * 577 + speedRate * 173 + totalPlay + 73 + 
                (fullscreen ? 1 : 0) + (autopilotUnlock ? 2 : 0) + (autopilotEnable ? 4 : 0);
        }

        // ランキングにデータ登録
        // return 登録順位（ランキングトップ=0）/ -1:ランキング外（低すぎる）
        public int AddPlayRecord(int speed, int size, int stage, DateTime playdate,bool autopilot)
        {
            // 登録するプレイ記録データ
            PlayRecord play = new PlayRecord(speed, size, stage, playdate,autopilot);
            return AddPlayRecord(play);
        }

        public int AddPlayRecord(PlayRecord play)
        {
            int result = -1;    // 登録したランク順位
            if (MAX_RECORDSPEED < play.speed)
            {   // 記録可能なスピード値の上限
                return result;
            }

            // ランキングはスピード毎にまとめている。そのスピードのランキングを取り出す
            List<PlayRecord> ranker = GetRanking(play.speed,play.autopilot);
            if(ranker == null)
            {   // このスピードのランキングはまだない
                ranker = new List<PlayRecord>();
                ranker.Add(play);
                playRoot[GetRank(play.speed,play.autopilot)] = ranker;
                result = 0;
            }
            else
            {
                int no;
                for(no=0; no < ranker.Count; no++)
                {   // 順位の場所に挿入
                    if (ranker[no].size <= play.size)
                    {
                        ranker.Insert(no, play);
                        result = no;
                        break;
                    }
                }
                if (result == -1)
                {   // 既存のランクより下なので最後に追加
                    if (ranker.Count < MAX_RANK)
                    {
                        ranker.Add(play);
                        result = no;
                    }
                }
                // ランキング最大数を超えた分は削除
                while(MAX_RANK < ranker.Count)
                {
                    ranker.RemoveAt(MAX_RANK);
                }
            }
            // ランキングデータ更新フラグ
            saveupdate = true;
            return result;
        }

        private int GetRank(int speed, bool autopilot)
        {
            return speed * 2 + (autopilot ? 1 : 0);
        }
        public List<PlayRecord> GetRanking(int speed, bool autopilot)
        {
            return playRoot[GetRank(speed,autopilot)];
        }

        private string GetFolder()
        {
            // 要注意。このメソッドはWindows依存の可能性あり。
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            folder += "\\";
            folder += COMPANY;
            return folder;
        }

        // アプリケーションファイル名を返す
        public string GetName(String Extension)
        {
            string name = GetFolder();
            name += "\\";
            name += filename;
            name += Extension;
            return name;
        }

        // セーブファイル名を返す
        private string GetName()
        {
            return GetName(".dat");
        }

        // ゲームの設定とランキングデータをセーブ
        public int Logging(String message)
        {
            // 書き込み先フォルダ存在チェックはしない。書けなければ諦める。
            try
            {
                using (var stream = File.Open(GetName(".log"), FileMode.Append))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(message+"\n");
                    }
                }
            }
            catch
            { // ファイル書き込み失敗
                return 2;
            }
            return 0; // 0はOK
        }

        // ゲームの設定とランキングデータをセーブ
        // return 0:success 1:ディレクトリエラー 2:書き込みエラー
        public int Save()
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
                        // ヘッダ書き込み
                        // もしヘッダ構成を変えた場合は、バージョンを増やすこと
                        writer.Write((Int32)HEADER_VERSION);
                        writer.Write((Int32)width);
                        writer.Write((Int32)height);
                        writer.Write((Int32)(fullscreen ? 1 : 0));
                        writer.Write((Int32)speedRate);
                        writer.Write((Int32)(autopilotUnlock ? 1 : 0));
                        writer.Write((Int32)(autopilotEnable ? 1 : 0));
                        writer.Write((Int32)totalPlay);
                        writer.Write((Int32)CheckCode());

                        // ランキングデータ書き込み
                        // もしデータフォーマットを変えた場合は、バージョンを増やすこと
                        writer.Write((Int32)PLAYRECORD_VERSION);
                        for(int rankspeed=0; rankspeed< (MAX_RECORDSPEED+1)*2; rankspeed++)
                        {
                            List<PlayRecord> ranker = playRoot[rankspeed];
                            if(ranker != null)
                            {
                                for(int no = 0; no < ranker.Count; no++)
                                {
                                    PlayRecord play = ranker[no];
                                    writer.Write((Int32)play.speed);
                                    writer.Write((Int32)play.size);
                                    writer.Write((Int32)play.stage);
                                    writer.Write((Int64)play.playdate.ToBinary());
                                    writer.Write((Int32)(play.autopilot ? 1 : 0));
                                    writer.Write((Int32)play.CheckCode());
                                }
                            }
                        }

                    }
                }
                saveupdate = false; // セーブデータ更新フラグを未更新へ
            }
            catch
            { // ファイル書き込み失敗
                return 2;
            }
            return 0; // 0はOK
        }

        // return true:成功もしくはランキングデータ読み込みエラー false:ヘッダ読み込み失敗
        public bool Load()
        { // セーブデータを読み込む
            bool rc = true;

            try
            {
                using (var stream = File.Open(GetName(), FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        int version;
                        int checkcode;

                        // ヘッダ部読み込み
                        rc = false; // ここで例外が発生すればヘッダ読み込みエラーを返すようセット
                        version = reader.ReadInt32();
                        // 未来のバージョンであれば読めないのでスキップ
                        if (version > HEADER_VERSION)
                        {
                            return rc;
                        }
                        width = reader.ReadInt32();
                        height = reader.ReadInt32();
                        Int32 flag = reader.ReadInt32();
                        fullscreen = (flag == 1 ? true : false);
                        speedRate = reader.ReadInt32();
                        flag = reader.ReadInt32();
                        autopilotUnlock = (flag == 1 ? true : false);
                        flag = reader.ReadInt32();
                        autopilotEnable = (flag == 1 ? true : false);
                        if (2 < version)
                        {   // 旧versionでは存在しなかったので読み込まない
                            totalPlay = reader.ReadInt32();
                            checkcode = reader.ReadInt32();
                            if(checkcode != CheckCode())
                            {   // 正しく読み込めていない、壊れているか改ざん
                                return rc;
                            }
                        }

                        // ランキングデータ
                        // ランキングデータは読める分だけ読むので、エラーがあればそれ以降は読み込みを中止するだけ。
                        // エラー発生前に読み込んだデータはそのまま生かす。
                        // データファイルの末尾や途中が破損しても、それより前の部分はそのまま生かすのが方針。
                        rc = true; // ランキングデータの読み込みは最後は必ず例外になるので成功をセット
                        version = reader.ReadInt32();
                        // 未来のバージョンであれば読めないのでスキップ
                        if (version > PLAYRECORD_VERSION)
                        {
                            return rc;
                        }
                        // 読み込み失敗するまでループ
                        for(int no=0; no < MAX_RANK * MAX_RECORDSPEED; no++)
                        {
                            int speed = reader.ReadInt32();
                            int size = reader.ReadInt32();
                            int stage = reader.ReadInt32();
                            DateTime date = DateTime.FromBinary(reader.ReadInt64());
                            bool autopilot = false;
                            if (1 < version)
                            {   // 旧versionでは存在しなかったので読み込まない
                                flag = reader.ReadInt32();
                                autopilot = (flag == 1 ? true : false);
                            }
                            int check = reader.ReadInt32();
                            PlayRecord play = new PlayRecord(speed, size, stage, date,autopilot);
                            if (play.CheckCode() != check)
                            {   // 正しく読み込めていない、壊れているか改ざん
                                return rc;
                            }
                            AddPlayRecord(play);
                        }
                    }
                }
            }
            catch
            {
                // 読み込み失敗
                // ランキングデータはファイルを無限に読み込もうとするので、正常系の最後は必ずここに来る。
            }
            // ランキングデータ更新フラグが立ってしまうので戻す
            saveupdate = false;
            return rc;
        }
    }
}
