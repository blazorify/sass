using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Blazorify.Sass.Tests {
	public class SassCompilerTests {
		private readonly SassCompiler compiler = new(NullLogger<SassCompiler>.Instance);

		[Fact]
		public void Compile_FlattensNestedRules() {
			var css = this.compiler.Compile(".card { .title { color: red; } }");

			Assert.Contains(".card .title", css);
			Assert.Contains("color: red;", css);
		}

		[Fact]
		public void Compile_CompressedStyle_StripsWhitespace() {
			var css = this.compiler.Compile(".card { color: red; }", new SassCompilerOptions { Style = "compressed" });

			Assert.Equal(".card{color:red}", css.Trim());
		}

		[Fact]
		public void Compile_InvalidScss_ThrowsWithSassError() {
			var exception = Assert.Throws<Exception>(() => this.compiler.Compile(".card { color: ; }"));

			Assert.Contains("SCSS compilation failed", exception.Message);
			Assert.Contains("Expected expression", exception.Message);
		}

		[Fact]
		public void Compile_NotQuiet_LogsSassWarnings() {
			var logger = new ListLogger<SassCompiler>();
			var compiler = new SassCompiler(logger);

			compiler.Compile("@warn \"check the palette\"; a { color: red; }", new SassCompilerOptions { Quiet = false });

			Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("check the palette"));
		}

		[Fact]
		public void Compile_Quiet_LogsNoSassWarnings() {
			var logger = new ListLogger<SassCompiler>();
			var compiler = new SassCompiler(logger);

			compiler.Compile("@warn \"check the palette\"; a { color: red; }", new SassCompilerOptions { Quiet = true });

			Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("check the palette"));
		}

		[Fact]
		public void CompileFile_WritesOutputFile() {
			using var temp = new TempDirectory();
			var input = temp.Write("site.scss", "$brand: #336699; a { color: $brand; }");
			var output = temp.Combine("site.css");

			this.compiler.CompileFile(input, output, new SassCompilerOptions());

			Assert.Contains("color: #336699;", File.ReadAllText(output));
		}

		[Fact]
		public void CompileFile_ResolvesUseFromIncludePaths() {
			using var temp = new TempDirectory();
			temp.Write("lib/_theme.scss", "$accent: blue;");
			var input = temp.Write("app/site.scss", "@use \"theme\"; a { color: theme.$accent; }");
			var output = temp.Combine("app/site.css");

			this.compiler.CompileFile(input, output, new SassCompilerOptions { IncludePaths = [temp.Combine("lib")] });

			Assert.Contains("color: blue;", File.ReadAllText(output));
		}

		[Fact]
		public void CompileFile_SourceMapEnabled_WritesMap() {
			using var temp = new TempDirectory();
			var input = temp.Write("site.scss", "a { color: red; }");
			var output = temp.Combine("site.css");

			this.compiler.CompileFile(input, output, new SassCompilerOptions { SourceMap = true });

			Assert.True(File.Exists(output + ".map"));
		}

		[Fact]
		public void CompileFile_SourceMapDisabled_WritesNoMap() {
			using var temp = new TempDirectory();
			var input = temp.Write("site.scss", "a { color: red; }");
			var output = temp.Combine("site.css");

			this.compiler.CompileFile(input, output, new SassCompilerOptions());

			Assert.True(File.Exists(output));
			Assert.False(File.Exists(output + ".map"));
		}
	}
}
