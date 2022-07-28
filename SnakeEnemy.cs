using Atode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snake82
{
    class SnakeEnemy : Snake
    {
        protected const int REBORN_TIME = 100;
        protected int rebornWait;
        protected bool emergencyCheck;

        public SnakeEnemy(int mapobjectno):base(mapobjectno)
        {
            headcolor = Color.LightPink;
            bodycolor = Color.Violet;

        }
        public new void Init(Point startPos)
        {
            base.Init(startPos);
            rebornWait = 0;
            emergencyCheck = true;
        }

        // ゲーム進行処理
        // return: 0=通常 1=deathからactiveに変わるときにtrueを返すので、呼び出し元はRebornを実行すること。
        public new int Update(Game1 g, CollisionMap map)
        {
            bool nextstep = base.Update(g, map);

            if (modenow == SnakeMode.Active)
            {
                // 次のマスへ進んだら、進行方向を決定する
                if (nextstep)
                {
                    TurnHead(g, map,false);
                    emergencyCheck = true;
                }
                if (emergencyCheck)
                {
                    // 次のマスに進むまで指定フレーム数以内なら、緊急回避発動チェック
                    if (posratio > (INTEGRAL_RANGE - Speed(g) * EMERGENCY_FRAME))
                    {
                        emergencyCheck = false;
                        TurnHead(g, map, true);
                    }
                }
            }

            // 一定以上生き残ると成長する
            Grow(map);

            // 再誕生の判定
            if (0 < rebornWait)
            {
                if (--rebornWait == 0)
                {
                    return 1;
                }
            }
            return 0;
        }

        // 敵スネークの知能ロジック
        // 次の進行方向を決定する
        private void TurnHead(Game1 g, CollisionMap map,bool emergency)
        {
            // 進行先の候補は前方、左、右の３つ
            // 危険点数をつけて、危険点数の逆数に比例してガチャして決める 
            int dfront;     // 前方の方向（上下左右）
            int dleft;
            int dright;

            switch (direction)
            {
                case 0: // 上へ
                    dfront = 0;
                    dleft = 1;
                    dright = 3;
                    break;
                case 1: // 左へ
                    dfront = 1;
                    dleft = 2;
                    dright = 0;
                    break;
                case 2: // 下へ
                    dfront = 2;
                    dleft = 3;
                    dright = 1;
                    break;
                case 3: // 右へ
                default:
                    dfront = 3;
                    dleft = 0;
                    dright = 2;
                    break;
            }
            // 進行方向ごとの危険度点数（危険なほど点数が高い）
            // -50は、自分自身（頭）がカウントされてしまっているのでそれを差し引く
            // 0だとゼロ除算エラーになるので+1
            int scorefront = map.ScoreMap(GetNextPoint(map, body[headno], dfront), MapType.Enemy) - 50 + 1;
            // 意味もなく曲がりまくらないよう前方優先にする。左右は危険度加点
            int scoreleft = map.ScoreMap(GetNextPoint(map, body[headno], dleft), MapType.Enemy) - 50 + 100;
            int scoreright = map.ScoreMap(GetNextPoint(map, body[headno], dright), MapType.Enemy) - 50 + 100;

            if (emergency)
            {
                // 緊急回避フラグtrueの場合、前方の障害物回避だけが目的
                if (scorefront < CollisionMap.SCORE_CENTER)
                {   // 前方マスには何もない
                    return;
                }
                scorefront++; // おまじない
            }

            // 危険度点数から安全度点数に逆数変換
            scorefront = (CollisionMap.SCORE_MAX * INTEGRAL_RANGE) / scorefront;
            scoreleft = (CollisionMap.SCORE_MAX * INTEGRAL_RANGE) / scoreleft;
            scoreright = (CollisionMap.SCORE_MAX * INTEGRAL_RANGE) / scoreright;
            int scoretotal = scorefront + scoreleft + scoreright;
            int score = g.rand.Next(scoretotal);
            // ランダム点数に対して、重みづけして回頭を判定
            if (score <= scoreleft)
            {
                SetDirection(dleft);
            }
            else
            {
                score -= scoreleft;
                if (score <= scoreright)
                {
                    SetDirection(dright);
                }
            }

        }

        // 成長の時期が来たら、体が伸びる＆スピードアップ
        public void Grow(CollisionMap map)
        {
            int growpoint = growstep + (growturn * map.mapwidth() / 10);

            if (map.mapwidth() < growpoint)
            {
                AddBody();
                if (growturn <= 3)       // 方向転換が3回以下なら1up
                {
                    SpeedUp(1);      // スピードアップ完了
                }
                // スピードアップ判定用パラメータ初期化
                growstep = 0;       // 移動距離カウンタ
                growturn = 0;       // 方向転換回数カウンタ
            }
        }

        // 蛇が死んだあと待機モードになる
        protected override void SetAfterDeath()
        {
            base.SetAfterDeath();
            rebornWait = REBORN_TIME;   // 再誕生までの時間
        }

        // 頭の衝突判定と衝突時処理
        // return: 0=変化なし 1=自機に当たって死んだ 2=それ以外
        public int CheckHit(Game1 g, CollisionMap map)
        {
            int rc = 0;

            if(modenow == SnakeMode.Active)
            {
                MapObject mo = GetHit(g,map);

                if (map.IsSnakeChip(mo.chip))
                {   // 頭がなにか蛇の体に衝突した
                    if (mo.chip == MapChip.SnakeHead)
                    {
                        // 何もしない。自機の頭であれば敵の方が勝つ
                    }
                    else
                    {
                        // 自分の頭以外であれば死ぬ
                        if ( mo.chip == MapChip.SnakeBody || mo.chip == MapChip.SnakeTail || mo.chip == MapChip.RainbowHead || mo.chip == MapChip.RainbowBody || mo.chip == MapChip.RainbowTail)
                        {   // 自機にぶつかった
                            SetDeath(true);
                            rc = 1;
                        }
                        else
                        {   // 敵にぶつかった
                            SetDeath(false);
                            rc = 2;
                        }
                    }
                }
            }
            return rc;
        }

        public new void Draw(Game1 g, int mapwidth, int mapheight, int uppadding)
        {
            // 敵は、死んだ後のスタンバイの敵は描画しない
            if (modenow != SnakeMode.Standby)
            {
                base.Draw(g, mapwidth, mapheight, uppadding);
            }
        }

    }
}
