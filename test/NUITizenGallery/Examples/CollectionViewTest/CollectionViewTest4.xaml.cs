﻿/*
 * Copyright(c) 2021 Samsung Electronics Co., Ltd.
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
using System.ComponentModel;
using Tizen.NUI;
using Tizen.NUI.Binding;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Tizen.NUI.Components.Extension;

namespace NUITizenGallery
{
    public partial class CollectionViewTest4 : ContentPage
    {
        DataTemplate LinearTemplate = new DataTemplate(() =>
        {
            var item = new RecyclerViewItem()
            {
                WidthSpecification = LayoutParamPolicies.MatchParent,
                HeightSpecification = 200,
            };
            item.SetBinding(View.BackgroundColorProperty, "BgColor");
            var label = new TextLabel()
            {
                ParentOrigin = Tizen.NUI.ParentOrigin.Center,
                PivotPoint = Tizen.NUI.PivotPoint.Center,
                PositionUsesPivotPoint = true,
            };
            label.PixelSize = 30;
            label.SetBinding(TextLabel.TextProperty, "Index");
            item.Add(label);

            return item;
        });
        DataTemplate GridTemplate = new DataTemplate(() =>
        {
            var item = new RecyclerViewItem()
            {
                WidthSpecification = (NUIApplication.GetDefaultWindow().WindowSize.Width / 2),
                HeightSpecification = 200,
            };
            item.SetBinding(View.BackgroundColorProperty, "BgColor");
            var label = new TextLabel()
            {
                ParentOrigin = Tizen.NUI.ParentOrigin.Center,
                PivotPoint = Tizen.NUI.PivotPoint.Center,
                PositionUsesPivotPoint = true,
            };
            label.PixelSize = 30;
            label.SetBinding(TextLabel.TextProperty, "Index");
            item.Add(label);

            return item;
        });

        void OnGridLayouterRadioChanged(object sender, SelectedChangedEventArgs e)
        {
            if (e.IsSelected)
            {
                ColView.ItemTemplate = GridTemplate;
                ColView.ItemsLayouter = new GridLayouter();
            }
            else
            {
                ColView.ItemTemplate = LinearTemplate;
                ColView.ItemsLayouter = new LinearLayouter();
            }
        }

        public CollectionViewTest4()
        {
            InitializeComponent();
            BindingContext = new TestSourceModel();

            var RadioGroup = new RadioButtonGroup();
            RadioGroup.Add(Linear);
            RadioGroup.Add(Grid);

            ColView.ItemTemplate = LinearTemplate;
        }

        protected override void Dispose(DisposeTypes type)
        {
            if (Disposed)
            {
                return;
            }

            if (type == DisposeTypes.Explicit)
            {
                RemoveAllChildren(true);
            }

            base.Dispose(type);
        }

        private void RemoveAllChildren(bool dispose = false)
        {
            RecursiveRemoveChildren(this, dispose);
        }

        private void RecursiveRemoveChildren(View parent, bool dispose)
        {
            if (parent == null)
            {
                return;
            }

            int maxChild = (int)parent.ChildCount;
            for (int i = maxChild - 1; i >= 0; --i)
            {
                View child = parent.GetChildAt((uint)i);
                if (child == null)
                {
                    continue;
                }

                RecursiveRemoveChildren(child, dispose);
                parent.Remove(child);
                if (dispose)
                {
                    child.Dispose();
                }
            }
        }
    }
}
