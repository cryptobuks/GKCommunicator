﻿/*
 *  "GKCommunicator", the chat and bulletin board of the genealogical network.
 *  Copyright (C) 2018 by Sergey V. Zhdanovskih.
 *
 *  This file is part of "GEDKeeper".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BencodeNET.Objects;

namespace GKNet.DHT
{
    public static class DHTHelper
    {
        public const int CompactNodeRecordLengthIP4 = 26; // 20 + 6
        public const int CompactNodeRecordLengthIP6 = 38; // 20 + 18
#if !IP6
        public const int CompactNodeRecordLength = CompactNodeRecordLengthIP4;
#else
        public const int CompactNodeRecordLength = CompactNodeRecordLengthIP6;
#endif

        private static Random r = new Random();
        private static SHA1 sha1 = new SHA1CryptoServiceProvider();

        private static byte[] fCurrentTransactionId = new byte[2];

        public static BString GetTransactionId()
        {
            lock (fCurrentTransactionId) {
                BString result = new BString((byte[])fCurrentTransactionId.Clone());
                if (fCurrentTransactionId[0] == 255) {
                    fCurrentTransactionId[0] = 0;
                    fCurrentTransactionId[1] += 1;
                } else {
                    fCurrentTransactionId[0] += 1;
                }
                return result;
            }
        }

        public static byte[] GetRandomID()
        {
            var r = new Random();
            byte[] result = new byte[20];
            r.NextBytes(result);
            return result;
        }

        public static byte[] GetRandomHashID()
        {
            var result = new byte[20];
            r.NextBytes(result);
            lock (sha1) {
                result = sha1.ComputeHash(result);
                return result;
            }
        }

        public static IPAddress PrepareAddress(IPAddress address)
        {
#if !IP6
               return address;
#else
            return address.MapToIPv6();
#endif
        }

        public static List<IPEndPoint> ParseValuesList(BList data)
        {
            var result = new List<IPEndPoint>();

            foreach (var item in data) {
                var str = item as BString;
                var itemBytes = str.Value;

                if (itemBytes.Length == 6) {
                    var ip = new IPAddress(itemBytes.Take(4).ToArray());
                    var port = BitConverter.ToUInt16(itemBytes, 4);
                    var xnode = new IPEndPoint(PrepareAddress(ip), port);
                    result.Add(xnode);
                } else if (itemBytes.Length == 18) {
                    var ip = new IPAddress(itemBytes.Take(16).ToArray());
                    var port = BitConverter.ToUInt16(itemBytes, 16);
                    var xnode = new IPEndPoint(PrepareAddress(ip), port);
                    result.Add(xnode);
                } else {
                }
            }

            return result;
        }

        public static List<DHTNode> ParseNodesListIP4(byte[] data)
        {
            var result = new List<DHTNode>();
            for (int i = 0; i < data.Length; i += 26) {
                var dd = data.Skip(i).Take(26).ToArray();
                //var bc = dd.ToHexString();
                //Console.WriteLine(bc);
                var b = dd[24];
                dd[24] = dd[25];
                dd[25] = b;
                var id = dd.Take(20).ToArray();
                var ip = new IPAddress(dd.Skip(20).Take(4).ToArray());
                var port = BitConverter.ToUInt16(dd, 24);
                var tt = new DHTNode(id, new IPEndPoint(PrepareAddress(ip), port));
                result.Add(tt);
            }
            return result;
        }

        public static List<DHTNode> ParseNodesListIP6(byte[] data)
        {
            var result = new List<DHTNode>();
            for (int i = 0; i < data.Length; i += 38) {
                var dd = data.Skip(i).Take(38).ToArray();
                //var bc = dd.ToHexString();
                //Console.WriteLine(bc);
                var b = dd[36];
                dd[36] = dd[37];
                dd[37] = b;
                var id = dd.Take(20).ToArray();
                var ip = new IPAddress(dd.Skip(20).Take(16).ToArray());
                var port = BitConverter.ToUInt16(dd, 36);
                var tt = new DHTNode(id, new IPEndPoint(PrepareAddress(ip), port));
                result.Add(tt);
            }
            return result;
        }

        public static string ToHexString(this byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in data) {
                var t = b / 16;
                sb.Append((char)(t + (t <= 9 ? '0' : 'W')));
                var f = b % 16;
                sb.Append((char)(f + (f <= 9 ? '0' : 'W')));
            }

            return sb.ToString();
        }

        public static byte[] GetNeighbor(byte[] target, byte[] nid)
        {
            var result = new byte[20];
            for (int i = 0; i < 10; i++)
                result[i] = target[i];
            for (int i = 10; i < 20; i++)
                result[i] = nid[i];
            return result;
        }

        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++) {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        // TODO: ATTENTION, the quantity is not more than according to specification!
        public static BList CompactPeers(IList<IDHTPeer> peersList)
        {
            BList values = null;
            if (peersList != null && peersList.Count > 0) {
                values = new BList();
                foreach (var peer in peersList) {
                    values.Add(new BString(CompactEndPoint(peer.EndPoint)));
                }
            }
            return values;
        }

        public static byte[] CompactNodes(IList<DHTNode> nodesList)
        {
            byte[] nodesArray = new byte[nodesList.Count * 26];
            var i = 0;
            foreach (var node in nodesList) {
                var compact = CompactNode(node);
                Array.Copy(compact, 0, nodesArray, i * 26, 26);
            }
            return nodesArray;
        }

        public static byte[] CompactNode(DHTNode node)
        {
            IPAddress address = node.EndPoint.Address;
            ushort port = (ushort)node.EndPoint.Port;

            var info = new byte[26];
            Array.Copy(node.ID, info, 20);
            Array.Copy(address.GetAddressBytes(), 0, info, 20, 4);
            info[24] = (byte)((port >> 8) & 0xFF);
            info[25] = (byte)(port & 0xFF);
            return info;
        }

        public static byte[] CompactEndPoint(IPEndPoint endPoint)
        {
            IPAddress address = endPoint.Address;
            ushort port = (ushort)endPoint.Port;

            var info = new byte[6];
            Array.Copy(address.GetAddressBytes(), 0, info, 0, 4);
            info[4] = (byte)((port >> 8) & 0xFF);
            info[5] = (byte)(port & 0xFF);
            return info;
        }

        /// <summary>
        /// Calculates the hash of the 'info'-dictionary.
        /// The info hash is a 20-byte SHA1 hash of the 'info'-dictionary of the torrent
        /// used to uniquely identify it and it's contents.
        ///
        /// <para>Example: 6D60711ECF005C1147D8973A67F31A11454AB3F5</para>
        /// </summary>
        /// <param name="info">The 'info'-dictionary of a torrent.</param>
        /// <returns>A byte-array of the 20-byte SHA1 hash.</returns>
        public static byte[] CalculateInfoHashBytes(BDictionary info)
        {
            using (var sha1 = SHA1.Create())
            using (var stream = new MemoryStream()) {
                info.EncodeTo(stream);
                stream.Position = 0;

                return sha1.ComputeHash(stream);
            }
        }

        /// <summary>
        /// Calculates the hash of the 'info'-dictionary.
        /// The info hash is a 20-byte SHA1 hash of the 'info'-dictionary of the torrent
        /// used to uniquely identify it and it's contents.
        ///
        /// <para>Example: 6D60711ECF005C1147D8973A67F31A11454AB3F5</para>
        /// </summary>
        /// <param name="info">The 'info'-dictionary of a torrent.</param>
        /// <returns>A string representation of the 20-byte SHA1 hash without dashes.</returns>
        public static string CalculateInfoHash(BDictionary info)
        {
            var hashBytes = CalculateInfoHashBytes(info);
            return BytesToHexString(hashBytes);
        }

        /// <summary>
        /// Converts the byte array to a hexadecimal string representation without hyphens.
        /// </summary>
        /// <param name="bytes"></param>
        public static string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
