using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Blazorify.Sass.Tests {
	internal sealed class ListLogger<T> : ILogger<T> {
		public ConcurrentQueue<(LogLevel Level, Exception? Exception)> Entries { get; } = new();

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
			return null;
		}

		public Boolean IsEnabled(LogLevel logLevel) {
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter) {
			this.Entries.Enqueue((logLevel, exception));
		}
	}
}
