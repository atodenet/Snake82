﻿using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atode
{
    // SceneGameクラスが長くなりすぎるのでファイルを分割した。最低だな
    partial class SceneGame : Scene
    {
        // アイテムや敵が新しく生まれる場所を選ぶ
        // ランダムに沢山の場所を選び、その中で最も周囲の密度が低い場所を返す。
        private Point SearchMapSpace(Game1 g)
        {
            Point ret;
            int otherscore;
            int othertop = 100000000;
            int trycount;

            ret.X = 0;
            ret.Y = 0;

            // 5x5マス内に他のチップがあれば点数をつける
            for (trycount = 0; trycount < 100; trycount++)
            {
                Point p;
                // マップの端の列は二重描画になるので避ける
                p.X = g.rand.Next(mapwidth() - 2) + 1;
                p.Y = g.rand.Next(mapheight() - 2) + 1;

                // 周囲の存在密度を点数化
                otherscore = map.ScoreMap(p,MapType.None);
                // これまででもっと点数が低ければ、これを戻り値として記憶
                if (othertop > otherscore)
                {
                    othertop = otherscore;
                    ret = p;
                }
            }
            return ret;
        }

        // アイテムを初期化、配置
        private void InitItem(Game1 g)
        {
            int no;
            int x;
            Point pos;

            // アイテム位置決定
            for (no = 0; no < ITEM_NUM; no++)
            {
                apple[no].Init(SearchMapSpace(g));
                apple[no].Plot(map);    // 次のアイテムが近所にならないために、マップに配置しておく
            }

            // スタートアップアニメーションのため、各アイテムの登場位置（見えない場所）をセット
            // 一番左を選ぶ
            int leftno = 0;
            x = apple[leftno].X();
            for(no=1; no < ITEM_NUM; no++)
            {
                if(apple[no].X() < x)
                {
                    x = apple[no].X();
                    leftno = no;
                }
            }
            pos.X = -1;
            pos.Y = apple[leftno].Y();
            apple[leftno].SetStartup(pos);

            // 一番右を選ぶ
            int rightno = 0;
            if (leftno == 0)
            {
                rightno = 1;
            }
            x = apple[rightno].X();
            for (no = 0; no < ITEM_NUM; no++)
            {
                if (no == leftno)
                {
                    continue;
                }
                if (x<apple[no].X())
                {
                    x = apple[no].X();
                    rightno = no;
                }
            }
            pos.X = mapwidth()+1;   // 画面の幅はマップの幅＋１（ミラー行があるので）
            pos.Y = apple[rightno].Y();
            apple[rightno].SetStartup(pos);

            // 左端と右端以外はすべて上から登場
            for (no = 0; no < ITEM_NUM; no++)
            {
                if (no == leftno || no==rightno)
                {
                    continue;
                }
                pos.X = apple[no].X();
                pos.Y = -1;
                apple[no].SetStartup(pos);
            }
        }
    }
}
