# Blazorify.Sass

Lightweight C# wrapper around the Dart Sass compiler with first-class support for ASP.NET Core/Blazor. Ship SCSS with your app, compile to CSS at startup, and optionally watch for changes during development.

- Bundles Dart Sass binaries for Windows, Linux, and macOS (x64)
- Simple DI extensions to register SCSS inputs/outputs
- File watching with debounce to recompile on change
- Direct API for one-off string or file compilation

## Install

```bash
dotnet add package Blazorify.Sass
```

## Quick start (ASP.NET Core / Blazor)

Register your SCSS files and run the compiler during app startup:

```csharp
using Blazorify.Sass;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBlazorifySass(sass =>
{
	sass.Register(new SassFileRegistration
	{
		// Source SCSS (can import/using other .scss files)
		InputPath = "Styles/app.scss",
		// Where the compiled CSS should be written
		OutputPath = "wwwroot/css/app.css",
		Watch = builder.Environment.IsDevelopment(),
		Options = new SassCompilerOptions
		{
			IncludePaths = { "Styles", "Styles/partials" },
			Style = "compressed",
			SourceMap = false,
			Quiet = true
		}
	});
});

var app = builder.Build();

app.UseBlazorifySass(); // Compiles registered files (and starts watchers when enabled)

app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
```

## Direct compilation

If you only need programmatic compilation, resolve `SassCompiler` and call it directly:

```csharp
var logger = app.Services.GetRequiredService<ILogger<SassCompiler>>();
var compiler = new SassCompiler(logger);

var css = compiler.Compile("$primary: #0ea5e9; .button { color: $primary; }",
	new SassCompilerOptions { Style = "compressed" });

Console.WriteLine(css); // .button{color:#0ea5e9}
```

## Options

`SassCompilerOptions` map closely to Dart Sass flags:

- `IncludePaths`: Additional directories to resolve `@import`/`@use`.
- `Style`: `"expanded"` (default) or `"compressed"`.
- `SourceMap`: `true` to emit source maps.
- `Quiet`: Suppress warnings.

Set options per file via `SassFileRegistration.Options` or per call when using `SassCompiler`.

## How it works

- Dart Sass binaries are packed under `runtimes/<RID>/native/dart-sass/`.
- `UseBlazorifySass` compiles each registered file and optionally starts a `FileSystemWatcher` to recompile on change (debounced at 300ms).
- Logging uses `ILogger`—enable `Debug` level to see the exact CLI calls.

## Notes

- Target framework: `net10.0`.
- Supported runtimes: win-x64, linux-x64, osx-x64 (additional RIDs would need binaries added to `runtimes/`).
- For production, keep `Watch = false` and precompile during startup or at build time.
