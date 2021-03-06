﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security;

namespace LumiSoft_edited
{
    /// <summary>
    /// This class implements STUN client. Defined in RFC 3489.
    /// </summary>
    public class STUN_Client
    {

        #region method GetPublicEP

        /// <summary>
        /// Resolves socket local end point to public end point.
        /// </summary>
        /// <param name="stunServer">STUN server.</param>
        /// <param name="port">STUN server port. Default port is 3478.</param>
        /// <param name="localEndPoint">Local endpoint to check</param>
        /// <returns>Returns public IP end point.</returns>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SocketException">An error is encountered when resolving stunServer</exception>
        /// <exception cref="IOException">Is raised when no connection to STUN server.</exception>
        public static IPEndPoint GetPublicEP(string stunServer, int port, IPEndPoint localEndPoint)
        {
            if (localEndPoint == null)
                throw new ArgumentNullException(nameof(localEndPoint));
            // create socket with ability to reuse same local end point
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp) {ExclusiveAddressUse = false};
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            try
            {
                socket.Bind(localEndPoint);
            }
            catch (Exception e) when(e is SocketException || e is SecurityException)
            {
                throw new ArgumentException("Socket can not bind localEndPoint");
            }

            try
            {
                // return public end point
                return GetPublicEP(stunServer, port, socket);
            }
            finally
            {
                // anyway close socket
                socket.Close();
            }
        }

        /// <summary>
        /// Resolves socket local end point to public end point.
        /// </summary>
        /// <param name="stunServer">STUN server.</param>
        /// <param name="port">STUN server port. Default port is 3478.</param>
        /// <param name="socket">Socket to check its public end point</param>
        /// <returns>Returns public IP end point. Or null if can not determine it</returns>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SocketException">An error is encountered when resolving stunServer</exception>
        /// <exception cref="IOException">Is raised when no connection to STUN server.</exception>
        public static IPEndPoint GetPublicEP(string stunServer, int port, Socket socket)
        {
            // check parameters 
            if (string.IsNullOrWhiteSpace(stunServer))
                throw new ArgumentException(nameof(stunServer));
            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentException(nameof(port));
            }
            if (socket.ProtocolType != ProtocolType.Udp)
            {
                throw new ArgumentException("This method is designed for only UDP");
            }

            // get end point of server
            IPEndPoint remoteEndPoint;
            try
            {
                remoteEndPoint = new IPEndPoint(System.Net.Dns.GetHostAddresses(stunServer)[0], port);
            }
            catch (Exception)
            {
                return null;
            }
                

            // try get my public end point or return null
            STUN_Message test = new STUN_Message();
            STUN_Message testresponse = DoTransaction(test, socket, remoteEndPoint, 1000);

            // Junk all late responses.
            DateTime startTime = DateTime.Now;
            while (startTime.AddMilliseconds(500) > DateTime.Now)
            {
                // We got response.
                if (socket.Poll(1, SelectMode.SelectRead))
                {
                    byte[] receiveBuffer = new byte[512];
                    socket.Receive(receiveBuffer);
                }
            }
            // return mapped end point
            return testresponse?.MappedAddress;
        }

        #endregion

        #region method DoTransaction

        /// <summary>
        /// Does STUN transaction. Returns transaction response or null if transaction failed.
        /// </summary>
        /// <param name="request">STUN message.</param>
        /// <param name="socket">Socket to use for send/receive.</param>
        /// <param name="remoteEndPoint">Remote end point.</param>
        /// <param name="timeout">Timeout in milli seconds.</param>
        /// <returns>Returns transaction response or null if transaction failed.</returns>
        private static STUN_Message DoTransaction(STUN_Message request, Socket socket, 
            IPEndPoint remoteEndPoint, int timeout)
        {
            byte[] requestBytes = request.ToByteData();
            DateTime startTime = DateTime.Now;
            // Retransmit with 500 ms.
            while (startTime.AddMilliseconds(timeout) > DateTime.Now)
            {
                try
                {
                    socket.SendTo(requestBytes, remoteEndPoint);

                    // We got response.
                    if (socket.Poll(500 * 1000, SelectMode.SelectRead))
                    {
                        byte[] receiveBuffer = new byte[512];
                        socket.Receive(receiveBuffer);

                        // Parse message
                        STUN_Message response = new STUN_Message();
                        response.Parse(receiveBuffer);

                        // Check that transaction ID matches or not response what we want.
                        if (CompareArray(request.TransactionID, response.TransactionID))
                        {
                            return response;
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        #endregion

        #region static method CompareArray

        /// <summary>
        /// Compares if specified array itmes equals.
        /// </summary>
        /// <param name="array1">Array 1.</param>
        /// <param name="array2">Array 2</param>
        /// <returns>Returns true if both arrays are equal.</returns>
        private static bool CompareArray(byte[] array1, byte[] array2)
        {
            // if both are null
            if (array1 == null && array2 == null)
                return true;
            // if only one of them is null
            if (array1 == null || array2 == null)
                return false;
            // if length does not match
            if (array1.Length != array2.Length)
                return false;
            // check array
            for (int i = 0; i < array1.Length; i++)
                if (array1[i] != array2[i])
                    return false;
            return true;
        }

        #endregion

        /// <summary>
        /// Implements STUN message. Defined in RFC 3489.
        /// </summary>
        class STUN_Message
        {
            #region enum AttributeType

            /// <summary>
            /// Specifies STUN attribute type.
            /// </summary>
            private enum AttributeType
            {
                MappedAddress = 0x0001,
                ResponseAddress = 0x0002,
                ChangeRequest = 0x0003,
                SourceAddress = 0x0004,
                ChangedAddress = 0x0005,
                Username = 0x0006,
                Password = 0x0007,
                MessageIntegrity = 0x0008,
                ErrorCode = 0x0009,
                UnknownAttribute = 0x000A,
                ReflectedFrom = 0x000B,
                XorMappedAddress = 0x8020,
                XorOnly = 0x0021,
                ServerName = 0x8022,
            }

            #endregion

            #region enum IPFamily

            /// <summary>
            /// Specifies IP address family.
            /// </summary>
            private enum IPFamily
            {
                IPv4 = 0x01,
                IPv6 = 0x02,
            }

            #endregion

            #region Properties

            public STUN_MessageType Type { get; set; } = STUN_MessageType.BindingRequest;
            public int MagicCookie { get; private set; } = 0;
            public byte[] TransactionID { get; private set; } = null;
            public IPEndPoint MappedAddress { get; set; } = null;
            public IPEndPoint ResponseAddress { get; set; } = null;
            public STUN_ChangeRequest ChangeRequest { get; set; } = null;
            public IPEndPoint SourceAddress { get; set; } = null;
            public IPEndPoint ChangedAddress { get; set; } = null;
            public string UserName { get; set; } = null;
            public string Password { get; set; } = null;
            public STUN_ErrorCode ErrorCode { get; set; } = null;
            public IPEndPoint ReflectedFrom { get; set; } = null;
            public string ServerName { get; set; } = null;

            #endregion

            /// <summary>
            /// Default constructor.
            /// </summary>
            public STUN_Message()
            {
                TransactionID = new byte[12];
                new Random().NextBytes(TransactionID);
            }


            #region method Parse

            /// <summary>
            /// Parses STUN message from raw data packet.
            /// </summary>
            /// <param name="data">Raw STUN message.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>data</b> is null reference.</exception>
            public void Parse(byte[] data)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                /* RFC 5389 6.             
                    All STUN messages MUST start with a 20-byte header followed by zero
                    or more Attributes.  The STUN header contains a STUN message type,
                    magic cookie, transaction ID, and message length.

                     0                   1                   2                   3
                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                     |0 0|     STUN Message Type     |         Message Length        |
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                     |                         Magic Cookie                          |
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                     |                                                               |
                     |                     Transaction ID (96 bits)                  |
                     |                                                               |
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                   The message length is the count, in bytes, of the size of the
                   message, not including the 20 byte header.
                */

                if (data.Length < 20)
                {
                    throw new ArgumentException("Invalid STUN message value !");
                }

                int offset = 0;

                //--- message header --------------------------------------------------

                // STUN Message Type
                int messageType = (data[offset++] << 8 | data[offset++]);
                if (Enum.IsDefined(typeof(STUN_MessageType), messageType))
                {
                    Type = (STUN_MessageType) messageType;
                }
                else
                {
                    throw new ArgumentException("Invalid STUN message type value !");
                }

                // Message Length
                int messageLength = (data[offset++] << 8 | data[offset++]);

                // Magic Cookie
                MagicCookie = (data[offset++] << 24 | data[offset++] << 16 | data[offset++] << 8 | data[offset++]);

                // Transaction ID
                TransactionID = new byte[12];
                Array.Copy(data, offset, TransactionID, 0, 12);
                offset += 12;

                //--- Message attributes ---------------------------------------------
                while ((offset - 20) < messageLength)
                {
                    ParseAttribute(data, ref offset);
                }
            }

            #endregion

            #region method ToByteData

            /// <summary>
            /// Converts this to raw STUN packet.
            /// </summary>
            /// <returns>Returns raw STUN packet.</returns>
            public byte[] ToByteData()
            {
                /* RFC 5389 6.             
                    All STUN messages MUST start with a 20-byte header followed by zero
                    or more Attributes.  The STUN header contains a STUN message type,
                    magic cookie, transaction ID, and message length.

                     0                   1                   2                   3
                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                     |0 0|     STUN Message Type     |         Message Length        |
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                     |                         Magic Cookie                          |
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                     |                                                               |
                     |                     Transaction ID (96 bits)                  |
                     |                                                               |
                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                   The message length is the count, in bytes, of the size of the
                   message, not including the 20 byte header.
                */

                // We allocate 512 for header, that should be more than enough.
                byte[] msg = new byte[512];

                int offset = 0;

                //--- message header -------------------------------------

                // STUN Message Type (2 bytes)
                msg[offset++] = (byte) (((int) this.Type >> 8) & 0x3F);
                msg[offset++] = (byte) ((int) this.Type & 0xFF);

                // Message Length (2 bytes) will be assigned at last.
                msg[offset++] = 0;
                msg[offset++] = 0;

                // Magic Cookie           
                msg[offset++] = (byte) ((this.MagicCookie >> 24) & 0xFF);
                msg[offset++] = (byte) ((this.MagicCookie >> 16) & 0xFF);
                msg[offset++] = (byte) ((this.MagicCookie >> 8) & 0xFF);
                msg[offset++] = (byte) ((this.MagicCookie >> 0) & 0xFF);

                // Transaction ID (16 bytes)
                Array.Copy(TransactionID, 0, msg, offset, 12);
                offset += 12;

                //--- Message attributes ------------------------------------

                /* RFC 3489 11.2.
                    After the header are 0 or more attributes.  Each attribute is TLV
                    encoded, with a 16 bit type, 16 bit length, and variable value:

                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |         Type                  |            Length             |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |                             Value                             ....
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                */

                if (this.MappedAddress != null)
                {
                    StoreEndPoint(AttributeType.MappedAddress, this.MappedAddress, msg, ref offset);
                }
                else if (this.ResponseAddress != null)
                {
                    StoreEndPoint(AttributeType.ResponseAddress, this.ResponseAddress, msg, ref offset);
                }
                else if (this.ChangeRequest != null)
                {
                    /*
                        The CHANGE-REQUEST attribute is used by the client to request that
                        the server use a different address and/or port when sending the
                        response.  The attribute is 32 bits long, although only two bits (A
                        and B) are used:

                         0                   1                   2                   3
                         0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                        |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                        The meaning of the flags is:

                        A: This is the "change IP" flag.  If true, it requests the server
                           to send the Binding Response with a different IP address than the
                           one the Binding Request was received on.

                        B: This is the "change port" flag.  If true, it requests the
                           server to send the Binding Response with a different port than the
                           one the Binding Request was received on.
                    */

                    // Attribute header
                    msg[offset++] = (int) AttributeType.ChangeRequest >> 8;
                    msg[offset++] = (int) AttributeType.ChangeRequest & 0xFF;
                    msg[offset++] = 0;
                    msg[offset++] = 4;

                    msg[offset++] = 0;
                    msg[offset++] = 0;
                    msg[offset++] = 0;
                    msg[offset++] =
                        (byte)
                        (Convert.ToInt32(this.ChangeRequest.ChangeIP) << 2 |
                         Convert.ToInt32(this.ChangeRequest.ChangePort) << 1);
                }
                else if (this.SourceAddress != null)
                {
                    StoreEndPoint(AttributeType.SourceAddress, this.SourceAddress, msg, ref offset);
                }
                else if (this.ChangedAddress != null)
                {
                    StoreEndPoint(AttributeType.ChangedAddress, this.ChangedAddress, msg, ref offset);
                }
                else if (this.UserName != null)
                {
                    byte[] userBytes = Encoding.ASCII.GetBytes(this.UserName);

                    // Attribute header
                    msg[offset++] = (int) AttributeType.Username >> 8;
                    msg[offset++] = (int) AttributeType.Username & 0xFF;
                    msg[offset++] = (byte) (userBytes.Length >> 8);
                    msg[offset++] = (byte) (userBytes.Length & 0xFF);

                    Array.Copy(userBytes, 0, msg, offset, userBytes.Length);
                    offset += userBytes.Length;
                }
                else if (this.Password != null)
                {
                    byte[] userBytes = Encoding.ASCII.GetBytes(this.UserName);

                    // Attribute header
                    msg[offset++] = (int) AttributeType.Password >> 8;
                    msg[offset++] = (int) AttributeType.Password & 0xFF;
                    msg[offset++] = (byte) (userBytes.Length >> 8);
                    msg[offset++] = (byte) (userBytes.Length & 0xFF);

                    Array.Copy(userBytes, 0, msg, offset, userBytes.Length);
                    offset += userBytes.Length;
                }
                else if (this.ErrorCode != null)
                {
                    /* 3489 11.2.9.
                        0                   1                   2                   3
                        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                        |                   0                     |Class|     Number    |
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                        |      Reason Phrase (variable)                                ..
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    */

                    byte[] reasonBytes = Encoding.ASCII.GetBytes(this.ErrorCode.ReasonText);

                    // Header
                    msg[offset++] = 0;
                    msg[offset++] = (int) AttributeType.ErrorCode;
                    msg[offset++] = 0;
                    msg[offset++] = (byte) (4 + reasonBytes.Length);

                    // Empty
                    msg[offset++] = 0;
                    msg[offset++] = 0;
                    // Class
                    msg[offset++] = (byte) Math.Floor((double) (this.ErrorCode.Code / 100));
                    // Number
                    msg[offset++] = (byte) (this.ErrorCode.Code & 0xFF);
                    // ReasonPhrase
                    Array.Copy(reasonBytes, msg, reasonBytes.Length);
                    offset += reasonBytes.Length;
                }
                else if (this.ReflectedFrom != null)
                {
                    StoreEndPoint(AttributeType.ReflectedFrom, this.ReflectedFrom, msg, ref offset);
                }

                // Update Message Length. NOTE: 20 bytes header not included.
                msg[2] = (byte) ((offset - 20) >> 8);
                msg[3] = (byte) ((offset - 20) & 0xFF);

                // Make reatval with actual size.
                byte[] retVal = new byte[offset];
                Array.Copy(msg, retVal, retVal.Length);

                return retVal;
            }

            #endregion


            #region method ParseAttribute

            /// <summary>
            /// Parses attribute from data.
            /// </summary>
            /// <param name="data">SIP message data.</param>
            /// <param name="offset">Offset in data.</param>
            private void ParseAttribute(byte[] data, ref int offset)
            {
                /* RFC 3489 11.2.
                    Each attribute is TLV encoded, with a 16 bit type, 16 bit length, and variable value:

                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |         Type                  |            Length             |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |                             Value                             ....
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                            
                */

                // Type
                AttributeType type = (AttributeType) (data[offset++] << 8 | data[offset++]);

                // Length
                int length = (data[offset++] << 8 | data[offset++]);

                // MAPPED-ADDRESS
                if (type == AttributeType.MappedAddress)
                {
                    MappedAddress = ParseEndPoint(data, ref offset);
                }
                // RESPONSE-ADDRESS
                else if (type == AttributeType.ResponseAddress)
                {
                    ResponseAddress = ParseEndPoint(data, ref offset);
                }
                // CHANGE-REQUEST
                else if (type == AttributeType.ChangeRequest)
                {
                    /*
                        The CHANGE-REQUEST attribute is used by the client to request that
                        the server use a different address and/or port when sending the
                        response.  The attribute is 32 bits long, although only two bits (A
                        and B) are used:

                         0                   1                   2                   3
                         0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                        |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                        The meaning of the flags is:

                        A: This is the "change IP" flag.  If true, it requests the server
                           to send the Binding Response with a different IP address than the
                           one the Binding Request was received on.

                        B: This is the "change port" flag.  If true, it requests the
                           server to send the Binding Response with a different port than the
                           one the Binding Request was received on.
                    */

                    // Skip 3 bytes
                    offset += 3;

                    ChangeRequest = new STUN_ChangeRequest((data[offset] & 4) != 0, (data[offset] & 2) != 0);
                    offset++;
                }
                // SOURCE-ADDRESS
                else if (type == AttributeType.SourceAddress)
                {
                    SourceAddress = ParseEndPoint(data, ref offset);
                }
                // CHANGED-ADDRESS
                else if (type == AttributeType.ChangedAddress)
                {
                    ChangedAddress = ParseEndPoint(data, ref offset);
                }
                // USERNAME
                else if (type == AttributeType.Username)
                {
                    UserName = Encoding.Default.GetString(data, offset, length);
                    offset += length;
                }
                // PASSWORD
                else if (type == AttributeType.Password)
                {
                    Password = Encoding.Default.GetString(data, offset, length);
                    offset += length;
                }
                // MESSAGE-INTEGRITY
                else if (type == AttributeType.MessageIntegrity)
                {
                    offset += length;
                }
                // ERROR-CODE
                else if (type == AttributeType.ErrorCode)
                {
                    /* 3489 11.2.9.
                        0                   1                   2                   3
                        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                        |                   0                     |Class|     Number    |
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                        |      Reason Phrase (variable)                                ..
                        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    */

                    int errorCode = (data[offset + 2] & 0x7) * 100 + (data[offset + 3] & 0xFF);

                    ErrorCode = new STUN_ErrorCode(errorCode, Encoding.Default.GetString(data, offset + 4, length - 4));
                    offset += length;
                }
                // UNKNOWN-ATTRIBUTES
                else if (type == AttributeType.UnknownAttribute)
                {
                    offset += length;
                }
                // REFLECTED-FROM
                else if (type == AttributeType.ReflectedFrom)
                {
                    ReflectedFrom = ParseEndPoint(data, ref offset);
                }
                // XorMappedAddress
                // XorOnly
                // ServerName
                else if (type == AttributeType.ServerName)
                {
                    ServerName = Encoding.Default.GetString(data, offset, length);
                    offset += length;
                }
                // Unknown
                else
                {
                    offset += length;
                }
            }

            #endregion

            #region method ParseEndPoint

            /// <summary>
            /// Pasrses IP endpoint attribute.
            /// </summary>
            /// <param name="data">STUN message data.</param>
            /// <param name="offset">Offset in data.</param>
            /// <returns>Returns parsed IP end point.</returns>
            private IPEndPoint ParseEndPoint(byte[] data, ref int offset)
            {
                /*
                    It consists of an eight bit address family, and a sixteen bit
                    port, followed by a fixed length value representing the IP address.

                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |x x x x x x x x|    Family     |           Port                |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |                             Address                           |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                */

                // Skip family
                offset++;
                offset++;

                // Port
                int port = (data[offset++] << 8 | data[offset++]);

                // Address
                byte[] ip = new byte[4];
                ip[0] = data[offset++];
                ip[1] = data[offset++];
                ip[2] = data[offset++];
                ip[3] = data[offset++];

                return new IPEndPoint(new IPAddress(ip), port);
            }

            #endregion

            #region method StoreEndPoint

            /// <summary>
            /// Stores ip end point attribute to buffer.
            /// </summary>
            /// <param name="type">Attribute type.</param>
            /// <param name="endPoint">IP end point.</param>
            /// <param name="message">Buffer where to store.</param>
            /// <param name="offset">Offset in buffer.</param>
            private void StoreEndPoint(AttributeType type, IPEndPoint endPoint, byte[] message, ref int offset)
            {
                /*
                    It consists of an eight bit address family, and a sixteen bit
                    port, followed by a fixed length value representing the IP address.

                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |x x x x x x x x|    Family     |           Port                |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |                             Address                           |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+             
                */

                // Header
                message[offset++] = (byte) ((int) type >> 8);
                message[offset++] = (byte) ((int) type & 0xFF);
                message[offset++] = 0;
                message[offset++] = 8;

                // Unused
                message[offset++] = 0;
                // Family
                message[offset++] = (byte) IPFamily.IPv4;
                // Port
                message[offset++] = (byte) (endPoint.Port >> 8);
                message[offset++] = (byte) (endPoint.Port & 0xFF);
                // Address
                byte[] ipBytes = endPoint.Address.GetAddressBytes();
                message[offset++] = ipBytes[0];
                message[offset++] = ipBytes[1];
                message[offset++] = ipBytes[2];
                message[offset++] = ipBytes[3];
            }

            #endregion

        }

        /// <summary>
        /// This enum specifies STUN message type.
        /// </summary>
        enum STUN_MessageType
        {
            /// <summary>
            /// STUN message is binding request.
            /// </summary>
            BindingRequest = 0x0001,

            /// <summary>
            /// STUN message is binding request response.
            /// </summary>
            BindingResponse = 0x0101,

            /// <summary>
            /// STUN message is binding requesr error response.
            /// </summary>
            BindingErrorResponse = 0x0111,

            /// <summary>
            /// STUN message is "shared secret" request.
            /// </summary>
            SharedSecretRequest = 0x0002,

            /// <summary>
            /// STUN message is "shared secret" request response.
            /// </summary>
            SharedSecretResponse = 0x0102,

            /// <summary>
            /// STUN message is "shared secret" request error response.
            /// </summary>
            SharedSecretErrorResponse = 0x0112,
        }

        /// <summary>
        /// This class implements STUN CHANGE-REQUEST attribute. Defined in RFC 3489 11.2.4.
        /// </summary>
        class STUN_ChangeRequest
        {
            public bool ChangeIP { get; set; } = true;
            public bool ChangePort { get; set; } = true;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public STUN_ChangeRequest()
            {
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="changeIP">Specifies if STUN server must send response to different IP than request was received.</param>
            /// <param name="changePort">Specifies if STUN server must send response to different port than request was received.</param>
            public STUN_ChangeRequest(bool changeIP, bool changePort)
            {
                ChangeIP = changeIP;
                ChangePort = changePort;
            }

        }

        /// <summary>
        /// This class implements STUN ERROR-CODE. Defined in RFC 3489 11.2.9.
        /// </summary>
        class STUN_ErrorCode
        {
            public int Code { get; set; } = 0;
            public string ReasonText { get; set; } = "";

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="code">Error code.</param>
            /// <param name="reasonText">Reason text.</param>
            public STUN_ErrorCode(int code, string reasonText)
            {
                Code = code;
                ReasonText = reasonText;
            }
        }
    }

}