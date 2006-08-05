//
// DesignerSupportService.cs: Service that provides facilities useful 
//    for visual designers.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class DesignerSupportService : AbstractService
	{
		PropertyPad propertyPad = null;
		ToolboxService toolboxService = null;
		CodeBehindService codeBehindService = new CodeBehindService ();
		
		#region PropertyPad
		
		public PropertyPad PropertyPad {
			get {
				return propertyPad;
			}
		}
		
		internal void SetPropertyPad (PropertyPad pad)
		{
			propertyPad = pad;
		}
		
		#endregion
		
		#region Toolbox
		
		public ToolboxService ToolboxService {
			get{
				//lazy load of toolbox contents
				if (toolboxService == null) {
					toolboxService = new ToolboxService ();
										
					string path = System.IO.Path.Combine (Runtime.Properties.ConfigDirectory, "Toolbox.xml");
					if (System.IO.File.Exists (path))
						toolboxService.LoadContents (path);
				}
				
				return toolboxService;
			}
		}
		
		#endregion
		
		#region CodeBehind
		
		public CodeBehindService CodeBehindService {
			get { return codeBehindService; }
		}
		
		#endregion
		
		#region IService implementations
		
		public override void InitializeService()
		{
			base.InitializeService ();
		}
		
		public override void UnloadService()
		{
			if (toolboxService != null)
				toolboxService.SaveContents (System.IO.Path.Combine (Runtime.Properties.ConfigDirectory, "Toolbox.xml"));
		}
		
		#endregion
	}
	
	public static class DesignerSupport
	{
		static DesignerSupportService designerSupportService;
		
		public static DesignerSupportService Service {
			get {
				if (designerSupportService == null)
					designerSupportService = (DesignerSupportService) ServiceManager.GetService (typeof(DesignerSupportService));
				return designerSupportService;
			}
		}
	}
}
