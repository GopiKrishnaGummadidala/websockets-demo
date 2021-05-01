﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocketsDemo.Web
{
    public class SquareService
    {
        private Dictionary<string, WebSocket> _users = new Dictionary<string, WebSocket>();
        private List<Square> _squares = new List<Square>(Square.GetInitialSquares());
        public async Task AddUser(WebSocket socket)
        {
            try
            {
                var name = GenerateName();
                _users.Add(name, socket);
                GiveUserTheirName(name, socket).Wait();
                AnnounceNewUser(name).Wait();
                SendSquares(socket).Wait();
                while (socket.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024 * 4];
                    WebSocketReceiveResult socketResponse;
                    var package = new List<byte>();
                    do
                    {
                        socketResponse = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        package.AddRange(new ArraySegment<byte>(buffer, 0, socketResponse.Count));
                    } while (!socketResponse.EndOfMessage);
                    var bufferAsString = System.Text.Encoding.ASCII.GetString(package.ToArray());
                    if (!string.IsNullOrEmpty(bufferAsString))
                    {
                        var changeRequest = SquareChangeRequest.FromJson(bufferAsString);
                        await HandleSquareChangeRequest(changeRequest);
                    }
                }
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch (Exception ex)
            { }
        }

        private string GenerateName()
        {
            var prefix = "WebUser";
            Random ran = new Random();
            var name = prefix + ran.Next(1, 1000);
            while (_users.ContainsKey(name))
            {
                name = prefix + ran.Next(1, 1000);
            }
            return name;
        }

        private async Task SendSquares(WebSocket socket)
        {
            var message = new SocketMessage<List<Square>>()
            {
                MessageType = "squares",
                Payload = _squares
            };

            await Send(message.ToJson(), socket);
        }

        private async Task SendAll(string message)
        {
            await Send(message, _users.Values.ToArray());
        }

        private async Task Send(string message, params WebSocket[] socketsToSendTo)
        {
            var sockets = socketsToSendTo.Where(s => s.State == WebSocketState.Open);
            foreach (var theSocket in sockets)
            {
                var stringAsBytes = System.Text.Encoding.ASCII.GetBytes(message);
                var byteArraySegment = new ArraySegment<byte>(stringAsBytes, 0, stringAsBytes.Length);
                await theSocket.SendAsync(byteArraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task GiveUserTheirName(string name, WebSocket socket)
        {
            var message = new SocketMessage<string>
            {
                MessageType = "name",
                Payload = name
            };
            await Send(message.ToJson(), socket);
        }

        private async Task AnnounceNewUser(string name)
        {
            var message = new SocketMessage<string>
            {
                MessageType = "announce",
                Payload = $"{name} has joined"
            };
            await SendAll(message.ToJson());
        }

        private async Task AnnounceSquareChange(SquareChangeRequest request)
        {
            var message = new SocketMessage<string>
            {
                MessageType = "announce",
                Payload = $"{request.Name} has changed square #{request.Id} to {request.Color}"
            };
            await SendAll(message.ToJson());
        }

        private async Task HandleSquareChangeRequest(SquareChangeRequest request)
        {
            var theSquare = _squares.First(sq => sq.Id == request.Id);
            theSquare.Color = request.Color;
            await SendSquaresToAll();
            await AnnounceSquareChange(request);
        }

        private async Task SendSquaresToAll()
        {
            var message = new SocketMessage<List<Square>>()
            {
                MessageType = "squares",
                Payload = _squares
            };

            await SendAll(message.ToJson());
        }
    }
}
