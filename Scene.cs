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
        Quit,
    }

    class Scene
    {   // シーンの基底クラス。ここからゲーム画面やタイトル画面を派生する。
        protected const int INTEGRAL_RANGE = 1000;
        protected const int KEY_RELEASE_WAIT = 30;  // キーが離されるまで待つフレーム数(ボタンを押すと次へ進んでしまうケースで、1プッシュで連続して判定されるのを防ぐ）
        protected int upcounter = 0;    // ゲームにとって時間を表す
        protected Scn nextScene = 0;    // 次のシーンを指定（自分自身ではシーンを切り替えることができないから呼び出し元に指示する）
        // 地面描画用変数
        public bool collisionVisible = false;       // コリジョン可視化フラグ
        protected int GROUND_WAVE_SPEED = 100;

        public CollisionMap map;                    // ゲームロジック用の全体マップ 表示用ではない 描画には使わない
        public int mapwidth() { return map.mapwidth(); }
        public int mapheight() { return map.mapheight(); }

        public void Init()
        {
            upcounter = 0;
            groundWaveVisible = false;

            nextScene = Scn.None;
        }
        public Scn Next()
        {
            return nextScene;
        }
        public void Update(Game1 game)
        {
            // ゲームのメインロジック
            upcounter++;
            // 地面の波
            if(groundWaveVisible)
            {
                groundWave += GROUND_WAVE_SPEED;
            }
        }
        public void Draw(Game1 game)
        {
            // フレーム描画
        }

        // 地面を描画
        protected void DrawGround(Game1 g,int ystart)
        {
            int x;
            int y;
            Rectangle grnd;
            Color col0;
            Color col1;
            Color col0a;
            Color col1a;
            for (y = ystart; y <= g.celheight(); y++)
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
                    for (x = 0; x < g.celwidth(); x++)
                    {
                        grnd = g.scr.TextBox(x, y);
                        tx = x - g.scr.celxshift(); // スタート時画面でのテキスト座標に換算
                        Primitive.FillRectangle(g.spriteBatch, WaveSize(grnd, x, y), (tx % 2 == 0 ? col0 : col1));
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
                    for (x = -1; x <= g.celwidth(); x++)
                    {   // テキスト画面は原点が画面下部中央
                        int tx = x - g.scr.celxshift();
                        grnd = g.scr.TextBox(x, g.celheight());
                        Primitive.FillRectangle(g.spriteBatch, grnd, (tx % 2 == 0 ? col0a : col1a));
                    }
                }
            }

            if( collisionVisible)
            {   // LogicMapを表示
                for (y = 0; y < mapheight(); y++)
                {
                    for(x=0; x < mapwidth(); x++)
                    {
                        int objno = map.map[x, y];
                        if(0 < objno)
                        {
                            col0 = Color.MidnightBlue;
                            if (1 <= objno && objno <= 4)
                            {
                                col0 = Color.Red;
                            }
                            grnd = g.scr.TextBox(x, y + ystart);
                            Primitive.FillRectangle(g.spriteBatch, grnd, col0);
                        }
                    }
                }

            }

        }
        public Color BackColor()
        { // 画面の背景色を返す
            return Color.MediumBlue;
        }

        // 地面波描画
        protected bool groundWaveVisible;           // 地面の波打ち有効化フラグ
        protected int groundWave;                   // 地面波位置
        protected int grountWavePeriod;             // 地面波期間
        protected int groundWaveWidth;              // 地面波の幅（片側）

        // 地面波開始
        protected void StartGrountWave()
        {
            grountWavePeriod = (mapwidth() + mapheight()) * INTEGRAL_RANGE * 3/ 4;
            groundWaveWidth = (mapwidth() + mapheight()) * INTEGRAL_RANGE / 6;
            groundWave = 0;
            groundWaveVisible = true;
        }
        // 地面波を反映して地面ボックスサイズを変更
        protected Rectangle WaveSize(Rectangle rect,int x,int y)
        {
            if (!groundWaveVisible)
            {
                return rect;
            }
            int pos;
            // 左上ではなく右下から波が始まるよう逆にする
            x = mapwidth() - 1 - x;
            y = mapheight() - 1 - y;
            pos = (x + y) * INTEGRAL_RANGE;

            // 波の頂点位置は二つ
            int peakcenter = groundWave % grountWavePeriod;
            int peak1 = peakcenter - grountWavePeriod / 2;
            int peak2 = peakcenter + grountWavePeriod / 2;

            // 波の頂点からの距離 画面内には波が二つある。 近い方を採用する。
            int len1 = Math.Abs(peak1 - pos);
            int len2 = Math.Abs(peak2 - pos);
            int len;
            if(groundWave <= grountWavePeriod)
            {
                // 始まったばかりの時は、これからやってくる方のみ表示
                len = len1;
            }
            else
            {
                // 近い方の波を採用
                len = Math.Min(len1, len2);
            }
            // 波の幅より外側は対象外
            if ( len < groundWaveWidth)
            {
                // 波の高さは二次曲線とする（とんがった一次曲線の波は不自然）
                float ratio = ((float)len * (float)len) / ((float)groundWaveWidth * (float)groundWaveWidth);
                // 頂点=0とはしない。頂点でもある程度資格を残す。
                ratio = ratio * 0.6f + 0.4f;
                int w = (int)((float)rect.Width * ratio);
                int h = (int)((float)rect.Height * ratio);

                rect.X += (rect.Width - w) / 2;
                rect.Y += (rect.Height - h) / 2;
                rect.Width = w;
                rect.Height = h;
            }
            return rect;
        }
    }
}
