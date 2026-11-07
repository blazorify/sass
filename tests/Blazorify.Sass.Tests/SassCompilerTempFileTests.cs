using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Blazorify.Sass.Tests {
	[CollectionDefinition(DisableParallelization = true)]
	public class TempPathCollection {
	}

	// Points TMPDIR at an empty folder, so it must not run alongside tests that use the temp path.
	[Collection(typeof(TempPathCollection))]
	public class SassCompilerTempFileTests : IDisposable {
		private readonly TempDirectory temp = new();

		private readonly String? originalTempPath = Environment.GetEnvironmentVariable("TMPDIR");

		private readonly SassCompiler compiler = new(NullLogger<SassCompiler>.Instance);

		public SassCompilerTempFileTests() {
			Environment.SetEnvironmentVariable("TMPDIR", this.temp.Path);
		}

		public void Dispose() {
			Environment.SetEnvironmentVariable("TMPDIR", this.originalTempPath);
			this.temp.Dispose();
		}

		[Fact]
		public void Compile_LeavesNoTempFiles() {
			Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows reads TMP, not TMPDIR.");

			this.compiler.Compile("a { color: red; }");

			Assert.Empty(Directory.GetFileSystemEntries(this.temp.Path));
		}

		[Fact]
		public void CompileFile_WithVariables_LeavesNoTempFiles() {
			Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows reads TMP, not TMPDIR.");

			Environment.SetEnvironmentVariable("TMPDIR", this.originalTempPath);
			using var project = new TempDirectory();
			var input = project.Write("site.scss", "@use \"blazorify\" as v; a { color: v.$brand; }");
			Environment.SetEnvironmentVariable("TMPDIR", this.temp.Path);

			this.compiler.CompileFile(input, project.Combine("site.css"), new SassCompilerOptions { Variables = { ["brand"] = "red" } });

			Assert.Empty(Directory.GetFileSystemEntries(this.temp.Path));
		}

		[Fact]
		public void Compile_InvalidScss_LeavesNoTempFiles() {
			Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows reads TMP, not TMPDIR.");

			Assert.Throws<Exception>(() => this.compiler.Compile("a { color: ; }"));

			Assert.Empty(Directory.GetFileSystemEntries(this.temp.Path));
		}
	}
}
