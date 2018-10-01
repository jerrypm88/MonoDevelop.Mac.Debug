﻿// This file has been autogenerated from a class added in the UI designer.

using System;
using CoreGraphics;
using AppKit;
using System.Collections.Generic;
using System.Linq;
using Xamarin.PropertyEditing.Mac;
using Xamarin.PropertyEditing.Themes;

namespace MonoDevelop.Mac.Debug
{
	class ViewDebugDelegate : IDisposable
	{
		const int ToolbarWindowWidth = 350;
		const int WindowMargin = 10;
		const int MaxIssues = 50;
	 	public static string Title = "Accessibility Inspector.NET";

		readonly NSWindow window;
		NSView view, nextKeyView, previousKeyView;

		readonly BorderedWindow debugOverlayWindow;
		readonly BorderedWindow debugNextOverlayWindow;
		readonly BorderedWindow debugPreviousOverlayWindow;
		readonly StatusWindow debugStatusWindow;
		readonly NSFirstResponderWatcher watcher;

		readonly List<NSMenuItem> menuItems;

		ToolbarWindow toolbarWindow;

		NSMenuItem inspectorMenuItem, firstOverlayMenuItem, nextOverlayMenuItem, previousOverlayMenuItem;

		#region Properties

		bool IsNextResponderOverlayVisible {
			get {
				return debugNextOverlayWindow.Visible;
			}
			set {
				debugNextOverlayWindow.Visible = value;
			}
		}

		bool IsPreviousResponderOverlayVisible {
			get {
				return debugPreviousOverlayWindow.Visible;
			}
			set {
				debugPreviousOverlayWindow.Visible = value;
			}
		}

		bool IsFirstResponderOverlayVisible {
			get {
				return debugOverlayWindow.Visible;
			}
			set {
				debugOverlayWindow.Visible = value;
			}
		}

		bool IsStatusWindowVisible {
			get {
				return debugStatusWindow.ParentWindow != null;
			}
			set {
				ShowStatusWindow (value);
			}
		}

		NSMenu Submenu {
			get {
				return NSApplication.SharedApplication.Menu?.ItemAt (0)?.Submenu;
			}
		}

		#endregion

		readonly List<BorderedWindow> detectedErrors = new List<BorderedWindow>();

		bool showDetectedErrors;
		public bool ShowDetectedErrors 
		{
			get => showDetectedErrors;
			set
			{
				if (showDetectedErrors == value)
				{
					return;
				}
				showDetectedErrors = value;

				foreach (var item in detectedErrors)
				{
					item.AlignWindowWithContentView();
					item.Visible = value;
				}
			}
		}

	 	static bool IsBlockedType (NSView view)
		{
			if (view is NSTableViewCell)
			{
				return true;
			}
			return false;
		}

		void Recursively (NSView customView, NodeView node)
		{
			if (string.IsNullOrEmpty (customView.AccessibilityLabel) && string.IsNullOrEmpty(customView.AccessibilityLabel) && customView.CanBecomeKeyView && !customView.Hidden) {
				if (!detectedErrors.Any (s => s.ContentViewIdentifier == customView.Identifier)) {
					var borderer = new BorderedWindow(customView, NSColor.Red);
					detectedErrors.Add(borderer);
					window.AddChildWindow(borderer, NSWindowOrderingMode.Above);
				}
			}

			if (customView.Subviews == null || IsBlockedType(customView)) {
				return;
			}

			if (detectedErrors.Count >= MaxIssues) {
				return;
			}

			foreach (var item in customView.Subviews) {
				var nodel = new NodeView (item);
				node.AddChild (nodel);
				try {
					Recursively(item, nodel);
				} catch (Exception ex) {
					Console.WriteLine(ex);
				}
			}
		}

		void ScanForViews ()
		{
			foreach (var item in detectedErrors)
			{
				item.Visible = false;
				window.RemoveChildWindow(item);
			}
			detectedErrors.Clear ();

			var nodeBase = new NodeView (window.ContentView);

			Recursively (window.ContentView, nodeBase);

			debugStatusWindow.SetOutlineData (nodeBase);

			toolbarWindow.IssuesFound = detectedErrors.Count;
		}

		public ViewDebugDelegate (NSWindow window)
		{
			this.window = window;

			if (debugOverlayWindow == null) {
				debugOverlayWindow = new BorderedWindow (CGRect.Empty, NSColor.Green);
				this.window.AddChildWindow (debugOverlayWindow, NSWindowOrderingMode.Above);
			}
			if (debugNextOverlayWindow == null) {
				debugNextOverlayWindow = new BorderedWindow (CGRect.Empty, NSColor.Red);
				this.window.AddChildWindow (debugNextOverlayWindow, NSWindowOrderingMode.Above);
			}

			if (debugPreviousOverlayWindow == null) {
				debugPreviousOverlayWindow = new BorderedWindow (CGRect.Empty, NSColor.Blue);
				this.window.AddChildWindow (debugPreviousOverlayWindow, NSWindowOrderingMode.Above);
			}

			if (debugStatusWindow == null) {
				debugStatusWindow = new StatusWindow (new CGRect(10, 10, 600, 700));
				debugStatusWindow.RaiseFirstResponder += (s, e) => {
					IsFirstResponderOverlayVisible = true;
					debugOverlayWindow.AlignWith (e);
				};
			}

			if (toolbarWindow == null)
			{
				toolbarWindow = new ToolbarWindow();
				toolbarWindow.SetContentSize(new CGSize(ToolbarWindowWidth, 30));
				toolbarWindow.ShowIssues += (sender, e) => {
					ShowDetectedErrors = !ShowDetectedErrors;
				};

				toolbarWindow.ThemeChanged += (sender, pressed) => {
					if (pressed) {
						PropertyEditorPanel.ThemeManager.Theme = PropertyEditorTheme.Dark;
						debugStatusWindow.Appearance = toolbarWindow.Appearance = window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameVibrantDark);
					} else {
						PropertyEditorPanel.ThemeManager.Theme = PropertyEditorTheme.Light;
						debugStatusWindow.Appearance = toolbarWindow.Appearance = window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameVibrantLight);
					}
				};

				toolbarWindow.ScanForIssues += (sender, e) => {
					ScanForViews();
				};

				toolbarWindow.KeyViewLoop += (sender, e) => {
					IsFirstResponderOverlayVisible = e;
					RefreshDebugData (window.FirstResponder);
				};

				toolbarWindow.NextKeyViewLoop += (sender, e) => {
					IsNextResponderOverlayVisible = e;
					RefreshDebugData (window.FirstResponder);
				};

				toolbarWindow.PreviousKeyViewLoop += (sender, e) => {
					IsPreviousResponderOverlayVisible = e;
					RefreshDebugData (window.FirstResponder);
				};
			}

			menuItems = new List<NSMenuItem> ();
			PopulateSubmenu ();

			watcher = new NSFirstResponderWatcher (window);
			watcher.Changed += (sender, e) => {
				RefreshDebugData (e);
			};

			ScanForViews();

			window.DidResize += OnRespositionViews;
			window.DidMove += OnRespositionViews;
		}

		void OnRespositionViews (object sender, EventArgs e)
		{
			debugStatusWindow.AlignRight (window, WindowMargin);
			toolbarWindow.AlignTop (window, WindowMargin);
		}

		void ShowStatusWindow (bool value)
		{
			if (value) {
				if (!IsStatusWindowVisible) {
					window.AddChildWindow (debugStatusWindow, NSWindowOrderingMode.Above);
					window.AddChildWindow(toolbarWindow, NSWindowOrderingMode.Above);
					RefreshStatusWindow ();
				}
			}
			else {
				toolbarWindow?.Close();
				debugStatusWindow?.Close ();
			}
		}

		void RefreshStatusWindow ()
		{
			toolbarWindow.AlignTop(window, WindowMargin);
			debugStatusWindow.AlignRight(window, WindowMargin);
			var anyFocusedView = view != null;
			if (!anyFocusedView)
				return;

			debugStatusWindow.GenerateStatusView (view, nextKeyView, previousKeyView);
		}

		void PopulateSubmenu ()
		{
			var submenu = Submenu;
			if (submenu == null)
				throw new NullReferenceException ("Menu cannot be null");

			int menuCount = 0;
			submenu.AutoEnablesItems = false;

			ClearSubmenuItems (submenu);

			menuItems.Clear ();

			menuItems.Add(new NSMenuItem(string.Format("{0} v{1}", Title, GetAssemblyVersion()), ShowHideDetailDebuggerWindow) { Enabled = false });
			inspectorMenuItem = new NSMenuItem ($"Show Window", ShowHideDetailDebuggerWindow);
			inspectorMenuItem.KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask | NSEventModifierMask.ShiftKeyMask;
			inspectorMenuItem.KeyEquivalent = "D";
			menuItems.Add (inspectorMenuItem);
			menuItems.Add(NSMenuItem.SeparatorItem);

			foreach (var item in menuItems) {
				submenu.InsertItem (item, menuCount++);
			}
		}

		void ClearSubmenuItems (NSMenu submenu)
		{
			foreach (var item in menuItems) {
				submenu.RemoveItem (item);
			}
		}


		void ShowHideDetailDebuggerWindow (object sender, EventArgs e)
		{
			IsStatusWindowVisible = !IsStatusWindowVisible;
			inspectorMenuItem.Title = string.Format ("{0} Window", ToMenuAction (!IsStatusWindowVisible));
		}

		string ToMenuAction (bool value)
		{
			return value ? "Show" : "Hide";
		}

		internal void RefreshDebugData (NSResponder firstResponder)
		{
			view = firstResponder as NSView;
			if (view != null) {
				debugOverlayWindow.AlignWith (view);
			}

			nextKeyView = view?.NextValidKeyView as NSView;
			if (nextKeyView != null) {
				debugNextOverlayWindow.AlignWith (nextKeyView);
			}

			previousKeyView = view?.PreviousValidKeyView as NSView;
			if (previousKeyView != null) {
				debugPreviousOverlayWindow.AlignWith (previousKeyView);
			}

			RefreshStatusWindow ();
		}

		internal void StartWatcher ()
		{
			watcher.Start ();
		}

		string GetAssemblyVersion ()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly ();
			var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo (assembly.Location);
			return fileVersionInfo.ProductVersion;
		}

		public void Dispose ()
		{
			ClearSubmenuItems (Submenu);
			debugOverlayWindow?.Close ();
			debugNextOverlayWindow?.Close ();
			debugPreviousOverlayWindow?.Close ();
			debugStatusWindow?.Close ();
			watcher.Dispose ();
		}
	}
}
