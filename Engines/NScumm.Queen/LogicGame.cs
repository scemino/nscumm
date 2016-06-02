//
//  QueenEngine.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Queen
{
	enum JoeWalkMode
	{
		NORMAL = 0,
		MOVE = 1,
		EXECUTE = 2,
		SPEAK = 3
	}

	public enum RoomDisplayMode
	{
		RDM_FADE_NOJOE = 0,
		// fade in, hide Joe
		RDM_FADE_JOE = 1,
		// fade in, display Joe
		RDM_NOFADE_JOE = 2,
		// screen does not dissolve into view
		RDM_FADE_JOE_XY = 3
		// display Joe at the current X, Y coords
	}

	class Joe
	{
		public ushort x, y;
		public ushort facing, cutFacing, prevFacing;
		public JoeWalkMode walk;
		public ushort scale;
	}

	static class Jso
	{
		public const int JSO_OBJECT_DESCRIPTION = 0;
		public const int JSO_OBJECT_NAME = 1;
		public const int JSO_ROOM_NAME = 2;
		public const int JSO_VERB_NAME = 3;
		public const int JSO_JOE_RESPONSE = 4;
		public const int JSO_ACTOR_ANIM = 5;
		public const int JSO_ACTOR_NAME = 6;
		public const int JSO_ACTOR_FILE = 7;
		public const int JSO_COUNT = 8;
	}

	public abstract class Logic
	{
		const int JOE_RESPONSE_MAX = 40;
		const int GAME_STATE_COUNT = 211;

		protected QueenEngine _vm;
		Joe _joe;
		int _puzzleAttemptCount;
		Journal _journal;
		ushort _numRooms;
		ushort _numNames;
		ushort _numObjects;
		ushort _numDescriptions;
		ushort[] _roomData;
		ushort[] _sfxName;
		ushort _numItems;
		ItemData[] _itemData;
		ushort _numGraphics;
		GraphicData[] _graphicData;
		ushort _numWalkOffs;
		WalkOffData[] _walkOffData;
		ushort _numObjDesc;
		ObjectDescription[] _objectDescription;
		ushort _numFurniture;
		FurnitureData[] _furnitureData;
		ushort _numActors;
		ushort _numAAnim;
		ushort _numAName;
		ushort _numAFile;
		ActorData[] _actorData;
		ushort _numGraphicAnim;
		GraphicAnim[] _graphicAnim;
		System.Collections.Generic.List<string> _jasStringList;
		int[] _jasStringOffset;

		Item[] _inventoryItem = new Item[4];

		Credits _credits;

		/// <summary>
		/// Cutscene counter.
		/// </summary>
		int _scene;

		public ObjectData[] ObjectData { get; private set; }

		public ushort EntryObj{ get; set; }

		public short[] GameState{ get; private set; }

		public ushort NewRoom {
			get;
			set;
		}

		public ushort OldRoom {
			get;
			set;
		}

		public ushort CurrentRoom {
			get;
			set;
		}

		public ushort[] RoomData { get { return _roomData; } }

		public ushort CurrentRoomData { get { return _roomData [CurrentRoom]; } }

		public GraphicAnim[] GraphicAnim { get { return _graphicAnim; } }

		public ushort GraphicAnimCount { get { return _numGraphicAnim; } }

		public ushort JoeX { get { return _joe.x; } }

		public ushort JoeY { get { return _joe.y; } }

		private ushort JoePrevFacing { 
			get { return _joe.prevFacing; }
			set { _joe.prevFacing = value; }
		}

		private ushort JoeFacing { 
			get { return _joe.facing; }
			set { _joe.facing = value; }
		}

		public void JoePos (ushort x, ushort y)
		{
			_joe.x = x;
			_joe.y = y;
		}

		public GraphicData[] GraphicData {
			get{ return _graphicData; }
		}

		protected Logic (QueenEngine vm)
		{
			_vm = vm;
			_joe = new Joe ();
			_joe.scale = 100;
			_joe.walk = JoeWalkMode.NORMAL;
			GameState = new short [GAME_STATE_COUNT];
			_puzzleAttemptCount = 0;
			_journal = new Journal (vm);
			ReadQueenJas ();
		}

		public bool InitPerson (ushort noun, string name, bool loadBank, out Person pp)
		{
			pp = null;
			ActorData pad = FindActor (noun, name);
			if (pad != null) {
				pp = new Person ();
				pp.actor = pad;
				pp.name = ActorName (pad.name);
				if (pad.anim != 0) {
					pp.anim = ActorAnim (pad.anim);
				} else {
					pp.anim = null;
				}
				if (loadBank && pad.file != 0) {
					_vm.BankMan.Load (ActorFile (pad.file), pad.bankNum);
					// if there is no valid actor file (ie pad.file is 0), the person
					// data is already loaded as it is included in objects room bank (.bbk)
				}
				pp.bobFrame = (ushort)(31 + pp.actor.bobNum);
			}
			return pad != null;
		}

		public void PlayCutaway(string cutFile, string next=null) {
			_vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);
			Cutaway.Run(cutFile, next, _vm);
		}

		string ActorFile (ushort num)
		{
			return _jasStringList [_jasStringOffset [Jso.JSO_ACTOR_FILE] + num - 1];
		}

		string ActorName (int num)
		{
			return _jasStringList [_jasStringOffset [Jso.JSO_ACTOR_NAME] + num - 1];
		}

		string ActorAnim (int num)
		{
			return _jasStringList [_jasStringOffset [Jso.JSO_ACTOR_ANIM] + num - 1];
		}

		ActorData FindActor (ushort noun, string name)
		{
			ushort obj = (ushort)(CurrentRoomData + noun);
			short img = ObjectData [obj].image;
			if (img != -3 && img != -4) {
				// TODO: warning("Logic::findActor() - Object %d is not a person", obj);
				return null;
			}

			// search Bob number for the person
			ushort bobNum = FindPersonNumber (obj, CurrentRoom);

			// search for a matching actor
			if (bobNum > 0) {
				for (ushort i = 1; i <= _numActors; ++i) {
					var pad = _actorData [i];
					if (pad.room == CurrentRoom && GameState [pad.gsSlot] == pad.gsValue) {
						if (bobNum == pad.bobNum || (name != null && string.Equals (ActorName (pad.name), name))) {
							return pad;
						}
					}
				}
			}
			return null;
		}

		private ushort FindPersonNumber (ushort obj, ushort room)
		{
			ushort num = 0;
			for (ushort i = (ushort)(_roomData [room] + 1); i <= obj; ++i) {
				short img = ObjectData [i].image;
				if (img == -3 || img == -4) {
					++num;
				}
			}
			return num;
		}

		public void SceneReset ()
		{
			_scene = 0;
		}

		public void ChangeRoom ()
		{
			if (!ChangeToSpecialRoom ())
				DisplayRoom (CurrentRoom, RoomDisplayMode.RDM_FADE_JOE, 100, 1, false);
			_vm.Display.ShowMouseCursor (true);
		}

		public void Update ()
		{
			if (_credits != null)
				_credits.Update ();

			// TODO: debugger
//				if (_vm.debugger().flags() & Debugger::DF_DRAW_AREAS) {
//					_vm.grid().drawZones();
//				}
		}

		public void Start ()
		{
			SetupSpecialMoveTable ();
			_vm.Command.Clear (false);
			_vm.Display.SetupPanel ();
			_vm.Graphics.UnpackControlBank ();
			_vm.Graphics.SetupMouseCursor ();
			SetupJoe ();
			_vm.Grid.SetupPanel ();
			InventorySetup ();

			OldRoom = 0;
			NewRoom = CurrentRoom;
		}

		protected void InventoryRefresh ()
		{
			ushort x = 182;
			for (int i = 0; i < 4; ++i) {
				ushort itemNum = (ushort)_inventoryItem [i];
				if (itemNum != 0) {
					ushort dstFrame = (ushort)((i == 0) ? 8 : 9);
					// unpack frame for object and draw it
					_vm.BankMan.Unpack (_itemData [itemNum].frame, dstFrame, 14);
					_vm.Graphics.DrawInventoryItem (dstFrame, x, 14);
				} else {
					// no object, clear the panel
					_vm.Graphics.DrawInventoryItem (0, x, 14);
				}
				x += 35;
			}
		}

		protected void DisplayRoom (ushort room, RoomDisplayMode mode, ushort scale, int comPanel, bool inCutaway)
		{
			// TODO: debug(6, "Logic::displayRoom(%d, %d, %d, %d, %d)", room, mode, scale, comPanel, inCutaway);

			EraseRoom ();

			if (_credits != null)
				_credits.NextRoom ();

			SetupRoom (RoomName (room), comPanel, inCutaway);
			if (mode != RoomDisplayMode.RDM_FADE_NOJOE) {
				SetupJoeInRoom (mode != RoomDisplayMode.RDM_FADE_JOE_XY, scale);
			}
			if (mode != RoomDisplayMode.RDM_NOFADE_JOE) {
				_vm.Update ();
				var joe = _vm.Graphics.Bobs [0];
				_vm.Display.PalFadeIn (CurrentRoom, joe.active, joe.x, joe.y);
			}
			if (mode != RoomDisplayMode.RDM_FADE_NOJOE && JoeX != 0 && JoeY != 0) {
				ushort jx = JoeX;
				ushort jy = JoeY;
				JoePos (0, 0);
				_vm.Walk.MoveJoe (0, jx, jy, inCutaway);
			}
		}

		protected abstract bool ChangeToSpecialRoom ();

		private void SetupJoeInRoom (bool b, ushort scale)
		{
			throw new NotImplementedException ();
		}

		private string RoomName (ushort roomNum)
		{
			//assert(roomNum >= 1 && roomNum <= _numRooms);
			return _jasStringList [_jasStringOffset [Jso.JSO_ROOM_NAME] + roomNum - 1];
		}

		private void SetupRoom (string room, int comPanel, bool inCutaway)
		{
			// load backdrop image, init dynalum, setup colors
			_vm.Display.SetupNewRoom (room, CurrentRoom);

			// setup graphics to enter fullscreen/panel mode
			_vm.Display.ScreenMode (comPanel, inCutaway);

			_vm.Grid.SetupNewRoom (CurrentRoom, _roomData [CurrentRoom]);

			short[] furn = new short[9];
			ushort furnTot = 0;
			for (ushort i = 1; i <= _numFurniture; ++i) {
				if (_furnitureData [i].room == CurrentRoom) {
					++furnTot;
					furn [furnTot] = _furnitureData [i].objNum;
				}
			}
			_vm.Graphics.SetupNewRoom (room, CurrentRoom, furn, furnTot);

			_vm.Display.ForceFullRefresh ();
		}

		private void EraseRoom ()
		{
			_vm.BankMan.EraseFrames (false);
			_vm.BankMan.Close (15);
			_vm.BankMan.Close (11);
			_vm.BankMan.Close (10);
			_vm.BankMan.Close (12);

			_vm.Display.PalFadeOut (CurrentRoom);

			// invalidates all persons animations
			_vm.Graphics.ClearPersonFrames ();
			_vm.Graphics.EraseAllAnims ();

			ushort cur = (ushort)(_roomData [OldRoom] + 1);
			ushort last = _roomData [OldRoom + 1];
			for (; cur <= last; ++cur) {
				var pod = ObjectData [cur];
				if (pod.name == 0) {
					// object has been deleted, invalidate image
					pod.image = 0;
				} else if (pod.image > -4000 && pod.image <= -10) {
					if (_graphicData [Math.Abs (pod.image + 10)].lastFrame == 0) {
						// static Bob
						pod.image = -1;
					} else {
						// animated Bob
						pod.image = -2;
					}
				}
			}
		}

		private void SetupJoe ()
		{
			LoadJoeBanks ("JOE_A.BBK", "JOE_B.BBK");
			JoePrevFacing = Defines.DIR_FRONT;
			JoeFacing = Defines.DIR_FRONT;
		}

		private void LoadJoeBanks (string animBank, string standBank)
		{
			_vm.BankMan.Load (animBank, 13);
			for (uint i = 11; i < 31; ++i) {
				_vm.BankMan.Unpack ((uint)(i - 10), i, 13);
			}
			_vm.BankMan.Close (13);

			_vm.BankMan.Load (standBank, 7);
			_vm.BankMan.Unpack (1, 35, 7);
			_vm.BankMan.Unpack (3, 36, 7);
			_vm.BankMan.Unpack (5, 37, 7);
		}

		private void InventorySetup ()
		{
			_vm.BankMan.Load ("OBJECTS.BBK", 14);
			if (_vm.Resource.IsInterview) {
				_inventoryItem [0] = (Item)1;
				_inventoryItem [1] = (Item)2;
				_inventoryItem [2] = (Item)3;
				_inventoryItem [3] = (Item)4;
			} else {
				_inventoryItem [0] = Item.ITEM_BAT;
				_inventoryItem [1] = Item.ITEM_JOURNAL;
				_inventoryItem [2] = Item.ITEM_NONE;
				_inventoryItem [3] = Item.ITEM_NONE;
			}
		}

		private void ReadQueenJas ()
		{
			short i;

			uint size;
			var jas = _vm.Resource.LoadFile ("QUEEN.JAS", 20, out size);
			var ptr = 0;

			_numRooms = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_numNames = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_numObjects = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_numDescriptions = jas.ToUInt16BigEndian (ptr);
			ptr += 2;

			ObjectData = new ObjectData[_numObjects + 1];
			for (i = 1; i <= _numObjects; i++) {
				ObjectData [i] = new ObjectData ();
				ObjectData [i].ReadFromBE (jas, ref ptr);
			}

			_roomData = new ushort[_numRooms + 2];
			_roomData [0] = 0;
			for (i = 1; i <= (_numRooms + 1); i++) {
				_roomData [i] = jas.ToUInt16BigEndian (ptr);
				ptr += 2;
			}
			_roomData [_numRooms + 1] = _numObjects;

			if ((_vm.Resource.IsDemo && _vm.Resource.Platform == Platform.DOS) ||
			    (_vm.Resource.IsInterview && _vm.Resource.Platform == Platform.Amiga)) {
				_sfxName = null;
			} else {
				_sfxName = new ushort[_numRooms + 1];
				_sfxName [0] = 0;
				for (i = 1; i <= _numRooms; i++) {
					_sfxName [i] = jas.ToUInt16BigEndian (ptr);
					ptr += 2;
				}
			}

			_numItems = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_itemData = new ItemData[_numItems + 1];
			for (i = 1; i <= _numItems; i++) {
				_itemData [i] = new ItemData ();
				_itemData [i].ReadFromBE (jas, ref ptr);
			}

			_numGraphics = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_graphicData = new GraphicData[_numGraphics + 1];
			for (i = 1; i <= _numGraphics; i++) {
				_graphicData [i] = new GraphicData ();
				_graphicData [i].ReadFromBE (jas, ref ptr);
			}

			_vm.Grid.ReadDataFrom (_numObjects, _numRooms, jas, ref ptr);

			_numWalkOffs = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_walkOffData = new WalkOffData[_numWalkOffs + 1];
			for (i = 1; i <= _numWalkOffs; i++) {
				_walkOffData [i] = new WalkOffData ();
				_walkOffData [i].readFromBE (jas, ref ptr);
			}

			_numObjDesc = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_objectDescription = new ObjectDescription[_numObjDesc + 1];
			for (i = 1; i <= _numObjDesc; i++) {
				_objectDescription [i] = new ObjectDescription ();
				_objectDescription [i].ReadFromBE (jas, ref ptr);
			}

			_vm.Command.ReadCommandsFrom (jas, ref ptr);

			EntryObj = jas.ToUInt16BigEndian (ptr);
			ptr += 2;

			_numFurniture = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_furnitureData = new FurnitureData[_numFurniture + 1];
			for (i = 1; i <= _numFurniture; i++) {
				_furnitureData [i] = new FurnitureData ();
				_furnitureData [i].ReadFromBE (jas, ref ptr);
			}

			// Actors
			_numActors = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_numAAnim = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_numAName = jas.ToUInt16BigEndian (ptr);
			ptr += 2;
			_numAFile = jas.ToUInt16BigEndian (ptr);
			ptr += 2;

			_actorData = new ActorData[_numActors + 1];
			for (i = 1; i <= _numActors; i++) {
				_actorData [i] = new ActorData ();
				_actorData [i].ReadFromBE (jas, ref ptr);
			}

			_numGraphicAnim = jas.ToUInt16BigEndian (ptr);
			ptr += 2;

			_graphicAnim = new GraphicAnim[_numGraphicAnim + 1];
			if (_numGraphicAnim == 0) {
				_graphicAnim [0] = new GraphicAnim ();
				_graphicAnim [0].ReadFromBE (jas, ref ptr);
			} else {
				for (i = 1; i <= _numGraphicAnim; i++) {
					_graphicAnim [i] = new GraphicAnim ();
					_graphicAnim [i].ReadFromBE (jas, ref ptr);
				}
			}

			CurrentRoom = ObjectData [EntryObj].room;
			EntryObj = 0;

			if (System.Text.Encoding.UTF8.GetString (jas, ptr, 5) != _vm.Resource.JASVersion) {
				// TODO: warning ("Unexpected queen.jas file format");
			}

			_jasStringList = _vm.Resource.LoadTextFile ("QUEEN2.JAS");
			_jasStringOffset = new int[8];
			_jasStringOffset [0] = 0;
			_jasStringOffset [1] = _jasStringOffset [0] + _numDescriptions;
			_jasStringOffset [2] = _jasStringOffset [1] + _numNames;
			_jasStringOffset [3] = _jasStringOffset [2] + _numRooms;
			_jasStringOffset [4] = _jasStringOffset [3] + 12;
			_jasStringOffset [5] = _jasStringOffset [4] + JOE_RESPONSE_MAX;
			_jasStringOffset [6] = _jasStringOffset [5] + _numAAnim;
			_jasStringOffset [7] = _jasStringOffset [6] + _numAName;

			// Patch for German text bug
			if (_vm.Resource.Language == Language.DE_DEU) {
				_jasStringList [_jasStringOffset [Jso.JSO_OBJECT_DESCRIPTION] + 296 - 1] = "Es bringt nicht viel, das festzubinden.";
			}
		}

		protected abstract void SetupSpecialMoveTable ();
	}

	public class LogicGame:Logic
	{
		public LogicGame (QueenEngine vm)
			: base (vm)
		{
		}

		protected override bool ChangeToSpecialRoom ()
		{
			if (CurrentRoom == Defines.ROOM_JUNGLE_PINNACLE) {
				HandlePinnacleRoom ();
				return true;
			} else if (CurrentRoom == Defines.FOTAQ_LOGO && GameState [Defines.VAR_INTRO_PLAYED] == 0) {
				DisplayRoom (CurrentRoom, RoomDisplayMode.RDM_FADE_NOJOE, 100, 2, true);
				PlayCutaway ("COPY.CUT");
				if (_vm.HasToQuit)
					return true;
				PlayCutaway ("CLOGO.CUT");
				if (_vm.HasToQuit)
					return true;
				if (_vm.Resource.Platform != Platform.Amiga) {
					// TODO:
//					if (ConfMan.getBool("alt_intro") && _vm.Resource.IsCD) {
//						PlayCutaway("CINTR.CUT");
//					} else {
					PlayCutaway ("CDINT.CUT");
//					}
				}
				if (_vm.HasToQuit)
					return true;
				PlayCutaway ("CRED.CUT");
				if (_vm.HasToQuit)
					return true;
				_vm.Display.PalSetPanel ();
				SceneReset ();
				CurrentRoom = Defines.ROOM_HOTEL_LOBBY;
				EntryObj = 584;
				DisplayRoom (CurrentRoom, RoomDisplayMode.RDM_FADE_JOE, 100, 2, true);
				PlayCutaway ("C70D.CUT");
				GameState [Defines.VAR_INTRO_PLAYED] = 1;
				InventoryRefresh ();
				return true;
			}
			return false;
		}

		void HandlePinnacleRoom ()
		{
			throw new NotImplementedException ();
		}

		protected override void SetupSpecialMoveTable ()
		{
			// TODO: 
//			_specialMoves[2] = AsmMakeJoeUseDress;
//			_specialMoves[3] = asmMakeJoeUseNormalClothes;
//			_specialMoves[4] = asmMakeJoeUseUnderwear;
//			_specialMoves[7] = asmStartCarAnimation;       // room 74
//			_specialMoves[8] = asmStopCarAnimation;        // room 74
//			_specialMoves[9] = asmStartFightAnimation;     // room 69
//			_specialMoves[10] = asmWaitForFrankPosition;   // c69e.cut
//			_specialMoves[11] = asmMakeFrankGrowing;       // c69z.cut
//			_specialMoves[12] = asmMakeRobotGrowing;       // c69z.cut
//			_specialMoves[14] = asmEndGame;
//			_specialMoves[15] = asmPutCameraOnDino;
//			_specialMoves[16] = asmPutCameraOnJoe;
//			_specialMoves[19] = asmSetAzuraInLove;
//			_specialMoves[20] = asmPanRightFromJoe;
//			_specialMoves[21] = asmSetLightsOff;
//			_specialMoves[22] = asmSetLightsOn;
//			_specialMoves[23] = asmSetManequinAreaOn;
//			_specialMoves[24] = asmPanToJoe;
//			_specialMoves[25] = asmTurnGuardOn;
//			_specialMoves[26] = asmPanLeft320To144;
//			_specialMoves[27] = asmSmoochNoScroll;
//			_specialMoves[28] = asmMakeLightningHitPlane;
//			_specialMoves[29] = asmScaleBlimp;
//			_specialMoves[30] = asmScaleEnding;
//			_specialMoves[31] = asmWaitForCarPosition;
//			_specialMoves[33] = asmAttemptPuzzle;
//			_specialMoves[34] = asmScrollTitle;
//			if (_vm.Resource.Platform == Platform.DOS) {
//				_specialMoves[5]  = asmSwitchToDressPalette;
//				_specialMoves[6]  = asmSwitchToNormalPalette;
//				_specialMoves[13] = asmShrinkRobot;
//				_specialMoves[17] = asmAltIntroPanRight;      // cintr.cut
//				_specialMoves[18] = asmAltIntroPanLeft;       // cintr.cut
//				_specialMoves[27] = asmSmooch;
//				_specialMoves[32] = asmShakeScreen;
//				_specialMoves[34] = asmScaleTitle;
//				_specialMoves[36] = asmPanRightToHugh;
//				_specialMoves[37] = asmMakeWhiteFlash;
//				_specialMoves[38] = asmPanRightToJoeAndRita;
//				_specialMoves[39] = asmPanLeftToBomb;         // cdint.cut
//			}
		}
	}
}

