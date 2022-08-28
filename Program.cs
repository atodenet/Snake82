using System;
using System.IO;
using System.Threading;

using Atode;

namespace Snake82
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            // 二重起動を防止する。
            // ロックファイル方式
            String lockfile = RecordData.GetName(".lock");
            // ロックファイル読み込み
            try
            {
                using (var stream = File.Open(lockfile, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        // ロックファイルの作成時刻を読み込み
                        DateTime lockdate = DateTime.FromBinary(reader.ReadInt64());
                        // 現在時刻との差分は？
                        TimeSpan elapsed = DateTime.Now - lockdate;
                        if( elapsed.TotalMinutes < 10d)
                        { // ロック時刻から10分未満であれば起動しない
                            return;
                        }
                    }
                }
            }
            catch
            {
                // 読み込み失敗したらここへ来る。
                // 読み込み失敗 = ロックされていないと見なし、そのままゲーム実行へ進む。
            }
            // ロックファイルを作成
            try
            {
                using (var stream = File.Open(lockfile, FileMode.Create))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write((Int64)DateTime.Now.ToBinary());
                    }
                }
            }
            catch
            { // ロックファイル書き込み失敗であれば、二重起動の危険があるのでゲームを起動しない
                return;
            }
            // 二重起動防止用ロックファイル処理完了

#if PREVENT_DUPLICATE_MUTEX
            // Mutexによる二重起動防止は
            // デバッグビルドだと期待通り動作するが、リリースビルドではチェックを素通りしてしまうため無意味だった。
            bool createnew = false;
            Mutex mymutex = new Mutex(false, "Global\\Snake82",out createnew);
            if( createnew == false)
            { // ミューテックスが既に存在するので、二重起動とみなす。
                return;
            }
#endif
            using (var game = new Game1())
                game.Run();

            // ロックファイルを削除
            File.Delete(lockfile);
        }
    }
}
