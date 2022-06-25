using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snake82
{
    // タイトルデモ画面用の蛇
    // 勝手に伸びる
    // 時々勝手にレインボウモードになる
    class SnakeDemo : Snake
    {
        private int lastdirection = 0;

        public SnakeDemo()
        {
            headcolor = Color.White;
            bodycolor = Color.AliceBlue;

        }
        public new void Init(int startx, int starty)
        {
            base.Init(startx, starty);
            // 基底クラスで設定した値を上書き
            speed = 40;
        }

        // return 0:通常 1:アイテムを食べた
        public int Update(Game1 g, int[,] map,bool eatitem)
        {
            int rc = 0;
            // 頭が一マス進んだらtrueを返す
            bool stepahead = base.Update(g, map);

            if (stepahead)
            {
                if (eatitem)
                {   // アイテム捕食中
                    int hitchip = GetHit(map);
                    if (hitchip == (int)Chip.Item)
                    {
                        rc = 1;
                        if (length < 10000)
                        {
                            AddBody();
                        }
                        SetRainbow();
                    }
                }
                else
                {   // アイテム捕食中でなければランダムで方向転換
                    if (lastdirection == -1)
                    {   // 2連続方向転換は見栄えが悪いので防ぐ, 前回方向転換しなかった場合のみ
                        lastdirection = SetDirection(g.rand.Next(4));
                    }
                    else
                    {
                        lastdirection = -1;
                    }

                }
            }
            return rc;
        }
    }
}
