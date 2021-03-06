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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GKNet;
using GKNet.Logging;

namespace GKCommunicatorApp
{
    public partial class ChatForm : Form, IChatForm
    {
        private delegate void NoArgDelegate();

        private readonly ICommunicatorCore fCore;
        private readonly ILogger fLogger;

        public ChatForm()
        {
            InitializeComponent();

            Closing += new CancelEventHandler(ChatForm_Closing);
            txtMemberName.Focus();

            if (File.Exists(ProtocolHelper.LOG_FILE)) {
                File.Delete(ProtocolHelper.LOG_FILE);
            }
            fLogger = LogManager.GetLogger(ProtocolHelper.LOG_FILE, ProtocolHelper.LOG_LEVEL, "ChatForm");

            fCore = new CommunicatorCore(this);

            UpdateStatus();
        }

        private void Connect()
        {
            fCore.Profile.UserName = txtMemberName.Text;
            fCore.TCPListenerPort = ProtocolHelper.PublicTCPPort;

            // join the P2P mesh from a worker thread
            NoArgDelegate executor = new NoArgDelegate(fCore.Connect);
            executor.BeginInvoke(null, null);
        }

        private void Disconnect()
        {
            fCore.Disconnect();

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            miConnect.Enabled = !fCore.IsConnected;
            miDisconnect.Enabled = fCore.IsConnected;

            btnSend.Enabled = fCore.IsConnected;
            btnSendToAll.Enabled = fCore.IsConnected;

            btnConnect.Enabled = !fCore.IsConnected;
            lblConnectionStatus.Visible = fCore.IsConnected;
        }

        private void ChatForm_Closing(object sender, CancelEventArgs e)
        {
            Disconnect();
        }

        private void AddTextChunk(string text, Color color)
        {
            int selStart = lstChatMsgs.Text.Length - 1;
            lstChatMsgs.AppendText(text);
            int selEnd = lstChatMsgs.Text.Length - 1;

            lstChatMsgs.SelectionStart = selStart;
            lstChatMsgs.SelectionLength = selEnd - selStart + 1;
            lstChatMsgs.SelectionColor = color;
        }

        private void AddChatText(string text, Color color)
        {
            lstChatMsgs.AppendText("\r\n");
            AddTextChunk(DateTime.Now.ToString(), Color.Red);
            lstChatMsgs.AppendText("\r\n");
            AddTextChunk(text, Color.Navy);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void btnSendToAll_Click(object sender, EventArgs e)
        {
            var msgText = txtChatMsg.Text;

            if (!String.IsNullOrEmpty(msgText)) {
                fCore.SendToAll(msgText);
                txtChatMsg.Clear();
                txtChatMsg.Focus();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var peerItem = (lstMembers.SelectedItem as Peer);
            var msgText = txtChatMsg.Text;

            if ((!String.IsNullOrEmpty(msgText)) && (peerItem != null)) {
                fCore.Send(peerItem, msgText);
                txtChatMsg.Clear();
                txtChatMsg.Focus();
            }
        }

        private void miDHTLog_Click(object sender, EventArgs e)
        {
            LoadExtFile(ProtocolHelper.LOG_FILE);
        }

        private void miSysInfo_Click(object sender, EventArgs e)
        {
            using (var dlgSysInfo = new SysInfoWin()) {
                dlgSysInfo.ShowDialog();
            }
        }

        private void miExternalIP_Click(object sender, EventArgs e)
        {
            LoadExtFile("http://checkip.dyndns.com/");
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void miConnect_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void miDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void miProfile_Click(object sender, EventArgs e)
        {
            // not implemented yet
        }

        #region IChatForm members

        void IChatForm.OnPeersListChanged()
        {
            Invoke((MethodInvoker)delegate {
                lstMembers.BeginUpdate();
                lstMembers.Items.Clear();
                foreach (var peer in fCore.Peers) {
                    lstMembers.Items.Add(peer);
                }
                lstMembers.EndUpdate();

                UpdateStatus();
            });
        }

        void IChatForm.OnMessageReceived(Peer sender, string message)
        {
            Invoke((MethodInvoker)delegate {
                AddChatText(message, Color.Black);

                UpdateStatus();
            });
        }

        void IChatForm.OnJoin(Peer member)
        {
            Invoke((MethodInvoker)delegate {
                //AddChatText(member + " joined the chatroom.");

                UpdateStatus();
            });
        }

        void IChatForm.OnLeave(Peer member)
        {
            Invoke((MethodInvoker)delegate {
                //AddChatText(member + " left the chatroom.");

                UpdateStatus();
            });
        }

        #endregion

        #region External functions

        public static void LoadExtFile(string fileName)
        {
            if (File.Exists(fileName)) {
                Process.Start(new ProcessStartInfo("file://" + fileName) { UseShellExecute = true });
            } else {
                Process.Start(fileName);
            }
        }

        #endregion
    }
}
