﻿// 32feet.NET - Personal Area Networking for .NET
//
// InTheHand.Net.Sockets.Win32Socket (Win32)
// 
// Copyright (c) 2017-2020 In The Hand Ltd, All rights reserved.
// This source code is licensed under the MIT License

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace InTheHand.Net.Sockets
{
    internal class Win32Socket : Socket
    {
        private int _socket = 0;

        public Win32Socket() : base((AddressFamily)32, SocketType.Stream, (ProtocolType)3)
        {
            // AF_BT, Type_Stream, Protocol_Rfcomm
            _socket = NativeMethods.socket(32, 1, 3);
        }
        
        internal Win32Socket(int socket) : base((AddressFamily)32, SocketType.Stream, (ProtocolType)3)
        {
            _socket = socket;
        }

        private void ThrowIfSocketClosed()
        {
            if (_socket == 0)
                throw new ObjectDisposedException("Win32Socket");
        }

        private static void ThrowOnSocketError(int result, bool throwOnDisconnected)
        {
            if (result == -1)
            {
                int socketError = NativeMethods.WSAGetLastError();

                if (socketError == 10057 && !throwOnDisconnected)
                    return;

                if(socketError != 0)
                    throw new SocketException(socketError);
            }
        }

        public new Socket Accept()
        {
            ThrowIfSocketClosed();

            int newSocket = NativeMethods.accept(_socket, null, 0);

            ThrowOnSocketError(newSocket, true);

            return new Win32Socket(newSocket);
        }

        public new void Bind(EndPoint localEP)
        {
            ThrowIfSocketClosed();

            if (localEP == null)
                throw new ArgumentNullException(nameof(localEP));

            var sockAddr = localEP.Serialize();

            int result = NativeMethods.bind(_socket, SocketAddressToArray(sockAddr), sockAddr.Size);

            ThrowOnSocketError(result, true);
        }

        public new bool Connected
        {
            get
            {
                if (_socket == 0)
                    return false;

                return RemoteEndPoint != null;
            }
        }

        private static byte[] SocketAddressToArray(SocketAddress socketAddress)
        {
            byte[] buffer = new byte[socketAddress.Size];

            for (int i = 0; i < socketAddress.Size; i++)
            {
                buffer[i] = socketAddress[i];
            }

            return buffer;
        }

        public new void Close()
        {
            if (_socket != 0)
            {
                int result = NativeMethods.closesocket(_socket);
                _socket = 0;
                ThrowOnSocketError(result, true);
            }
        }

        public new void Connect(EndPoint remoteEP)
        {
            ThrowIfSocketClosed();

            if (remoteEP == null)
                throw new ArgumentNullException(nameof(remoteEP));

            var sockAddr = remoteEP.Serialize();

            int result = NativeMethods.connect(_socket, SocketAddressToArray(sockAddr), 30);

            ThrowOnSocketError(result, true);
        }

        public new int Receive(byte[] buffer)
        {
            return Receive(buffer, buffer.Length, 0);
        }

        public new int Receive(byte[] buffer, SocketFlags socketFlags)
        {
            return Receive(buffer, buffer.Length, socketFlags);
        }

        public new int Receive(byte[] buffer, int size, SocketFlags socketFlags)
        {
            ThrowIfSocketClosed();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (size > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(size));

            int result = NativeMethods.recv(_socket, buffer, size, (int)socketFlags);

            ThrowOnSocketError(result, true);

            return result;
        }

        public new EndPoint LocalEndPoint
        {
            get
            {
                byte[] addr = new byte[30];
                int len = addr.Length;
                int result = NativeMethods.getsockname(_socket, addr, ref len);
                ThrowOnSocketError(result, false);

                return new BluetoothEndPoint(addr);
            }
        }

        public new EndPoint RemoteEndPoint
        {
            get
            {
                byte[] addr = new byte[30];
                int len = addr.Length;
                int result = NativeMethods.getpeername(_socket, addr, ref len);
                if (result == 0)
                    return new BluetoothEndPoint(addr);

                ThrowOnSocketError(result, false);

                return null;
            }
        }

        public new int Send(byte[] buffer)
        {
            return Send(buffer, buffer.Length, 0);
        }

        public new int Send(byte[] buffer, SocketFlags socketFlags)
        {
            return Send(buffer, buffer.Length, socketFlags);
        }

        public new int Send(byte[] buffer, int size, SocketFlags socketFlags)
        {
            return Send(buffer, 0, size, socketFlags);
        }

        public new int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            ThrowIfSocketClosed();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (size + offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(size));

            byte[] requiredBuffer = new byte[size];
            Buffer.BlockCopy(buffer, offset, requiredBuffer, 0, size);

            int result = NativeMethods.send(_socket, requiredBuffer, size, (int)socketFlags);

            ThrowOnSocketError(result, true);

            return result;
        }

        public new void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
        {
            int result = NativeMethods.setsockopt(_socket, (int)optionLevel, (int)optionName, BitConverter.GetBytes(Convert.ToInt32(optionValue)), Marshal.SizeOf(typeof(int)));
            ThrowOnSocketError(result, true);
        }

        public new void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            int result = NativeMethods.setsockopt(_socket, (int)optionLevel, (int)optionName, BitConverter.GetBytes(optionValue), Marshal.SizeOf(typeof(int)));
            ThrowOnSocketError(result, true);
        }

        protected override void Dispose(bool disposing)
        {
            Close();
        }
        
        private static class NativeMethods
        {
            private const string winsockDll = "ws2_32.dll";

            [DllImport(winsockDll)]
#pragma warning disable IDE1006 // Naming Styles - these are Win32 function names
            internal static extern int socket(int af, int type, int protocol);

            [DllImport(winsockDll)]
            internal static extern int closesocket(int s);

            [DllImport(winsockDll)]
            internal static extern int connect(int s, [MarshalAs(UnmanagedType.LPArray)] byte[] name, int namelen);

            [DllImport(winsockDll)]
            internal static extern int recv(int s, byte[] buf, int len, int flags);

            [DllImport(winsockDll)]
            internal static extern int send(int s, byte[] buf, int len, int flags);

            [DllImport(winsockDll)]
            internal static extern int bind(int s, byte[] name, int namelen);

            [DllImport(winsockDll)]
            internal static extern int accept(int s, byte[] addr, int addrlen);

            [DllImport(winsockDll)]
            internal static extern int getsockname(int s, byte[] addr, ref int addrlen);

            [DllImport(winsockDll)]
            internal static extern int getpeername(int s, byte[] addr, ref int addrlen);


            [DllImport(winsockDll)]
            internal static extern int setsockopt(int s, int level, int optname, byte[] optval, int optlen);
#pragma warning restore IDE1006 // Naming Styles

            [DllImport(winsockDll)]
            internal static extern int WSAGetLastError();
        }
    }
}