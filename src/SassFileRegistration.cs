using System;

namespace Blazorify.Sass {
	public class SassFileRegistration {
		public required String InputPath { get; set; }
		public required String OutputPath { get; set; }
		public SassCompilerOptions Options { get; set; } = new();
		public Boolean Watch { get; set; } = false;
	}
}
