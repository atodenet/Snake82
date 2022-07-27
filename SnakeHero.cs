using Atode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;


namespace Snake82
{
    class SnakeHero : Snake
    {
        public SnakeHero(int mapobjectno):base(mapobjectno)
        {
            headcolor = Color.White;
            bodycolor = Color.AliceBlue;
        }
        public new void Init(Point startPos)
        {
            base.Init(startPos);
            // 基底クラスで設定した値を上書き
            direction = 0; dirlast = 0; // 上向き
            dirratio = INTEGRAL_RANGE;
        }

        // 頭が次のマスへ進んだらtrueを返す
        public new bool Update(Game1 g, CollisionMap map)
        {
            bool newMoved = base.Update(g, map);
            return newMoved;
        }

        // 蛇が死んだあと普通は待機だが、自機は死体描画モードになる
        protected override void SetAfterDeath()
        {
            modenow = SnakeMode.Corpse;   // 自機は死んでも消えない、死体を表示する
        }


        // 成長する 敵を倒したり、アイテムを獲得したり
        public void Grow(int height)
        {
            AddBody();
            SetRainbow();

            // スピードアップ判定
            int spup = 0;   // 1;   // デフォルト値
            if (growstep <= height) // 移動距離が高さ未満なら1up
            {
                Debug.WriteLine("Growstep Speedup");
                spup++;
            }
            if(growturn <= 3)       // 方向転換が3回以下なら1up
            {
                Debug.WriteLine("Growturn Speedup");
                spup++;
            }
            Debug.WriteLine("Speedup "+spup);
            SpeedUp(spup);      // スピードアップ完了

            // スピードアップ判定用パラメータ初期化
            growstep = 0;       // 移動距離カウンタ
            growturn = 0;       // 方向転換回数カウンタ
        }

        // 頭の衝突判定と衝突時処理
        // return: 衝突したオブジェクト
        public MapObject CheckHit(Game1 g, CollisionMap map)
        {
            MapObject mo = GetHit(g,map);

            if (mo.chip == MapChip.Item)
            {   // アイテムを取った
                Grow(map.mapheight());
            }
            else if (mo.chip == MapChip.SnakeBody || mo.chip == MapChip.SnakeTail || mo.chip == MapChip.RainbowBody || mo.chip == MapChip.RainbowTail)
            {   // 頭が自分の体に衝突した
                SetDeath(true);
            }
            else if (mo.chip == MapChip.EnemyHead || mo.chip == MapChip.EnemyBody || mo.chip == MapChip.EnemyTail)
            {   // 頭が敵に衝突した
                if ( IsRainbow() )
                {   // 無敵モード 敵を倒す
                    Grow(map.mapheight());
                }
                else
                {
                    SetDeath(true);
                    // 衝突したオブジェクトは返さない
                    // 呼び出し元では衝突後の処理（敵を殺す）をしないため
                    mo = map.GetNone();
                }
            }
            else if(mo.chip != MapChip.None&& mo.chip != MapChip.SnakeHead&& mo.chip != MapChip.RainbowHead)
            {
            //    throw new SystemException();
            }
            return mo;
        }

        // ゲームマップに自身をプロットする（画面描画ではない）
        public new void Plot(CollisionMap map)
        {
            if ( IsRainbow() )
            {   // 無敵モードの時はオブジェクト番号が変わる。レインボー蛇をプロットする。
                Plot(map, MAPOBJECT_SNAKEPATTERN);
            }
            else
            {   // 通常時はオブジェクト番号はデフォルトのまま。
                Plot(map,0);
            }
        }

        public void DrawStartup(Game1 g, int mapwidth, int mapheight, int uppadding, int ratio1, int range)
        {
            Color color = headcolor;
            Point pos = body[headno];
            Point startup;

            startup.X = pos.X;
            startup.Y = mapheight+1;    // 画面の高さはマップの高さ＋１（ミラー行があるので）
            Rectangle rect = g.scr.TextBoxBetween(pos.X, pos.Y + uppadding, startup.X, startup.Y + uppadding, ratio1, range);
            Vector2 sft = new Vector2(8f, 8f); // テクスチャ上の中心位置（画面上ではない）
            rect.X += rect.Width / 2;
            rect.Y += rect.Height / 2;
            float rot = GetHeadRotation();
            g.spriteBatch.Draw(g.fonts, rect, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
        }
    }
}
