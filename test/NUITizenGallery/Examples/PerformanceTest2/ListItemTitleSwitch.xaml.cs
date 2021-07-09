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
using Tizen.NUI.BaseComponents;
using Tizen.NUI;

namespace NUITizenGallery
{
    public partial class ListItemTitleSwitch : View
    {
        public ListItemTitleSwitch(string title)
        {
            InitializeComponent();
            TextLabelTitle.Text = title;
            ListItemSwitch.IsSelected = true;

            LinearLayout itemLayout = new LinearLayout();
            itemLayout.LinearOrientation = LinearLayout.Orientation.Horizontal;
            itemLayout.LinearAlignment = LinearLayout.Alignment.Center;            
            itemLayout.Padding = new Extents(5, 5, 5, 5);
            itemLayout.CellPadding = new Size2D(100, 10);
            this.Layout = itemLayout;
        }
    }
}
