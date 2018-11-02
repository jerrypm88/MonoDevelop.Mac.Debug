﻿// This file has been autogenerated from a class added in the UI designer.

using System.Collections.Generic;

namespace MonoDevelop.Inspector
{
    public interface IRectangle : INativeObject
    {

    }

    public enum InspectorViewMode
    {
        Native,
        Xwt
    }

    public interface IViewWrapper
	{
		bool Hidden { get; }
		string Identifier { get; }
        IRectangle AccessibilityFrame { get; }
		List<IViewWrapper> Subviews { get; }
		IViewWrapper NextValidKeyView { get; }
		IViewWrapper PreviousValidKeyView { get; }
        IRectangle Frame { get; }
		IViewWrapper Superview { get; }
		string AccessibilityTitle { get; set; }
		string AccessibilityHelp { get; set; }
		object AccessibilityParent { get; set; }
		bool CanBecomeKeyView { get; }
		object NativeView { get; }
		object View { get; }
		string NodeName { get; }

		void RemoveFromSuperview ();
	}
}