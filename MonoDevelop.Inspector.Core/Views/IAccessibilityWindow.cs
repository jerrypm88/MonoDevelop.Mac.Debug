﻿// This file has been autogenerated from a class added in the UI designer.

using System;

namespace MonoDevelop.Inspector
{
    public interface IAccessibilityWindow : IInspectorManagerWindow
	{
		event EventHandler<IViewWrapper> RaiseAccessibilityIssueSelected;
		event EventHandler AuditRequested;
		event EventHandler ShowErrorsRequested;

		string IssuesLabel { get; set; }
	}
}
