//
// RoundedFrame.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace Hyena.Widgets
{
    public class RoundedFrame : Bin, IScrollableImplementor
    {
        private Theme theme;
        protected Theme Theme {
            get { 
                if (theme == null) {
                    InitTheme ();
                }
                return theme;
            }
        }

        private void InitTheme () {
            theme = Hyena.Gui.Theming.ThemeEngine.CreateTheme (this);
            frame_width = (int)theme.Context.Radius + 1;
        }

        private Widget child;
        private Gdk.Rectangle child_allocation;
        private bool fill_color_set;
        private Cairo.Color fill_color;
        private bool draw_border = true;
        private Pattern fill_pattern;
        private int frame_width;

        // Ugh, this is to avoid the GLib.MissingIntPtrCtorException seen by some; BGO #552169
        protected RoundedFrame (IntPtr ptr) : base (ptr)
        {
        }

        public RoundedFrame ()
        {
        }

        public void SetFillColor (Cairo.Color color)
        {
            fill_color = color;
            fill_color_set = true;
            QueueDraw ();
        }

        public void UnsetFillColor ()
        {
            fill_color_set = false;
            QueueDraw ();
        }

        public Pattern FillPattern {
            get { return fill_pattern; }
            set {
                if (fill_pattern == value) {
                    return;
                }
                if (fill_pattern != null) {
                    fill_pattern.Dispose ();
                }
                fill_pattern = value;
                QueueDraw ();
            }
        }

        public bool DrawBorder {
            get { return draw_border; }
            set { draw_border = value; QueueDraw (); }
        }

        public Gtk.ScrollablePolicy HscrollPolicy {
            get; set;
        }

        public Gtk.ScrollablePolicy VscrollPolicy {
            get; set;
        }

        public Gtk.Adjustment Vadjustment {
            get; set;
        }

        public Gtk.Adjustment Hadjustment {
            get; set;
        }

#region Gtk.Widget Overrides

        protected override void OnStyleUpdated ()
        {
            base.OnStyleUpdated ();
            InitTheme ();
        }

        protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
        {
            var requisition = SizeRequested ();
            minimum_height = natural_height = requisition.Height;
        }

        protected override void OnGetPreferredWidth (out int minimum_width, out int natural_width)
        {
            var requisition = SizeRequested ();
            minimum_width = natural_width = requisition.Width;
        }

        protected Requisition SizeRequested ()
        {
            var requisition = new Requisition ();
            if (child != null && child.Visible) {
                // Add the child's width/height
                Requisition child_requisition, nat_requisition;
                child.GetPreferredSize (out child_requisition, out nat_requisition);
                requisition.Width = Math.Max (0, child_requisition.Width);
                requisition.Height = child_requisition.Height;
            } else {
                requisition.Width = 0;
                requisition.Height = 0;
            }

            // Add the frame border
            requisition.Width += ((int)BorderWidth + frame_width) * 2;
            requisition.Height += ((int)BorderWidth + frame_width) * 2;
            return requisition;
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);

            child_allocation = new Gdk.Rectangle ();

            if (child == null || !child.Visible) {
                return;
            }

            child_allocation.X = (int)BorderWidth + frame_width;
            child_allocation.Y = (int)BorderWidth + frame_width;
            child_allocation.Width = (int)Math.Max (1, Allocation.Width - child_allocation.X * 2);
            child_allocation.Height = (int)Math.Max (1, Allocation.Height - child_allocation.Y -
                (int)BorderWidth - frame_width);

            child_allocation.X += Allocation.X;
            child_allocation.Y += Allocation.Y;

            child.SizeAllocate (child_allocation);
        }

        protected override bool OnDrawn (Cairo.Context cr)
        {
            CairoHelper.TransformToWindow (cr, this, Window);

            DrawFrame (cr);
            if (child != null) {
                PropagateDraw (child, cr);
            }
            return false;
        }

        private void DrawFrame (Cairo.Context cr)
        {
            int x = child_allocation.X - frame_width;
            int y = child_allocation.Y - frame_width;
            int width = child_allocation.Width + 2 * frame_width;
            int height = child_allocation.Height + 2 * frame_width;

            Gdk.Rectangle rect = new Gdk.Rectangle (x, y, width, height);

            Theme.Context.ShowStroke = draw_border;

            if (fill_color_set) {
                Theme.DrawFrameBackground (cr, rect, fill_color);
            } else if (fill_pattern != null) {
                Theme.DrawFrameBackground (cr, rect, fill_pattern);
            } else {
                Theme.DrawFrameBackground (cr, rect, true);
                Theme.DrawFrameBorder (cr, rect);
            }
        }

#endregion

#region Gtk.Container Overrides

        protected override void OnAdded (Widget widget)
        {
            child = widget;
            base.OnAdded (widget);
        }

        protected override void OnRemoved (Widget widget)
        {
            if (child == widget) {
                child = null;
            }

            base.OnRemoved (widget);
        }

#endregion

    }
}
