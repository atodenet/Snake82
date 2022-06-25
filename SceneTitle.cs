using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace Atode
{
    class SceneTitle : Scene
    {
        private SnakeDemo snake;
        private Item item;

        private bool deathmode = false;

        public SceneTitle()
        {
            snake = new SnakeDemo();
            item = new Item();
        }

        public void Init(Game1 g)
        {
            // 蛇が死ぬモードと死なないモードを繰り返す
            deathmode = !deathmode;

            // ゲームマップは画面サイズより狭い 画面の上１行はステータス行、右端と左端はミラー行
            AllocMap( g.celwidth() - 1, g.celheight() - 1);

            // 自機を初期化、位置を指定
            snake.Init(g.celwidth() / 2, g.celheight() / 2 + 2);
            // アイテムは最初存在しない
            item.SetDeath();

            base.Init();
        }
        public new void Update(Game1 g)
        {
            // ゲームのメインロジック

            if (g.inp.AnyKey() )
            {   // タイトル画面は、何かキーが押されたらゲームへ進む
                nextScene = (int)Scn.Game;
            }
            // マップ描画
            ClearMap();
            snake.Plot(map);
            item.Plot(map);
            // 自機を動かす
            bool eatitem = (item.mode == (int)SnakeMode.Active);  // アイテムがあればまっすぐ取りに行く
            if( snake.Update(g, map, eatitem) == 1)
            {
                item.SetDeath();
            }
            // アイテムを動かす
            item.Update();

            if(deathmode)
            {   // 無敵モードではない 衝突判定を行う
                int hitchip = snake.GetHit(map);
                if (hitchip == (int)Chip.Snake || hitchip == (int)Chip.Enemy)
                {   // 頭が蛇に衝突した（自分の頭を除く）
                    snake.SetDeath();
                    item.SetDeath();    // アイテムも消す
                }
                if( snake.mode == (int)SnakeMode.Standby)
                {
                    snake.Init(g.celwidth() / 2, g.celheight() / 2 + 2);

                }
            }
            // アイテムが無ければ
            if( item.mode == (int)SnakeMode.Death && snake.mode == (int)SnakeMode.Active)
            {   // アイテムを配置する
                if (g.rand.Next(20) < 1)
                {
                    // 場所を決める
                    const int ITEM_FORWARD = 2; // 進行方向の何マス先にアイテムを置くか
                    Point po = snake.GetHead();
                    switch (snake.direction)
                    {
                        case 0:
                            po.Y -= ITEM_FORWARD;
                            break;
                        case 1:
                            po.X -= ITEM_FORWARD;
                            break;
                        case 2:
                            po.Y += ITEM_FORWARD;
                            break;
                        case 3:
                            po.X += ITEM_FORWARD;
                            break;
                    }
                    // 座標がマップ内であること＋ミラー行（上端＆左端）でないこと
                    if (0 < po.X && po.X < mapwidth() && 0 < po.Y && po.Y < mapheight())
                    {
                        int score = ScoreMap(po);
                        if (score < 100 || deathmode == false)
                        {
                            item.Reborn(po);
                        }
                    }
                }
            }

            base.Update(g);
        }
        public new void Draw(Game1 g)
        {
            // フレーム描画
            // spriteのドット絵をボケない指定
            g.spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            base.DrawGround(g,0);

            // 蛇を描画
            snake.Draw(g,mapwidth(),mapheight(),0);
            item.Draw(g, 0);

            // タイトル文字表示 色は明滅させる
            int blinking = (upcounter % 120);
            if( 60 <= blinking)
            {
                blinking = 120 - blinking;
            }
            Color titlecolor = new Color(255, blinking*2 + 130, blinking*2 + 130);
            g.DrawString("Snake82", g.celwidth() / 2 - 4, g.celheight() / 2 - 2, titlecolor);
            // 操作表示
            int x = g.celwidth() / 2 - 5;
            int y = g.celheight() / 2;
            g.DrawString("PUSH", x, y, Color.LightSalmon);
            x += 5;
            g.spriteBatch.Draw(g.fonts, g.scr.TextBox(x++, y), g.Font((char)2), Color.LightSalmon);
            g.spriteBatch.Draw(g.fonts, g.scr.TextBox(x++, y), g.Font((char)0), Color.LightSalmon);
            g.spriteBatch.Draw(g.fonts, g.scr.TextBox(x++, y), g.Font((char)1), Color.LightSalmon);
            g.spriteBatch.Draw(g.fonts, g.scr.TextBox(x, y), g.Font((char)3), Color.LightSalmon);

            g.spriteBatch.End();

            base.Draw(g);
        }
    }
}
