using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Blazorify.Sass {
	public class SassCompiler {
		private readonly ILogger<SassCompiler> logger;

		private readonly String dartPath;

		private readonly String snapshotPath;

		private static String GetRuntimeID() {
			var os = OperatingSystem.IsWindows() ? "win"
				: OperatingSystem.IsLinux() ? "linux"
				: OperatingSystem.IsMacOS() ? "osx"
				: "unknown";

			var arch = RuntimeInformation.ProcessArchitecture switch {
				Architecture.X64 => "x64",
				Architecture.Arm64 => "arm64",
				Architecture.Arm => "arm",
				_ => "unknown"
			};

			return os == "unknown" || arch == "unknown" ? os : $"{os}-{arch}";
		}

		private static Boolean IsMuslRuntime() {
			if (!OperatingSystem.IsLinux()) {
				return false;
			}

			if (File.Exists("/etc/alpine-release")) {
				return true;
			}

			var muslLoaderPaths = new[] {
				"/lib/ld-musl-x86_64.so.1",
				"/lib/ld-musl-aarch64.so.1",
				"/lib/ld-musl-armhf.so.1",
			};

			foreach (var muslLoaderPath in muslLoaderPaths) {
				if (File.Exists(muslLoaderPath)) {
					return true;
				}
			}

			return false;
		}

		public SassCompiler(
			ILogger<SassCompiler> logger
		) {
			this.logger = logger;

			if (IsMuslRuntime()) {
				throw new PlatformNotSupportedException(
					"Blazorify.Sass bundles glibc-based Linux Dart Sass binaries. musl-based distributions such as Alpine are not supported. Use a glibc-based .NET runtime image or precompile your CSS before startup."
				);
			}


			var dartFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dart.exe" : "dart";

			// Two layouts, because a RID-specific publish - which is what `dotnet publish --os --arch`
			// and the SDK's PublishContainer target produce - flattens native assets to the output root,
			// while a portable publish keeps them under runtimes/<rid>/native/. The bundled `sass`
			// launcher cannot bridge the two: it is a shell script that execs "$path/src/dart", so in a
			// flattened layout it looks for a directory that is not there. Running the Dart runtime
			// against the snapshot directly is what both layouts have in common.
			var portableDirectory = Path.Combine(
				AppContext.BaseDirectory,
				"runtimes",
				GetRuntimeID(),
				"native",
				"dart-sass",
				"src"
			);

			var portableDart = Path.Combine(portableDirectory, dartFileName);
			var flattenedDart = Path.Combine(AppContext.BaseDirectory, dartFileName);

			if (File.Exists(portableDart)) {
				this.dartPath = portableDart;
				this.snapshotPath = Path.Combine(portableDirectory, "sass.snapshot");
			} else {
				this.dartPath = flattenedDart;
				this.snapshotPath = Path.Combine(AppContext.BaseDirectory, "sass.snapshot");
			}

			if (!File.Exists(this.dartPath) || !File.Exists(this.snapshotPath)) {
				this.logger.LogError(
					"Could not find Dart Sass. Looked for {portableDart} and {flattenedDart}.",
					portableDart,
					flattenedDart
				);
			}
		}

		public String Compile(String scss) {
			return this.Compile(scss, new SassCompilerOptions());
		}

		public String Compile(String scss, SassCompilerOptions options) {
			var inputPath = $"{Path.GetTempFileName()}.scss";
			var outputPath = $"{Path.GetTempFileName()}.css";

			this.logger.LogDebug("[Compile] Write {size} to {tempScss}", scss.Length, inputPath);

			File.WriteAllText(inputPath, scss);

			this.CompileFile(inputPath, outputPath, options);

			var result = File.ReadAllText(outputPath);

			File.Delete(inputPath);
			File.Delete(outputPath);

			return result;
		}

		public void CompileFile(String inputPath, String outputPath, SassCompilerOptions options) {
			var args = new List<String>() {
				$"\"{this.snapshotPath}\"",
				$"\"{inputPath}\" \"{outputPath}\""
			};

			foreach (var includePath in options.IncludePaths) {
				args.Add($"--load-path=\"{includePath}\"");
			}

			if (options.Style == "compressed") {
				args.Add("--style=compressed");
			}

			if (!options.SourceMap) {
				args.Add("--no-source-map");
			}

			if (options.Quiet) {
				args.Add("--quiet");
			}

			this.logger.LogDebug("[Compile] {dartPath} {sassArgs}", this.dartPath, String.Join(" ", args));

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = this.dartPath,
					Arguments = String.Join(" ", args),
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			process.Start();
			var stderr = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode != 0) {
				throw new Exception($"SCSS compilation failed:\n{stderr}");
			}
		}
	}
}
