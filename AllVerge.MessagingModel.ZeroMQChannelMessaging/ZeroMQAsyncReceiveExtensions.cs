using NetMQ;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    static class ZeroMQAsyncReceiveExtensions
    {
		public static async Task<(RoutingKey, bool)> ReceiveRoutingKeyAsync(this NetMQSocket socket, CancellationToken cancellationToken)
		{
			if (socket == null)

				throw new ArgumentNullException(nameof(socket));

			var (bytes, more) = await socket.ReceiveFrameBytesAsync(cancellationToken);

			return (new RoutingKey(bytes), more);
		}

		/// <summary>
		/// Receives a signal from <paramref name="socket" />, asynchronously.
		/// </summary>
		/// <param name="socket">The socket to receive from.</param>
		/// <param name="cancellationToken">The token used to propagate notification that this operation should be canceled.</param>
		/// <returns>A <see cref="bool"/> valued task whose result is <c>true</c> if a valid signal was observed, otherwise <c>false</c>.</returns>
		public static Task<bool> ReceiveSignalAsync(this NetMQSocket socket, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (NetMQRuntime.Current == null)
			{
				throw new InvalidOperationException("NetMQRuntime must be created before calling async functions");
			}
			socket.AttachToRuntime();
			Msg msg = default(Msg);
			msg.InitEmpty();
			if (socket.TryReceive(ref msg, TimeSpan.Zero))
			{
				bool isMultiFrame = msg.HasMore;
				while (msg.HasMore)
				{
					socket.Receive(ref msg);
				}
				if (!isMultiFrame && msg.Size == 8)
				{
					long signalValue = NetworkOrderBitsConverter.ToInt64(msg.Data);
					if ((signalValue & 0x7FFFFFFFFFFFFF00) == 8603657889541918976L)
					{
						msg.Close();
						return Task.FromResult((signalValue & 0xFF) == 0);
					}
				}
			}

			TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();

			cancellationToken.Register(() => source.SetCanceled());

			void Listener(object sender, NetMQSocketEventArgs args)
			{
				if (socket.TryReceive(ref msg, TimeSpan.Zero))
				{
					bool isMultiFrame = msg.HasMore;
					while (msg.HasMore)
					{
						socket.Receive(ref msg);
					}
					if (!isMultiFrame && msg.Size == 8)
					{
						long signalValue = NetworkOrderBitsConverter.ToInt64(msg.Data);
						if ((signalValue & 0x7FFFFFFFFFFFFF00) == 8603657889541918976L)
						{
							msg.Close();
							socket.ReceiveReady -= Listener;
							source.SetResult((signalValue & 0xFF) == 0);
						}
					}
				}
			}

			socket.ReceiveReady += Listener;

			return source.Task;
		}

		internal static void AttachToRuntime(this NetMQSocket socket)
		{
			if (NetMQRuntime.Current != null)
			{
				NetMQRuntime.Current.Add(socket);
			}
		}
	}
}
