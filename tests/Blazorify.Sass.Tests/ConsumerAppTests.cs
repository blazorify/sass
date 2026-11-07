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
		private static readonly String[] LinuxRuntimes = ["linux-x64", "linux-arm64"];

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

		private static void RestoreWithoutExecutableBits(TempDirectory temp) {
			var version = Metadata("BlazorifySassVersion");

			temp.Write("App/App.csproj", $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<TargetFramework>net10.0</TargetFramework>
						<OutputType>Exe</OutputType>
						<RestorePackagesPath>{temp.Combine("packages")}</RestorePackagesPath>
						<RestoreAdditionalProjectSources>{Metadata("BlazorifySassPackageSource")}</RestoreAdditionalProjectSources>
					</PropertyGroup>
					<ItemGroup>
						<PackageReference Include="Blazorify.Sass" Version="{version}" />
					</ItemGroup>
				</Project>
				""");
			temp.Write("App/Program.cs", "System.Console.WriteLine();");

			var restore = Run("dotnet", "restore", temp.Combine("App"));
			Assert.True(restore.ExitCode == 0, restore.Output);

			foreach (var runtime in LinuxRuntimes) {
				foreach (var executable in Executables) {
					var path = PackageLayoutTests.DartSassFile(temp.Combine($"packages/blazorify.sass/{version}"), runtime, executable);
					File.SetUnixFileMode(path, File.GetUnixFileMode(path) & ~(UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute));
				}
			}
		}

		private static void AssertSassRuns(String appDirectory) {
			foreach (var runtime in LinuxRuntimes) {
				foreach (var executable in Executables) {
					var mode = File.GetUnixFileMode(PackageLayoutTests.DartSassFile(appDirectory, runtime, executable));
					Assert.True(mode.HasFlag(UnixFileMode.UserExecute), $"{runtime}/{executable} is {mode}");
				}
			}

			var current = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
			var sass = Run(PackageLayoutTests.DartSassFile(appDirectory, current, "sass"), "--version", appDirectory);
			Assert.True(sass.ExitCode == 0, sass.Output);
		}

		[Fact]
		public void Build_MakesSassExecutable() {
			Assert.SkipUnless(OperatingSystem.IsLinux(), "The chmod targets cover the Linux binaries only.");

			using var temp = new TempDirectory();
			RestoreWithoutExecutableBits(temp);

			var build = Run("dotnet", "build -c Release --no-restore -o \"../out\"", temp.Combine("App"));
			Assert.True(build.ExitCode == 0, build.Output);

			AssertSassRuns(temp.Combine("out"));
		}

		[Fact]
		public void Publish_MakesSassExecutable() {
			Assert.SkipUnless(OperatingSystem.IsLinux(), "The chmod targets cover the Linux binaries only.");

			using var temp = new TempDirectory();
			RestoreWithoutExecutableBits(temp);

			var publish = Run("dotnet", "publish -c Release --no-restore -o \"../out\"", temp.Combine("App"));
			Assert.True(publish.ExitCode == 0, publish.Output);

			AssertSassRuns(temp.Combine("out"));
		}
	}
}
