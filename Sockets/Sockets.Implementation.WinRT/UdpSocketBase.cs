using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Sockets.Plugin.Abstractions;

// ReSharper disable once CheckNamespace

namespace Sockets.Plugin
{
    /// <summary>
    ///     Base class for WinRT udp socket wrapper.
    /// </summary>
    public abstract class UdpSocketBase
    {
        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Native socket field around which UdpSocketBase wraps.
        /// </summary>
        protected DatagramSocket _backingDatagramSocket;

        /// <summary>
        ///     Fired when a udp datagram has been received.
        /// </summary>
        public EventHandler<UdpSocketMessageReceivedEventArgs> MessageReceived { get; set; }

        /// <summary>
        ///     Default constructor for <code>UdpSocketBase</code>
        /// </summary>
        protected UdpSocketBase()
        {
            SetBackingSocket();
        }

        private void SetBackingSocket()
        {
            var socket = new DatagramSocket();
            socket.MessageReceived += DatagramMessageReceived;

            _backingDatagramSocket = socket;
            ;
        }

        /// <summary>
        ///     Sends the specified data to the 'default' target of the underlying DatagramSocket.
        ///     There may be no 'default' target. depending on the state of the object.
        /// </summary>
        /// <param name="data">A byte array of data to be sent.</param>
        protected async Task SendAsync(byte[] data)
        {
            var stream = _backingDatagramSocket.OutputStream.AsStreamForWrite();

            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        /// <summary>
        ///     Sends the specified data to the endpoint at the specified address/port pair.
        /// </summary>
        /// <param name="data">A byte array of data to send.</param>
        /// <param name="address">The remote address to which the data should be sent.</param>
        /// <param name="port">The remote port to which the data should be sent.</param>
        protected async Task SendToAsync(byte[] data, string address, int port)
        {
            var hn = new HostName(address);
            var sn = port.ToString();

            var stream = (await _backingDatagramSocket.GetOutputStreamAsync(hn, sn)).AsStreamForWrite();

            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        internal async void DatagramMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var remoteAddress = args.RemoteAddress.CanonicalName;
            var remotePort = args.RemotePort;
            byte[] allBytes;

            var stream = args.GetDataStream().AsStreamForRead();
            using (var mem = new MemoryStream())
            {
                await stream.CopyToAsync(mem);
                allBytes = mem.ToArray();
            }

            var wrapperArgs = new UdpSocketMessageReceivedEventArgs(remoteAddress, remotePort, allBytes);

            if (MessageReceived != null)
                MessageReceived(this, wrapperArgs);
        }

        internal async Task CloseSocketAsync()
        {
            await Task.Run(() =>
            {
                _backingDatagramSocket.MessageReceived -= DatagramMessageReceived;
                _backingDatagramSocket.Dispose();
                SetBackingSocket();
            });
        }
    }
}