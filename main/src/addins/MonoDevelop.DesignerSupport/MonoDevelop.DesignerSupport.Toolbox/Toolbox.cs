/* 
 * Toolbox.cs - A toolbox widget
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Gtk;
using System.Reflection;
using System.Collections;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.ComponentModel;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class Toolbox : VBox, IPropertyPadProvider
	{
		ToolboxService toolboxService;
		
		ItemToolboxNode selectedNode;
	//	Hashtable expandedCategories = new Hashtable ();
		
		ToolboxWidget toolboxWidget;
		ScrolledWindow scrolledWindow;
		
		ToggleButton filterToggleButton;
		ToggleButton catToggleButton;
		ToggleButton compactModeToggleButton;
		Entry filterEntry;
		MonoDevelop.Ide.Gui.PadFontChanger fontChanger;
		
		public Toolbox (ToolboxService toolboxService, IPadWindow container)
		{			
			this.toolboxService = toolboxService;
			
			#region Toolbar
			DockItemToolbar toolbar = container.GetToolbar (PositionType.Top);
		
			filterToggleButton = new ToggleButton ();
			filterToggleButton.Image = new Image (Stock.Find, IconSize.Menu);
			filterToggleButton.Toggled += new EventHandler (toggleFiltering);
			filterToggleButton.TooltipText = GettextCatalog.GetString ("Show search box");
			toolbar.Add (filterToggleButton);
			
			catToggleButton = new ToggleButton ();
			catToggleButton.Image = new Image (MonoDevelop.Core.Gui.ImageService.GetPixbuf ("md-design-categorise", IconSize.Menu));
			catToggleButton.Toggled += new EventHandler (toggleCategorisation);
			catToggleButton.TooltipText = GettextCatalog.GetString ("Show categories");
			toolbar.Add (catToggleButton);
			
			compactModeToggleButton = new ToggleButton ();
			compactModeToggleButton.Image = new Image (MonoDevelop.Core.Gui.ImageService.GetPixbuf ("md-design-listboxtoggle", IconSize.Menu));
			compactModeToggleButton.Toggled += new EventHandler (ToggleCompactMode);
			compactModeToggleButton.TooltipText = GettextCatalog.GetString ("Use compact display");
			toolbar.Add (compactModeToggleButton);
	
			VSeparator sep = new VSeparator ();
			toolbar.Add (sep);
			
			Button toolboxAddButton = new Button (new Gtk.Image (Stock.Add, IconSize.Menu));
			toolbar.Add (toolboxAddButton);
			toolboxAddButton.TooltipText = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.Clicked += new EventHandler (toolboxAddButton_Clicked);
			toolbar.ShowAll ();
			
			filterEntry = new Entry();
			filterEntry.WidthRequest = 150;
			filterEntry.Changed += new EventHandler (filterTextChanged);
			
			#endregion
			
			toolboxWidget = new ToolboxWidget ();
			toolboxWidget.SelectedItemChanged += delegate {
				selectedNode = this.toolboxWidget.SelectedItem != null ? this.toolboxWidget.SelectedItem.Tag as ItemToolboxNode : null;
				toolboxService.SelectItem (selectedNode);
			};
			this.toolboxWidget.DragBegin += delegate(object sender, Gtk.DragBeginArgs e) {
				
				if (this.toolboxWidget.SelectedItem != null) {
					this.toolboxWidget.HideTooltipWindow ();
					toolboxService.DragSelectedItem (this.toolboxWidget, e.Context);
				}
			};
			this.toolboxWidget.ActivateSelectedItem += delegate {
				toolboxService.UseSelectedItem ();
			};
			
			fontChanger = new MonoDevelop.Ide.Gui.PadFontChanger (toolboxWidget, toolboxWidget.SetCustomFont, toolboxWidget.QueueResize);
			
			this.toolboxWidget.ButtonReleaseEvent += OnButtonRelease;
			
			scrolledWindow = new ScrolledWindow ();
			base.PackEnd (scrolledWindow, true, true, 0);
			base.FocusChain = new Gtk.Widget [] { scrolledWindow };
			
			//Initialise self
			scrolledWindow.ShadowType = ShadowType.None;
			scrolledWindow.VscrollbarPolicy = PolicyType.Automatic;
			scrolledWindow.HscrollbarPolicy = PolicyType.Never;
			scrolledWindow.WidthRequest = 150;
			scrolledWindow.Add (this.toolboxWidget);
			
			//update view when toolbox service updated
			toolboxService.ToolboxContentsChanged += delegate { Refresh (); };
			toolboxService.ToolboxConsumerChanged += delegate { Refresh (); };
			Refresh ();
			
			//set initial state
			filterToggleButton.Active = false;
			this.toolboxWidget.ShowCategories = catToggleButton.Active = true;
			compactModeToggleButton.Active = MonoDevelop.Core.PropertyService.Get ("ToolboxIsInCompactMode", false);
			this.toolboxWidget.IsListMode  = !compactModeToggleButton.Active;
			this.ShowAll ();
		}
		
		#region Toolbar event handlers
		
		void ToggleCompactMode (object sender, EventArgs e)
		{
			this.toolboxWidget.IsListMode = !compactModeToggleButton.Active;
			MonoDevelop.Core.PropertyService.Set ("ToolboxIsInCompactMode", compactModeToggleButton.Active);
		}
		
		void toggleFiltering (object sender, EventArgs e)
		{
			if (!filterToggleButton.Active && (base.Children.Length == 3)) {
				filterEntry.Text = "";
				base.Remove (filterEntry);
			} else if (base.Children.Length == 2) {
				base.PackStart (filterEntry, false, false, 4);
				filterEntry.Show ();
				filterEntry.GrabFocus ();
			} else throw new Exception ("Unexpected number of widgets");
		}
		
		void toggleCategorisation (object sender, EventArgs e)
		{
			this.toolboxWidget.ShowCategories = catToggleButton.Active;
		}
		
		void filterTextChanged (object sender, EventArgs e)
		{
			foreach (Category cat in toolboxWidget.Categories) {
				bool hasVisibleChild = false;
				foreach (Item child in cat.Items) {
					child.IsVisible = ((ItemToolboxNode) child.Tag).Filter (filterEntry.Text);
					hasVisibleChild |= child.IsVisible;
				}
				cat.IsVisible = hasVisibleChild;
			}
			toolboxWidget.QueueDraw ();
		}
		
		void toolboxAddButton_Clicked (object sender, EventArgs e)
		{
			toolboxService.AddUserItems ();
		}
		
		private void OnButtonRelease(object sender, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3) {
				ShowPopup ();
			}
		}
		
		void ShowPopup ()
		{
			CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/DesignerSupport/ToolboxItemContextMenu");
			IdeApp.CommandService.ShowContextMenu (eset, this);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnDeleteItem ()
		{
			if (MonoDevelop.Core.Gui.MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to remove the selected Item?"), MonoDevelop.Core.Gui.AlertButton.Delete))
				toolboxService.RemoveUserItem (selectedNode);
		}

		[CommandUpdateHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnUpdateDeleteItem (CommandInfo info)
		{
			// Hack manually filter out gtk# widgets & container since they cannot be re added
			// because they're missing the toolbox attributes.
			info.Enabled = selectedNode != null
				&& (selectedNode.ItemDomain != GtkWidgetDomain
				    || (selectedNode.Category != "Widgets" && selectedNode.Category != "Container"));
		}
		
		static readonly string GtkWidgetDomain = GettextCatalog.GetString ("GTK# Widgets");
		
		#endregion
		
		#region GUI population
		
		Dictionary<string, Category> categories = new Dictionary<string, Category> ();
		void AddItems (IEnumerable<ItemToolboxNode> nodes)
		{
			foreach (ItemToolboxNode itbn in nodes) {
				Item newItem = new Item (itbn.Icon, itbn.Name, String.IsNullOrEmpty (itbn.Description) ? itbn.Name : itbn.Description, itbn);
				if (!categories.ContainsKey (itbn.Category))
					categories[itbn.Category] = new Category (itbn.Category);
				if (newItem.Text != null)
					categories[itbn.Category].Add (newItem);
			}
		}
		
		public void Refresh ()
		{
			// GUI assert here is to catch Bug 434065 - Exception while going to the editor
			MonoDevelop.Core.Gui.DispatchService.AssertGuiThread ();
			
			if (toolboxService.Initializing) {
				toolboxWidget.CustomMessage = GettextCatalog.GetString ("Initializing...");
				return;
			}
			
			toolboxWidget.CustomMessage = null;
			
			categories.Clear ();
			AddItems (toolboxService.GetCurrentToolboxItems ());
			
			Drag.SourceUnset (toolboxWidget);
			toolboxWidget.ClearCategories ();
			foreach (Category category in categories.Values) {
				category.IsExpanded = true;
				toolboxWidget.AddCategory (category);
			}
			toolboxWidget.QueueResize ();
			Gtk.TargetEntry[] targetTable = toolboxService.GetCurrentDragTargetTable ();
			if (targetTable != null)
				Drag.SourceSet (toolboxWidget, Gdk.ModifierType.Button1Mask, targetTable, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			compactModeToggleButton.Visible = toolboxWidget.CanIconizeToolboxCategories;
		}
		
		public override void Dispose ()
		{
			if (fontChanger != null) {
				fontChanger.Dispose ();
				fontChanger = null;
			}
			base.Dispose ();
		}
		
		#endregion
		
		#region IPropertyPadProvider
		
		object IPropertyPadProvider.GetActiveComponent ()
		{
			return selectedNode;
		}

		object IPropertyPadProvider.GetProvider ()
		{
			return selectedNode;
		}

		void IPropertyPadProvider.OnEndEditing (object obj)
		{
		}

		void IPropertyPadProvider.OnChanged (object obj)
		{
		}
		
		#endregion
	}
}
