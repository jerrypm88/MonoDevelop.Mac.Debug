﻿// This file has been autogenerated from a class added in the UI designer.

using System;
using CoreGraphics;
using AppKit;
using System.Collections.Generic;
using System.Linq;
using Xamarin.PropertyEditing.Mac;
using Xamarin.PropertyEditing.Themes;
using MonoDevelop.Mac.Debug.Services;

namespace MonoDevelop.Mac.Debug
{
	class InspectorManager : IDisposable
	{
		const string Name = "Accessibility .NET Inspector";
		const int ToolbarWindowWidth = 350;
		const int WindowMargin = 10;
	
		NSWindow window;
		NSView view, nextKeyView, previousKeyView;

		readonly BorderedWindow debugOverlayWindow;
		readonly BorderedWindow debugNextOverlayWindow;
		readonly BorderedWindow debugPreviousOverlayWindow;
		readonly InspectorWindow inspectorWindow;
		readonly AccessibilityWindow accessibilityWindow;
		readonly NSFirstResponderWatcher watcher;

		readonly List<NSMenuItem> menuItems;

		ToolbarWindow toolbarWindow;

		NSMenuItem inspectorMenuItem, firstOverlayMenuItem, nextOverlayMenuItem, previousOverlayMenuItem;

		readonly AccessibilityService accessibilityService;
		List<BorderedWindow> detectedErrors = new List<BorderedWindow> ();

		#region Properties

		bool IsNextResponderOverlayVisible {
			get => debugNextOverlayWindow.Visible;
			set => debugNextOverlayWindow.Visible = value;
		}

		bool IsPreviousResponderOverlayVisible {
			get => debugPreviousOverlayWindow.Visible;
			set => debugPreviousOverlayWindow.Visible = value;
		}

		bool IsFirstResponderOverlayVisible {
			get => debugOverlayWindow.Visible;
			set => debugOverlayWindow.Visible = value;
		}

		bool IsStatusWindowVisible {
			get => inspectorWindow.ParentWindow != null;
			set => ShowStatusWindow (value);
		}

		NSMenu Submenu {
			get => NSApplication.SharedApplication.Menu?.ItemAt (0)?.Submenu;
		}

		#endregion

		#region Error Detector

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
				ShowErrors(value);
			}
		}

		void ShowErrors (bool value)
		{
			showDetectedErrors = value;
			foreach (var item in detectedErrors)
			{
				item.AlignWindowWithContentView();
				item.Visible = value;
			}
		}

		#endregion

		public void SetWindow (NSWindow window)
		{
			if (this.window != null)
			{
				this.window.RemoveChildWindow(debugOverlayWindow);
				this.window.RemoveChildWindow(debugNextOverlayWindow);
				this.window.RemoveChildWindow(debugPreviousOverlayWindow);
				this.window.RemoveChildWindow(inspectorWindow);
				this.window.RemoveChildWindow(accessibilityWindow);
				this.window.RemoveChildWindow(toolbarWindow);

				AccessibilityService.Current.Reset ();

				this.window.DidResize -= OnRespositionViews;
				this.window.DidMove -= OnRespositionViews;
			}

			this.window = window;

			if (this.window == null)
			{
				return;
			}
		
			this.window.AddChildWindow(debugOverlayWindow, NSWindowOrderingMode.Above);
			this.window.AddChildWindow(debugNextOverlayWindow, NSWindowOrderingMode.Above);
			this.window.AddChildWindow(debugPreviousOverlayWindow, NSWindowOrderingMode.Above);

			AccessibilityService.Current.ScanErrors (this.window);

			this.window.DidResize += OnRespositionViews;
			this.window.DidMove += OnRespositionViews;
		}

		public InspectorManager ()
		{
			accessibilityService = AccessibilityService.Current;
			accessibilityService.ScanFinished += (s, e) => {

				foreach (var item in detectedErrors)
				{
					var child = window.ChildWindows.FirstOrDefault(d => d == item);
					if (child != null)
					{
						window.RemoveChildWindow(child);
					}
				}
				detectedErrors.Clear();

				foreach (var error in accessibilityService.DetectedErrors) {
					var borderer = new BorderedWindow(error.View, NSColor.Red);
					detectedErrors.Add(borderer);
					window.AddChildWindow (borderer, NSWindowOrderingMode.Above);
				}

				if (showDetectedErrors)
					ShowErrors(true);

				inspectorWindow.GenerateTree(window);
			};


			debugOverlayWindow = new BorderedWindow (CGRect.Empty, NSColor.Green);
			debugNextOverlayWindow = new BorderedWindow (CGRect.Empty, NSColor.Red);
			debugPreviousOverlayWindow = new BorderedWindow (CGRect.Empty, NSColor.Blue);

			accessibilityWindow = new AccessibilityWindow(new CGRect(10, 10, 600, 700));
			accessibilityWindow.Title = "Accessibility Panel";
			accessibilityWindow.ShowErrorsRequested += (sender, e) => {
				ShowDetectedErrors = !ShowDetectedErrors;
			};
			accessibilityWindow.AuditRequested += (sender, e) => accessibilityService.ScanErrors(window);


			inspectorWindow = new InspectorWindow (new CGRect(10, 10, 600, 700));
			inspectorWindow.Title = "Inspector Panel";
			inspectorWindow.RaiseFirstResponder += (s, e) => {
				if (window.ChildWindows.Contains (debugOverlayWindow))
					window.RemoveChildWindow(debugOverlayWindow);
				window.AddChildWindow(debugOverlayWindow, NSWindowOrderingMode.Above);

				IsFirstResponderOverlayVisible = true;
				ChangeFocusedView(e);
			};

			toolbarWindow = new ToolbarWindow();
			toolbarWindow.SetContentSize(new CGSize(ToolbarWindowWidth, 30));
		
			toolbarWindow.ThemeChanged += (sender, pressed) => {
				if (pressed) {
					PropertyEditorPanel.ThemeManager.Theme = PropertyEditorTheme.Dark;
					inspectorWindow.Appearance = inspectorWindow.Appearance = toolbarWindow.Appearance = window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameVibrantDark);
				} else {
					PropertyEditorPanel.ThemeManager.Theme = PropertyEditorTheme.Light;
					inspectorWindow.Appearance = inspectorWindow.Appearance = toolbarWindow.Appearance = window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameVibrantLight);
				}
			};

			toolbarWindow.KeyViewLoop += (sender, e) => {
				IsFirstResponderOverlayVisible = e;
				ChangeFocusedView (window.FirstResponder as NSView);
			};

			toolbarWindow.NextKeyViewLoop += (sender, e) => {
				IsNextResponderOverlayVisible = e;
				ChangeFocusedView (window.FirstResponder as NSView);
			};

			toolbarWindow.PreviousKeyViewLoop += (sender, e) => {
				IsPreviousResponderOverlayVisible = e;
				ChangeFocusedView (window.FirstResponder as NSView);
			};

			menuItems = new List<NSMenuItem> ();
			PopulateSubmenu ();

			watcher = new NSFirstResponderWatcher (window);
			watcher.Changed += (sender, e) => {
				ChangeFocusedView (e as NSView);
			};

			accessibilityWindow.RaiseAccessibilityIssueSelected += (s, e) =>
			{
				if (e == null)
				{
					return;
				}
				if (window.ChildWindows.Contains(debugOverlayWindow))
					window.RemoveChildWindow(debugOverlayWindow);
				window.AddChildWindow(debugOverlayWindow, NSWindowOrderingMode.Above);

				IsFirstResponderOverlayVisible = true;
				ChangeFocusedView(e);
			};
		}

		void OnRespositionViews (object sender, EventArgs e)
		{
			inspectorWindow.AlignRight (window, WindowMargin);
			accessibilityWindow.AlignLeft(window, WindowMargin);
			toolbarWindow.AlignTop (window, WindowMargin);
		}

		void ShowStatusWindow (bool value)
		{

			if (value) {
				if (!IsStatusWindowVisible) {
					window.AddChildWindow(accessibilityWindow, NSWindowOrderingMode.Above);
					window.AddChildWindow (inspectorWindow, NSWindowOrderingMode.Above);
					window.AddChildWindow(toolbarWindow, NSWindowOrderingMode.Above);
					RefreshStatusWindow ();
				}
			}
			else {

				accessibilityWindow?.Close();
				toolbarWindow?.Close();
				inspectorWindow?.Close ();
			}
		}

		void RefreshStatusWindow ()
		{
			toolbarWindow.AlignTop(window, WindowMargin);
			inspectorWindow.AlignRight(window, WindowMargin);
			accessibilityWindow.AlignLeft (window, WindowMargin);
			var anyFocusedView = view != null;
			if (!anyFocusedView)
				return;

			inspectorWindow.GenerateStatusView (view, nextKeyView, previousKeyView);
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

			menuItems.Add(new NSMenuItem(string.Format("{0} v{1}", Name, GetAssemblyVersion()), ShowHideDetailDebuggerWindow) { Enabled = false });
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

		string ToMenuAction (bool value) => value ? "Show" : "Hide";

		internal void ChangeFocusedView (NSView nextView)
		{
			if (nextView == null || view == nextView) {
				return;
			}

			view = nextView;
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
			inspectorWindow?.Close ();
			accessibilityWindow?.Close();
			watcher.Dispose ();
		}
	}
}
