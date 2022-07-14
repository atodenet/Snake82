using Atode;
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

        public SnakeDemo(int mapobjectno):base(mapobjectno)
        {
            headcolor = Color.White;
            bodycolor = Color.AliceBlue;

        }

        // return 0:通常 1:アイテムを食べた
        public int Update(Game1 g, CollisionMap map,bool eatItemMode)
        {
            int rc = 0;
            // 頭が一マス進んだらtrueを返す
            bool newMoved = base.Update(g, map);

            if (newMoved)
            {
                if (eatItemMode)
                {   // アイテム捕食中
                    MapObject mo = GetHit(map);
                    if (mo.chip == MapChip.Item)
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
