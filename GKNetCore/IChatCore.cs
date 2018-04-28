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

using System.Collections.Generic;
using System.Net;

namespace GKNet
{
    public interface IChatCore
    {
        string MemberName { get; set; }
        IList<Peer> Peers { get; }
        int TCPListenerPort { get; set; }

        void Connect();
        void Disconnect();
        void Join(string member);
        void Leave(string member);
        void Send(Peer target, string message);
        void SendToAll(string message);

        void AddPeer(IPAddress peerAddress, int port);
        Peer FindPeer(IPAddress peerAddress);
    }
}