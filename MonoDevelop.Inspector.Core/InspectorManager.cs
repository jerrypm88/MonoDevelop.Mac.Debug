﻿// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using MonoDevelop.Inspector.Services;

namespace MonoDevelop.Inspector
{
    internal class InspectorManager : IDisposable
	{
		const string Name = "Accessibility .NET Inspector";
		const int ToolbarWindowWidth = 500;
		const int ToolbarWindowHeight = 30;
		const int WindowMargin = 10;

		IViewWrapper view, nextKeyView, previousKeyView;
		IMainWindowWrapper selectedWindow;
		InspectorViewMode ViewMode
        {
			get => selectedWindow.ViewMode;
			set => selectedWindow.ViewMode = value;
		}

		readonly IBorderedWindow debugOverlayWindow;
		readonly IBorderedWindow debugNextOverlayWindow;
		readonly IBorderedWindow debugPreviousOverlayWindow;
		readonly IInspectorWindow inspectorWindow;
		readonly IAccessibilityWindow accessibilityWindow;

	 	readonly List<IMenuItemWrapper> menuItems = new List<IMenuItemWrapper>();

		IToolbarWindow toolbarWindow;

		IMenuItemWrapper inspectorMenuItem, firstOverlayMenuItem, nextOverlayMenuItem, previousOverlayMenuItem;

		readonly AccessibilityService accessibilityService;
		List<IBorderedWindow> detectedErrors = new List<IBorderedWindow> ();

		#region Properties

		bool isNextResponderOverlayVisible;
		bool IsNextResponderOverlayVisible {
			get => isNextResponderOverlayVisible;
			set {
				isNextResponderOverlayVisible = value;

				if (debugNextOverlayWindow != null) {
					debugNextOverlayWindow.SetParentWindow (selectedWindow);
					debugNextOverlayWindow.Visible = value;

					if (nextKeyView != null) {
						debugNextOverlayWindow.AlignWith (nextKeyView);
					}
					debugNextOverlayWindow.OrderFront ();
				}
			}
		}

		bool isPreviousResponderOverlayVisible;
		bool IsPreviousResponderOverlayVisible {
			get => isPreviousResponderOverlayVisible;
			set {
				isPreviousResponderOverlayVisible = value;
				if (debugPreviousOverlayWindow != null) {
					debugPreviousOverlayWindow.SetParentWindow (selectedWindow);
					debugPreviousOverlayWindow.Visible = value;

					if (previousKeyView != null) {
						debugPreviousOverlayWindow.AlignWith (previousKeyView);
					}
					debugNextOverlayWindow.OrderFront ();
				}
			}
		}

		bool isFirstResponderOverlayVisible;
		bool IsFirstResponderOverlayVisible {
			get => isFirstResponderOverlayVisible;
			set {
				isFirstResponderOverlayVisible = value;

				if (debugOverlayWindow != null) {
					debugOverlayWindow.SetParentWindow (selectedWindow);
					debugOverlayWindow.Visible = value;
					if (view != null) {
						debugOverlayWindow.AlignWith (view);
					}
					debugOverlayWindow.OrderFront ();
				}
			}
		}

		bool IsStatusWindowVisible {
			get => inspectorWindow.HasParentWindow;
			set => ShowStatusWindow (value);
		}

        IMenuWrapper Submenu {
			get {
                return Delegate.GetSubMenu(); ;
			}
		}

		#endregion

		#region Error Detector

		bool showDetectedErrors;
		internal bool ShowDetectedErrors 
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

		public void SetWindow (IMainWindowWrapper selectedWindow)
		{
			var needsReattach = selectedWindow != this.selectedWindow;

			if (this.selectedWindow != null) {
				Delegate.RemoveAllErrorWindows (this.selectedWindow);
			}

			if (this.selectedWindow != null)
			{
				this.selectedWindow.ResizeRequested -= OnRespositionViews;
				this.selectedWindow.MovedRequested -= OnRespositionViews;
			}

			PopulateSubmenu();

			this.selectedWindow = selectedWindow;
			if (this.selectedWindow == null) {
				return;
			}

			RefreshOverlaysVisibility ();

			AccessibilityService.Current.ScanErrors (Delegate, selectedWindow, ViewMode);

			this.selectedWindow.ResizeRequested += OnRespositionViews;
			this.selectedWindow.MovedRequested += OnRespositionViews;
			this.selectedWindow.LostFocus += OnRespositionViews;
        }

        void RefreshOverlaysVisibility ()
		{
			IsPreviousResponderOverlayVisible = IsPreviousResponderOverlayVisible;
			IsNextResponderOverlayVisible = IsNextResponderOverlayVisible;
			IsFirstResponderOverlayVisible = IsFirstResponderOverlayVisible;
		}

		internal IInspectDelegate Delegate;

		public InspectorManager (IInspectDelegate inspectorDelegate, 
        IBorderedWindow overlay, 
        IBorderedWindow next, 
        IBorderedWindow previous, IAccessibilityWindow accWindow, IInspectorWindow inWindow, IToolbarWindow toolWindow)
		{
			Delegate = inspectorDelegate;
			accessibilityService = AccessibilityService.Current;
			accessibilityService.ScanFinished += (s, e) => {

				Delegate.RemoveAllErrorWindows (selectedWindow);
				detectedErrors.Clear();

				foreach (var error in accessibilityService.DetectedErrors) {
					var borderer = Delegate.CreateErrorWindow(error.View);
					detectedErrors.Add(borderer);
					selectedWindow.AddChildWindow (borderer);
				}

				if (showDetectedErrors)
					ShowErrors(true);

				inspectorWindow.GenerateTree(selectedWindow, ViewMode);
				selectedWindow.RecalculateKeyViewLoop();
			};

            debugOverlayWindow = overlay; //new MacBorderedWindow (CGRect.Empty, NSColor.Green);
            debugNextOverlayWindow = next; // new MacBorderedWindow (CGRect.Empty, NSColor.Red);
            debugPreviousOverlayWindow = previous; // new MacBorderedWindow (CGRect.Empty, NSColor.Blue);

            accessibilityWindow = accWindow; // new MacAccessibilityWindow(new CGRect(10, 10, 600, 700));
			accessibilityWindow.SetTitle ("Accessibility Panel");
			accessibilityWindow.ShowErrorsRequested += (sender, e) => {
				ShowDetectedErrors = !ShowDetectedErrors;
			};

			accessibilityWindow.AuditRequested += (sender, e) => accessibilityService.ScanErrors(inspectorDelegate, selectedWindow, ViewMode);

            inspectorWindow = inWindow; //new InspectorWindow (inspectorDelegate, new CGRect(10, 10, 600, 700));
			inspectorWindow.SetTitle ("Inspector Panel");
			inspectorWindow.RaiseFirstResponder += (s, e) => {
				if (selectedWindow.ContainsChildWindow (debugOverlayWindow))
					debugOverlayWindow.Close ();
				selectedWindow.AddChildWindow(debugOverlayWindow);

				//IsFirstResponderOverlayVisible = true;
				ChangeFocusedView(e);
			};
			inspectorWindow.RaiseDeleteItem += (s, e) =>
			{
				  RemoveView(e);
			};

            toolbarWindow = toolWindow; //new MacToolbarWindow (this);

			toolbarWindow.SetContentSize(ToolbarWindowWidth, ToolbarWindowHeight);
		
			toolbarWindow.ThemeChanged += (sender, isDark) => {
                inspectorWindow.SetAppareance(isDark);
                Delegate.SetAppearance(isDark, 
                inspectorWindow, 
                accessibilityWindow,
                toolbarWindow, 
                selectedWindow
                );
            };

			toolbarWindow.InspectorViewModeChanged += (object sender, InspectorViewMode e) => {
				ViewMode = e;
				AccessibilityService.Current.ScanErrors (Delegate, selectedWindow, e);
			};

			toolbarWindow.ItemDeleted += (sender, e) =>
			{
				RemoveView(view);
			};

			toolbarWindow.FontChanged += (sender, e) =>
			{
				Delegate.SetFont(view, e.Font);
				//NativeViewHelper.SetFont(view, e.Font);
			};

			toolbarWindow.ItemImageChanged += async (sender, e) =>
			{
				await Delegate.InvokeImageChanged(view, selectedWindow);
			};

			toolbarWindow.KeyViewLoop += (sender, e) => {
				IsFirstResponderOverlayVisible = e;
				ChangeFocusedView (selectedWindow.FirstResponder);
			};

			toolbarWindow.NextKeyViewLoop += (sender, e) => {
				IsNextResponderOverlayVisible = e;
				ChangeFocusedView (selectedWindow.FirstResponder);
			};

			toolbarWindow.PreviousKeyViewLoop += (sender, e) => {
				IsPreviousResponderOverlayVisible = e;
				ChangeFocusedView (selectedWindow.FirstResponder);
			};

			accessibilityWindow.RaiseAccessibilityIssueSelected += (s, e) =>
			{
				if (e == null)
				{
					return;
				}
				IsFirstResponderOverlayVisible = true;
				ChangeFocusedView(e);
			};
		}

		void RemoveView (IViewWrapper toRemove)
		{
			var parent = toRemove?.PreviousValidKeyView;
			toRemove.RemoveFromSuperview();
			ChangeFocusedView(parent);
		}

		void OnRespositionViews (object sender, EventArgs e)
		{
			inspectorWindow.AlignRight (selectedWindow, WindowMargin);
			accessibilityWindow.AlignLeft(selectedWindow, WindowMargin);
			toolbarWindow.AlignTop (selectedWindow, WindowMargin);
			RefreshOverlaysVisibility ();
		}

		void ShowStatusWindow (bool value)
		{
			if (value) {
				if (!IsStatusWindowVisible) {
					selectedWindow.AddChildWindow(accessibilityWindow);
					selectedWindow.AddChildWindow (inspectorWindow);
					selectedWindow.AddChildWindow(toolbarWindow);
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
			toolbarWindow.AlignTop(selectedWindow, WindowMargin);
			inspectorWindow.AlignRight(selectedWindow, WindowMargin);
			accessibilityWindow.AlignLeft (selectedWindow, WindowMargin);
			var anyFocusedView = view != null;
			if (!anyFocusedView)
				return;

			inspectorWindow.GenerateStatusView (view, Delegate, ViewMode);
		}

		void PopulateSubmenu ()
		{
			var submenu = Submenu;
			if (submenu == null) {
				using (EventLog eventLog = new EventLog("Application"))
				{
					eventLog.Source = "Application";
					eventLog.WriteEntry("Submenu is null in Accessibility Inspector", EventLogEntryType.Error, 101, 1);
				}
				return;
			}

			Delegate.ClearSubmenuItems(menuItems, submenu);
			menuItems.Clear();

            int menuCount = 0;

            var menuItem = Delegate.CreateMenuItem(string.Format("{0} v{1}", Name, GetAssemblyVersion()), ShowHideDetailDebuggerWindow);
            menuItems.Add(menuItem);
            inspectorMenuItem = Delegate.GetShowWindowMenuItem (ShowHideDetailDebuggerWindow);
            menuItems.Add (inspectorMenuItem);
            menuItems.Add(Delegate.GetSeparatorMenuItem());

			foreach (var item in menuItems) {
				submenu.InsertItem (item, menuCount++);
			}
		}


		void ShowHideDetailDebuggerWindow (object sender, EventArgs e)
		{
			IsStatusWindowVisible = !IsStatusWindowVisible;
			inspectorMenuItem.SetTitle (string.Format ("{0} Window", ToMenuAction (!IsStatusWindowVisible)));
		}

		string ToMenuAction (bool value) => value ? "Show" : "Hide";

		public event EventHandler<IViewWrapper> FocusedViewChanged;

		internal void ChangeFocusedView (IViewWrapper nextView)
		{
			if (selectedWindow == null || nextView == null || view == nextView) {
				//FocusedViewChanged?.Invoke(this, nextView);
				return;
			}

			view = nextView;
			nextKeyView = view?.NextValidKeyView;
			previousKeyView = view?.PreviousValidKeyView;

			RefreshStatusWindow ();

            RefreshOverlaysVisibility();

            toolbarWindow.ChangeView(this, nextView);

            FocusedViewChanged?.Invoke(this, nextView);
		}

		string GetAssemblyVersion ()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly ();
			var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo (assembly.Location);
			return fileVersionInfo.ProductVersion;
		}

		public void Dispose ()
		{
			Delegate.ClearSubmenuItems (menuItems, Submenu);
			debugOverlayWindow?.Close ();
			debugNextOverlayWindow?.Close ();
			debugPreviousOverlayWindow?.Close ();
			inspectorWindow?.Close ();
			accessibilityWindow?.Close();
		}
	}
}
