/*
 * Copyright (c) 2016 Samsung Electronics Co., Ltd All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the License);
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an AS IS BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ElmSharp
{
    public enum TooltipOrientation
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
    }

    public enum AspectControl
    {
        None = 0, /* Preference on scaling unset */
        Neither = 1, /* Same effect as unset preference on scaling */
        Horizontal = 2, /* Use all horizontal container space to place an object, using the given aspect */
        Vertical = 3, /* Use all vertical container space to place an object, using the given aspect */
        Both = 4 /* Use all horizontal @b and vertical container spaces to place an object (never growing it out of those bounds), using the given aspect */
    }

    /// <summary>
    /// The EcasObject is a base class for other widget class
    /// </summary>
    public abstract class EvasObject
    {
        private IntPtr _realHandle = IntPtr.Zero;
        private EvasCanvas _evasCanvas;

        private event EventHandler _backButtonPressed;

        private event EventHandler _moreButtonPressed;

        private Interop.Eext.EextEventCallback _backButtonHandler;
        private Interop.Eext.EextEventCallback _moreButtonHandler;

        public IntPtr Handle { get; protected set; }
        public EvasObject Parent { get; private set; }

        public IntPtr RealHandle
        {
            get
            {
                return _realHandle == IntPtr.Zero ? Handle : _realHandle;
            }
            protected set
            {
                _realHandle = value;
            }
        }

        EvasObjectEvent _deleted;
        EvasObjectEvent<EvasKeyEventArgs> _keyup;
        EvasObjectEvent<EvasKeyEventArgs> _keydown;
        EvasObjectEvent _moved;
        EvasObjectEvent _resized;
        EventHandler _renderPost;
        Interop.Evas.EvasCallback _renderPostCallback = null;
        Interop.Elementary.Elm_Tooltip_Content_Cb _tooltipContentCallback = null;

        GetTooltipContentDelegate _tooltipContentDelegate = null;

        readonly HashSet<IInvalidatable> _eventStore = new HashSet<IInvalidatable>();

        /// <summary>
        /// Creates and initializes a new instance of the EvasObject class with parent EvasObject class parameter.
        /// </summary>
        /// <param name="parent">Parent EvasObject class </param>
        protected EvasObject(EvasObject parent) : this()
        {
            Debug.Assert(parent == null || parent.IsRealized);
            Realize(parent);
        }

        /// <summary>
        /// Creates and initializes a new instance of the EvasObject class.
        /// </summary>
        protected EvasObject()
        {
            _backButtonHandler = new Interop.Eext.EextEventCallback((d, o, i) => { _backButtonPressed?.Invoke(this, EventArgs.Empty); });
            _moreButtonHandler = new Interop.Eext.EextEventCallback((d, o, i) => { _moreButtonPressed?.Invoke(this, EventArgs.Empty); });

            OnInstantiated();

            _tooltipContentCallback = (d, o, t) =>
            {
                return _tooltipContentDelegate?.Invoke();
            };
        }

        // C# Finalizer was called on GC thread
        // So, We can't access to EFL object
        // And When Finalizer was called, Field can be already released.
        //~EvasObject()
        //{
        //    Unrealize();
        //}

        /// <summary>
        /// Deleted will be triggered when widght is deleted
        /// </summary>
        public event EventHandler Deleted;

        /// <summary>
        /// KeyUp will be triggered when key is loose
        /// </summary>
        public event EventHandler<EvasKeyEventArgs> KeyUp;

        /// <summary>
        /// KeyDown will be triggered when key is preesd down
        /// </summary>
        public event EventHandler<EvasKeyEventArgs> KeyDown;

        /// <summary>
        /// BackButtonPressed will be triggered when Back button is pressed
        /// </summary>
        public event EventHandler BackButtonPressed
        {
            add
            {
                if (_backButtonPressed == null)
                {
                    Interop.Eext.eext_object_event_callback_add(RealHandle, Interop.Eext.EextCallbackType.EEXT_CALLBACK_BACK, _backButtonHandler, IntPtr.Zero);
                }
                _backButtonPressed += value;
            }
            remove
            {
                _backButtonPressed -= value;
                if (_backButtonPressed == null)
                {
                    Interop.Eext.eext_object_event_callback_del(RealHandle, Interop.Eext.EextCallbackType.EEXT_CALLBACK_BACK, _backButtonHandler);
                }
            }
        }

        /// <summary>
        /// MoreButtonPressed will be triggered when More button is pressed
        /// </summary>
        public event EventHandler MoreButtonPressed
        {
            add
            {
                if (_moreButtonPressed == null)
                {
                    Interop.Eext.eext_object_event_callback_add(RealHandle, Interop.Eext.EextCallbackType.EEXT_CALLBACK_MORE, _moreButtonHandler, IntPtr.Zero);
                }
                _moreButtonPressed += value;
            }
            remove
            {
                _moreButtonPressed -= value;
                if (_moreButtonPressed == null)
                {
                    Interop.Eext.eext_object_event_callback_del(RealHandle, Interop.Eext.EextCallbackType.EEXT_CALLBACK_MORE, _moreButtonHandler);
                }
            }
        }

        /// <summary>
        /// Moved will be triggered when widght is moved
        /// </summary>
        public event EventHandler Moved
        {
            add { _moved.On += value; }
            remove { _moved.On -= value; }
        }

        /// <summary>
        /// Current widget's size Resized Event Handler
        /// </summary>
        public event EventHandler Resized
        {
            add { _resized.On += value; }
            remove { _resized.On -= value; }
        }

        /// <summary>
        /// Current widget RenderPost Event Handler
        /// </summary>
        public event EventHandler RenderPost
        {
            add
            {
                _renderPost += value;
                if (_renderPostCallback == null)
                {
                    _renderPostCallback = new Interop.Evas.EvasCallback((o, e, d) => _renderPost?.Invoke(this, EventArgs.Empty));
                    Interop.Evas.evas_event_callback_add(Interop.Evas.evas_object_evas_get(RealHandle), Interop.Evas.ObjectCallbackType.RenderPost, _renderPostCallback, IntPtr.Zero);
                }
            }
            remove
            {
                _renderPost -= value;
                if (_renderPost == null)
                {
                    Interop.Evas.evas_event_callback_del(Interop.Evas.evas_object_evas_get(RealHandle), Interop.Evas.ObjectCallbackType.RenderPost, _renderPostCallback);
                    _renderPostCallback = null;
                }
            }
        }

        /// <summary>
        /// Called back when a widget's tooltip is activated and needs content.
        /// </summary>
        /// <returns></returns>
        public delegate EvasObject GetTooltipContentDelegate();

        /// <summary>
        /// Get widget's status of Realized or not.
        /// </summary>
        public bool IsRealized { get { return Handle != IntPtr.Zero; } }

        /// <summary>
        /// Gets EvasCanvas
        /// </summary>
        public EvasCanvas EvasCanvas
        {
            get
            {
                if (_evasCanvas == null)
                    _evasCanvas = new EvasCanvas(Handle);
                return _evasCanvas;
            }
        }

        /// <summary>
        /// Gets the current class's Name.
        /// </summary>
        public string ClassName
        {
            get
            {
                return Interop.Eo.eo_class_name_get(Interop.Eo.eo_class_get(RealHandle));
            }
        }

        /// <summary>
        /// Sets or gets the horizontal pointer hints for an object's weight.
        /// </summary>
        public double WeightX
        {
            get
            {
                return Interop.Evas.GetWeightX(Handle);
            }
            set
            {
                Interop.Evas.SetWeightX(Handle, value);
            }
        }

        /// <summary>
        /// Sets or gets the vertical pointer hints for an object's weight.
        /// </summary>
        public double WeightY
        {
            get
            {
                return Interop.Evas.GetWeightY(Handle);
            }
            set
            {
                Interop.Evas.SetWeightY(Handle, value);
            }
        }

        /// <summary>
        /// Sets or gets the horizontal alignment hint of an object's alignment.
        /// </summary>
        public virtual double AlignmentX
        {
            get
            {
                return Interop.Evas.GetAlignX(Handle);
            }
            set
            {
                Interop.Evas.SetAlignX(Handle, value);
            }
        }

        /// <summary>
        /// Sets or gets the vertical alignment hint of an object's alignment.
        /// </summary>
        public virtual double AlignmentY
        {
            get
            {
                return Interop.Evas.GetAlignY(Handle);
            }
            set
            {
                Interop.Evas.SetAlignY(Handle, value);
            }
        }

        /// <summary>
        /// Sets or gets the Width hints for an object's minimum size.
        /// </summary>
        public int MinimumWidth
        {
            get
            {
                int w, h;
                Interop.Evas.evas_object_size_hint_min_get(RealHandle, out w, out h);
                return w;
            }
            set
            {
                int h = MinimumHeight;
                Interop.Evas.evas_object_size_hint_min_set(RealHandle, value, h);
            }
        }

        /// <summary>
        /// Sets or gets the Height hints for an object's minimum size.
        /// </summary>
        public int MinimumHeight
        {
            get
            {
                int w, h;
                Interop.Evas.evas_object_size_hint_min_get(RealHandle, out w, out h);
                return h;
            }
            set
            {
                int w = MinimumWidth;
                Interop.Evas.evas_object_size_hint_min_set(RealHandle, w, value);
            }
        }

        /// <summary>
        /// Gets the visible state of the given Evas object.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return Interop.Evas.evas_object_visible_get(Handle);
            }
        }

        /// <summary>
        /// Sets or gets the position and (rectangular) size of the given Evas object.
        /// </summary>
        public Rect Geometry
        {
            get
            {
                int x, y, w, h;
                Interop.Evas.evas_object_geometry_get(Handle, out x, out y, out w, out h);
                Rect rect = new Rect(x, y, w, h);
                return rect;
            }
            set
            {
                Interop.Evas.evas_object_geometry_set(Handle, value.X, value.Y, value.Width, value.Height);
            }
        }

        /// <summary>
        /// Sets or gets the general or main color of the given Evas object.
        /// </summary>
        public virtual Color Color
        {
            get
            {
                int r, g, b, a;
                Interop.Evas.evas_object_color_get(RealHandle, out r, out g, out b, out a);
                return Color.FromRgba(r, g, b, a);
            }
            set
            {
                Interop.Evas.SetPremultipliedColor(RealHandle, value.R, value.G, value.B, value.A);
            }
        }

        /// <summary>
        /// Sets or gets the map enabled state.
        /// </summary>
        public bool IsMapEnabled
        {
            get
            {
                return Interop.Evas.evas_object_map_enable_get(Handle);
            }
            set
            {
                Interop.Evas.evas_object_map_enable_set(Handle, value);
            }
        }

        /// <summary>
        /// Sets or gets current object transformation map.
        /// </summary>
        public EvasMap EvasMap
        {
            get
            {
                IntPtr evasMap = Interop.Evas.evas_object_map_get(Handle);
                return new EvasMap(evasMap);
            }
            set
            {
                Interop.Evas.evas_object_map_set(Handle, value.Handle);
            }
        }

        /// <summary>
        /// Sets or gets whether an object is to repeat events.
        /// </summary>
        public bool RepeatEvents
        {
            get
            {
                return Interop.Evas.evas_object_repeat_events_get(RealHandle);
            }
            set
            {
                Interop.Evas.evas_object_repeat_events_set(RealHandle, value);
            }
        }

        /// <summary>
        /// Sets or gets whether events on a smart object's member should get propagated up to its parent.
        /// </summary>
        public bool PropagateEvents
        {
            get
            {
                return Interop.Evas.evas_object_propagate_events_get(RealHandle);
            }
            set
            {
                Interop.Evas.evas_object_propagate_events_set(RealHandle, value);
            }
        }

        /// <summary>
        /// Sets or gets whether an object is set to pass (ignore) events.
        /// </summary>
        public bool PassEvents
        {
            get
            {
                return Interop.Evas.evas_object_pass_events_get(RealHandle);
            }
            set
            {
                Interop.Evas.evas_object_pass_events_set(RealHandle, value);
            }
        }

        /// <summary>
        /// Sets or Gets style for this object tooltip.
        /// </summary>
        public string TooltipStyle
        {
            get
            {
                return Interop.Elementary.elm_object_tooltip_style_get(RealHandle);
            }
            set
            {
                Interop.Elementary.elm_object_tooltip_style_set(RealHandle, value);
            }
        }

        /// <summary>
        /// Sets or gets the orientation of Tooltip.
        /// </summary>
        public TooltipOrientation TooltipOrientation
        {
            get
            {
                return (TooltipOrientation)Interop.Elementary.elm_object_tooltip_orient_get(RealHandle);
            }
            set
            {
                Interop.Elementary.elm_object_tooltip_orient_set(RealHandle, (int)value);
            }
        }

        /// <summary>
        /// Sets or gets size restriction state of an object's tooltip.
        /// </summary>
        public bool TooltipWindowMode
        {
            get
            {
                return Interop.Elementary.elm_object_tooltip_window_mode_get(RealHandle);
            }
            set
            {
                Interop.Elementary.elm_object_tooltip_window_mode_set(RealHandle, value);
            }
        }

        /// <summary>
        /// Sets the content to be shown in the tooltip object.
        /// </summary>
        public GetTooltipContentDelegate TooltipContentDelegate
        {
            get
            {
                return _tooltipContentDelegate;
            }
            set
            {
                _tooltipContentDelegate = value;
                if (value != null)
                {
                    Interop.Elementary.elm_object_tooltip_content_cb_set(RealHandle, _tooltipContentCallback, IntPtr.Zero, null);
                }
                else
                {
                    Interop.Elementary.elm_object_tooltip_content_cb_set(RealHandle, null, IntPtr.Zero, null);
                }
            }
        }

        /// <summary>
        /// Gets the movement freeze by 1
        /// This gets the movement freeze count by one.
        /// </summary>
        public int TooltipMoveFreezeCount
        {
            get
            {
                return Interop.Elementary.elm_object_tooltip_move_freeze_get(RealHandle);
            }
        }

        /// <summary>
        /// Sets or gets whether an Evas object is to freeze (discard) events.
        /// </summary>
        public bool AllEventsFrozen
        {
            get
            {
                return Interop.Evas.evas_object_freeze_events_get(RealHandle);
            }
            set
            {
                Interop.Evas.evas_object_freeze_events_set(RealHandle, value);
            }
        }

        /// <summary>
        /// Sets or gets the layer of its canvas that the given object will be part of.
        /// </summary>
        public virtual int Layer
        {
            get
            {
                return Interop.Evas.evas_object_layer_get(Handle);
            }
            set
            {
                Interop.Evas.evas_object_layer_set(Handle, value);
            }
        }

        /// <summary>
        /// Clips one object to another.
        /// </summary>
        /// <param name="clip">The object to clip object by</param>
        public void SetClip(EvasObject clip)
        {
            Interop.Evas.evas_object_clip_set(Handle, clip);
        }

        /// <summary>
        /// Sets the hints for an object's alignment.
        /// </summary>
        /// <param name="x">The horizontal alignment hint as double value ranging from 0.0 to 1.0,The default alignment hint value is 0.5 </param>
        /// <param name="y">The vertical alignment hint as double value ranging from 0.0 to 1.0,The default alignment hint value is 0.5 </param>
        public void SetAlignment(double x, double y)
        {
            Interop.Evas.evas_object_size_hint_align_set(Handle, x, y);
        }

        /// <summary>
        /// Sets the hints for an object's weight.
        /// </summary>
        /// <param name="x">The non-negative double value to use as horizontal weight hint</param>
        /// <param name="y">The non-negative double value to use as vertical weight hint</param>
        public void SetWeight(double x, double y)
        {
            Interop.Evas.evas_object_size_hint_weight_set(Handle, x, y);
        }

        /// <summary>
        /// Sets the text for an object's tooltip.
        /// </summary>
        /// <param name="text">The text value to display inside the tooltip</param>
        public void SetTooltipText(string text)
        {
            Interop.Elementary.elm_object_tooltip_text_set(RealHandle, text);
        }

        /// <summary>
        /// Unsets an object's tooltip.
        /// </summary>
        public void UnsetTooltip()
        {
            Interop.Elementary.elm_object_tooltip_unset(RealHandle);
        }

        /// <summary>
        /// This increments the tooltip movement freeze count by one.
        /// If the count is more than 0, the tooltip position will be fixed.
        /// </summary>
        public void PushTooltipMoveFreeze()
        {
            Interop.Elementary.elm_object_tooltip_move_freeze_push(RealHandle);
        }

        /// <summary>
        /// This decrements the tooltip freeze count by one.
        /// </summary>
        public void PopTooltipMoveFreeze()
        {
            Interop.Elementary.elm_object_tooltip_move_freeze_pop(RealHandle);
        }

        /// <summary>
        /// Force hide tooltip of object.
        /// </summary>
        public void HideTooltip()
        {
            Interop.Elementary.elm_object_tooltip_hide(RealHandle);
        }

        /// <summary>
        /// Force show tooltip of object.
        /// </summary>
        public void ShowTooltip()
        {
            Interop.Elementary.elm_object_tooltip_show(RealHandle);
        }

        /// <summary>
        /// Makes the current object visible.
        /// </summary>
        public void Show()
        {
            Interop.Evas.evas_object_show(Handle);
        }

        /// <summary>
        /// Makes the current object invisible.
        /// </summary>
        public void Hide()
        {
            Interop.Evas.evas_object_hide(Handle);
        }

        /// <summary>
        /// Changes the size of the current object.
        /// </summary>
        /// <param name="w">The new width</param>
        /// <param name="h">The new height</param>
        public void Resize(int w, int h)
        {
            Interop.Evas.evas_object_resize(Handle, w, h);
        }

        /// <summary>
        /// Moves the current object to the given location.
        /// </summary>
        /// <param name="x">The X position to move the object to.</param>
        /// <param name="y">The Y position to move the object to.</param>
        public void Move(int x, int y)
        {
            Interop.Evas.evas_object_move(Handle, x, y);
        }

        /// <summary>
        /// Lowers obj to the bottom of its layer.
        /// </summary>
        public void Lower()
        {
            Interop.Evas.evas_object_lower(Handle);
        }

        /// <summary>
        /// Define IntPtr operator
        /// </summary>
        /// <param name="obj">Parent object</param>
        public static implicit operator IntPtr(EvasObject obj)
        {
            if (obj == null)
                return IntPtr.Zero;
            return obj.Handle;
        }

        /// <summary>
        /// Requests keyname key events be directed to current obj.
        /// </summary>
        /// <param name="keyname">The key to request events for</param>
        /// <param name="exclusive">Set TRUE to request that the obj is the only object receiving the keyname events,otherwise set FALSE</param>
        /// <returns>If the call succeeded is true,otherwise is false</returns>
        public bool KeyGrab(string keyname, bool exclusive)
        {
            return Interop.Evas.evas_object_key_grab(Handle, keyname, 0, 0, exclusive);
        }

        /// <summary>
        /// Removes the grab on keyname key events.
        /// </summary>
        /// <param name="keyname">The key the grab is set for</param>
        public void KeyUngrab(string keyname)
        {
            Interop.Evas.evas_object_key_ungrab(Handle, keyname, 0, 0);
        }

        /// <summary>
        /// Mark smart object as changed.
        /// </summary>
        public void MarkChanged()
        {
            Interop.Evas.evas_object_smart_changed(RealHandle);
        }

        /// <summary>
        /// Call the calculate smart function immediately.
        /// This will force immediate calculations needed for renderization of this object.
        /// </summary>
        public void Calculate()
        {
            Interop.Evas.evas_object_smart_calculate(RealHandle);
        }

        /// <summary>
        /// Sets the hints for an object's aspect ratio.
        /// </summary>
        /// <param name="aspect">The policy or type of aspect ratio to apply to object</param>
        /// <param name="w">The integer to use as aspect width ratio term</param>
        /// <param name="h">The integer to use as aspect height ratio term</param>
        public void SetSizeHintAspect(AspectControl aspect, int w, int h)
        {
            Interop.Evas.evas_object_size_hint_aspect_set(Handle, (int)aspect, w, h);
        }

        /// <summary>
        /// Gets the hints for an object's aspect ratio.
        /// </summary>
        /// <param name="aspect">The policy or type of aspect ratio to apply to object</param>
        /// <param name="w">The integer to use as aspect width ratio term</param>
        /// <param name="h">The integer to use as aspect height ratio term</param>
        public void GetSizeHintAspect(out AspectControl aspect, out int w, out int h)
        {
            int aspectRatio;
            Interop.Evas.evas_object_size_hint_aspect_get(Handle, out aspectRatio, out w, out h);
            aspect = (AspectControl)aspectRatio;
        }

        /// <summary>
        /// Stack immediately below anchor.
        /// </summary>
        /// <param name="anchor">The object below which to stack.</param>
        public void StackBelow(EvasObject anchor)
        {
            Interop.Evas.evas_object_stack_below(Handle, anchor);
        }

        /// <summary>
        /// Stack immediately above anchor.
        /// </summary>
        /// <param name="anchor">The object above which to stack.</param>
        public void StackAbove(EvasObject anchor)
        {
            Interop.Evas.evas_object_stack_above(Handle, anchor);
        }

        /// <summary>
        /// Raise to the top of its layer.
        /// </summary>
        public void RaiseTop()
        {
            Interop.Evas.evas_object_raise(Handle);
        }

        /// <summary>
        /// Get the geometry of a line number.
        /// </summary>
        /// <param name="lineNumber">the line number.</param>
        /// <param name="x">x coord of the line.</param>
        /// <param name="y">y coord of the line.</param>
        /// <param name="w">w coord of the line.</param>
        /// <param name="h">h coord of the line.</param>
        /// <returns></returns>
        public bool GetTextBlockGeometryByLineNumber(int lineNumber, out int x, out int y, out int w, out int h)
        {
            return Interop.Evas.evas_object_textblock_line_number_geometry_get(RealHandle, lineNumber, out x, out y, out w, out h);
        }

        internal IntPtr GetData(string key)
        {
            return Interop.Evas.evas_object_data_get(RealHandle, key);
        }

        internal void SetData(string key, IntPtr data)
        {
            Interop.Evas.evas_object_data_set(RealHandle, key, data);
        }

        internal IntPtr DeleteData(string key)
        {
            return Interop.Evas.evas_object_data_del(RealHandle, key);
        }

        /// <summary>
        /// The callback of Invalidate Event
        /// </summary>
        protected virtual void OnInvalidate()
        {
        }

        /// <summary>
        /// The callback of Instantiated Event
        /// </summary>
        protected virtual void OnInstantiated()
        {
        }

        /// <summary>
        /// The callback of Realized Event
        /// </summary>
        protected virtual void OnRealized()
        {
        }

        /// <summary>
        /// The callback of Unrealize Event
        /// </summary>
        protected virtual void OnUnrealize()
        {
        }

        /// <summary>
        /// Creates a widget handle.
        /// </summary>
        /// <param name="parent">Parent EvasObject</param>
        /// <returns>Handle IntPtr</returns>
        protected abstract IntPtr CreateHandle(EvasObject parent);

        /// <summary>
        /// For this object bind Parent object.Init handle and all kinds of EvasObjectEvent.
        /// </summary>
        /// <param name="parent">Parent object</param>
        public void Realize(EvasObject parent)
        {
            if (!IsRealized)
            {
                Parent = parent;
                Handle = CreateHandle(parent);
                Debug.Assert(Handle != IntPtr.Zero);

                (parent as Window)?.AddChild(this);

                OnRealized();
                _deleted = new EvasObjectEvent(this, EvasObjectCallbackType.Del);
                _keydown = new EvasObjectEvent<EvasKeyEventArgs>(this, RealHandle, EvasObjectCallbackType.KeyDown, EvasKeyEventArgs.Create);
                _keyup = new EvasObjectEvent<EvasKeyEventArgs>(this, RealHandle, EvasObjectCallbackType.KeyUp, EvasKeyEventArgs.Create);
                _moved = new EvasObjectEvent(this, EvasObjectCallbackType.Move);
                _resized = new EvasObjectEvent(this, EvasObjectCallbackType.Resize);

                _deleted.On += (s, e) => MakeInvalidate();
                _keydown.On += (s, e) => KeyDown?.Invoke(this, e);
                _keyup.On += (s, e) => KeyUp?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Removes current object relationship with others.
        /// </summary>
        public void Unrealize()
        {
            if (IsRealized)
            {
                if (_renderPostCallback != null)
                {
                    Interop.Evas.evas_event_callback_del(Interop.Evas.evas_object_evas_get(Handle), Interop.Evas.ObjectCallbackType.RenderPost, _renderPostCallback);
                    _renderPostCallback = null;
                }

                OnUnrealize();
                IntPtr toBeDeleted = Handle;
                Handle = IntPtr.Zero;

                DisposeEvent();

                (Parent as Window)?.RemoveChild(this);

                Interop.Evas.evas_object_del(toBeDeleted);
                Parent = null;
            }
        }

        private void MakeInvalidate()
        {
            Deleted?.Invoke(this, EventArgs.Empty);
            OnInvalidate();
            Handle = IntPtr.Zero;

            MakeInvalidateEvent();

            (Parent as Window)?.RemoveChild(this);
            Parent = null;
            _deleted = null;
        }

        private void DisposeEvent()
        {
            foreach (var evt in _eventStore)
            {
                evt.Dispose();
            }
            _eventStore.Clear();
        }

        private void MakeInvalidateEvent()
        {
            foreach (var evt in _eventStore)
            {
                evt.MakeInvalidate();
            }
            _eventStore.Clear();
        }

        internal void AddToEventLifeTracker(IInvalidatable item)
        {
            _eventStore.Add(item);
        }
    }
}