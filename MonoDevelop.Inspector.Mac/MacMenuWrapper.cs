﻿// This file has been autogenerated from a class added in the UI designer.

using AppKit;

namespace MonoDevelop.Inspector.Mac
{
    class MacMenuWrapper : IMenuWrapper
    {
        private NSMenu submenu;

        public MacMenuWrapper(NSMenu submenu)
        {
            this.submenu = submenu;
        }

        public void InsertItem(IMenuItemWrapper menuItem, int index)
        {
            var item = (NSMenuItem)menuItem.NativeObject;
            submenu.InsertItem(item, index);
        }

        public object NativeObject => submenu;
    }
}
