using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace Atode
{
    public class Vga
    {   // 画面やウィンドウ、キャラクター画面管理用のクラス
        // クラス名は Screenだったが、頭ScがSceneと被るので変更
        public enum VgaType
        {   // 画面サイズがFullHDとHDの二種類しかなかった頃の残骸。今はフルスクリーン時の画面サイズ変更用に残っている
            None = 0,
            fullHD,
            HD,
        }
        private const int CEL_SIZE_DEF = 32;        // キャラクターの仮想サイズ（フルHD時一辺ドット数）
        private const int CEL_WIDTH_DEF = 60;       // 横キャラクター数の初期値
        private const int CEL_HEIGHT_DEF = 33;      // 縦キャラクター数の初期値
        private const int MID_WIDTH = 1920;         // 32*60 キャラクター数から計算した画面サイズ（FullHD用）
        private const int MID_HEIGHT = 1056;        // 32*33 キャラクター数から計算した画面サイズ
        private const int MID_WIDTH_FULL = 1920;    // FullHD物理画面サイズ
        private const int MID_HEIGHT_FULL = 1080;   // 
        private const int SMALL_WIDTH = 1260;       // 21*60 original 1280（HD用）
        private const int SMALL_HEIGHT = 693;       // 21*33 original 720
        private const int SMALL_WIDTH_FULL = 1280;  // HD物理画面サイズ
        private const int SMALL_HEIGHT_FULL = 720;  // 
        public VgaType vgamode = VgaType.fullHD;
        private Viewport _viewport;
        public int edgeWidth = 0;       // ゲーム画面領域より画面サイズが大きい場合に余る幅
        public int edgeHeight = 0;      // ゲーム画面領域より画面サイズが大きい場合に余る高さ
        private int _windowWidth = 0;   // ウィンドウモードの幅
        private int _windowHeight = 0;  // ウィンドウモードの高さ
        private int _celWidth = CEL_WIDTH_DEF;      // 現在の横キャラクター数
        private int _celHeight = CEL_HEIGHT_DEF;
        private int _celWidthNext = 0;  // 横キャラクター数の変更中（遷移中）
        private int _celHeightNext = 0; // 遷移後の次の縦キャラクター数
        private int _celNextRatio = 0;  // 新しいキャラクター画面サイズへの遷移率
        private int _celXShift = 0;      // X原点が初期から移動した量 画面がどれだけ左側に拡張されたか

        public int viewwidth() { return _viewport.Width; }      // ゲーム画面のドット幅
        public int viewheight() { return _viewport.Height; }
        public int celwidth() { return _celWidth; }         // ゲーム画面のキャラクター幅
        public int celheight() { return _celHeight; }
        public int windowwidth() { return _windowWidth; }   // ウィンドウの幅
        public int windowheight() { return _windowHeight; }
        public int celnextratio() { return _celNextRatio; }
        public int celxshift() { return _celXShift; }


        public Vga()
        {
        }

        public void Init(Game1 game,int width, int height,bool fullscreen)
        {
            if( width == 0)
            {
                // レンダリング画面の解像度 デフォルトのままだと800x480
                game.graphics.PreferredBackBufferWidth = MID_WIDTH;
                game.graphics.PreferredBackBufferHeight = MID_HEIGHT;
            }
            else
            {
                game.graphics.PreferredBackBufferWidth = width;
            }
            if (height == 0)
            {
                // レンダリング画面の解像度 デフォルトのままだと800x480
                game.graphics.PreferredBackBufferHeight = MID_HEIGHT;
            }
            else
            {
                game.graphics.PreferredBackBufferHeight = height;
            }
            // FullHD未満の環境ではHD解像度に切り替え
            if (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width <= game.graphics.PreferredBackBufferWidth)
            {
                vgamode = VgaType.HD;
                game.graphics.PreferredBackBufferWidth = SMALL_WIDTH;
                game.graphics.PreferredBackBufferHeight = SMALL_HEIGHT;
            }
            // ウィンドウサイズを初期設定
            game.graphics.ApplyChanges();            // コンストラクタだと設定が反映されない
            // 現在のウィンドウサイズ
            _windowWidth = game.graphics.PreferredBackBufferWidth;
            _windowHeight = game.graphics.PreferredBackBufferHeight;

            // 最初からフルスクリーン指定
            if (fullscreen)
            {
                if (game.graphics.IsFullScreen == false)
                {
                    ToggleFull(game);
                }
            }
        }

        // Full screen mode <> Window mode switcher
        public void ToggleFull(Game1 game)
        {
            if (game.graphics.IsFullScreen)
            {
                Debug.WriteLine("to Window");
                game.IsMouseVisible = true;
                game.graphics.PreferredBackBufferWidth = _windowWidth;
                game.graphics.PreferredBackBufferHeight = _windowHeight;
            }
            else
            {
                Debug.WriteLine("to Fullscreen");
                game.IsMouseVisible = false;
                _windowWidth = game.graphics.PreferredBackBufferWidth;
                _windowHeight = game.graphics.PreferredBackBufferHeight;
                game.graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                game.graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            game.graphics.ApplyChanges();
            game.graphics.ToggleFullScreen();

        }

        // 実画面とキャラクター画面の縦横比が違うため、上下または左右に余白ができることがある
        // 余白のサイズを計算する
        public void CalcEdge(GraphicsDevice device)
        {
            int nowwidth = device.PresentationParameters.BackBufferWidth;
            int nowheight = device.PresentationParameters.BackBufferHeight;
            // 幅に合わせた高さを計算
            int newheight = nowwidth * CEL_HEIGHT_DEF / CEL_WIDTH_DEF;
            if (newheight < nowheight)
            { // ウィンドウの高さが余る
                edgeWidth = 0;
                edgeHeight = nowheight - newheight;
            }
            else if (newheight > nowheight)
            { // 今のウィンドウ高さに合わせると幅が余る
                int newwidth = nowheight * CEL_WIDTH_DEF / CEL_HEIGHT_DEF;
                edgeWidth = nowwidth - newwidth;
                edgeHeight = 0;
            }
            else
            {
                edgeWidth = 0;
                edgeHeight = 0;
            }
        }
        public void Resize(Game1 game)
        {
            if (game.graphics.IsFullScreen==false)
            {   // ウィンドウのリサイズ専用（フルスクリーンモードは縦横比調整できない）
                CalcEdge(game.GraphicsDevice);
                if( 0 < edgeWidth)
                {
                    game.graphics.PreferredBackBufferWidth = game.GraphicsDevice.PresentationParameters.BackBufferWidth - edgeWidth;
                    game.graphics.PreferredBackBufferHeight = game.GraphicsDevice.PresentationParameters.BackBufferHeight;
                    game.graphics.ApplyChanges();
                }
                else if( 0 < edgeHeight)
                {
                    game.graphics.PreferredBackBufferWidth = game.GraphicsDevice.PresentationParameters.BackBufferWidth ;
                    game.graphics.PreferredBackBufferHeight = game.GraphicsDevice.PresentationParameters.BackBufferHeight - edgeHeight;
                    game.graphics.ApplyChanges();
                }
                // ウィンドウサイズを保存
                _windowWidth = game.graphics.PreferredBackBufferWidth;
                _windowHeight = game.graphics.PreferredBackBufferHeight;
            }
        }

        // 画面サイズをフルHD、HDに切り替える
        public void ToggleSize(Game1 game)
        {
            //if (game.graphics.IsFullScreen)   // ウィンドウモードでも使える
            {
                if (vgamode == VgaType.fullHD)
                {
                    vgamode = VgaType.HD;
                    Debug.WriteLine("to Small ScreenSize(Full)");
                    game.graphics.PreferredBackBufferWidth = SMALL_WIDTH_FULL;
                    game.graphics.PreferredBackBufferHeight = SMALL_HEIGHT_FULL;
                }
                else
                {
                    vgamode = VgaType.fullHD;
                    Debug.WriteLine("to Default ScreenSize(Full)");
                    game.graphics.PreferredBackBufferWidth = MID_WIDTH_FULL;
                    game.graphics.PreferredBackBufferHeight = MID_HEIGHT_FULL;
                }
                game.graphics.ApplyChanges();
            }
        }

        public void SetViewport(Game1 game)
        { // 全画面時は横か縦かどちらか余る

            if (game.graphics.IsFullScreen )
            {
                CalcEdge(game.GraphicsDevice);

                _viewport.X = edgeWidth / 2;
                _viewport.Y = edgeHeight / 2;
                _viewport.Width = game.GraphicsDevice.PresentationParameters.BackBufferWidth - edgeWidth;
                _viewport.Height = game.GraphicsDevice.PresentationParameters.BackBufferHeight - edgeHeight;
                game.GraphicsDevice.Viewport = _viewport;
            }
            else
            {
                _viewport.X = 0;
                _viewport.Y = 0;
                _viewport.Width = game.GraphicsDevice.PresentationParameters.BackBufferWidth;
                _viewport.Height = game.GraphicsDevice.PresentationParameters.BackBufferHeight;
                game.GraphicsDevice.Viewport = _viewport;

            }
        }

        // テキスト画面座標からビュー画面描画座標に変換する
        // 左上を(0,0)原点に変更 （テキスト画面は左右中央がX=0列、画面最下行がY=0行 上方向へ+Y）
        // 先に SetViewport()が必要
        public Rectangle TextBox(int cx, int cy,int sizex,int sizey)
        {   
            Rectangle box;

            int x1 = cx * _viewport.Width / celwidth();
            int x2 = (cx+sizex)  * _viewport.Width / celwidth();
            int y1 = cy * _viewport.Height / celheight();
            int y2 = (cy+sizey)  * _viewport.Height / celheight();

            if (0 < _celNextRatio)
            { // キャラクター画面サイズ遷移中
                int nx1 = cx * _viewport.Width / _celWidthNext;
                int nx2 = (cx + sizex) * _viewport.Width / _celWidthNext;
                int ny1 = cy * _viewport.Height / _celHeightNext;
                int ny2 = (cy + sizey) * _viewport.Height / _celHeightNext;
                x1 = (x1 * (100 - _celNextRatio) + nx1 * _celNextRatio) / 100;
                x2 = (x2 * (100 - _celNextRatio) + nx2 * _celNextRatio) / 100;
                y1 = (y1 * (100 - _celNextRatio) + ny1 * _celNextRatio) / 100;
                y2 = (y2 * (100 - _celNextRatio) + ny2 * _celNextRatio) / 100;
                
                if( celwidth()+2 <= _celWidthNext)
                {   // 幅が2以上増える場合、左にも隙間をつくる
                    int head = ((_celWidthNext - celwidth())*(_viewport.Width / _celWidthNext) * _celNextRatio / 100) / 2;
                    x1 += head;
                    x2 += head;
                }
            }

            box.X = x1;
            box.Y = y1;
            box.Width = x2 - x1;
            box.Height = y2 - y1;

            return box;
        }
        public Rectangle TextBox(int cx, int cy)
        {
            return TextBox(cx, cy, 1, 1);
        }

        // 1と2の中間座標を返す
        // ratio1が0であれば2の座標
        public Rectangle TextBoxBetween(int cx1, int cy1, int cx2, int cy2, int ratio1, int range)
        {
            Rectangle r1 = TextBox(cx1, cy1);
            Rectangle r2 = TextBox(cx2, cy2);
            Rectangle r;

            r.Width = Math.Max(r1.Width,r2.Width);
            r.Height = Math.Max(r1.Height, r2.Height);
            r.X = (r1.X * ratio1 + r2.X * (range - ratio1)) / range;
            r.Y = (r1.Y * ratio1 + r2.Y * (range - ratio1)) / range;
            return r;
        }

        // ビュー画面描画座標を返すが、指定された仮想ドット数だけずらす。影とかエフェクト用
        public Rectangle TextBoxOffset(int cx, int cy, int offsetx, int offsety)
        {
            Rectangle box = TextBox(cx, cy);
            // (画面幅 / 横キャラ数 = ビュー画面上のキャラクター幅）* (ずらし幅 / 仮想キャラクター幅） 精度のため計算順変更
            box.X += _viewport.Width * offsetx / celwidth() / CEL_SIZE_DEF;
            box.Y += _viewport.Height * offsety / celheight() / CEL_SIZE_DEF;
            return box;
        }

#if false
        // キャラクターn個分のドット幅を返す
        public int TextWidth(int num,int x)
        {
            int x1 = x * _viewport.Width / celwidth();
            int x2 = (x+num) * _viewport.Width / celwidth();

            return x2 - x1;
        }
        // キャラクターn個分のドット高さを返す
        public int TextHeight(int num, int y)
        {
            int y1 = y * _viewport.Height/ celheight();
            int y2 = (y + num) * _viewport.Height/ celheight();

            return y2 - y1;
        }
#endif
        public void CelInit(int width, int height)
        {   // キャラクター画面サイズの初期化
            _celWidth = width;
            _celHeight = height;
            _celWidthNext = 0;
            _celHeightNext = 0;
            _celNextRatio = 0;
            _celXShift = 0;
        }
        public void CelInit()
        {   // キャラクター画面サイズの初期化（デフォルト値）
            CelInit(CEL_WIDTH_DEF, CEL_HEIGHT_DEF);
        }

        // 変更後のキャラクター画面サイズの指定 キャラクター高さを指定する。キャラクター幅は自動計算で決定
        // nextRatioは、現行画面サイズに変更後の画面サイズを混ぜる比率(1~100%)
        // nextRatioに100%を指定すれば、遷移中にならずいきなりキャラクター画面サイズを切り替える
        // return 前回の遷移の完了:1 設定成功:0
        public int CelResizeNext(int height, int nextRatio)
        { 
            int rc = 0;

            if( height < 10 )
            {   // 最低高さ10とする
                return -1;
            }
            if (0 < _celNextRatio)
            { // まだcelNextへの移行が終わっていないのなら、このタイミングで強制移行完了
                _celXShift += (_celWidthNext - _celWidth) / 2;
                _celWidth = _celWidthNext;
                _celHeight = _celHeightNext;
                _celNextRatio = 0;
                rc = 1;
            }
            if (100 <= nextRatio)
            { // 遷移中にならずいきなりキャラクター画面サイズを切り替える
                int widthnext = height * CEL_WIDTH_DEF / CEL_HEIGHT_DEF;
                _celXShift += (widthnext - _celWidth) / 2;
                _celWidth = widthnext;
                _celHeight = height;
            }
            else
            { // 99%までなら遷移中になりキャラクター画面サイズは今は切り替えない
                _celWidthNext = height * CEL_WIDTH_DEF / CEL_HEIGHT_DEF;
                _celHeightNext = height;
                if (nextRatio <= 0)
                {
                    nextRatio = 1;
                }
                _celNextRatio = nextRatio;
            }
            return rc;
        }

        // キャラクター画面サイズ変更率の増加 このクラスにおけるUpdateに相当する 
        // 100%を指定すれば1回で遷移完了
        // return 遷移完了:1 キャラクター画面サイズに変化なし:0
        public int CelNextAdd(int addRatio)
        {  
            if (_celNextRatio <= 0)
            { // 今は遷移中ではない
                return -1;
            }
            _celNextRatio += addRatio;
            if (_celNextRatio < 0)
            { // 遷移のキャンセル 画面サイズ変更を無かったことにする
                _celNextRatio = 0;
            }
            else if (100 <= _celNextRatio)
            { // 遷移を完了させる
                _celXShift += (_celWidthNext - _celWidth) / 2;
                _celWidth = _celWidthNext;
                _celHeight = _celHeightNext;
                _celNextRatio = 0;
                return 1;
            }
            return 0;
        }
    }
}
