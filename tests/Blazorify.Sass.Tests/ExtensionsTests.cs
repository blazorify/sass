using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blazorify.Sass.Tests {
	public class ExtensionsTests {
		private static IApplicationBuilder CreateApp(Action<SassBuildManager> configure) {
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddBlazorifySass(configure);

			return new ApplicationBuilder(services.BuildServiceProvider());
		}

		[Fact]
		public void UseBlazorifySass_CompilesRegisteredFiles() {
			using var temp = new TempDirectory();
			var input = temp.Write("site.scss", "a { color: red; }");
			var output = temp.Combine("wwwroot/site.css");
			Directory.CreateDirectory(temp.Combine("wwwroot"));

			CreateApp(manager => manager.Register(new SassFileRegistration { InputPath = input, OutputPath = output })).UseBlazorifySass();

			Assert.Contains("color: red;", File.ReadAllText(output));
		}

		[Fact]
		public void UseBlazorifySass_WatchesFilesRegisteredWithWatch() {
			using var temp = new TempDirectory();
			var input = temp.Write("site.scss", "a { color: red; }");
			var output = temp.Combine("site.css");

			CreateApp(manager => manager.Register(new SassFileRegistration { InputPath = input, OutputPath = output, Watch = true })).UseBlazorifySass();
			temp.Write("site.scss", "a { color: green; }");

			Assert.Contains("color: green;", Eventually.ReadWhen(output, css => css.Contains("green")));
		}

		[Fact]
		public void AddBlazorifySass_WithoutConfiguration_RegistersNothingToCompile() {
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddBlazorifySass();

			var manager = services.BuildServiceProvider().GetRequiredService<SassBuildManager>();

			Assert.Empty(manager.RegisteredFiles);
		}
	}
}
