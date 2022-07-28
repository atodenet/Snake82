using Atode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snake82
{
    class SnakeRush : Snake
    {
        private const int MAX_LENGTH = 8;   // 体の長さ
        public SnakeRush(int mapobjectno, Color color) : base(mapobjectno)
        {
            bodycolor = color;
            headcolor = Color.White;
            headcolor.R = (byte)Math.Min(bodycolor.R + 70, 255);
            headcolor.G = (byte)Math.Min(bodycolor.G + 70, 255);
            headcolor.B = (byte)Math.Min(bodycolor.B + 70, 255);
        }
        public void Init(Point startPos,int rushdirection)
        {
            base.Init(startPos);
            direction = dirnow = dirlast = rushdirection;   // 進行方向
            speed = INTEGRAL_RANGE / 3;
            modenow = SnakeMode.Active;
        }

        // ゲーム進行処理
        // return: 0=通常 1=deathからactiveに変わるときにtrueを返すので、呼び出し元はRebornを実行すること。
        public new bool Update(Game1 g, CollisionMap map)
        {
            // 最初は体長１にしかできない。マス移動時しか体長を伸ばせない。
            if (length < MAX_LENGTH)
            {
                AddBody();  // これは次にマス移動するときに体を伸ばす指定
            }
            bool rc = base.Update(g, map);
            if (rc)
            {
                Rectangle rect = GetTailRect(g, map);
                switch (direction)
                {
                    case 1:
                        // 左へ進む
                        if(rect.X + rect.Width < 0)
                        {
                            SetAfterDeath();
                        }
                        break;
                    default:
                        // 右へ進む
                        if (g.scr.viewwidth()< rect.X)
                        {
                            SetAfterDeath();
                        }
                        break;
                }

            }
            return rc;
        }
        // 指定地点から、指定された方向に1マス進んだ地点を返す
        // マップ端で無限ループしない、ラッシュスネーク専用バージョン
        public override Point GetNextPoint(CollisionMap map, Point po, int direc)
        {
            switch (direc)
            {
                case 0: // 上へ
                    --po.Y;
                    break;
                case 1: // 左へ
                    --po.X;
                    break;
                case 2: // 下へ
                    ++po.Y;
                    break;
                case 3: // 右へ
                    ++po.X;
                    break;
            }
            return po;
        }

        // 蛇を画面に描画する
        public new void Draw(Game1 g, int mapwidth, int mapheight, int uppadding)
        {
            Rectangle rect;

            for (int no = length - 1; 0 <= no; no--)
            {   // 蛇の体を１個ずつ描画 頭は最後
                Point pos1 = body[headno - no];
                Point pos2 = body[headno - no - 1];
                Color color = (no == 0 ? headcolor : bodycolor);

                rect = g.scr.TextBoxBetween(pos1.X, pos1.Y + uppadding, pos2.X, pos2.Y + uppadding, posratio, INTEGRAL_RANGE);

                if (no == 0)
                {   // 先頭はhead描画 頭だけ方向がある
                    Vector2 sft = new Vector2(8f, 8f); // テクスチャ上の中心位置（画面上ではない）
                    rect.X += rect.Width / 2;
                    rect.Y += rect.Height / 2;
                    float rot = GetHeadRotation();

                    g.spriteBatch.Draw(g.fonts, rect, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
                }
                else
                {   // 体を描画
                    g.spriteBatch.Draw(g.fonts, rect, g.Font((char)5), color);
                }
            }
        }
    }
}
