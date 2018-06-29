using NUnit.Framework;
using System;
using System.CodeDom.Compiler;
using Xamarin.Android.Tools;
using System.IO;
using System.Linq;
using Microsoft.CSharp;

namespace CscDimCodeProvider.Tests
{
	[TestFixture ()]
	public class Test
	{
		[Test ()]
		public void SimpleCompile ()
		{
			var outcome = Path.GetFullPath ("c.dll");
			if (File.Exists (outcome))
				File.Delete (outcome);
			var solution = Path.Combine ("..", "..", "..", "packages");
			var csc = Path.Combine (Directory.GetDirectories (solution, "xamarin.android.csc.dim.*").Last (), "tools", "csc.exe");
			var provider = new CscDimCodeDomProvider (csc);
                var compiler = provider.CreateCompiler ();
			var p = new CompilerParameters () {
                        OutputAssembly = outcome
                };
                compiler.CompileAssemblyFromSource (p, "public class C {}");
 		}
	}
}
