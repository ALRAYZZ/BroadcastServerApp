using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BroadcastServer;

class Program
{
	private static List<TcpClient> clients = new List<TcpClient>();
	private const int port = 5000;
	private static TcpListener listener;
	private static bool isRunning = true;


	static async Task Main(string[] args)
	{
		//Create a listener that listens on any IP address and the port 5000
		listener = new TcpListener(IPAddress.Any, port);
		listener.Start();
		Console.WriteLine($"Server started on port {port}");

		_ = Task.Run(() => ListenForShutdown());

		while (isRunning)
		{
			try
			{
				//Accept a client that is trying to connect to the server
				TcpClient client = await listener.AcceptTcpClientAsync();
				clients.Add(client);
				Console.WriteLine("Client connected");

				//Handle the client in a separate thread
				_ = Task.Run(() => HandleClientASync(client));
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
			{
				//This exception is thrown when the listener is stopped
				break;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception: {ex.Message}");
			}
		}
		foreach (var client in clients)
		{
			client.Close();
		}
		Console.WriteLine("Server has shut down.");
	}

	private static void ListenForShutdown()
	{
		while (true)
		{
			string command = Console.ReadLine();

			if (command?.ToLower() == "exit")
			{
				isRunning = false;
				Console.WriteLine("Server shutdown initiated");
				listener.Stop();
				break;
			}
		}
	}

	private static async Task HandleClientASync(TcpClient tcpClient)
	{
		//Get the stream of the client meaning the data that is being sent to the server
		NetworkStream stream = tcpClient.GetStream();
		//Create a buffer to store the data that is being sent to the server
		byte[] buffer = new byte[1024];

		while (true)
		{
			//Read the data that is being sent to the server
			int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			//Check if the client is still connected
			if (bytesRead == 0)
			{
				//Remove the client from the list
				clients.Remove(tcpClient);
				Console.WriteLine("Client disconnected");
				break;
			}

			string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			Console.WriteLine($"Received: {message}");
			await BroadcastMessageAync(message, tcpClient);
		}
	}

	private static async Task BroadcastMessageAync(string message, TcpClient sender)
	{
		//Convert the message to a byte array
		byte[] buffer = Encoding.UTF8.GetBytes(message);

		foreach (var client in clients)
		{
			//Check if the client is the sender if it is don't send the message to the sender
			if (client == sender) continue;

			//Get the stream of the client
			NetworkStream stream = client.GetStream();
			//Send the message to the client
			await stream.WriteAsync(buffer, 0, buffer.Length);
		}
	}
}
