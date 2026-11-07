using System;
using System.IO;
using Xunit;

namespace Blazorify.Sass.Tests {
	public class PackageLayoutTests {
		[Theory]
		[InlineData("win-x64", "sass.bat")]
		[InlineData("linux-x64", "sass")]
		[InlineData("linux-x64", "src/dart")]
		[InlineData("linux-arm64", "sass")]
		[InlineData("linux-arm64", "src/dart")]
		[InlineData("osx-x64", "sass")]
		[InlineData("osx-x64", "src/dart")]
		public void Build_CopiesSassForEveryShippedRuntime(String runtime, String file) {
			Assert.True(File.Exists(DartSassFile(AppContext.BaseDirectory, runtime, file)));
		}

		internal static String DartSassFile(String root, String runtime, String file) {
			return Path.Combine(root, "runtimes", runtime, "native", "dart-sass", file);
		}
	}
}
