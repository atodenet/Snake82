using Snake82;
using Microsoft.Xna.Framework;
using System;

namespace Atode
{
    enum Scn
    {
        None = 0,
        Boot,
        Title,
        Game,
    }

    class Scene
    {   // シーンの基底クラス。ここからゲーム画面やタイトル画面を派生する。
        protected const int INTEGRAL_RANGE = 1000;
        protected int upcounter = 0;    // ゲームにとって時間を表す
        protected int nextScene = 0;    // 次のシーンを指定（自分自身ではシーンを切り替えることができないから呼び出し元に指示する）

        public int[,] map;              // ゲームロジック用の全体マップ 表示用ではない 描画には使わない
        public int mapwidth() { return map.GetLength(0); }
        public int mapheight() { return map.GetLength(1); }

        public void Init()
        {
            upcounter = 0;
            
            nextScene = (int)Scn.None;
        }
        public int Next()
        {
            return nextScene;
        }
        public void Update(Game1 game)
        {
            // ゲームのメインロジック
            upcounter++;
        }
        public void Draw(Game1 game)
        {
            // フレーム描画
        }
        protected void DrawGround(Game1 g,int ystart)
        {   // 地面を描画
            Rectangle grnd;
            Color col0;
            Color col1;
            Color col0a;
            Color col1a;
            for (int y = ystart; y <= g.celheight(); y++)
            {
                // 地面のマス目の色
                if (y % 2 == 0)
                {
                    col0 = new Color(0x80, 0xA0, 0);
                    col1 = new Color(0x40, 0xA0, 0);
                }
                else
                {
                    col0 = new Color(0x60, 0x98, 0);
                    col1 = new Color(0x80, 0x98, 0);
                }
                // 画面外の、遷移中に浮き上がってくる
                col0a = new Color((int)col0.R * g.scr.celnextratio() / 150,
                                    (int)col0.G * g.scr.celnextratio() / 150,
                                    (int)col0.B * g.scr.celnextratio() / 150);
                col1a = new Color((int)col1.R * g.scr.celnextratio() / 150,
                                    (int)col1.G * g.scr.celnextratio() / 150,
                                    (int)col1.B * g.scr.celnextratio() / 150);

                if ( y < g.celheight())
                {   // 画面サイズ内部分
                    int tx;
                    int ty = g.celheight() - y - 1; ;
                    for (int x = 0; x < g.celwidth(); x++)
                    {   // テキスト画面は原点が画面下部中央
                        grnd = g.scr.TextBox(x, y);
                        tx = x - g.scr.celxshift(); // スタート時画面でのテキスト座標に換算
                        Primitive.FillRectangle(g.spriteBatch, grnd, (tx % 2 == 0 ? col0 : col1));
                    }
                    if (0 < g.scr.celnextratio())
                    {   // 画面サイズ遷移中は左右を描画
                        tx = -1 - g.scr.celxshift(); // 左端の更に左をスタート時画面でのテキスト座標に換算
                        grnd = g.scr.TextBox(-1, y);
                        Primitive.FillRectangle(g.spriteBatch, grnd, (tx % 2 == 0 ? col0a : col1a));
                        tx = g.celwidth() - g.scr.celxshift(); // 右端の更に右を スタート時画面でのテキスト座標に換算
                        grnd = g.scr.TextBox(g.celwidth(), y);
                        Primitive.FillRectangle(g.spriteBatch, grnd, (tx % 2 == 0 ? col0a : col1a));
                    }
                }
                else if(0 < g.scr.celnextratio())
                {   // 画面サイズ遷移中は最下段のさらに下を描画する
                    for (int x = -1; x <= g.celwidth(); x++)
                    {   // テキスト画面は原点が画面下部中央
                        int tx = x - g.scr.celxshift();
                        grnd = g.scr.TextBox(x, g.celheight());
                        Primitive.FillRectangle(g.spriteBatch, grnd, (tx % 2 == 0 ? col0a : col1a));
                    }
                }

            }

        }
        public Color BackColor()
        { // 画面の背景色を返す
            return Color.MediumBlue;
        }
        protected void ClearMap()
        {   // マップ初期化
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    map[x, y] = 0;
                }

            }
        }
        protected void AllocMap(int x, int y)
        {   // マップ配列作成
            map = new int[x,y];
        }
    }
}
