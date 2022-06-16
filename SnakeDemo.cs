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

        // 頭が一マス進んだらtrueを返す
        public new bool Update(Game1 g, int[,] map)
        {
            bool rc;

            rc = base.Update(g, map);

            if (rc)
            {
                if (g.rand.Next(3) < 2)
                {
                    if (length < 10000)
                    {
                        AddBody();
                    }
                    if(length % 10 == 0)
                    {
                        SetRainbow();
                    }
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
