#region Copyright notice
/*
A Firmata Helper Library v1.2.x
-------------------------------
Encoding control and configuration messages for Firmata enabled MCUs.

For more information on Firmata see: http://firmata.org
Get the source from: https://github.com/jens-a-e/VVVVirmata
Any issues & feature requests should be posted to: https://github.com/jens-a-e/VVVVirmata/issues

Copyleft 2011-2013
Jens Alexander Ewald, http://lea.io
Supported by http://www.muthesius-kunsthochschule.de

Inspired by the Sharpduino project by Tasos Valsamidis
See http://code.google.com/p/sharpduino if interested.


Copyright notice
----------------

This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>
*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Firmata {

  public static class Util {

    #region Command utils
    public static bool ContainsCommand(byte[] msg, byte cmd) {
      foreach (byte b in msg) {
        if (VerifiyCommand(b,cmd)) return true;
      }
      return false;
    }

    public static bool VerifiyCommand(byte b, byte cmd) {
      return GetCommand(b) == cmd;
    }

    public static byte GetCommand (byte data) {
      // Commands with channel data need to be masked
      int masked = data & 0xF0;
      return (
           masked != Command.DIGITAL_MESSAGE
        && masked != Command.ANALOG_MESSAGE
        && masked != Command.REPORT_DIGITAL
        && masked != Command.REPORT_ANALOG
      ) ? data :  (byte)(data & 0xF0);
    }
    #endregion

    #region Message formatting

    /// <summary>
    /// Encode a digital message as a byte sequence, ready to be sent.
    /// </summary>
    /// <returns>
    /// The formatted/encoded message.
    /// </returns>
    /// <param name='port'>
    /// Which digital port to target.
    /// </param>
    /// <param name='vals'>
    /// The values of the pins.
    /// </param>
    public static byte[] EncodeDigitalMessage(int port, int[] vals) {
      int state = ValuesToPortState(vals);
      byte[] command = {(byte)(Command.DIGITAL_MESSAGE | port), LSB(state), MSB(state) };
      return command;
    }

    /// <summary>
    /// Decodes a digital port message.
    /// </summary>
    /// <returns>
    /// Returns true, if successfully decoded. Otherwise false.
    /// </returns>
    /// <param name='data'>
    /// The raw message.
    /// </param>
    /// <param name='port'>
    /// Gets set to the port as encoded in the message.
    /// </param>
    /// <param name='vals'>
    /// Gets set to an array of integer values, relative to their port state.
    /// </param>
    public static bool DecodeDigitalMessage(byte[] data, out int port, out int[] vals) {
      if (data.Length<3){
        port = 0;
        vals = new int[Constants.BitsPerPort];
        return false;
      }
      port = (data[0] & 0x0f);
      vals = ValuesFromPortState( FromBytes(data[1], data[2]) );
      return true;
    }

    /// <summary>
    /// Encode an analog message as a byte sequence, ready to sent.
    /// </summary>
    /// <returns>
    /// The formatted analog message.
    /// </returns>
    /// <param name='pin'>
    /// The ananloh pin to set.
    /// </param>
    /// <param name='value'>
    /// The value to set the pin to.
    /// </param>
    public static byte[] EncodeAnalogMessage(int pin, int val) {
      byte[] command = {(byte)(Command.ANALOG_MESSAGE | pin), LSB(val), MSB(val) };
      return command;
    }

    /// <summary>
    /// Decodes the analog message.
    /// </summary>
    /// <returns>
    /// The analog message.
    /// </returns>
    /// <param name='data'>
    /// If set to <c>true</c> data.
    /// </param>
    /// <param name='pin'>
    /// If set to <c>true</c> pin.
    /// </param>
    /// <param name='val'>
    /// If set to <c>true</c> value.
    /// </param>
    public static bool DecodeAnalogMessage(byte[] data, out int pin, out int val) {
      if (data.Length<3){
        pin = 0;
        val = 0;
        return false;
      }
      pin = (data[0] & 0x0f);
      val = FromBytes(data[1], data[2]);
      return true;
    }
    #endregion

    #region Command Helpers

    /// <summary>
    /// Set/toggle the digital pin reporting for a certain port.
    /// </summary>
    /// <returns>
    /// The formatted command sequence.
    /// </returns>
    /// <param name='state'>
    /// 1=reporting enabled, 0=reporting disabled.
    /// </param>
    /// <param name='port'>
    /// Port to set.
    /// </param>
    public static byte[] ReportDigitalCommand(int state, int port) {
      byte[] cmd = { (byte)(Command.REPORT_DIGITAL | port), (byte) state};
      return cmd;
    }

    /// <summary>
    /// Set/toggle the analog pin reporting for a certain pin.
    /// </summary>
    /// <returns>
    /// The formatted command sequence.
    /// </returns>
    /// <param name='state'>
    /// 1=reporting enabled, 0=reporting disabled.
    /// </param>
    /// <param name='port'>
    /// Pin to set.
    /// </param>
    public static byte[] ReportAnalogCommand(int state, int pin) {
      byte[] cmd = {(byte)(Command.REPORT_ANALOG | pin), (byte) state};
      return cmd;
    }

    public static byte[] SetPinMode(int pin, PinMode mode) {
      byte[] cmd = { (byte) Command.SET_PIN_MODE, (byte) pin, (byte) mode };
      return cmd;
    }

    public static byte[] SystemReset() {
      return new byte[] {
        Command.SYSTEM_RESET
      };
    }

    public static byte[] SysexCommand( byte sysex_command, int[] command_data=null ) {
      int size = command_data!=null ? 3+2*command_data.Length : 3;
      byte[] cmd = new byte[size];
      cmd[0] = Command.SYSEX_START;
      cmd[1] = sysex_command;
      if (command_data!=null) {
        for (int i=2; i<size-1; i+=2) {
          cmd[i] = LSB(command_data[i-2]);
          cmd[i+1] = MSB(command_data[i-2]);
        }
      }
      cmd[size-1] = Command.SYSEX_END;
      return cmd;
    }

    public static byte[] RequestFirmwareInformation() {
      return new byte[] {
        Command.SYSEX_START,
        Command.REPORT_FIRMWARE,
        Command.SYSEX_END
      };
    }

    public static byte[] RequestFirmwareVersion() {
      return new byte[] {
        Command.REPORT_VERSION,
      };
    }

    public static byte[] RequestCapabilityreport() {
      return new byte [] {
        Command.SYSEX_START,
        Command.CAPABILITY_QUERY,
        Command.SYSEX_END
      };
    }

    public static byte[] RequestAnalogMapping() {
      return new byte[] {
        Command.SYSEX_START,
        Command.ANALOG_MAPPING_QUERY,
        Command.SYSEX_END
      };
    }

    public static byte[] RequestPinState() {
      return new byte[] {
        Command.SYSEX_START,
        Command.PIN_STATE_QUERY,
        Command.SYSEX_END
      };
    }

    public static byte[] SetSamplingInterval (int interval) {
      return SysexCommand(Command.SAMPLING_INTERVAL, new int[]{ interval });
    }

    public static byte[] ServoConfig (int pin, int minPulse, int maxPulse) {
      return SysexCommand(Command.SERVO_CONFIG, new int[] { minPulse, maxPulse });
    }

    public static byte[] I2CConfig(int delayMicroseconds) {
      return SysexCommand( Command.I2C_CONFIG, new int[] { delayMicroseconds });
    }

    /* I2C read/write request
     * -------------------------------
     * 0  START_SYSEX (0xF0) (MIDI System Exclusive)
     * 1  I2C_REQUEST (0x76)
     * 2  slave address (LSB)
     * 3  slave address (MSB) + read/write and address mode bits
          {7: always 0} + {6: reserved} + {5: address mode, 1 means 10-bit mode} +
          {4-3: read/write, 00 => write, 01 => read once, 10 => read continuously, 11 => stop reading} +
          {2-0: slave address MSB in 10-bit mode, not used in 7-bit mode}
     * 4  data 0 (LSB)
     * 5  data 0 (MSB)
     * 6  data 1 (LSB)
     * 7  data 1 (MSB)
     * ...
     * n  END_SYSEX (0xF7)
     */
    public static byte[] I2CRequest(int slaveAddress, int[] data=null, I2CMode mode=I2CMode.READ) {
      int size = (data!=null ? data.Length : 0 ) + 1;
      int[] _data = new int[size];

      slaveAddress &= 0x3FFF; // Use only 14 bits
      slaveAddress = (mode & I2CMode.TENBIT)>0 ? slaveAddress & 0x03FF : slaveAddress & 0x007F;

      slaveAddress |= (int) mode << 7;
      _data[0] = slaveAddress;

      if(data!=null) {
        for(int i=1; i < size; i++) {
          _data[i] = data[i-1];
        }
      }

      return SysexCommand(Command.I2C_REQUEST, _data);
    }

    #endregion

    #region Byte utils

    /// <summary>
    /// Get the integer value that was sent using the 7-bit messages of the firmata protocol
    /// </summary>
    public static int FromBytes(byte LSB, byte MSB) {
      return ((MSB & 0x7F) << 7) | (LSB & 0x7F);
    }

    /// <summary>
    /// Retrieve the least significant byte.
    /// </summary>
    /// <param name='val'>
    /// Value to convert.
    /// </param>
    public static byte LSB(int val) {
      return (byte)( val & 0x7F );
    }

    /// <summary>
    /// Retrieve the most significant byte.
    /// </summary>
    /// <param name='val'>
    /// Value to convert.
    /// </param>
    public static byte MSB(int val) {
      return (byte)((val >> 7) & 0x7F);
    }

    /// <summary>
    /// Split an integer value to two 7-bit parts so it can be sent using the firmata protocol
    /// </summary>
    public static void ToBytes(int val, out byte _LSB, out byte _MSB) {
      _LSB = LSB(val);
      _MSB = MSB(val);
    }

    /// <summary>
    /// Send an array of boolean values indicating the state of each individual
    /// pin and get a byte representing a port
    /// </summary>
    public static int ValuesToPortState(int[] pins) {
      int state = 0x0000;
      for (int i = 0; i < pins.Length; i++) {
        state |= (pins[i] & 0x01) << i;
      }
      return state;
    }

    /// <summary>
    /// Convert a port state to an array of integers.
    /// </summary>
    /// <returns>
    /// Returns the decoded array.
    /// </returns>
    /// <param name='state'>
    /// The port state.
    /// </param>
    public static int[] ValuesFromPortState(int state) {
      int[] port = new int[Constants.BitsPerPort];
      for (int i = 0; i < Constants.BitsPerPort; i++) {
        port[i] = (state>>i) & 0x01;
      }
      return port;
    }

    #endregion

    public static string PinModeToString(PinMode mode) {
      string s = "";
            switch(mode) {
              case PinMode.INPUT:   s="INPUT";  break;
              case PinMode.OUTPUT:  s="OUTPUT"; break;
              case PinMode.ANALOG:  s="ANALOG"; break;
              case PinMode.PWM:     s="PWM";    break;
              case PinMode.SERVO:   s="SERVO";  break;
              case PinMode.SHIFT:   s="SHIFT";  break;
              case PinMode.I2C:     s="I2C";    break;
            }
      return s;
    }

    public static string CommandBufferToString(Queue<byte> CommandBuffer, string Glue = "\r\n")
    {
      string s = string.Empty;
      while (CommandBuffer.Count > 0) {
        byte b = CommandBuffer.Dequeue();
        int currentSize = s.Length;
        // The Command Switch:
        switch(Util.GetCommand(b)) {
          case Command.SYSEX_START:
            s+="Sysex (";
            byte sb = CommandBuffer.Dequeue();
            while(sb != Command.SYSEX_END) {
              switch(sb){
                case Command.SAMPLING_INTERVAL:
                  s+="SamplingInterval: ";
                  byte _LSB = CommandBuffer.Dequeue();
                  byte _MSB = CommandBuffer.Dequeue();
                  s+=Util.FromBytes(_MSB,_LSB).ToString();
                  break;
                case Command.EXTENDED_ANALOG:
                  int pin  = CommandBuffer.Dequeue() & 0x7f;
                  s+="Extended Analog Message for pin ";
                  s+=pin.ToString() + ": ";
                  s+=Util.FromBytes(CommandBuffer.Dequeue(),CommandBuffer.Dequeue()).ToString();
                  break;
                case Command.REPORT_FIRMWARE:
                  s+="ReportFirmwareVersion";
                  break;
                default:
                  s+= String.Format("{0:X}", sb);
                break;
              }
              sb = CommandBuffer.Dequeue();
            }
            s+=")";
            break;

          case Command.SYSTEM_RESET:
            s+="Reset!";
            break;

          case Command.SET_PIN_MODE:
            s+="Set PinMode of pin ";
            s+=CommandBuffer.Dequeue().ToString();
            s+=" to ";
            s+=PinModeToString((PinMode)CommandBuffer.Dequeue());
            break;

          case Command.REPORT_DIGITAL:
            s+="DIGITAL Pin Reporting for port ";
            s+=(b&0x0f).ToString();
            s+=" set to: ";
            s+=CommandBuffer.Dequeue().ToString();
            break;

          case Command.REPORT_ANALOG:
            s+="ANALOG Pin Reporting for pin ";
            s+=(b&0x0f).ToString();
            s+=" set to: ";
            s+=CommandBuffer.Dequeue().ToString();
            break;

          case Command.DIGITAL_MESSAGE:
            s+="Digital message for port ";
            s+=(b&0x0f).ToString();
            s+=": ";
            s+=Convert.ToString(CommandBuffer.Dequeue(), 2);
            break;

          case Command.ANALOG_MESSAGE:
            s+="Analog message for pin ";
            s+=(b&0x0f).ToString();
            s+=": ";
            s+=Util.FromBytes(CommandBuffer.Dequeue(),CommandBuffer.Dequeue()).ToString();
            break;
        }
        if (s.Length != currentSize) s+=Glue;
      }
      return s;
    }
  }

}