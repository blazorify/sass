using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Blazorify.Sass.Tests {
	public class SassWatcherTests {
		[Fact]
		public void Watch_RecompilesWhenPartialChanges() {
			using var temp = new TempDirectory();
			temp.Write("_colors.scss", "$accent: red;");
			var input = temp.Write("site.scss", "@use \"colors\"; a { color: colors.$accent; }");
			var output = temp.Combine("site.css");
			var compiler = new SassCompiler(NullLogger<SassCompiler>.Instance);
			using var watcher = new SassWatcher(compiler, NullLogger<SassWatcher>.Instance);

			watcher.Watch(input, output, new SassCompilerOptions());
			temp.Write("_colors.scss", "$accent: green;");

			Assert.Contains("color: green;", Eventually.ReadWhen(output, css => css.Contains("green")));
		}

		[Fact]
		public void Dispose_StopsRecompiling() {
			using var temp = new TempDirectory();
			var input = temp.Write("site.scss", "a { color: red; }");
			var output = temp.Combine("site.css");
			var compiler = new SassCompiler(NullLogger<SassCompiler>.Instance);
			var watcher = new SassWatcher(compiler, NullLogger<SassWatcher>.Instance);

			watcher.Watch(input, output, new SassCompilerOptions());
			watcher.Dispose();
			temp.Write("site.scss", "a { color: green; }");
			Thread.Sleep(2000);

			Assert.False(File.Exists(output));
		}
	}
}
