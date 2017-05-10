/*
* Copyright (c) 2016 Samsung Electronics Co., Ltd.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

using System;
using System.Runtime.InteropServices;
using Tizen.NUI;
using Tizen.NUI.UIComponents;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Constants;

namespace HelloWorldTest
{
    class Example : NUIApplication
    {
        private Animation _animation;
        private TextLabel _text;
        private int cnt;
        private View _view;

        public Example() : base()
        {
        }

        public Example(string stylesheet) : base(stylesheet)
        {
        }

        public Example(string stylesheet, WindowMode windowMode) : base(stylesheet, windowMode)
        {
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Initialize();
        }

        public void Initialize()
        {
            Window window = Window.Instance;
            window.BackgroundColor = Color.White;
            window.TouchEvent += OnWindowTouched;
            window.KeyEvent += OnWindowKeyEvent;

            TextLabel pixelLabel = new TextLabel("Test Pixel Size 32.0f");
            pixelLabel.Position2D = new Position2D(10, 10);
            pixelLabel.PixelSize = 32.0f;
            window.GetDefaultLayer().Add(pixelLabel);

            TextLabel pointLabel = new TextLabel("Test Point Size 32.0f");
            pointLabel.Position2D = new Position2D(10, 100);
            pointLabel.PointSize = 32.0f;
            window.GetDefaultLayer().Add(pointLabel);

            TextLabel ellipsis = new TextLabel("Ellipsis of TextLabel is enabled.");
            ellipsis.Size2D = new Size2D(100, 100);
            ellipsis.Position2D = new Position2D(10, 250);
            ellipsis.PointSize = 20.0f;
            ellipsis.Ellipsis = true;
            window.GetDefaultLayer().Add(ellipsis);

            _text = new TextLabel("Hello NUI World");
            _text.ParentOrigin = ParentOrigin.Center;
            _text.AnchorPoint = AnchorPoint.Center;
            _text.HorizontalAlignment = HorizontalAlignment.Center;
            _text.PointSize = 32.0f;
            _text.TextColor = Color.Magenta;
            window.GetDefaultLayer().Add(_text);

            _view = new View();
            _view.Size = new Size(100, 100, 100);
            _view.SizeWidth = 50;
            Tizen.Log.Debug("NUI", "[1]_view SizeWidth=" + _view.SizeWidth);

            _animation = new Animation
            {
                Duration = 2000
            };
            _animation.AnimateTo(_text, "Orientation", new Rotation(new Radian(new Degree(180.0f)), PositionAxis.X), 0, 500);
            _animation.AnimateTo(_text, "Orientation", new Rotation(new Radian(new Degree(0.0f)), PositionAxis.X), 500, 1000);
            _animation.AnimateBy(_text, "ScaleX", 3, 1000, 1500);
            _animation.AnimateBy(_text, "ScaleY", 4.0f, 1500, 2000);
            _animation.EndAction = Animation.EndActions.Discard;
            _animation.Finished += AnimationFinished;

            _view.SizeWidth = 50;
            Tizen.Log.Debug("NUI", "[2]_view SizeWidth=" + _view.SizeWidth);
        }

        public void AnimationFinished(object sender, EventArgs e)
        {
            Tizen.Log.Debug("NUI", "AnimationFinished()! cnt=" + (cnt));
            if (_animation)
            {
                Tizen.Log.Debug("NUI", "Duration= " + _animation.Duration + "EndAction= " + _animation.EndAction);
            }
            _view.SizeWidth = 50;
            Tizen.Log.Debug("NUI", "[3]_view SizeWidth=" + _view.SizeWidth);
        }

        public void OnWindowKeyEvent(object sender, Window.KeyEventArgs e)
        {
            if (e.Key.State == Key.StateType.Down)
            {
                if (e.Key.KeyPressedName == "Up")
                {
                    if (_animation)
                    {
                        _animation.Finished += AnimationFinished;
                        cnt++;
                        Tizen.Log.Debug("NUI", "AnimationFinished added!");
                    }
                }
                else if (e.Key.KeyPressedName == "Down")
                {
                    if (_animation)
                    {
                        _animation.Finished -= AnimationFinished;
                        cnt--;
                        Tizen.Log.Debug("NUI", "AnimationFinished removed!");
                    }
                }
            }
        }

        public void OnWindowTouched(object sender, Window.TouchEventArgs e)
        {
            if (e.Touch.GetState(0) == PointStateType.Down)
            {
                if (_animation)
                {
                    //_animation.Stop(Dali.Constants.Animation.EndAction.Stop);
                    //_animation.Reset();
                }
                _animation.Play();
            }
        }

        [STAThread]
        static void _Main(string[] args)
        {
            //Tizen.Log.Debug("NUI", "Main() called!");
            Example example = new Example();
            example.Run(args);
        }
    }
}
