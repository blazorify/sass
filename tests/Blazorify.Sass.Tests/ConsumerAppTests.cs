using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Xunit;

namespace Blazorify.Sass.Tests {
	// Current NuGet marks extracted files executable, but older clients and package caches
	// copied from Windows do not. The chmod targets cover that case, so these tests strip the
	// executable bits from the restored package before building a sample app against it.
	[SupportedOSPlatform("linux")]
	public class ConsumerAppTests {
		private static readonly String[] UnixRuntimes = ["linux-x64", "linux-arm64", "osx-x64", "osx-arm64"];

		private static readonly String[] Executables = ["sass", "src/dart"];

		private static String Metadata(String key) {
			return typeof(ConsumerAppTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == key).Value!;
		}

		private static (Int32 ExitCode, String Output) Run(String fileName, String arguments, String workingDirectory) {
			using var process = Process.Start(new ProcessStartInfo {
				FileName = fileName,
				Arguments = arguments,
				WorkingDirectory = workingDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			})!;

			var stderr = process.StandardError.ReadToEndAsync();
			var stdout = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			return (process.ExitCode, stdout + stderr.Result);
		}

		private static String CurrentLinuxRuntime() {
			return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
		}

		private static void RestoreWithoutExecutableBits(TempDirectory temp, String runtimeIdentifier = "") {
			var version = Metadata("BlazorifySassVersion");

			temp.Write("App/App.csproj", $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<TargetFramework>net10.0</TargetFramework>
						<OutputType>Exe</OutputType>
						<RuntimeIdentifier>{runtimeIdentifier}</RuntimeIdentifier>
						<SelfContained>false</SelfContained>
						<RestorePackagesPath>{temp.Combine("packages")}</RestorePackagesPath>
						<RestoreAdditionalProjectSources>{Metadata("BlazorifySassPackageSource")}</RestoreAdditionalProjectSources>
					</PropertyGroup>
					<ItemGroup>
						<PackageReference Include="Blazorify.Sass" Version="{version}" />
					</ItemGroup>
				</Project>
				""");
			temp.Write("App/Program.cs", """
				var compiler = new Blazorify.Sass.SassCompiler(Microsoft.Extensions.Logging.Abstractions.NullLogger<Blazorify.Sass.SassCompiler>.Instance);
				System.Console.WriteLine(compiler.Compile(".card { .title { color: red; } }"));
				""");

			var restore = Run("dotnet", "restore", temp.Combine("App"));
			Assert.True(restore.ExitCode == 0, restore.Output);

			foreach (var runtime in UnixRuntimes) {
				foreach (var executable in Executables) {
					var path = PackageLayoutTests.DartSassFile(temp.Combine($"packages/blazorify.sass/{version}"), runtime, executable);
					File.SetUnixFileMode(path, File.GetUnixFileMode(path) & ~(UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute));
				}
			}
		}

		private static void AssertAppCompiles(String appDirectory) {
			var app = Run("dotnet", "App.dll", appDirectory);

			Assert.True(app.ExitCode == 0, app.Output);
			Assert.Contains(".card .title", app.Output);
		}

		private static void AssertSassExecutable(String appDirectory) {
			foreach (var runtime in UnixRuntimes) {
				foreach (var executable in Executables) {
					var mode = File.GetUnixFileMode(PackageLayoutTests.DartSassFile(appDirectory, runtime, executable));
					Assert.True(mode.HasFlag(UnixFileMode.UserExecute), $"{runtime}/{executable} is {mode}");
				}
			}
		}

		[Fact]
		public void Build_MakesSassExecutable() {
			Assert.SkipUnless(OperatingSystem.IsLinux(), "Runs the Linux binaries; the chmod targets do not run on Windows.");

			using var temp = new TempDirectory();
			RestoreWithoutExecutableBits(temp);

			var build = Run("dotnet", "build -c Release --no-restore -o \"../out\"", temp.Combine("App"));
			Assert.True(build.ExitCode == 0, build.Output);

			AssertSassExecutable(temp.Combine("out"));
			AssertAppCompiles(temp.Combine("out"));
		}

		[Fact]
		public void Publish_MakesSassExecutable() {
			Assert.SkipUnless(OperatingSystem.IsLinux(), "Runs the Linux binaries; the chmod targets do not run on Windows.");

			using var temp = new TempDirectory();
			RestoreWithoutExecutableBits(temp);

			var publish = Run("dotnet", "publish -c Release --no-restore -o \"../out\"", temp.Combine("App"));
			Assert.True(publish.ExitCode == 0, publish.Output);

			AssertSassExecutable(temp.Combine("out"));
			AssertAppCompiles(temp.Combine("out"));
		}

		[Fact]
		public void BuildForRuntime_CompilesScss() {
			Assert.SkipUnless(OperatingSystem.IsLinux(), "Runs the Linux binaries; the chmod targets do not run on Windows.");

			using var temp = new TempDirectory();
			RestoreWithoutExecutableBits(temp, CurrentLinuxRuntime());

			var build = Run("dotnet", "build -c Release --no-restore -o \"../out\"", temp.Combine("App"));
			Assert.True(build.ExitCode == 0, build.Output);

			AssertAppCompiles(temp.Combine("out"));
		}

		// A runtime-specific publish copies native assets flat into the output root.
		[Fact]
		public void PublishForRuntime_CompilesScss() {
			Assert.SkipUnless(OperatingSystem.IsLinux(), "Runs the Linux binaries; the chmod targets do not run on Windows.");

			using var temp = new TempDirectory();
			RestoreWithoutExecutableBits(temp, CurrentLinuxRuntime());

			var publish = Run("dotnet", "publish -c Release --no-restore -o \"../out\"", temp.Combine("App"));
			Assert.True(publish.ExitCode == 0, publish.Output);

			AssertAppCompiles(temp.Combine("out"));
		}
	}
}
