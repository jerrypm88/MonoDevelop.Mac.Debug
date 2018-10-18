﻿
using System;
using System.Collections.Generic;
using AppKit;

namespace MonoDevelop.Mac.Debug
{
	public static class InspectorContext
	{
		readonly static List<IWindowWrapper> windows = new List<IWindowWrapper>();
		static InspectorManager manager { get; set; }

		static InspectorContext ()
		{
			var macDelegate = new MacInspectorDelegate();
			manager = new InspectorManager (macDelegate);
		}

		public static void Attach (IWindowWrapper window) 
		{
			if (!windows.Contains (window)) {
				windows.Add(window);
			}
			manager.SetWindow(window);
		}

		public static void ChangeFocusedView(IViewWrapper nSView) => manager.ChangeFocusedView(nSView);

		public static void Remove (IWindowWrapper window)
		{
			windows.Remove(window);
			manager.SetWindow(null);
		}
	}
}
