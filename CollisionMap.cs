using Snake82;
using Microsoft.Xna.Framework;
using System;

namespace Atode
{
    public enum MapType
    {
        None = 0,
        Hero,
        Enemy,
        Item,
    }
    public enum MapChip
    {   // ゲームマップ上の記号値
        None = 0,
        SnakeHead,
        SnakeBody,
        RainbowHead,
        RainbowBody,
        EnemyHead,
        EnemyBody,
        Item,
    }
    public struct MapObject
    {
        public MapChip chip;    // MapChip
        public int Localno;     // type毎（Apple,Enemyそれぞれの）の行列番号
        public int objectno;    // MapObjectの番号

        public MapObject(MapChip chipnumber, int localnumber, int objectnumber) : this()
        {
            this.chip = chipnumber;
            this.Localno = localnumber;
            this.objectno = objectnumber;
        }
    }

    // コリジョンマップ
    // ゲームロジック用の全体マップ 表示用ではない 描画には使わない
    // 衝突判定、アイテム取得などゲームロジック用のオブジェクト配置図
    class CollisionMap
    {
        public const int SCORE_MAX_OUTER = (100 * 4 + 50 * 8 + 33 * 8 + 25 * 4);    // 中心点を除く、周囲5x5全てに存在する場合のスコア
        public const int SCORE_CENTER = SCORE_MAX_OUTER*2;                          // これくらいにしないと中心点に物があってもなかなかよけてくれない
        public const int SCORE_MAX = SCORE_MAX_OUTER + SCORE_CENTER;                // 中心点＋周囲が全部埋まった場合のスコア
        protected MapObject[] obj;

        public int[,] map;              // ゲームロジック用の全体マップ 表示用ではない 描画には使わない
        public int mapwidth() { return map.GetLength(0); }
        public int mapheight() { return map.GetLength(1); }

        public CollisionMap(int maxobject)
        {
            obj = new MapObject[maxobject];
            // 先頭のNULLオブジェクトは常に自動的にセット
            SetObject(0, MapChip.None, 0);
        }
        public void SetObject(int mapobjectno,MapChip chiptype,int localnumber)
        {
            obj[mapobjectno] = new MapObject(chiptype, localnumber, mapobjectno);
        }

        public void AllocMap(int x, int y)
        {   // マップ配列作成
            map = new int[x, y];
        }
        public void ClearMap()
        {   // マップ初期化
            for (int y = 0; y < mapheight(); y++)
            {
                for (int x = 0; x < mapwidth(); x++)
                {
                    map[x, y] = 0;
                }

            }
        }

        public void Plot(int mapobjectno,Point pos)
        {
            map[pos.X, pos.Y] = mapobjectno;
        }

        // 指定した位置のオブジェクトを返す
        public MapObject GetHit(Point pos)
        {
            int objno = map[pos.X, pos.Y];
            return obj[objno];
        }
        // NULLオブジェクトを利用したい
        public MapObject GetNone()
        {
            return obj[0];
        }

        public bool IsSnakeChip(MapChip chip)
        {
            if( chip == MapChip.SnakeHead ||
                chip == MapChip.SnakeBody ||
                chip == MapChip.RainbowHead ||
                chip == MapChip.RainbowBody ||
                chip == MapChip.EnemyHead ||
                chip == MapChip.EnemyBody)
            {
                return true;
            }
            return false;
        }

        // 周囲の存在密度を点数化する
        // 指定地点の近くに何かが存在するほど点数が高い
        // enemyEye : true = 敵の目にはレインボウモードは見えない。レインボウに対して突進させるため。
        // 目の前に隣接して何かあれば100点
        // その場所自体に何かあれば1164点
        public int ScoreMap(Point p,bool enemyEye)
        {
            int score = 0;
            // 周囲5x5マスをすべてサーチ
            for (int yc = p.Y - 2; yc <= p.Y + 2; yc++)
            {
                int y = yc;
                if (y < 0)
                {
                    y += mapheight();
                }
                else if (mapheight() <= y)
                {
                    y -= mapheight();
                }
                for (int xc = p.X - 2; xc <= p.X + 2; xc++)
                {
                    int x = xc;
                    if (x < 0)
                    {
                        x += mapwidth();
                    }
                    else if (mapwidth() <= x)
                    {
                        x -= mapwidth();
                    }
                    MapObject mo = obj[map[x, y]];
                    if (mo.chip != MapChip.None)
                    {
                        if(enemyEye && (mo.chip == MapChip.RainbowHead || mo.chip == MapChip.RainbowBody))
                        {
                            // 敵の目で、かつ対象がレインボーであればカウント対象外
                        }
                        else
                        {
                            int distance = Math.Abs(yc - p.Y) + Math.Abs(xc - p.X);
                            if (0 < distance)
                            {   // 中心点から遠いほど、その地点のスコアは低い
                                int singleScore = 100 / distance;
                                if(mo.chip==MapChip.SnakeHead || mo.chip == MapChip.RainbowHead)
                                {   // 頭は特に避けて欲しいので高点数
                                    singleScore = 200;
                                }
                                score += singleScore;
                            }
                            else
                            {   // 中心点にすでに何かが存在する場合、周囲5x5全てに存在する場合のスコアを付与 
                                // 敵やアイテムを配置するにしろ、すでに何か存在する場合は出来る限り避ける必要があるため
                                score += SCORE_CENTER;
                            }

                        }
                    }
                }
            }

            return score;
        }
    }
}
