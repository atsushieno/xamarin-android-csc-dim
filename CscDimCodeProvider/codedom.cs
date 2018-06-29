using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using Microsoft.CSharp;

namespace Xamarin.Android.Tools
{
	public class CscDimCodeDomProvider : CSharpCodeProvider
	{
		public CscDimCodeDomProvider (string cscFullPath)
		{
			csc_full_path = cscFullPath;
		}

		string csc_full_path;

		public override ICodeCompiler CreateCompiler ()
		{
			return new CscDimCodeCompiler (this, csc_full_path);
		}
	}

	static class Extensions
	{
		public static void SafeDelete (this TempFileCollection t)
		{
			if (t.KeepFiles)
				return;
			foreach (string e in t)
				try {
					File.Delete (e);
				} catch { } // ignore all exceptions
		}
	}

	partial class CscDimCodeCompiler : ICodeCompiler
	{
		CodeDomProvider provider;
		string csc_full_path;

		public CscDimCodeCompiler (CodeDomProvider provider, string cscFullPath)
		{
			this.provider = provider;
			this.csc_full_path = cscFullPath;
		}

		private string FileExtension { get { return ".cs"; } }

		CompilerResults ICodeCompiler.CompileAssemblyFromDom (CompilerParameters options, CodeCompileUnit e)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}

			try {
				return FromDom (options, e);
			} finally {
				options.TempFiles.SafeDelete ();
			}
		}

		CompilerResults ICodeCompiler.CompileAssemblyFromFile (CompilerParameters options, string fileName)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}

			try {
				return FromFile (options, fileName);
			} finally {
				options.TempFiles.SafeDelete ();
			}
		}

		CompilerResults ICodeCompiler.CompileAssemblyFromSource (CompilerParameters options, string source)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}

			try {
				return FromSource (options, source);
			} finally {
				options.TempFiles.SafeDelete ();
			}
		}

		CompilerResults ICodeCompiler.CompileAssemblyFromSourceBatch (CompilerParameters options, string [] sources)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}

			try {
				return FromSourceBatch (options, sources);
			} finally {
				options.TempFiles.SafeDelete ();
			}
		}

		CompilerResults ICodeCompiler.CompileAssemblyFromFileBatch (CompilerParameters options, string [] fileNames)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}
			if (fileNames == null) {
				throw new ArgumentNullException (nameof (fileNames));
			}

			try {
				// Try opening the files to make sure they exists.  This will throw an exception
				// if it doesn't
				foreach (string fileName in fileNames) {
					File.OpenRead (fileName).Dispose ();
				}

				return FromFileBatch (options, fileNames);
			} finally {
				options.TempFiles.SafeDelete ();
			}
		}

		CompilerResults ICodeCompiler.CompileAssemblyFromDomBatch (CompilerParameters options, CodeCompileUnit [] ea)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}

			try {
				return FromDomBatch (options, ea);
			} finally {
				options.TempFiles.SafeDelete ();
			}
		}

		private CompilerResults FromDom (CompilerParameters options, CodeCompileUnit e)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}

			return FromDomBatch (options, new CodeCompileUnit [1] { e });
		}


		private CompilerResults FromFile (CompilerParameters options, string fileName)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}
			if (fileName == null) {
				throw new ArgumentNullException (nameof (fileName));
			}

			// Try opening the file to make sure it exists.  This will throw an exception
			// if it doesn't
			File.OpenRead (fileName).Dispose ();

			return FromFileBatch (options, new string [1] { fileName });
		}

		private CompilerResults FromSource (CompilerParameters options, string source)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}

			return FromSourceBatch (options, new string [1] { source });
		}

		private CompilerResults FromDomBatch (CompilerParameters options, CodeCompileUnit [] ea)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}
			if (ea == null) {
				throw new ArgumentNullException (nameof (ea));
			}

			string [] filenames = new string [ea.Length];

			for (int i = 0; i < ea.Length; i++) {
				if (ea [i] == null) {
					continue;       // the other two batch methods just work if one element is null, so we'll match that. 
				}

				ResolveReferencedAssemblies (options, ea [i]);
				filenames [i] = options.TempFiles.AddExtension (i + FileExtension);
				using (var fs = new FileStream (filenames [i], FileMode.Create, FileAccess.Write, FileShare.Read))
				using (StreamWriter sw = new StreamWriter (fs, Encoding.UTF8)) {
					((ICodeGenerator)this).GenerateCodeFromCompileUnit (ea [i], sw, null);//Options); XXXXX
					sw.Flush ();
				}
			}

			return FromFileBatch (options, filenames);
		}

		private void ResolveReferencedAssemblies (CompilerParameters options, CodeCompileUnit e)
		{
			if (e.ReferencedAssemblies.Count > 0) {
				foreach (string assemblyName in e.ReferencedAssemblies) {
					if (!options.ReferencedAssemblies.Contains (assemblyName)) {
						options.ReferencedAssemblies.Add (assemblyName);
					}
				}
			}
		}

		private CompilerResults FromSourceBatch (CompilerParameters options, string [] sources)
		{
			if (options == null) {
				throw new ArgumentNullException (nameof (options));
			}
			if (sources == null) {
				throw new ArgumentNullException (nameof (sources));
			}

			string [] filenames = new string [sources.Length];

			for (int i = 0; i < sources.Length; i++) {
				string name = options.TempFiles.AddExtension (i + FileExtension);
				using (var fs = new FileStream (name, FileMode.Create, FileAccess.Write, FileShare.Read))
				using (var sw = new StreamWriter (fs, Encoding.UTF8)) {
					sw.Write (sources [i]);
					sw.Flush ();
				}
				filenames [i] = name;
			}

			return FromFileBatch (options, filenames);
		}

	}
}

