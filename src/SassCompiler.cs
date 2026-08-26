using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Blazorify.Sass {
	public class SassCompiler {
		private readonly ILogger<SassCompiler> logger;

		private readonly String sassPath;
		private readonly String? snapshotPath;

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


			var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var wrapper = Path.Combine(
				AppContext.BaseDirectory,
				"runtimes",
				GetRuntimeID(),
				"native",
				"dart-sass",
				windows ? "sass.bat" : "sass"
			);

			// A RID-specific publish flattens runtimes/{rid}/native/** into the output root, so the
			// wrapper loses the src/ folder it execs relative to itself and fails with "not found".
			// The Dart VM and the snapshot land beside it and run perfectly well, so they are driven
			// directly when the bundled layout is absent. The wrapper stays the preferred route: it is
			// what every non-published build has, and replacing it outright is what broke hot reload
			// the last time this was attempted.
			var dart = Path.Combine(AppContext.BaseDirectory, windows ? "dart.exe" : "dart");
			var snapshot = Path.Combine(AppContext.BaseDirectory, "sass.snapshot");

			if (File.Exists(wrapper)) {
				this.sassPath = wrapper;
			} else if (File.Exists(dart) && File.Exists(snapshot)) {
				this.sassPath = dart;
				this.snapshotPath = snapshot;
			} else {
				throw new InvalidOperationException(
					$"Could not find Dart Sass. Looked for the bundled wrapper at '{wrapper}' and for a "
					+ $"flattened RID-specific publish at '{dart}' with '{snapshot}'. Neither is present, so "
					+ "nothing can compile SCSS in this process."
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
			var args = new List<String>();

			if (this.snapshotPath is not null) {
				args.Add($"\"{this.snapshotPath}\"");
			}

			args.Add($"\"{inputPath}\" \"{outputPath}\"");

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

			this.logger.LogDebug("[Compile] {sassPath} {sassArgs}", this.sassPath, String.Join(" ", args));

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = this.sassPath,
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
