﻿/*
* This file is part of the libCEC(R) library.
*
* libCEC(R) is Copyright (C) 2011-2020 Pulse-Eight Limited.  All rights reserved.
* libCEC(R) is an original work, containing original code.
*
* libCEC(R) is a trademark of Pulse-Eight Limited.
*
* This program is dual-licensed; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
*
*
* Alternatively, you can license this library under a commercial license,
* please contact Pulse-Eight Licensing for more information.
*
* For more information contact:
* Pulse-Eight Licensing       <license@pulse-eight.com>
*     http://www.pulse-eight.com/
*     http://www.pulse-eight.net/
*
* Author: Lars Op den Kamp <lars@opdenkamp.eu>
*
*/

using CecSharp;
using System.Windows.Forms;
using LibCECTray.Properties;

namespace LibCECTray.controller.actions
{
  class ConnectToDevice : UpdateProcess
  {
    public ConnectToDevice(LibCecSharp lib, LibCECConfiguration config)
    {
      _lib = lib;
      _config = config;
    }

    private static bool HasConnectedOnce = false;

    public override void Process()
    {
      SendEvent(UpdateEventType.StatusText, Resources.action_opening_connection);
      SendEvent(UpdateEventType.ProgressBar, 10);

      //TODO read the com port setting from the configuration
      var adapters = _lib.FindAdapters(string.Empty);
      while (adapters.Length == 0)
      {
        if (!HasConnectedOnce)
        {
          var result = MessageBox.Show(Resources.could_not_connect_try_again, Resources.app_name, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2, (MessageBoxOptions)0x40000);
          if (result == DialogResult.No)
          {
            SendEvent(UpdateEventType.ExitApplication);
            return;
          }
        }
        adapters = _lib.FindAdapters(string.Empty);
      }

      HasConnectedOnce = true;
      while (!_lib.Open(adapters[0].ComPort, 10000))
      {
        var result = MessageBox.Show(Resources.could_not_connect_try_again, Resources.app_name, MessageBoxButtons.YesNo);
        if (result != DialogResult.No) continue;
        SendEvent(UpdateEventType.ExitApplication);
        return;
      }

      SendEvent(UpdateEventType.ProgressBar, 20);
      SendEvent(UpdateEventType.StatusText, Resources.action_sending_power_on);
      if ((_config.WakeDevices.Addresses.Length > 0) && (!_config.ActivateSource || (_config.WakeDevices.Addresses.Length > 1)))
        _lib.PowerOnDevices(CecLogicalAddress.Broadcast);

      if (_lib.IsActiveDevice(CecLogicalAddress.Tv))
      {
        SendEvent(UpdateEventType.StatusText, Resources.action_detecting_tv_vendor);
        SendEvent(UpdateEventType.ProgressBar, 30);
        SendEvent(UpdateEventType.TVVendorId, (int)_lib.GetDeviceVendorId(CecLogicalAddress.Tv));
      }

      SendEvent(UpdateEventType.ProgressBar, 50);
      SendEvent(UpdateEventType.StatusText, Resources.action_detecting_avr);

      bool hasAVRDevice = _lib.IsActiveDevice(CecLogicalAddress.AudioSystem);
      SendEvent(UpdateEventType.HasAVRDevice, hasAVRDevice);

      if (hasAVRDevice)
      {
        SendEvent(UpdateEventType.ProgressBar, 60);
        SendEvent(UpdateEventType.StatusText, Resources.action_detecting_avr_vendor);
        SendEvent(UpdateEventType.AVRVendorId, (int)_lib.GetDeviceVendorId(CecLogicalAddress.AudioSystem));
      }
      if (_config.ActivateSource)
      {
        SendEvent(UpdateEventType.ProgressBar, 70);
        SendEvent(UpdateEventType.StatusText, Resources.action_activating_source);
        _lib.SetActiveSource(CecDeviceType.Reserved);
      }

      SendEvent(UpdateEventType.ProgressBar, 80);
      SendEvent(UpdateEventType.StatusText, Resources.action_reading_device_configuration);

      _lib.GetCurrentConfiguration(_config);
      SendEvent(_config);

      SendEvent(UpdateEventType.ProgressBar, 90);
      SendEvent(UpdateEventType.StatusText, Resources.action_polling_active_devices);
      SendEvent(UpdateEventType.PollDevices);

      SendEvent(UpdateEventType.Connected);
      SendEvent(UpdateEventType.ProgressBar, 100);

      if (!_lib.IsActiveDevice(CecLogicalAddress.Tv))
        RegisterWarning(CecAlert.TVPollFailed);

      SystemIdleMonitor.Instance.Suspended = false;
      SendEvent(UpdateEventType.StatusReady);
    }

    private readonly LibCecSharp _lib;
    private readonly LibCECConfiguration _config;
  }
}
