﻿using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using Foundation;

namespace MonoDevelop.Inspector.Mac
{
	class OutlineView : NSOutlineView
	{
		public event EventHandler<ushort> KeyPress;

		public override void KeyDown(NSEvent theEvent)
		{
			base.KeyDown(theEvent);
			KeyPress?.Invoke(this, theEvent.KeyCode);
		}

		readonly MainNode Data = new MainNode ();

		public event EventHandler SelectionNodeChanged;

		Node selectedNode;
		public Node SelectedNode {
			get => selectedNode;
		 	set {
				if (selectedNode == value) {
					return;
				}

				selectedNode = value;
				SelectionNodeChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		public OutlineView ()
		{
			TranslatesAutoresizingMaskIntoConstraints = false;
			AllowsExpansionToolTips = true;
			AllowsMultipleSelection = false;
			AutosaveTableColumns = false;
			FocusRingType = NSFocusRingType.None;
			IndentationPerLevel = 16;
			RowHeight = 17;
			NSTableColumn column = new NSTableColumn ("Values");
			column.Title = "View Outline";
			AddColumn (column);
			OutlineTableColumn = column;

			Delegate = new OutlineViewDelegate ();
			DataSource = new OutlineViewDataSource (Data);
		}

		public void SetData (Node data)
		{
			this.Data.Node = data;
			ReloadData ();
			ExpandItem (ItemAtRow (0), true);
		}

		class OutlineViewDelegate : NSOutlineViewDelegate
		{
			public OutlineViewDelegate ()
			{
			}

			const string identifer = "myCellIdentifier";
			public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
			{
				var view = (NSTextField)outlineView.MakeView (identifer, this);
				if (view == null) {
					view = NativeViewHelper.CreateLabel (((Node)item).Name);
				}
				return view;
			}

			public override bool ShouldSelectItem (NSOutlineView outlineView, NSObject item)
			{
				((OutlineView) outlineView).SelectedNode = (Node)item;
				return true;
			}
		}

		internal void FocusNode (Node node)
		{
			if (this.RowCount < 0 ) {
				return;
			}
			var index = RowForItem (node);
			if (index >= 0) {
				SelectRow (index, false);
			}
		}
	}

	class MainNode
	{
		public Node Node {
			get;
			set;
		}

		public MainNode ()
		{
			Node = new Node ("test");
		}
	}

	public class NodeView : Node
	{
		static string GetName (IViewWrapper view)
		{
			var name = string.Format("{0} ({1})", view.NodeName, view.Identifier ?? "N.I");
			if (view.Hidden) {
				name += " (hidden)";
			}
			return name;
		}

        static string GetName(IConstrainWrapper view)
        {
            var name = string.Format("{0} ({1})", view.NodeName, view.Identifier ?? " (constraint)");
            return name;
        }

        public readonly INativeObject Wrapper;

        public NodeView (IViewWrapper view) : base (GetName (view))
		{
			this.Wrapper = view;
		}

        public NodeView(IConstrainContainerWrapper text) : base(text.NodeName)
        {
            this.Wrapper = text;
        }

        public NodeView(IConstrainWrapper constrain) : base(GetName(constrain))
        {
            this.Wrapper = constrain;
        }
    }

    public class Node : NSObject
	{
		public string Name { get; private set; }
		List<Node> Children;

		public Node (string name)
		{
			Name = name;
			Children = new List<Node> ();
		}

		public Node AddChild (string name)
		{
			Node n = new Node (name);
			Children.Add (n);
			return n;
		}

		public void AddChild (Node node)
		{
			Children.Add (node);
		}

		public Node GetChild (int index)
		{
			return Children [index];
		}

		public int ChildCount { get { return Children.Count; } }
		public bool IsLeaf { get { return ChildCount == 0; } }
	}

	class OutlineViewDataSource : NSOutlineViewDataSource
	{
		MainNode mainNode;
		public OutlineViewDataSource (MainNode mainNode)
		{
			this.mainNode = mainNode; 
		}

		public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			item = item == null ? mainNode.Node : item;
			return ((Node)item).ChildCount;
		}

		public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
		{
			item = item == null ? mainNode.Node : item;
			return ((Node)item).GetChild ((int)childIndex);
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			item = item == null ? mainNode.Node : item;
			return !((Node)item).IsLeaf;
		}
	}
}
