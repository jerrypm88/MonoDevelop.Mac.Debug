﻿// This file has been autogenerated from a class added in the UI designer.

using System;
using CoreGraphics;
using AppKit;
using System.Collections.Generic;

namespace MonoDevelop.Mac.Debug
{
	public class StatusWindow : NSWindow
	{
		NSTextView textView;

		public StatusWindow (IntPtr handle) : base (handle)
		{

		}

		public StatusWindow (CGRect frame) : base (frame, NSWindowStyle.Titled | NSWindowStyle.Resizable, NSBackingStore.Buffered, false)
		{
			IsOpaque = false;
			ShowsToolbarButton = false;
			IgnoresMouseEvents = true;
			this.Title = "KeyViewLoop Debug Window";

			var containerView = new NSScrollView (this.Frame) {
				BorderType = NSBorderType.NoBorder, 
				HasVerticalScroller = true,
				HasHorizontalScroller = false
			};

			textView = new NSTextView (Frame) {
				VerticallyResizable = true, 
			};
			containerView.DocumentView = textView;
			ContentView = containerView;
		}

		public void GenerateStatusView (List<string> elements)
		{
			textView.Value = string.Join (Environment.NewLine, elements);
		}

		public void AlignWith (CGRect targetFrame)
		{
			var frame = Frame;
			frame.Location = new CGPoint (targetFrame.Right + 5, targetFrame.Bottom - frame.Height);
			SetFrame (frame, true);
		}

	}
}
