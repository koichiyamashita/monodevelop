
using System;
using System.IO;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment.Gui
{
	public partial class SourcesZipEditorWidget : Gtk.Bin
	{
		IFileFormat[] formats;
		SourcesZipPackageBuilder target;
		bool loading;
		
		public SourcesZipEditorWidget (PackageBuilder target, CombineEntry entry, IFileFormat selectedFormat)
		{
			this.Build();
			this.target = (SourcesZipPackageBuilder) target;
			loading = true;
			
			formats = Services.ProjectService.FileFormats.GetFileFormatsForObject (entry);
			foreach (IFileFormat format in formats)
				comboFormat.AppendText (format.Name);

			if (selectedFormat == null) selectedFormat = this.target.FileFormat;
			int sel = Array.IndexOf (formats, selectedFormat);
			if (sel == -1) sel = 0;
			comboFormat.Active = sel;
			this.target.FileFormat = formats [sel];
			
			string[] archiveFormats = DeployService.SupportedArchiveFormats;
			int zel = 1;
			for (int n=0; n<archiveFormats.Length; n++) {
				comboZip.AppendText (archiveFormats [n]);
				if (this.target.TargetFile.EndsWith (archiveFormats [n]))
					zel = n;
			}
			
			if (!string.IsNullOrEmpty (this.target.TargetFile)) {
				string ext = archiveFormats [zel];
				folderEntry.Path = System.IO.Path.GetDirectoryName (this.target.TargetFile);
				entryZip.Text = System.IO.Path.GetFileName (this.target.TargetFile.Substring (0, this.target.TargetFile.Length - ext.Length));
				comboZip.Active = zel;
			}
			loading = false;
		}

		protected virtual void OnFolderEntryPathChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}

		protected virtual void OnEntryZipChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}

		protected virtual void OnComboZipChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}

		protected virtual void OnComboFormatChanged(object sender, System.EventArgs e)
		{
			UpdateTarget ();
		}
		
		public IFileFormat Format {
			get { return formats [comboFormat.Active]; }
		}
		
		public string TargetFolder {
			get { return folderEntry.Path; }
		}
		
		public string TargetZipFile {
			get {
				if (TargetFolder.Length == 0 || entryZip.Text.Length == 0)
					return "";
				else
					return System.IO.Path.Combine (TargetFolder, entryZip.Text + comboZip.ActiveText);
			}
		}
		
		void UpdateTarget ()
		{
			if (loading)
				return;
			target.FileFormat = Format;
			target.TargetFile = TargetZipFile;
		}
	}
	
	class SourcesZipDeployEditor: IPackageBuilderEditor
	{
		public bool CanEdit (PackageBuilder target, CombineEntry entry)
		{
			return target is SourcesZipPackageBuilder;
		}
		
		public Gtk.Widget CreateEditor (PackageBuilder target, CombineEntry entry)
		{
			return new SourcesZipEditorWidget (target, entry, null);
		}
	}
}
