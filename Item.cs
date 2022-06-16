using Atode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snake82
{
    class Item
    {
        protected const int REBORN_TIME = 30;
        protected const int INTEGRAL_RANGE = 1000;
        protected const int FADEIN_SPEED = 20;
        protected Position body;
        protected int reborncount = 0;
        protected int rebornwait;
        protected int fadein;
        public int mode;

        public void Init(Position pos)
        {
            mode = (int)SnakeMode.Active;
            body = pos;
            reborncount = 0;
            rebornwait = 0;
            fadein = INTEGRAL_RANGE;
        }
        public void Reborn(Position pos)
        {
            mode = (int)SnakeMode.Active;
            body = pos;
            fadein = 0;
        }
        public void Plot(int[,] map)
        {
            // 死んだらmapに置かない
            if (mode == (int)SnakeMode.Active)
            {
                map[body.x, body.y] = (int)Chip.Item;
            }
        }
        public bool CheckHit(Position p)
        {
            if (body.x == p.x && body.y == p.y)
            {
                return true;
            }
            return false;
        }
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
                g.spriteBatch.Draw(g.fonts, g.scr.TextBox(body.x, body.y + uppadding), g.Font((char)6), color);
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
        public void SetDeath()
        {
            mode = (int)SnakeMode.Death;
            reborncount++;
            rebornwait = reborncount * REBORN_TIME;
        }
    }
}
