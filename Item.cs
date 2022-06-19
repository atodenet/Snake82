using Atode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snake82
{
    // SnakeクラスとItemクラスに共通の基底クラスを作ればよかった。
    // たいした問題ではないので放置
    class Item
    {
        protected const int REBORN_TIME = 30;
        protected const int INTEGRAL_RANGE = 1000;
        protected const int FADEIN_SPEED = 20;
        protected Point body;
        protected Point startup;
        protected int reborncount = 0;
        protected int rebornwait;
        protected int fadein;
        public int mode;

        // アイテム初期配置
        public void Init(Point pos)
        {
            mode = (int)SnakeMode.Active;
            body = pos;
            reborncount = 0;
            rebornwait = 0;
            fadein = INTEGRAL_RANGE;
        }

        // アイテムは食われたら死亡ステータスへ変更
        public void SetDeath()
        {
            mode = (int)SnakeMode.Death;
            reborncount++;
            rebornwait = reborncount * REBORN_TIME;
        }

        // 食われて死んでいたアイテムを復活させる
        public void Reborn(Point pos)
        {
            mode = (int)SnakeMode.Active;
            body = pos;
            fadein = 0;
        }

        // ゲーム開始時の登場前の待機位置
        public void SetStartup(Point pos)
        {
            startup = pos;
        }
        public void Plot(int[,] map)
        {
            // 死んだらmapに置かない
            if (mode == (int)SnakeMode.Active)
            {
                map[body.X, body.Y] = (int)Chip.Item;
            }
        }
        public bool CheckHit(Point p)
        {
            if (body.X == p.X && body.Y == p.Y)
            {
                return true;
            }
            return false;
        }

        // 画面描画 通常用
        public void Draw(Game1 g,int uppadding)
        {
            if (mode == (int)SnakeMode.Active)
            {
                Color color = Color.Red;
                if (fadein < INTEGRAL_RANGE)
                {
                    float alpha = (float)(fadein) / (float)INTEGRAL_RANGE;  // アルファ値 0f～1f
                    color = color * alpha;
                }
                g.spriteBatch.Draw(g.fonts, g.scr.TextBox(body.X, body.Y + uppadding), g.Font((char)6), color);
            }
        }

        // 画面描画 スタートアップアニメーション専用
        public void DrawStartup(Game1 g, int uppadding,int ratio1,int range)
        {
            if (mode == (int)SnakeMode.Active)
            {
                Color color = Color.Red;
                if (fadein < INTEGRAL_RANGE)
                {
                    float alpha = (float)(fadein) / (float)INTEGRAL_RANGE;  // アルファ値 0f～1f
                    color = color * alpha;
                }
                Rectangle rect = g.scr.TextBoxBetween(body.X, body.Y + uppadding, startup.X, startup.Y + uppadding, ratio1, range);
                g.spriteBatch.Draw(g.fonts, rect, g.Font((char)6), color);
            }
        }

        // ゲーム進行処理
        // deathからactiveに変わるときにtrueを返すので、呼び出し元はRebornを実行すること。
        public bool Update()
        {
            if( 0<rebornwait)
            {
                if( --rebornwait == 0)
                {
                    return true;
                }
            }
            if (fadein < INTEGRAL_RANGE)
            {
                fadein += FADEIN_SPEED;
                if (INTEGRAL_RANGE < fadein)
                {
                    fadein = INTEGRAL_RANGE;
                }
            }
            return false;
        }
        public void ExpandMap(int xadd)
        {
            body.X += xadd;
        }
        public int X()
        {
            return body.X;
        }

        public int Y()
        {
            return body.Y;
        }
    }
}
