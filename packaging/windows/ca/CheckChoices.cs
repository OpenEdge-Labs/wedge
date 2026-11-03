// SPDX-License-Identifier: Apache-2.0
// Copyright (c) OpenEdge Labs

// Checks the setup choices before anything is installed, with the same rules
// wedge applies (crates/wedge-core/src/auth.rs, crates/wedge/src/setup.rs),
// so a mistake shows as a message on the dialog page instead of a failed
// install. The dialogs run CheckPorts and CheckAdmin, which set WEDGE_ERROR;
// the install sequence runs CheckChoices, which stops a silent install with
// the same message. Values are never written to the log.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using WixToolset.Dtf.WindowsInstaller;

namespace WedgeSetup
{
    public static class Checks
    {
        const int MinPasswordChars = 12;
        const int MaxPasswordBytes = 1024;
        static readonly Regex UserName = new Regex("^[A-Za-z0-9._@-]{1,64}$");

        /// <summary>Web and admin ports: numbers 1-65535, different, and
        /// free (unless an older wEdge, which still holds them, is
        /// installed).</summary>
        [CustomAction]
        public static ActionResult CheckPorts(Session session)
        {
            session["WEDGE_ERROR"] = PortsError(session) ?? "";
            return ActionResult.Success;
        }

        /// <summary>The administrator account, when a password is given.</summary>
        [CustomAction]
        public static ActionResult CheckAdmin(Session session)
        {
            session["WEDGE_ERROR"] = AdminError(session) ?? "";
            return ActionResult.Success;
        }

        /// <summary>Both checks for an install without dialogs: stops it with
        /// the message before anything changes.</summary>
        [CustomAction]
        public static ActionResult CheckChoices(Session session)
        {
            string error = PortsError(session) ?? AdminError(session);
            if (error == null)
            {
                return ActionResult.Success;
            }
            session.Log("wEdge setup: " + error);
            using (var record = new Record(1))
            {
                record[1] = error;
                session.Message(InstallMessage.Error | (InstallMessage)MessageButtons.OK, record);
            }
            return ActionResult.Failure;
        }

        static string PortsError(Session session)
        {
            int web, admin;
            if (!TryPort(session["HTTPPORT"], out web))
            {
                return "The web port must be a number from 1 to 65535.";
            }
            if (!TryPort(session["ADMINPORT"], out admin))
            {
                return "The admin port must be a number from 1 to 65535.";
            }
            if (web == admin)
            {
                return "The web port and the admin port must be different.";
            }
            // An older wEdge still runs on its ports until setup removes it.
            if (!string.IsNullOrEmpty(session["WIX_UPGRADE_DETECTED"]))
            {
                return null;
            }
            bool everyInterface = session["PUBLICACCESS"] == "1";
            if (!IsFree(everyInterface ? IPAddress.Any : IPAddress.Loopback, web))
            {
                return "Port " + web + " is already in use by another program. Choose another web port, or stop that program first.";
            }
            if (!string.IsNullOrEmpty(session["ADMINPASSWORD"]) && !IsFree(IPAddress.Loopback, admin))
            {
                return "Port " + admin + " is already in use by another program. Choose another admin port.";
            }
            return null;
        }

        static string AdminError(Session session)
        {
            string password = session["ADMINPASSWORD"];
            if (string.IsNullOrEmpty(password))
            {
                return null; // no account: the admin page stays off
            }
            if (!UserName.IsMatch(session["ADMINUSER"] ?? ""))
            {
                return "The user name must be 1 to 64 characters: letters, digits, '.', '_', '-' or '@'.";
            }
            if (CodePoints(password) < MinPasswordChars)
            {
                return "The password must have at least " + MinPasswordChars + " characters.";
            }
            if (Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
            {
                return "The password is too long (at most " + MaxPasswordBytes + " bytes).";
            }
            foreach (char c in password)
            {
                if (char.IsControl(c))
                {
                    return "The password must not contain control characters.";
                }
            }
            if (password.Contains("\"") || password.EndsWith("\\"))
            {
                return "The password cannot contain a double quote or end with a backslash.";
            }
            return null;
        }

        static bool TryPort(string text, out int port)
        {
            return int.TryParse(text, out port) && port >= 1 && port <= 65535;
        }

        static bool IsFree(IPAddress address, int port)
        {
            var listener = new TcpListener(address, port);
            listener.ExclusiveAddressUse = true;
            try
            {
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                listener.Stop();
            }
        }

        // Characters as wedge counts them (Unicode scalar values), not UTF-16 units.
        static int CodePoints(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    i++;
                }
                count++;
            }
            return count;
        }
    }
}
