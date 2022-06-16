using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace Atode
{
    public struct Position
    {
        public int x;
        public int y;
    }
    class SceneGame : Scene
    {
        private const int ITEM_NUM = 3;
        private const int UPPER_PADDING = 1;
        private SnakeHero hero;
        private Item[] apple;


        public SceneGame()
        {
            hero = new SnakeHero();
            apple = new Item[ITEM_NUM];
            for (int no = 0; no < ITEM_NUM; no++)
            {
                apple[no] = new Item();
            }
        }

        private Position SearchMapSpace(Game1 g)
        {
            Position ret;
            int otherscore;
            int othertop = 100000000;
            int trycount;

            ret.x = 0;
            ret.y = 0;

            // 5x5マス内に他のチップがあれば点数をつける
            for(trycount=0; trycount<100; trycount++)
            {   
                Position p;
                // マップの端の列は二重描画になるので避ける
                p.x = g.rand.Next(mapwidth() - 2) + 1;
                p.y = g.rand.Next(mapheight() - 2) + 1;

                otherscore = 0;
                // 周囲の存在密度を点数化
                for (int yc = p.y - 2; yc <= p.y + 2; yc++)
                {
                    int y = yc;
                    if (y < 0)
                    {
                        y += mapheight();
                    }
                    else if(mapheight()<=y)
                    {
                        y -= mapheight();
                    }
                    for (int xc = p.x - 2; xc <= p.x + 2; xc++)
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
                        if (map[x, y] != (int)Chip.None)
                        {
                            int distance = Math.Abs(yc-p.y) + Math.Abs(xc-p.x);
                            if (0 < distance)
                            {
                                otherscore += 100 / distance;
                            }
                            else
                            {
                                otherscore += 10000000;
                            }
                        }
                    }
                }
                // 周囲の存在密度集計完了
                if (othertop > otherscore)
                {
                    othertop = otherscore;
                    ret = p;
                }
            }
            return ret;
        }
        public void Init(Game1 g)
        {
            // ゲームマップは画面サイズより狭い 画面の上１行はステータス行、右端と左端はミラー行
            AllocMap(g.celwidth() - 1, g.celheight() - 2);

            // 自機を初期化、位置を指定
            hero.Init(g.celwidth()/2,g.celheight()/2);

            // マップ作成
            hero.Plot(map);
            // アイテム位置決定
            for(int no=0; no< ITEM_NUM; no++)
            {
                apple[no].Init(SearchMapSpace(g));
                apple[no].Plot(map);
            }
            

            base.Init();
        }
        public new void Update(Game1 g)
        {
            int no;
            // ゲームのメインロジック
            if (g.inp.Get((int)Key.Back))
            {  // Backボタンでゲーム終了、タイトルへ
                nextScene = (int)Scn.Title;
            }

            if (g.inp.Get((int)Key.Up))
            {
                hero.SetDirection(0);
            }
            if (g.inp.Get((int)Key.Down))
            {
                hero.SetDirection(2);
            }
            if (g.inp.Get((int)Key.Left))
            {
                hero.SetDirection(1);
            }
            if (g.inp.Get((int)Key.Right))
            {
                hero.SetDirection(3);
            }

            // マップ描画
            ClearMap();
            for (no = 0; no < ITEM_NUM; no++)
            {
                apple[no].Plot(map);
            }
            hero.Plot(map);
            // 自機を動かす
            hero.Update(g, map);
            int rc = hero.CheckHit(map);
            if (rc == 1)
            {   // アイテムを取った
                Position headpos = hero.GetHead();
                // どのアイテムか探す
                for(no=0; no<ITEM_NUM; no++)
                {
                    if( apple[no].CheckHit(headpos))
                    {   // このアイテムが取られたので、アイテムを消す
                        apple[no].SetDeath();
                        break;
                    }
                }
            }
            // アイテムを動かす
            for (no = 0; no < ITEM_NUM; no++)
            {
                if (apple[no].Update())
                {   // 消えていたアイテムが再誕生
                    apple[no].Reborn(SearchMapSpace(g));
                }
            }

            // 死んだらゲームオーバー表示
            if (hero.mode == (int)SnakeMode.Standby)
            {
                nextScene = (int)Scn.Title;
            }

            base.Update(g);
        }
        public new void Draw(Game1 g)
        {
            // フレーム描画
            // spriteのドット絵をボケない指定
            g.spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            base.DrawGround(g,1);

            // アイテムを描画

            for(int no=0; no<ITEM_NUM; no++)
            {
                apple[no].Draw(g, UPPER_PADDING);
            }
            // 蛇を描画
            hero.Draw(g, mapwidth(), mapheight(), UPPER_PADDING);

            // 上部ステータス行
            Rectangle starea;
            starea = g.scr.TextBox(0, 0);
            starea.Width += g.scr.TextWidth(g.celwidth() - 1, 1);
            Primitive.FillRectangle(g.spriteBatch, starea, Color.Honeydew);
            g.DrawString(hero.length.ToString(), (g.celwidth()-1)/2, 0, Color.MidnightBlue);
            g.spriteBatch.End();

            base.Draw(g);
        }
    }
}
