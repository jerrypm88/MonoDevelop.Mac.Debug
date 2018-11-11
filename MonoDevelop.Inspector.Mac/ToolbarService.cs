﻿// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using AppKit;
using MonoDevelop.Inspector.Mac.Touchbar;

namespace MonoDevelop.Inspector.Mac
{
    class MacToolbarWrapperDelegateWrapper : IToolbarWrapperDelegateWrapper
    {
        readonly TouchBarBaseDelegate touchBarDelegate;
        public MacToolbarWrapperDelegateWrapper (TouchBarBaseDelegate touchBarDelegate)
        {
            this.touchBarDelegate = touchBarDelegate;
        }

        public object NativeObject => touchBarDelegate;
    }

    class ToolbarService
    {
        readonly Dictionary<Type, TouchBarBaseDelegate> dicctionary = new Dictionary<Type, TouchBarBaseDelegate>();

        IInspectDelegate inspectorDelegate;
        public void SetDelegate (IInspectDelegate inspectorDelegate)
        {
            this.inspectorDelegate = inspectorDelegate;


        }

        public IToolbarWrapperDelegateWrapper GetTouchBarDelegate(object element)
        {
            return new MacToolbarWrapperDelegateWrapper (dicctionary[typeof(NSView)]);
        }

        public ToolbarService()
        {
            var colorPickerDelegate = new ColorPickerDelegate();
            dicctionary.Add(typeof(NSView), colorPickerDelegate);
        }

        public static ToolbarService Current = new ToolbarService();
    }
}
