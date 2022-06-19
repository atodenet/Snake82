using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Atode
{
    class SceneBoot : Scene
    {
        private const int FONTCOUNT = 90;
        private const int DEGREESTART = 105;    // カウントダウンして0になったらタイトル画面へ移行
        private const int BLACKOUT_STAY = 25;           // アニメが終わってからの余韻期間
        int degree;
        int blackcount;
        bool degreestart;
        private int[,] mapfont;
        private Color[,] mapcolor;

        public void Init(Game1 g)
        {
            int x, ax, bx;
            int y, ay, by;

            degree = DEGREESTART;
            blackcount = BLACKOUT_STAY;

            mapfont = new int[g.celwidth(), g.celheight()];
            mapcolor = new Color[g.celwidth(), g.celheight()];
            degreestart = true;
#if DEBUGoff
            degreestart = false;
#endif

            for (x=0; x<g.celwidth(); x++)
            {
                for(y=0; y<g.celheight(); y++)
                {
                    mapfont[x, y] = g.rand.Next(FONTCOUNT) + 33;
                    mapcolor[x, y] = SelectColor(g.rand.Next(18));
                }
            }
            bx = x = g.rand.Next(g.celwidth() - 8);
            by = y = g.rand.Next(g.celheight());
            // 隠しサインなので気にするな
            mapfont[x, y] = 'B';
            mapcolor[x++, y] = Color.DodgerBlue;
            mapfont[x, y] = 'i';
            mapcolor[x++, y] = Color.DodgerBlue;
            mapfont[x, y] = 'o';
            mapcolor[x++, y] = Color.DodgerBlue;
            mapfont[x, y] = '_';
            mapcolor[x++, y] = Color.DodgerBlue;
            mapfont[x, y] = '1';
            mapcolor[x++, y] = Color.DodgerBlue;
            mapfont[x, y] = '0';
            mapcolor[x++, y] = Color.DodgerBlue;
            mapfont[x, y] = '0';
            mapcolor[x++, y] = Color.DodgerBlue;
            mapfont[x, y] = '%';
            mapcolor[x++, y] = Color.DodgerBlue;

            var atode = "atode.net";
            do
            {
                ax = x = g.rand.Next(g.celwidth() - atode.Length);
                ay = y = g.rand.Next(g.celheight());
            } while (ay == by);
            for(int i=0; i<atode.Length; i++)
            {
                mapfont[x, y] = atode[i];
                mapcolor[x++, y] = Color.DarkOrange;
            }

            base.Init();
        }
        private Color SelectColor(int seed)
        {
            Color col;

            switch( seed)
            {
                case 0:
                    col = Color.Cyan;       // 水色
                    break;
                case 1:
                    col = Color.Fuchsia;    // 紫
                    break;
                case 2:
                case 3:
                    col = Color.Yellow;     // 黄色
                    break;
                case 4:
                case 5:
                    col = Color.Lime;       // 緑
                    break;
                case 6:
                case 7:
                    col = new Color(255,70,70);    // 赤
                    break;
                default:
                    col = Color.White;      // 白の面積が一番多い
                    break;
            }
            return col;
        }
        public new void Update(Game1 game)
        {
            // ゲームのメインロジック
            if (game.inp.AnyKey())
            {
                nextScene = (int)Scn.Title;
            }
#if DEBUGoff
            if (game.inp.Get((int)Key.A))
            {
                degreestart = true;
                nextScene = (int)Scn.None;
            }
#endif
            if ( 0 < degree && degreestart)
            {   // ここは感覚で調整する値
                if( 75 < degree )
                {
                    degree--;
                }
                else
                {
                    degree -= 5;
                }
            }
            if( degree <= 0)
            {
                degree = 0;
                if( --blackcount <= 0)
                {
                    nextScene = (int)Scn.Title;
                }
            }

            base.Update(game);
        }
        public new void Draw(Game1 g)
        {
            // フレーム描画
            int x;
            int y;
            float alpha; // 透明度

            // 徐々に透明になるが、最後まで完全な透明にはならない
            alpha = (float)(degree*2+DEGREESTART) / (float)(DEGREESTART*3);
            // spriteのドット絵をボケない指定
            g.spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            for (x = 0; x < g.celwidth(); x++)
            {
                for (y = 0; y < g.celheight(); y++)
                {
                    Rectangle pos = g.scr.TextBox(x, y);
                    pos.Y = g.scr.viewheight() / 2 + (int)((double)(pos.Y - g.scr.viewheight()/2) * Math.Sin(MathHelper.ToRadians(degree)));
                    pos.Height = (int)((double)pos.Height * Math.Sin(MathHelper.ToRadians(degree)));
                    if(pos.Height <= 0)
                    {
                        pos.Height = 1;
                    }
                    g.spriteBatch.Draw(g.fonts, pos, g.Font((char)mapfont[x,y]), mapcolor[x,y] * alpha);
                }
            }

            g.spriteBatch.End();

            base.Draw(g);
        }

        public new Color BackColor()
        {   // 起動画面のみ背景色は真っ黒
            return Color.Black;
        }
    }
}
