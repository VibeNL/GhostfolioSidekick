using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.BrokerAPIs.TradeRepublic
{
	/// <summary>
	/// Client for the Trade Republic WebSocket API (wss://api.traderepublic.com/).
	/// Protocol: text-based messages with numeric IDs.
	///   Connect:     connect {version} {json}
	///   Request:     {id} {json}
	///   Subscribe:   sub {id} {json}
	///   Unsubscribe: unsub {id}
	///   Response:    {id} {json}  |  {id} C  (subscription confirmed)  |  {id} E {error}
	/// </summary>
	internal sealed class TradeRepublicApiClient : IAsyncDisposable
	{
		private const string WsUrl = "wss://api.traderepublic.com/";
		private const int ProtocolVersion = 31;

		private readonly ClientWebSocket _ws = new();
		private readonly CancellationTokenSource _cts = new();
		private readonly SemaphoreSlim _sendLock = new(1, 1);
		private readonly Dictionary<int, TaskCompletionSource<JsonElement>> _pending = [];
		private readonly TaskCompletionSource _connectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _counter;
		private Task? _receiveTask;

		public async Task ConnectAsync(CancellationToken cancellationToken)
		{
			await _ws.ConnectAsync(new Uri(WsUrl), cancellationToken);
			_receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

			var connectPayload = JsonSerializer.Serialize(new
			{
				locale = "en",
				platformId = "webtrading",
				platformVersion = "web",
				clientId = "app.traderepublic.com",
				clientVersion = "1.0.0"
			});

			await SendTextAsync($"connect {ProtocolVersion} {connectPayload}", cancellationToken);
			await _connectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
		}

		/// <summary>Sends a request and waits for the matching response.</summary>
		public async Task<JsonElement> SendRequestAsync(string jsonPayload, CancellationToken cancellationToken)
		{
			var id = Interlocked.Increment(ref _counter);
			var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

			lock (_pending)
			{
				_pending[id] = tcs;
			}

			await SendTextAsync($"{id} {jsonPayload}", cancellationToken);
			return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(120), cancellationToken);
		}

		/// <summary>Subscribes, waits for the first data message, then unsubscribes.</summary>
		public async Task<JsonElement> SubscribeOnceAsync(string jsonPayload, CancellationToken cancellationToken)
		{
			var id = Interlocked.Increment(ref _counter);
			var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

			lock (_pending)
			{
				_pending[id] = tcs;
			}

			await SendTextAsync($"sub {id} {jsonPayload}", cancellationToken);
			try
			{
				return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(120), cancellationToken);
			}
			finally
			{
				try
				{
					await SendTextAsync($"unsub {id}", CancellationToken.None);
				}
				catch
				{
					// best effort
				}
			}
		}

		private async Task SendTextAsync(string message, CancellationToken cancellationToken)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			await _sendLock.WaitAsync(cancellationToken);
			try
			{
				await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
			}
			finally
			{
				_sendLock.Release();
			}
		}

		private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
		{
			var buffer = new byte[65536];
			try
			{
				while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
				{
					var sb = new StringBuilder();
					WebSocketReceiveResult result;

					do
					{
						var segment = new ArraySegment<byte>(buffer);
						result = await _ws.ReceiveAsync(segment, cancellationToken);

						if (result.MessageType == WebSocketMessageType.Close)
							return;

						sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
					}
					while (!result.EndOfMessage);

					ProcessMessage(sb.ToString());
				}
			}
			catch (OperationCanceledException)
			{
				// expected on shutdown
			}
			catch
			{
				// ignore other errors
			}
			finally
			{
				FailAllPending();
			}
		}

		private void ProcessMessage(string message)
		{
			if (message == "connected")
			{
				_connectedTcs.TrySetResult();
				return;
			}

			var firstSpace = message.IndexOf(' ');
			if (firstSpace < 0) return;

			if (!int.TryParse(message.AsSpan(0, firstSpace), out var id)) return;

			var rest = message[(firstSpace + 1)..];

			// Skip subscription confirmations
			if (rest.Length == 1 && rest[0] == 'C') return;

			// Handle errors (format: "E {...}")
			if (rest.StartsWith("E ", StringComparison.Ordinal))
			{
				var errorMsg = rest.Length > 2 ? rest[2..] : "Unknown error";
				lock (_pending)
				{
					if (_pending.Remove(id, out var errorTcs))
						errorTcs.TrySetException(new InvalidOperationException($"Trade Republic API error for message {id}: {errorMsg}"));
				}

				return;
			}

			lock (_pending)
			{
				if (_pending.Remove(id, out var tcs))
				{
					try
					{
						var element = JsonDocument.Parse(rest).RootElement.Clone();
						tcs.TrySetResult(element);
					}
					catch (Exception ex)
					{
						tcs.TrySetException(ex);
					}
				}
			}
		}

		private void FailAllPending()
		{
			lock (_pending)
			{
				foreach (var tcs in _pending.Values)
					tcs.TrySetException(new InvalidOperationException("Trade Republic WebSocket connection closed"));
				_pending.Clear();
			}

			_connectedTcs.TrySetException(new InvalidOperationException("Trade Republic WebSocket connection closed unexpectedly"));
		}

		public async ValueTask DisposeAsync()
		{
			await _cts.CancelAsync();

			if (_receiveTask != null)
			{
				try { await _receiveTask.ConfigureAwait(false); }
				catch { /* ignore */ }
			}

			if (_ws.State == WebSocketState.Open)
			{
				try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None); }
				catch { /* ignore */ }
			}

			_ws.Dispose();
			_cts.Dispose();
			_sendLock.Dispose();
		}
	}
}
