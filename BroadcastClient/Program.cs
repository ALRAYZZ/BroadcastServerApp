using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BroadcastClient;

class Program
{
	private const string serverIp = "127.0.0.1";
	private const int port = 5000;

	static async Task Main(string[] args)
	{
		using TcpClient client = new TcpClient();
		await client.ConnectAsync(serverIp, port);
		Console.WriteLine("Connected to server");

		NetworkStream stream = client.GetStream();

		_ = Task.Run(() => ReceiveMessagesAsync(stream));



		while (true)
		{
			string message = Console.ReadLine();
			//This if statement is used to check if the message is empty, if it is empty it will continue to the next iteration avoiding sending an empty message
			if (string.IsNullOrEmpty(message)) continue;

			byte[] buffer = Encoding.UTF8.GetBytes(message);
			await stream.WriteAsync(buffer, 0, buffer.Length);
		}
	}

	private static async void ReceiveMessagesAsync(NetworkStream stream)
	{
		byte[] buffer = new byte[1024];

		while (true)
		{
			int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			if (bytesRead == 0)
			{
				Console.WriteLine("Server disconnected");
				break;
			}

			string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			Console.WriteLine($"Received: {message}");
		}
	}
}
