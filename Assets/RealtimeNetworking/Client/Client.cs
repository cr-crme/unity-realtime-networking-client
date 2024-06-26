namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System;
    using System.Linq;
    using DevelopersHub.RealtimeNetworking.Common;

    public class Client
    {

        private static int _dataBufferSize = 4096;
        private static int _connectTimeout = 5000;
        private int _id = 0; public int id { get { return _id; } }
        private string _sendToken = "xxxxx"; public string sendToken { get { return _sendToken; } }
        private string _receiveToken = "xxxxx"; public string receiveToken { get { return _receiveToken; } }
        public TCP tcp;
        public UDP udp;
        private bool _isConnected = false; public bool isConnected { get { return _isConnected; } }
        private delegate void PacketHandler(Packet _packet);
        private static Dictionary<int, PacketHandler> _packetHandlers;
        private bool _connecting = false;
        
        private static Client _instance = null; public static Client instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Client();
                }
                return _instance;
            }
        }


        public void ConnectToServer(string ip, int port)
        {
            if (_isConnected || _connecting)
            {
                return;
            }

            _connecting = true;

            tcp = new TCP();
            udp = new UDP(ip, port);

            _packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)Packet.ID.INITIALIZATION, Receiver.Initialization },
                { (int)Packet.ID.CUSTOM, Receiver.ReceiveCustom },
                { (int)Packet.ID.INTERNAL, Receiver.ReceiveInternal },
            };

            tcp.Connect(ip, port);
        }

        public class TCP
        {
            public TcpClient socket;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public void Connect(string ip, int port)
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = _dataBufferSize,
                    SendBufferSize = _dataBufferSize
                };
                receiveBuffer = new byte[_dataBufferSize];
                IAsyncResult result = null;
                bool waiting = false;
                try
                {
                    result = socket.BeginConnect(ip, port, ConnectCallback, socket);
                    waiting = result.AsyncWaitHandle.WaitOne(_connectTimeout, false);
                }
                catch (Exception)
                {
                    instance._connecting = false;
                    RealtimeNetworking.instance._Connection(false);
                    return;
                }
                if (!waiting || !socket.Connected)
                {
                    instance._connecting = false;
                    RealtimeNetworking.instance._Connection(false);
                    return;
                }
            }

            private void ConnectCallback(IAsyncResult result)
            {
                socket.EndConnect(result);
                if (!socket.Connected)
                {
                    return;
                }
                instance._connecting = false;
                instance._isConnected = true;
                stream = socket.GetStream();
                receivedData = new Packet();
                stream.BeginRead(receiveBuffer, 0, _dataBufferSize, ReceiveCallback, null);
            }

            public void SendData(Packet _packet)
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int length = stream.EndRead(result);
                    if (length <= 0)
                    {
                        instance.Disconnect();
                        return;
                    }
                    byte[] data = new byte[length];
                    Array.Copy(receiveBuffer, data, length);
                    receivedData.Reset(CheckData(data));
                    stream.BeginRead(receiveBuffer, 0, _dataBufferSize, ReceiveCallback, null);
                }
                catch
                {
                    Disconnect();
                }
            }

            private bool CheckData(byte[] _data)
            {
                int length = 0;
                receivedData.SetBytes(_data);
                if (receivedData.UnreadLength() >= 4)
                {
                    length = receivedData.ReadInt();
                    if (length <= 0)
                    {
                        return true;
                    }
                }
                while (length > 0 && length <= receivedData.UnreadLength())
                {
                    byte[] _packetBytes = receivedData.ReadBytes(length);
                    Threading.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int id = _packet.ReadInt();
                            _packetHandlers[id](_packet);
                        }
                    });
                    length = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        length = receivedData.ReadInt();
                        if (length <= 0)
                        {
                            return true;
                        }
                    }
                }
                if (length <= 1)
                {
                    return true;
                }
                return false;
            }

            private void Disconnect()
            {
                instance.Disconnect();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }

        }

        public class UDP
        {
            public UdpClient socket;
            public IPEndPoint endPoint;

            public UDP(string ip, int port)
            {
                endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            }

            public void Connect(int port)
            {
                socket = new UdpClient(port);
                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);
                using (Packet _packet = new Packet())
                {
                    SendData(_packet);
                }
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    _packet.InsertInt(instance._id);
                    if (socket != null)
                    {
                        socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending data to server via UDP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    byte[] data = socket.EndReceive(result, ref endPoint);
                    socket.BeginReceive(ReceiveCallback, null);
                    if (data.Length < 4)
                    {
                        instance.Disconnect();
                        return;
                    }
                    CheckData(data);
                }
                catch
                {
                    Disconnect();
                }
            }

            private void CheckData(byte[] data)
            {
                using (Packet _packet = new Packet(data))
                {
                    int length = _packet.ReadInt();
                    data = _packet.ReadBytes(length);
                }
                Threading.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(data))
                    {
                        int _packetId = _packet.ReadInt();
                        _packetHandlers[_packetId](_packet);
                    }
                });
            }

            private void Disconnect()
            {
                instance.Disconnect();
                endPoint = null;
                socket = null;
            }
        }

        public void _Disconnect()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (_isConnected)
            {
                _isConnected = false;
                if (tcp != null && tcp.socket != null)
                {
                    tcp.socket.Close();
                }
                if (udp != null && udp.socket != null)
                {
                    udp.socket.Close();
                }
            }
        }

        public void ConnectionResponse(bool result, int id, string token1, string token2)
        {
            _id = id;
            _sendToken = token1;
            _receiveToken = token2;
            RealtimeNetworking.instance._Connection(true);
        }

    }
}
