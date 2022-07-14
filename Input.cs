using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace Atode
{
    enum Key
    {
        Up,
        Down,
        Left,
        Right,
        A,
        B,
        Fullscreen,
        Screensize,
        Pause,
        Back,
        LB,
        Pup,
        Endmark
    }
    public class Input
    {
        private const int REACTION_HARDWARE = 60;       // ハードウェア系操作の連打判定防止クールタイム
        private const int REACTION_SYSTEM = 40;         // システム系操作の連打判定防止クールタイム
        private const int REACTION_GAME_FAST = 10;      // ゲーム本番中の連打判定防止クールタイム
        private const int REACTION_GAME_SLOW = 20;      // ゲーム外メニュー画面等の連打判定防止クールタイム
        private int reactionGame = REACTION_GAME_SLOW;  // ゲーム操作入力の連打判定防止クールタイム
        private int iCounter = 0;
        public bool bFullscreen = false;
        public bool bScreensize = false;
        public bool bPause = false;
        private int[] lasttime;
        private int[] pressed;

        public Input()
        {
            lasttime = new int[(int)Key.Endmark];
            System.Array.Clear(lasttime, 0, (int)Key.Endmark);
            pressed = new int[(int)Key.Endmark];
            System.Array.Clear(pressed, 0, (int)Key.Endmark);
        }
        public bool Get(int key)
        {
            if( pressed[key] != 0)
            {
                return true;
            }
            return false;
        }
        public bool AnyKey()
        {
            if( pressed[(int)Key.Up] == 1 ||
                pressed[(int)Key.Down] == 1 ||
                pressed[(int)Key.Left] == 1 ||
                pressed[(int)Key.Right] == 1 ||
                pressed[(int)Key.A] == 1 ||
                pressed[(int)Key.B] == 1 ||
                pressed[(int)Key.LB] == 1)
            {
                return true;
            }
            return false;
        }

        // ゲーム中は高速連打が可能とする
        public void SetReactionFast(bool fast)
        {
            if (fast)
            {
                reactionGame = REACTION_GAME_FAST;
            }
            else
            {
                reactionGame = REACTION_GAME_SLOW;
            }
        }
        public void Update(bool isFullScreen)
        {
            iCounter++;

            // ボタンは押されていない（デフォルト）
            System.Array.Clear(pressed, 0, (int)Key.Endmark);

            // Get the current gamepad state.
            GamePadState pad = GamePad.GetState(PlayerIndex.One);
            KeyboardState kb = Keyboard.GetState();

            // 60fps連打状態を防ぐため、一度trueにしたら一定期間は入力抑止
            // シーン終了（タイトル画面ならプログラム終了）
            if (kb.IsKeyDown(Keys.Escape) ||
                (pad.IsConnected && (pad.Buttons.Back == ButtonState.Pressed)))
            { // Backボタン = Esc
                if (lasttime[(int)Key.Back] + REACTION_SYSTEM < iCounter)
                {
                    Debug.WriteLine("Escape or Back");
                    pressed[(int)Key.Back] = 1;
                    lasttime[(int)Key.Back] = iCounter;
                }
            }

            // システム系は、ゲーム入力系とは扱いが異なる。
            // フルスクリーン切り替え
            // RightStick押し込みはトグル操作だが、キーボードはF4キーでトグルだと無限にウィンドウ-フルの切り替えが続いてしまう
            // 全画面化はF4、ウィンドウ化はF5に固定
            if ((kb.IsKeyDown(Keys.F4) && !isFullScreen) ||
                (kb.IsKeyDown(Keys.F5) && isFullScreen) ||
                (pad.IsConnected && (pad.Buttons.RightStick == ButtonState.Pressed) ))
                {
                if (lasttime[(int)Key.Fullscreen] + REACTION_HARDWARE < iCounter)
                {
                    Debug.WriteLine("RightStick or F4");
                    bFullscreen = true;
                    lasttime[(int)Key.Fullscreen] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.F3) )
            {   // 全画面解像度切り替え
                if (lasttime[(int)Key.Screensize] + REACTION_SYSTEM < iCounter)
                {
                    Debug.WriteLine("F3");
                    bScreensize = true;
                    lasttime[(int)Key.Screensize] = iCounter;
                }
            }
            
            if (kb.IsKeyDown(Keys.P) ||
                (pad.IsConnected && (pad.Buttons.Start == ButtonState.Pressed)))
            {   // ゲームポーズ
                if (lasttime[(int)Key.Pause] + REACTION_SYSTEM < iCounter)
                {
                    Debug.WriteLine("Start or P");
                    bPause = !bPause;
                    lasttime[(int)Key.Pause] = iCounter;

                }
            }

            // ゲーム入力系
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up) ||
                (pad.IsConnected && (pad.DPad.Up == ButtonState.Pressed)))
            {   // 上 = W
                if (lasttime[(int)Key.Up] + reactionGame < iCounter)
                {
                    pressed[(int)Key.Up] = 1;
                    lasttime[(int)Key.Up] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down) ||
                (pad.IsConnected && (pad.DPad.Down == ButtonState.Pressed)))
            {   // 下 = S
                if (lasttime[(int)Key.Down] + reactionGame < iCounter)
                {
                    pressed[(int)Key.Down] = 1;
                    lasttime[(int)Key.Down] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left) ||
                (pad.IsConnected && (pad.DPad.Left == ButtonState.Pressed)))
            {   // 左 = A
                if (lasttime[(int)Key.Left] + reactionGame < iCounter)
                {
                    pressed[(int)Key.Left] = 1;
                    lasttime[(int)Key.Left] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right) ||
                (pad.IsConnected && (pad.DPad.Right == ButtonState.Pressed)))
            {   // 右 = D
                if (lasttime[(int)Key.Right] + reactionGame < iCounter)
                {
                    pressed[(int)Key.Right] = 1;
                    lasttime[(int)Key.Right] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.Z) || kb.IsKeyDown(Keys.Enter) ||
                (pad.IsConnected && (pad.Buttons.A == ButtonState.Pressed)))
            {   // Aボタン = Z = Enter
                if (lasttime[(int)Key.A] + reactionGame < iCounter)
                {
                    pressed[(int)Key.A] = 1;
                    lasttime[(int)Key.A] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.X) || kb.IsKeyDown(Keys.Space) ||
                (pad.IsConnected && (pad.Buttons.B == ButtonState.Pressed)))
            {   // Bボタン = X = Space
                if (lasttime[(int)Key.B] + reactionGame < iCounter)
                {
                    pressed[(int)Key.B] = 1;
                    lasttime[(int)Key.B] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift) ||
                (pad.IsConnected && (pad.Buttons.LeftShoulder == ButtonState.Pressed)))
            {   // LBボタン = SHiftキー
                if (lasttime[(int)Key.LB] + reactionGame < iCounter)
                {
                    pressed[(int)Key.LB] = 1;
                    lasttime[(int)Key.LB] = iCounter;
                }
            }

            if (kb.IsKeyDown(Keys.PageUp))
            {   // Pupキー
                if (lasttime[(int)Key.Pup] + reactionGame < iCounter)
                {
                    pressed[(int)Key.Pup] = 1;
                    lasttime[(int)Key.Pup] = iCounter;
                }
            }
        }

    }
}
