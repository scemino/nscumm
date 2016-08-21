//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core;
using NScumm.Core.Audio.OPL;
using System;

namespace NScumm.Sky.Music
{
	class AdLibChannel : IChannelBase
	{
		public AdLibChannel (IOpl opl, byte[] musicData, ushort startOfData)
		{
			_opl = opl;
			_musicData = musicData;
			_channelData.loopPoint = startOfData;
			_channelData.eventDataPtr = startOfData;
			_channelData.channelActive = true;
			_channelData.tremoVibro = 0;
			_channelData.assignedInstrument = 0xFF;
			_channelData.channelVolume = 0x7F;
			_channelData.nextEventTime = GetNextEventTime ();

			_channelData.adlibChannelNumber = _channelData.lastCommand = _channelData.note =
            _channelData.adlibReg1 = _channelData.adlibReg2 = _channelData.freqOffset = 0;
			_channelData.frequency = 0;
			_channelData.instrumentData = null;

			ushort instrumentDataLoc;

			if (SystemVars.Instance.GameVersion.Version.Minor == 109) {
				//instrumentDataLoc = (_musicData[0x11D0] << 8) | _musicData[0x11CF];
				//_frequenceTable = (uint16 *)(_musicData + 0x835);
				//_registerTable = _musicData + 0xE35;
				//_opOutputTable = _musicData + 0xE47;
				//_adlibRegMirror = _musicData + 0xF4A;

				instrumentDataLoc = _musicData.ToUInt16 (0x1204);
				_frequenceTable = new UShortAccess (_musicData, 0x868);
				_registerTable = new ByteAccess (_musicData, 0xE68);
				_opOutputTable = new ByteAccess (_musicData, 0xE7A);
				_adlibRegMirror = new ByteAccess (_musicData, 0xF7D);
			} else if (SystemVars.Instance.GameVersion.Version.Minor == 267) {
				instrumentDataLoc = _musicData.ToUInt16 (0x11FB);
				_frequenceTable = new UShortAccess (_musicData, 0x7F4);
				_registerTable = new ByteAccess (_musicData, 0xDF4);
				_opOutputTable = new ByteAccess (_musicData, 0xE06);
				_adlibRegMirror = new ByteAccess (_musicData, 0xF55);
			} else {
				instrumentDataLoc = _musicData.ToUInt16 (0x1205);
				_frequenceTable = new UShortAccess (_musicData, 0x7FE);
				_registerTable = new ByteAccess (_musicData, 0xDFE);
				_opOutputTable = new ByteAccess (_musicData, 0xE10);
				_adlibRegMirror = new ByteAccess (_musicData, 0xF5F);
			}

			_instrumentMap = new ByteAccess (_musicData, instrumentDataLoc);
			_instruments = new StructAccess<InstrumentStruct> (_musicData, instrumentDataLoc + 0x80, InstrumentStruct.Size, (d,o) => new InstrumentStruct(d,o) );
		}

		public void Dispose ()
		{
			StopNote ();
		}


		public bool IsActive {
			get { return _channelData.channelActive; }
		}

		public void UpdateVolume (ushort volume)
		{
			// Do nothing. The mixer handles the music volume for us.
		}

		/// <summary>
		/// This class uses the same area for the register mirror as the original
		/// asm driver did(_musicData[0xF5F..0x105E]), so the cache is indeed shared
		/// by all instances of the class.
		/// </summary>
		/// <param name="regNum"></param>
		/// <param name="value"></param>
		void SetRegister (byte regNum, byte value)
		{
			if (_adlibRegMirror [regNum] != value) {
				_opl.WriteReg (regNum, value);
				_adlibRegMirror [regNum] = value;
			}
		}

		void StopNote ()
		{
			if ((_channelData.note & 0x20) != 0) {
				unchecked {
					_channelData.note &= (byte)~0x20;
				}
				SetRegister ((byte)(0xB0 | _channelData.adlibChannelNumber), _channelData.note);
			}
		}

		int GetNextEventTime ()
		{
			int retV = 0;
			byte cnt, lVal = 0;
			for (cnt = 0; cnt < 4; cnt++) {
				lVal = _musicData [_channelData.eventDataPtr];
				_channelData.eventDataPtr++;
				retV = (retV << 7) | (lVal & 0x7F);
				if ((lVal & 0x80) == 0)
					break;
			}
			if ((lVal & 0x80) != 0) {
				return -1; // should never happen
			} else
				return retV;
		}

		public byte Process (ushort aktTime)
		{
			if (!_channelData.channelActive) {
				return 0;
			}

			byte returnVal = 0;

			_channelData.nextEventTime -= aktTime;
			byte opcode;
			while ((_channelData.nextEventTime < 0) && (_channelData.channelActive)) {
				opcode = _musicData [_channelData.eventDataPtr];
				_channelData.eventDataPtr++;
				if ((opcode & 0x80) != 0) {
					if (opcode == 0xFF) {
						// dummy opcode
					} else if (opcode >= 0x90) {
						switch (opcode & 0xF) {
						case 0:
							com90_caseNoteOff ();
							break;
						case 1:
							com90_stopChannel ();
							break;
						case 2:
							com90_setupInstrument ();
							break;
						case 3:
							returnVal = com90_updateTempo ();
							break;
						case 5:
							com90_getFreqOffset ();
							break;
						case 6:
							com90_getChannelVolume ();
							break;
						case 7:
							com90_getTremoVibro ();
							break;
						case 8:
							com90_loopMusic ();
							break;
						case 9:
							com90_keyOff ();
							break;
						case 12:
							com90_setLoopPoint ();
							break;

						default:
							throw new InvalidOperationException (string.Format ("AdLibChannel: Unknown music opcode 0x{0:X2}", opcode));
						}
					} else {
						// new adlib channel assignment
						_channelData.adlibChannelNumber = (byte)(opcode & 0xF);
						_channelData.adlibReg1 = _registerTable [((opcode & 0xF) << 1) | 0];
						_channelData.adlibReg2 = _registerTable [((opcode & 0xF) << 1) | 1];
					}
				} else {
					_channelData.lastCommand = opcode;
					StopNote ();
					// not sure why this "if" is necessary...either a bug in my
					// code or a bug in the music data (section 1, music 2)
					if (_channelData.instrumentData != null || _channelData.tremoVibro != 0) {
						SetupInstrument (opcode);

						opcode = _musicData [_channelData.eventDataPtr];
						_channelData.eventDataPtr++;
						SetupChannelVolume (opcode);
					} else
						_channelData.eventDataPtr++;
				}
				if (_channelData.channelActive)
					_channelData.nextEventTime += GetNextEventTime ();
			}
			return returnVal;
		}

		void SetupInstrument (byte opcode)
		{
			ushort nextNote;
			if (_channelData.tremoVibro != 0) {
				byte newInstrument = _instrumentMap [opcode];
				if (newInstrument != _channelData.assignedInstrument) {
					_channelData.assignedInstrument = newInstrument;
					_channelData.instrumentData = _instruments [newInstrument];
					AdlibSetupInstrument ();
				}
				_channelData.lastCommand = _channelData.instrumentData.bindedEffect;
				nextNote = GetNextNote (_channelData.lastCommand);
			} else {
				nextNote = GetNextNote ((byte)(opcode - 0x18 + _channelData.instrumentData.bindedEffect));
			}
			_channelData.frequency = nextNote;
			SetRegister ((byte)(0xA0 | _channelData.adlibChannelNumber), (byte)nextNote);
			SetRegister ((byte)(0xB0 | _channelData.adlibChannelNumber), (byte)((nextNote >> 8) | 0x20));
			_channelData.note = (byte)((nextNote >> 8) | 0x20);
		}

		void SetupChannelVolume (byte volume)
		{
			byte resultOp;
			int resVol = ((volume + 1) * (_channelData.instrumentData.totOutLev_Op2 + 1)) << 1;
			resVol &= 0xFFFF;
			resVol *= (_channelData.channelVolume + 1) << 1;
			resVol >>= 16;
			System.Diagnostics.Debug.Assert (resVol < 0x81);
			resultOp = (byte)(((_channelData.instrumentData.scalingLevel << 6) & 0xC0) | _opOutputTable [resVol]);
			SetRegister ((byte)(0x40 | _channelData.adlibReg2), resultOp);
			if ((_channelData.instrumentData.feedBack & 1) != 0) {
				resVol = ((volume + 1) * (_channelData.instrumentData.totOutLev_Op1 + 1)) << 1;
				resVol &= 0xFFFF;
				resVol *= (_channelData.channelVolume + 1) << 1;
				resVol >>= 16;
			} else
				resVol = _channelData.instrumentData.totOutLev_Op1;
			System.Diagnostics.Debug.Assert (resVol < 0x81);
			resultOp = (byte)(((_channelData.instrumentData.scalingLevel << 2) & 0xC0) | _opOutputTable [resVol]);
			SetRegister ((byte)(0x40 | _channelData.adlibReg1), resultOp);
		}

		void AdlibSetupInstrument ()
		{
			SetRegister ((byte)(0x60 | _channelData.adlibReg1), _channelData.instrumentData.ad_Op1);
			SetRegister ((byte)(0x60 | _channelData.adlibReg2), _channelData.instrumentData.ad_Op2);
			SetRegister ((byte)(0x80 | _channelData.adlibReg1), _channelData.instrumentData.sr_Op1);
			SetRegister ((byte)(0x80 | _channelData.adlibReg2), _channelData.instrumentData.sr_Op2);
			SetRegister ((byte)(0xE0 | _channelData.adlibReg1), _channelData.instrumentData.waveSelect_Op1);
			SetRegister ((byte)(0xE0 | _channelData.adlibReg2), _channelData.instrumentData.waveSelect_Op2);
			SetRegister ((byte)(0xC0 | _channelData.adlibChannelNumber), _channelData.instrumentData.feedBack);
			SetRegister ((byte)(0x20 | _channelData.adlibReg1), _channelData.instrumentData.ampMod_Op1);
			SetRegister ((byte)(0x20 | _channelData.adlibReg2), _channelData.instrumentData.ampMod_Op2);
		}

		ushort GetNextNote (byte param)
		{
			short freqIndex = (short)((_channelData.freqOffset) - 0x40);
			if (freqIndex >= 0x3F)
				freqIndex++;
			freqIndex *= 2;
			freqIndex += (short)(param << 6);
			ushort freqData = _frequenceTable [freqIndex % 0x300];
			if ((freqIndex % 0x300 >= 0x1C0) || (freqIndex / 0x300 > 0)) {
				return (ushort)((((freqIndex / 0x300) - 1) << 10) + (freqData & 0x7FF));
			} else {
				// looks like a bug. dunno why. It's what the ASM code says.
				return (ushort)((freqData) >> 1);
			}
		}


		//- command 90h routines

		void com90_caseNoteOff ()
		{
			if (_musicData [_channelData.eventDataPtr] == _channelData.lastCommand)
				StopNote ();
			_channelData.eventDataPtr++;
		}

		void com90_stopChannel ()
		{
			StopNote ();
			_channelData.channelActive = false;
		}

		void com90_setupInstrument ()
		{
			_channelData.channelVolume = 0x7F;
			_channelData.freqOffset = 0x40;
			_channelData.assignedInstrument = _musicData [_channelData.eventDataPtr];
			_channelData.eventDataPtr++;
			_channelData.instrumentData = _instruments [_channelData.assignedInstrument];
			AdlibSetupInstrument ();
		}

		byte com90_updateTempo ()
		{
			return _musicData [_channelData.eventDataPtr++];
		}

		void com90_getFreqOffset ()
		{
			_channelData.freqOffset = _musicData [_channelData.eventDataPtr++];
			if ((_channelData.note & 0x20) != 0) {
				ushort nextNote = GetNextNote ((byte)(_channelData.lastCommand - 0x18 + _channelData.instrumentData.bindedEffect));
				SetRegister ((byte)(0xA0 | _channelData.adlibChannelNumber), (byte)nextNote);
				SetRegister ((byte)(0xB0 | _channelData.adlibChannelNumber), (byte)((nextNote >> 8) | 0x20));
				_channelData.note = (byte)((nextNote >> 8) | 0x20);
			}
		}

		void com90_getChannelVolume ()
		{
			_channelData.channelVolume = _musicData [_channelData.eventDataPtr++];
		}

		void com90_getTremoVibro ()
		{
			_channelData.tremoVibro = _musicData [_channelData.eventDataPtr++];
		}

		void com90_loopMusic ()
		{
			_channelData.eventDataPtr = _channelData.loopPoint;
		}

		void com90_keyOff ()
		{
			StopNote ();
		}

		void com90_setLoopPoint ()
		{
			_channelData.loopPoint = _channelData.eventDataPtr;
		}

		IOpl _opl;
		byte[] _musicData;
		AdLibChannelType _channelData;
		StructAccess<InstrumentStruct> _instruments;
		UShortAccess _frequenceTable;
		ByteAccess _instrumentMap;
		ByteAccess _registerTable, _opOutputTable;
		ByteAccess _adlibRegMirror;
	}


	class InstrumentStruct
	{
		public const int Size = 16;

		public byte ad_Op1 {
			get { return Data [Offset]; }
			set { Data [Offset] = value; }
		}

		public byte ad_Op2 {
			get { return Data [Offset + 1]; }
			set { Data [Offset + 1] = value; }
		}

		public byte sr_Op1 {
			get { return Data [Offset + 2]; }
			set { Data [Offset + 2] = value; }
		}

		public byte sr_Op2 {
			get { return Data [Offset + 3]; }
			set { Data [Offset + 3] = value; }
		}

		public byte ampMod_Op1 {
			get { return Data [Offset + 4]; }
			set { Data [Offset + 4] = value; }
		}

		public byte ampMod_Op2 {
			get { return Data [Offset + 5]; }
			set { Data [Offset + 5] = value; }
		}

		public byte waveSelect_Op1 {
			get { return Data [Offset + 6]; }
			set { Data [Offset + 6] = value; }
		}

		public byte waveSelect_Op2 {
			get { return Data [Offset + 7]; }
			set { Data [Offset + 7] = value; }
		}

		public byte bindedEffect {
			get { return Data [Offset + 8]; }
			set { Data [Offset + 8] = value; }
		}

		public byte feedBack {
			get { return Data [Offset + 9]; }
			set { Data [Offset + 9] = value; }
		}

		public byte totOutLev_Op1 {
			get { return Data [Offset + 10]; }
			set { Data [Offset + 10] = value; }
		}

		public byte totOutLev_Op2 {
			get { return Data [Offset + 11]; }
			set { Data [Offset + 11] = value; }
		}

		public byte scalingLevel {
			get { return Data [Offset + 12]; }
			set { Data [Offset + 12] = value; }
		}

		public byte pad1 {
			get { return Data [Offset + 13]; }
			set { Data [Offset + 13] = value; }
		}

		public byte pad2 {
			get { return Data [Offset + 14]; }
			set { Data [Offset + 14] = value; }
		}

		public byte pad3 {
			get { return Data [Offset + 15]; }
			set { Data [Offset + 15] = value; }
		}

		public byte[] Data { get; }

		public int Offset { get; }

		public InstrumentStruct (byte[] data, int offset)
		{
			Data = data;
			Offset = offset;
		}
	}

	struct AdLibChannelType
	{
		public ushort eventDataPtr;
		public int nextEventTime;
		public ushort loopPoint;
		public byte adlibChannelNumber;
		public byte lastCommand;
		public bool channelActive;
		public byte note;
		public byte adlibReg1, adlibReg2;
		public InstrumentStruct instrumentData;
		public byte assignedInstrument;
		public byte channelVolume;
		public byte padding;
		// field_12 / not used by original driver
		public byte tremoVibro;
		public byte freqOffset;
		public ushort frequency;
	}
}
