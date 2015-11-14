using System;
using NScumm.Core;

namespace NScumm.Sky
{
    partial class Logic
    {
        private void SetupMcodeTable()
        {
            _mcodeTable = new Func<uint, uint, uint, bool>[]
            {
                FnCacheChip,
                FnCacheFast,
                FnDrawScreen,
                FnAr,
                FnArAnimate,
                FnIdle,
                FnInteract,
                FnStartSub,
                FnTheyStartSub,
                FnAssignBase,
                FnDiskMouse,
                FnNormalMouse,
                FnBlankMouse,
                FnCrossMouse,
                FnCursorRight,
                FnCursorLeft,
                FnCursorDown,
                FnOpenHand,
                FnCloseHand,
                FnGetTo,
                FnSetToStand,
                FnTurnTo,
                FnArrived,
                FnLeaving,
                FnSetAlternate,
                FnAltSetAlternate,
                FnKillId,
                FnNoHuman,
                FnAddHuman,
                FnAddButtons,
                FnNoButtons,
                FnSetStop,
                FnClearStop,
                FnPointerText,
                FnQuit,
                FnSpeakMe,
                FnSpeakMeDir,
                FnSpeakWait,
                FnSpeakWaitDir,
                FnChooser,
                FnHighlight,
                FnTextKill,
                FnStopMode,
                FnWeWait,
                FnSendSync,
                FnSendFastSync,
                FnSendRequest,
                FnClearRequest,
                FnCheckRequest,
                FnStartMenu,
                FnUnhighlight,
                FnFaceId,
                FnForeground,
                FnBackground,
                FnNewBackground,
                FnSort,
                FnNoSpriteEngine,
                FnNoSpritesA6,
                FnResetId,
                FnToggleGrid,
                FnPause,
                FnRunAnimMod,
                FnSimpleMod,
                FnRunFrames,
                FnAwaitSync,
                FnIncMegaSet,
                FnDecMegaSet,
                FnSetMegaSet,
                FnMoveItems,
                FnNewList,
                FnAskThis,
                FnRandom,
                FnPersonHere,
                FnToggleMouse,
                FnMouseOn,
                FnMouseOff,
                FnFetchX,
                FnFetchY,
                FnTestList,
                FnFetchPlace,
                FnCustomJoey,
                FnSetPalette,
                FnTextModule,
                FnChangeName,
                FnMiniLoad,
                FnFlushBuffers,
                FnFlushChip,
                FnSaveCoods,
                FnPlotGrid,
                FnRemoveGrid,
                FnEyeball,
                FnCursorUp,
                FnLeaveSection,
                FnEnterSection,
                FnRestoreGame,
                FnRestartGame,
                FnNewSwingSeq,
                FnWaitSwingEnd,
                FnSkipIntroCode,
                FnBlankScreen,
                FnPrintCredit,
                FnLookAt,
                FnLincTextModule,
                FnTextKill2,
                FnSetFont,
                FnStartFx,
                FnStopFx,
                FnStartMusic,
                FnStopMusic,
                FnFadeDown,
                FnFadeUp,
                FnQuitToDos,
                FnPauseFx,
                FnUnPauseFx,
                FnPrintf
            };
        }

        private bool FnCacheChip(uint a, uint b, uint c)
        {
            _skySound.FnStopFx();
            _skyDisk.FnCacheChip(_skyCompact.FetchCptRaw((ushort)a));
            return true;
        }

        private bool FnCacheFast(uint a, uint b, uint c)
        {
            _skyDisk.FnCacheFast(_skyCompact.FetchCptRaw((ushort)a));
            return true;
        }

        private bool FnDrawScreen(uint a, uint b, uint c)
        {
            //TODO:debug(5, "Call: fnDrawScreen(%X, %X)", a, b);
            SystemVars.Instance.CurrentPalette = a;
            _skyScreen.FnDrawScreen(a, b);

            if (_scriptVariables[SCREEN] == 32)
            {
                /* workaround for script bug #786482
                    Under certain circumstances, which never got completely cleared,
                    the gardener can get stuck in an animation, waiting for a sync
                    signal from foster.
                    This is most probably caused by foster leaving the screen before
                    sending the sync.
                    To work around that, we simply send a sync to the gardener every time
                    we enter the screen. If he isn't stuck (and thus not waiting for sync)
                    it will be ignored anyways */

                // TODO: debug(1, "sending gardener sync");
                FnSendSync(ID_SC32_GARDENER, 1, 0);
            }
            return true;
        }

        private bool FnAr(uint x, uint y, uint c)
        {
            _compact.Core.downFlag = 1; // assume failure in-case logic is interupted by speech (esp Joey)

            _compact.Core.arTargetX = (ushort)x;
            _compact.Core.arTargetY = (ushort)y;
            _compact.Core.logic = L_AR; // Set to AR mode

            _compact.Core.xcood &= 0xfff8;
            _compact.Core.ycood &= 0xfff8;

            return false; // drop out of script
        }

        private bool FnArAnimate(uint a, uint b, uint c)
        {
            _compact.Core.mood = 0; // high level 'not stood still'
            _compact.Core.logic = L_AR_ANIM;
            return false; // drop out of script
        }

        private bool FnIdle(uint a, uint b, uint c)
        {
            // set the player idling
            _compact.Core.logic = 0;
            return true;
        }

        private bool FnInteract(uint targetId, uint b, uint c)
        {
            _compact.Core.mode += 4; // next level up
            _compact.Core.logic = L_SCRIPT;
            var cpt = _skyCompact.FetchCpt((ushort)targetId);

            SkyCompact.GetSub(_compact, _compact.Core.mode).Field = cpt.Core.actionScript;
            SkyCompact.GetSub(_compact, (ushort)(_compact.Core.mode + 2)).Field = 0;

            return false;
        }

        private bool FnStartSub(uint scr, uint b, uint c)
        {
            _compact.Core.mode += 4;
            SkyCompact.GetSub(_compact, _compact.Core.mode).Field = (ushort)(scr & 0xffff);
            SkyCompact.GetSub(_compact, (ushort)(_compact.Core.mode + 2)).Field = (ushort)(scr >> 16);
            return false;
        }

        private bool FnTheyStartSub(uint mega, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.mode += 4;
            SkyCompact.GetSub(cpt, cpt.Core.mode).Field = (ushort)(scr & 0xffff);
            SkyCompact.GetSub(cpt, (ushort)(cpt.Core.mode + 2)).Field = (ushort)(scr >> 16);
            return true;
        }

        private bool FnAssignBase(uint id, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            cpt.Core.mode = C_BASE_MODE;
            cpt.Core.logic = L_SCRIPT;
            cpt.Core.baseSub = (ushort)(scr & 0xffff);
            cpt.Core.baseSub_off = (ushort)(scr >> 16);
            return true;
        }

        private bool FnDiskMouse(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_DISK, 11, 11);
            return true;
        }

        private bool FnNormalMouse(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_NORMAL, 0, 0);
            return true;
        }

        private bool FnBlankMouse(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_BLANK, 0, 0);
            return true;
        }

        private bool FnCrossMouse(uint a, uint b, uint c)
        {
            if (_scriptVariables[OBJECT_HELD] != 0)
                _skyMouse.FnOpenCloseHand(false);
            else
                _skyMouse.SpriteMouse(MOUSE_CROSS, 4, 4);
            return true;
        }

        private bool FnCursorRight(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_RIGHT, 9, 4);
            return true;
        }

        private bool FnCursorLeft(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_LEFT, 0, 5);
            return true;
        }

        private bool FnCursorDown(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_DOWN, 9, 4);
            return true;
        }

        private bool FnCursorUp(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_UP, 9, 4);
            return true;
        }

        private bool FnOpenHand(uint a, uint b, uint c)
        {
            _skyMouse.FnOpenCloseHand(true);
            return true;
        }

        private bool FnCloseHand(uint a, uint b, uint c)
        {
            _skyMouse.FnOpenCloseHand(false);
            return true;
        }

        private bool FnGetTo(uint targetPlaceId, uint mode, uint c)
        {
            _compact.Core.upFlag = (ushort)mode; // save mode for action script
            _compact.Core.mode += 4; // next level up
            Compact cpt = _skyCompact.FetchCpt(_compact.Core.place);
            if (cpt == null)
            {
                // TODO: warning("can't find _compact's getToTable. Place compact is NULL");
                return false;
            }
            var raw = _skyCompact.FetchCptRaw(cpt.Core.getToTableId);
            if (raw == null)
            {
                //TODO:  warning("Place compact's getToTable is NULL");
                return false;
            }

            var getToTable = new UShortAccess(raw, 0);
            while (getToTable.Value != targetPlaceId)
                getToTable.Offset += 4;

            // get new script
            SkyCompact.GetSub(_compact, _compact.Core.mode).Field = getToTable[1];
            SkyCompact.GetSub(_compact, (ushort)(_compact.Core.mode + 2)).Field = 0;

            return false; // drop out of script
        }

        private bool FnSetToStand(uint a, uint b, uint c)
        {
            _compact.Core.mood = 1; // high level stood still

            _compact.Core.grafixProgId = _skyCompact.GetCompactElem(_compact, (ushort)(C_STAND_UP + _compact.Core.megaSet + _compact.Core.dir * 4)).Field;
            _compact.Core.grafixProgPos = 0;

            UShortAccess standList = _skyCompact.GetGrafixPtr(_compact);

            _compact.Core.offset = standList.Value; // get frames offset
            _compact.Core.logic = L_SIMPLE_MOD;
            _compact.Core.grafixProgPos++;
            SimpleAnim();
            return false; // drop out of script
        }

        private bool FnTurnTo(uint dir, uint b, uint c)
        {
            // turn compact to direction dir

            ushort curDir = _compact.Core.dir; // get current direction
            _compact.Core.dir = (ushort)(dir & 0xffff); // set new direction

            UShortAccess tt = _skyCompact.GetTurnTable(_compact, curDir);

            if (tt[(int)dir] == 0)
                return true; // keep going

            _compact.Core.turnProgId = tt[(int)dir]; // put turn program in
            _compact.Core.turnProgPos = 0;
            _compact.Core.logic = L_TURNING;

            Turn();

            return false; // drop out of script
        }

        private bool FnArrived(uint scriptVar, uint b, uint c)
        {
            _compact.Core.leaving = (ushort)(scriptVar & 0xffff);
            _scriptVariables[(int)(scriptVar / 4)]++;
            return true;
        }

        private bool FnLeaving(uint a, uint b, uint c)
        {
            _compact.Core.atWatch = 0;

            if (_compact.Core.leaving != 0)
            {
                _scriptVariables[_compact.Core.leaving / 4]--;
                _compact.Core.leaving = 0; // I shall do this only once
            }

            return true; // keep going
        }

        private bool FnSetAlternate(uint scr, uint b, uint c)
        {
            _compact.Core.alt = (ushort)(scr & 0xffff);
            _compact.Core.logic = L_ALT;
            return false;
        }

        private bool FnAltSetAlternate(uint target, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)target);
            cpt.Core.alt = (ushort)(scr & 0xffff);
            cpt.Core.logic = L_ALT;
            return false;
        }

        private bool FnKillId(uint id, uint b, uint c)
        {
            if (id != 0)
            {
                Compact cpt = _skyCompact.FetchCpt((ushort)id);
                if ((cpt.Core.status & (1 << 7)) != 0)
                    _skyGrid.RemoveObjectFromWalk(cpt);
                cpt.Core.status = 0;
            }
            return true;
        }

        private bool FnNoHuman(uint a, uint b, uint c)
        {
            if (_scriptVariables[MOUSE_STOP] == 0)
            {
                _scriptVariables[MOUSE_STATUS] &= 1;
                RunGetOff();
                FnBlankMouse(0, 0, 0);
            }
            return true;
        }

        private void RunGetOff()
        {
            uint getOff = _scriptVariables[GET_OFF];
            _scriptVariables[GET_OFF] = 0;
            if (getOff != 0)
                Script((ushort)(getOff & 0xffff), (ushort)(getOff >> 16));
        }

        private bool FnAddHuman(uint a, uint b, uint c)
        {
            return _skyMouse.FnAddHuman();
        }

        private bool FnAddButtons(uint a, uint b, uint c)
        {
            _scriptVariables[MOUSE_STATUS] |= 4;
            return true;
        }

        private bool FnNoButtons(uint a, uint b, uint c)
        {
            //remove the mouse buttons
            _scriptVariables[MOUSE_STATUS] &= 0xFFFFFFFB;
            return true;
        }

        private bool FnSetStop(uint a, uint b, uint c)
        {
            _scriptVariables[MOUSE_STOP] |= 1;
            return true;
        }

        private bool FnClearStop(uint a, uint b, uint c)
        {
            _scriptVariables[MOUSE_STOP] = 0;
            return true;
        }

        private bool FnPointerText(uint a, uint b, uint c)
        {
            _skyText.FnPointerText(a, _skyMouse.MouseX, _skyMouse.MouseY);
            return true;
        }

        private bool FnQuit(uint a, uint b, uint c)
        {
            return false;
        }

        private bool FnSpeakMe(uint targetId, uint mesgNum, uint animNum)
        {
            /* WORKAROUND for #2687172: When Mrs. Piermont is talking
               on the phone in her apartment, ignore her fnSpeakMe calls
               on other screens, as the lack of speech files for these lines
               will cause Foster's speech to be aborted if the timing is bad.
            */
            if (targetId == 0x4039 && animNum == 0x9B && _scriptVariables[SCREEN] != 38)
            {
                return false;
            }

            StdSpeak(_skyCompact.FetchCpt((ushort)targetId), mesgNum, animNum);
            return false;   //drop out of script
        }

        private bool FnSpeakMeDir(uint targetId, uint mesgNum, uint animNum)
        {
            //must be player so don't cause script to drop out
            //this function sets the directional option whereby
            //the anim chosen is linked to c_dir
            animNum += (uint)(_compact.Core.dir << 1);  //2 sizes (large and small)
            return FnSpeakMe(targetId, mesgNum, animNum);
        }

        private bool FnSpeakWait(uint id, uint message, uint animation)
        {
            // non player mega char speaks
            // player will wait for it to finish before continuing script processing
            _compact.Core.flag = (ushort)(id & 0xffff);
            _compact.Core.logic = L_LISTEN;
            return FnSpeakMe(id, message, animation);
        }

        private bool FnSpeakWaitDir(uint a, uint b, uint c)
        {
            /* non player mega chr$ speaks	S2(20Jan93tw)
            the player will wait for it to finish
            before continuing script processing
            this function sets the directional option whereby
            the anim chosen is linked to c_dir -

            _compact is player
            a is ID to speak (not us)
            b is text message number
            c is base of mini table within anim_talk_table */

            //# ifdef __DC__
            //            __builtin_alloca(4); // Works around a gcc bug (wrong-code/11736)
            //#endif

            _compact.Core.flag = (ushort)a;
            _compact.Core.logic = L_LISTEN;

            var speaker = _skyCompact.FetchCpt((ushort)a);
            if (c != 0)
            {
                c += (uint)(speaker.Core.dir << 1);
                StdSpeak(speaker, b, c);
            }
            else
                StdSpeak(speaker, b, c);

            return false;
        }

        private bool FnChooser(uint a, uint b, uint c)
        {
            // setup the text questions to be clicked on
            // read from TEXT1 until 0

            SystemVars.Instance.SystemFlags |= SystemFlags.CHOOSING; // can't save/restore while choosing

            _scriptVariables[THE_CHOSEN_ONE] = 0; // clear result

            int p = TEXT1;
            ushort ycood = TOP_LEFT_Y; // rolling coordinate

            while (_scriptVariables[p] != 0)
            {
                uint textNum = _scriptVariables[p++];

                DisplayedText lowText = _skyText.LowTextManager(textNum, GAME_SCREEN_WIDTH, 0, 241, false);

                byte[] data = lowText.TextData;
                var header = ServiceLocator.Platform.ToStructure<DataFileHeader>(data, 0);

                // stipple the text

                uint size = (uint)(header.s_height * header.s_width);
                uint index = 0;
                uint width = header.s_width;
                uint height = header.s_height;

                var dataPos = ServiceLocator.Platform.SizeOf<DataFileHeader>();

                while (index < size)
                {
                    if (index % width <= 1)
                        index ^= 1; //index++;
                    if (data[dataPos + index] == 0)
                        data[dataPos + index] = 1;
                    index += 2;
                }

                Compact textCompact = _skyCompact.FetchCpt(lowText.CompactNum);

                textCompact.Core.getToFlag = (ushort)textNum;
                textCompact.Core.downFlag = (ushort)_scriptVariables[p++]; // get animation number

                textCompact.Core.status |= ST_MOUSE; // mouse detects

                textCompact.Core.xcood = TOP_LEFT_X; // set coordinates
                textCompact.Core.ycood = ycood;
                ycood += (ushort)height;
            }

            if (p == TEXT1)
                return true;

            _compact.Core.logic = L_CHOOSE; // player frozen until choice made
            FnAddHuman(0, 0, 0); // bring back mouse

            return false;
        }

        private bool FnHighlight(uint itemNo, uint pen, uint c)
        {
            pen -= 11;
            pen ^= 1;
            pen += 241;
            Compact textCompact = _skyCompact.FetchCpt((ushort)itemNo);
            var sprData = SkyEngine.ItemList[textCompact.Core.flag];
            _skyText.ChangeTextSpriteColor(sprData, (byte)pen);
            return true;
        }

        private bool FnTextKill(uint a, uint b, uint c)
        {
            // Kill of text items that are mouse detectable

            uint id = FIRST_TEXT_COMPACT;

            for (int i = 10; i > 0; i--)
            {
                Compact cpt = _skyCompact.FetchCpt((ushort)id);
                if ((cpt.Core.status & (1 << 4)) != 0)
                    cpt.Core.status = 0;
                id++;
            }
            return true;
        }

        private bool FnStopMode(uint a, uint b, uint c)
        {
            _compact.Core.logic = L_STOPPED;
            return false;
        }

        private bool FnWeWait(uint id, uint b, uint c)
        {
            // We have hit another mega
            // we are going to wait for it to move

            _compact.Core.waitingFor = (ushort)id;
            StopAndWait();
            return true; // not sure about this
        }

        private bool FnSendSync(uint mega, uint sync, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.sync = (ushort)(sync & 0xffff);
            return false;
        }

        private bool FnSendFastSync(uint mega, uint sync, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.sync = (ushort)(sync & 0xffff);
            return true;
        }

        private bool FnSendRequest(uint target, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)target);
            cpt.Core.request = (ushort)(scr & 0xffff);
            return false;
        }

        private bool FnClearRequest(uint target, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)target);
            cpt.Core.request = 0;
            return true;
        }

        private bool FnCheckRequest(uint a, uint b, uint c)
        {
            // check for interaction request

            if (_compact.Core.request == 0)
                return true;

            _compact.Core.mode = C_ACTION_MODE; // into action mode

            _compact.Core.actionSub = _compact.Core.request;
            _compact.Core.actionSub_off = 0;

            _compact.Core.request = 0; // trash request
            return false; // drop from script
        }

        private bool FnStartMenu(uint firstObject, uint b, uint c)
        {
            // initialize the top menu bar
            // firstObject is o0 for game menu, k0 for linc

            uint i;
            firstObject /= 4;

            // (1) FIRST, SET UP THE 2 ARROWS SO THEY APPEAR ON SCREEN

            Compact cpt = _skyCompact.FetchCpt(47);
            cpt.Core.status = ST_MOUSE + ST_FOREGROUND + ST_LOGIC + ST_RECREATE;
            cpt.Core.screen = (ushort)(_scriptVariables[SCREEN] & 0xffff);

            cpt = _skyCompact.FetchCpt(48);
            cpt.Core.status = ST_MOUSE + ST_FOREGROUND + ST_LOGIC + ST_RECREATE;
            cpt.Core.screen = (ushort)(_scriptVariables[SCREEN] & 0xffff);

            // (2) COPY OBJECTS FROM NON-ZERO INVENTORY VARIABLES INTO OBJECT DISPLAY LIST (& COUNT THEM)

            // sort the objects and pad with blanks

            uint menuLength = 0;
            for (i = firstObject; i < firstObject + _objectList.Length; i++)
            {
                if (_scriptVariables[(int)i] != 0)
                    _objectList[menuLength++] = _scriptVariables[(int)i];
            }
            _scriptVariables[MENU_LENGTH] = menuLength;

            // (3) OK, NOW TOP UP THE LIST WITH THE REQUIRED NO. OF BLANK OBJECTS (for min display length 11)

            uint blankId = 51;
            for (i = menuLength; i < 11; i++)
                _objectList[i] = blankId++;

            // (4) KILL ID's OF ALL 20 OBJECTS SO UNWANTED ICONS (SCROLLED OFF) DON'T REMAIN ON SCREEN
            // (There should be a better way of doing this - only kill id of 12th item when menu has scrolled right)

            for (i = 0; i < _objectList.Length; i++)
            {
                if (_objectList[i] != 0)
                    _skyCompact.FetchCpt((ushort)_objectList[i]).Core.status = ST_LOGIC;
                else break;
            }

            // (5) NOW FIND OUT WHICH OBJECT TO START THE DISPLAY FROM (depending on scroll offset)

            if (menuLength < 11) // check we can scroll
                _scriptVariables[SCROLL_OFFSET] = 0;
            else if (menuLength < _scriptVariables[SCROLL_OFFSET] + 11)
                _scriptVariables[SCROLL_OFFSET] = menuLength - 11;

            // (6) AND FINALLY, INITIALIZE THE 11 OBJECTS SO THEY APPEAR ON SCREEEN

            ushort rollingX = TOP_LEFT_X + 28;
            for (i = 0; i < 11; i++)
            {
                cpt = _skyCompact.FetchCpt((ushort)_objectList[_scriptVariables[SCROLL_OFFSET] + i]);

                cpt.Core.status = ST_MOUSE + ST_FOREGROUND + ST_LOGIC + ST_RECREATE;
                cpt.Core.screen = (ushort)(_scriptVariables[SCREEN] & 0xffff);

                cpt.Core.xcood = rollingX;
                rollingX += 24;

                if (_scriptVariables[MENU] == 2)
                    cpt.Core.ycood = 136;
                else
                    cpt.Core.ycood = 112;
            }

            return true;
        }

        private bool FnUnhighlight(uint item, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)item);
            cpt.Core.frame--;
            cpt.Core.getToFlag = 0;
            return true;
        }

        private bool FnFaceId(uint otherId, uint b, uint c)
        {
            // return the direction to turn to face another id
            // pass back result in c_just_flag

            Compact cpt = _skyCompact.FetchCpt((ushort)otherId);

            short x = (short)(_compact.Core.xcood - cpt.Core.xcood);

            if (x < 0)
            { // we're to the left
                x = (short)-x;
                _compact.Core.getToFlag = 3;
            }
            else
            { // it's to the left
                _compact.Core.getToFlag = 2;
            }

            // now check y

            // we must find the true bottom of the sprite
            // it is not enough to use y coord because changing
            // sprite offsets can ruin the formula - instead we
            // will use the bottom of the mouse collision area

            short y = (short)(_compact.Core.ycood - (cpt.Core.ycood + cpt.Core.mouseRelY + cpt.Core.mouseSizeY));

            if (y < 0)
            { // it's below
                y = (short)-y;
                if (y >= x)
                    _compact.Core.getToFlag = 1;
            }
            else
            { // it's above
                if (y >= x)
                    _compact.Core.getToFlag = 0;
            }
            return true;
        }

        private bool FnForeground(uint sprite, uint b, uint c)
        {
            // Make sprite a foreground sprite
            Compact cpt = _skyCompact.FetchCpt((ushort)sprite);
            cpt.Core.status &= 0xfff8;
            cpt.Core.status |= ST_FOREGROUND;
            return true;
        }

        private bool FnBackground(uint a, uint b, uint c)
        {
            // Make us a background sprite
            _compact.Core.status &= 0xfff8;
            _compact.Core.status |= ST_BACKGROUND;
            return true;
        }

        private bool FnNewBackground(uint sprite, uint b, uint c)
        {
            // Make sprite a background sprite
            Compact cpt = _skyCompact.FetchCpt((ushort)sprite);
            cpt.Core.status &= 0xfff8;
            cpt.Core.status |= ST_BACKGROUND;
            return true;
        }

        private bool FnSort(uint mega, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.status &= 0xfff8;
            cpt.Core.status |= ST_SORT;
            return true;
        }

        private bool FnNoSpriteEngine(uint a, uint b, uint c)
        {
            // stop the compact printing
            // remove foreground, background & sort
            _compact.Core.status &= 0xfff8;
            return true;
        }

        private bool FnNoSpritesA6(uint us, uint b, uint c)
        {
            // stop the compact printing
            // remove foreground, background & sort
            Compact cpt = _skyCompact.FetchCpt((ushort)us);
            cpt.Core.status &= 0xfff8;
            return true;
        }

        private bool FnResetId(uint id, uint resetBlock, uint c)
        {
            // used when a mega is to be restarted
            // eg - when a smaller mega turn to larger
            // - a mega changes rooms...

            var cpt = _skyCompact.FetchCpt((ushort)id);
            var rst = new UShortAccess(_skyCompact.FetchCptRaw((ushort)resetBlock), 0);

            if (cpt == null)
            {
                // TODO: warning("fnResetId(): Compact %d (id) == NULL", id);
                return true;
            }

            if (rst == null)
            {
                // TODO: warning("fnResetId(): Compact %d (resetBlock) == NULL", resetBlock);
                return true;
            }

            ushort off;
            while ((off = rst[0]) != 0xffff)
            {
                rst.Offset += 2;
                _skyCompact.GetCompactElem(cpt, off).Field = rst[0];
                rst.Offset += 2;
            }
            return true;
        }

        private bool FnToggleGrid(uint a, uint b, uint c)
        {
            // Toggle a mega's grid plotting
            _compact.Core.status ^= ST_GRID_PLOT;
            return true;
        }

        private bool FnPause(uint cycles, uint b, uint c)
        {
            // Set mega to L_PAUSE
            _compact.Core.flag = (ushort)(cycles & 0xffff);
            _compact.Core.logic = L_PAUSE;
            return false; // drop out of script
        }

        private bool FnRunAnimMod(uint animNo, uint b, uint c)
        {
            _compact.Core.grafixProgId = (ushort)animNo;
            _compact.Core.grafixProgPos = 0;

            _compact.Core.offset = _skyCompact.GetGrafixPtr(_compact).Value;
            _compact.Core.grafixProgPos++;
            _compact.Core.logic = L_MOD_ANIMATE;
            Anim();
            return false; // drop from script
        }

        private bool FnSimpleMod(uint animSeqNo, uint b, uint c)
        {
            _compact.Core.grafixProgId = (ushort)animSeqNo;
            _compact.Core.grafixProgPos = 0;

            _compact.Core.logic = L_SIMPLE_MOD;
            _compact.Core.offset = _skyCompact.GetGrafixPtr(_compact).Value;
            _compact.Core.grafixProgPos++;
            SimpleAnim();
            return false;
        }

        private bool FnRunFrames(uint sequenceNo, uint b, uint c)
        {
            _compact.Core.grafixProgId = (ushort)sequenceNo;
            _compact.Core.grafixProgPos = 0;

            _compact.Core.logic = L_FRAMES;
            _compact.Core.offset = _skyCompact.GetGrafixPtr(_compact).Value;
            _compact.Core.grafixProgPos++;
            SimpleAnim();
            return false;
        }

        private bool FnAwaitSync(uint a, uint b, uint c)
        {
            if (_compact.Core.sync != 0)
                return true;

            _compact.Core.logic = L_WAIT_SYNC;
            return false;
        }

        private bool FnIncMegaSet(uint a, uint b, uint c)
        {
            _compact.Core.megaSet += NEXT_MEGA_SET;
            return true;
        }

        private bool FnDecMegaSet(uint a, uint b, uint c)
        {
            _compact.Core.megaSet -= NEXT_MEGA_SET;
            return true;
        }

        private bool FnSetMegaSet(uint mega, uint setNo, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.megaSet = (ushort)(setNo * NEXT_MEGA_SET);
            return true;
        }

        private bool FnMoveItems(uint listNo, uint screenNo, uint c)
        {
            // Move a list of id's to another screen
            var p = new UShortAccess(_skyCompact.FetchCptRaw((ushort)CptIds.MoveList), 0);
            p = new UShortAccess(_skyCompact.FetchCptRaw(p[(int)listNo]), 0);
            for (int i = 0; i < 2; i++)
            {
                if (p.Value == 0)
                    return true;
                Compact cpt = _skyCompact.FetchCpt(p.Value); p.Offset += 2;
                cpt.Core.screen = (ushort)(screenNo & 0xffff);
            }
            return true;
        }

        private bool FnNewList(uint a, uint b, uint c)
        {
            // Reset the chooser list
            for (int i = 0; i < 16; i++)
                _scriptVariables[TEXT1 + i] = 0;
            return true;
        }

        private bool FnAskThis(uint textNo, uint animNo, uint c)
        {
            // find first free position
            var p = TEXT1;
            while (_scriptVariables[p] != 0)
                p += 2;
            _scriptVariables[p++] = textNo;
            _scriptVariables[p] = animNo;
            return true;
        }

        private bool FnRandom(uint a, uint b, uint c)
        {
            _scriptVariables[RND] = (ushort)(_rnd.Next(65536) & a);
            return true;
        }

        private bool FnPersonHere(uint id, uint room, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.screen == room ? 1U : 0;
            return true;
        }

        private bool FnToggleMouse(uint a, uint b, uint c)
        {
            _skyCompact.FetchCpt((ushort)a).Core.status ^= ST_MOUSE;
            return true;
        }

        private bool FnMouseOn(uint a, uint b, uint c)
        {
            //switch on the mouse highlight
            Compact cpt = _skyCompact.FetchCpt((ushort)a);
            cpt.Core.status |= ST_MOUSE;
            return true;
        }

        private bool FnMouseOff(uint a, uint b, uint c)
        {
            //switch off the mouse highlight
            Compact cpt = _skyCompact.FetchCpt((ushort)a);
            unchecked
            {
                cpt.Core.status &= (ushort)~ST_MOUSE;
            }
            return true;
        }

        private bool FnFetchX(uint id, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.xcood;
            return true;
        }

        private bool FnFetchY(uint id, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.ycood;
            return true;
        }

        private bool FnTestList(uint id, uint x, uint y)
        {
            _scriptVariables[RESULT] = 0; // assume fail
            var list = new UShortAccess(_skyCompact.FetchCptRaw((ushort)id), 0);

            while (list.Value != 0)
            {
                if ((x >= list[0]) && (x < list[1]) && (y >= list[2]) && (y < list[3]))
                    _scriptVariables[RESULT] = list[4];
                list.Offset += 5 * 2;
            }
            return true;
        }

        private bool FnFetchPlace(uint id, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.place;
            return true;
        }

        private bool FnCustomJoey(uint id, uint b, uint c)
        {
            // return id's x & y coordinate & c_mood (i.e. stood still yes/no)
            // used by Joey-Logic - done in code like this because scripts can't
            // get access to another megas compact as easily

            Compact cpt = _skyCompact.FetchCpt((ushort)id);

            _scriptVariables[PLAYER_X] = cpt.Core.xcood;
            _scriptVariables[PLAYER_Y] = cpt.Core.ycood;
            _scriptVariables[PLAYER_MOOD] = cpt.Core.mood;
            _scriptVariables[PLAYER_SCREEN] = cpt.Core.screen;
            return true;
        }

        private bool FnSetPalette(uint a, uint b, uint c)
        {
            _skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort)a));
            SystemVars.Instance.CurrentPalette = a;
            return true;
        }

        private bool FnTextModule(uint a, uint b, uint c)
        {
            _skyText.FnTextModule(a, b);
            return true;
        }

        private bool FnChangeName(uint id, uint textNo, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            cpt.Core.cursorText = (ushort)textNo;
            return true;
        }

        private bool FnMiniLoad(uint a, uint b, uint c)
        {
            _skyDisk.FnMiniLoad((ushort)a);
            return true;
        }

        private bool FnFlushBuffers(uint a, uint b, uint c)
        {
            _skyDisk.FnFlushBuffers();
            return true;
        }

        private bool FnFlushChip(uint a, uint b, uint c)
        {
            // this should be done automatically
            return true;
        }

        private bool FnSaveCoods(uint a, uint b, uint c)
        {
            _skyMouse.FnSaveCoods();
            return true;
        }

        private bool FnPlotGrid(uint x, uint y, uint width)
        {
            _skyGrid.PlotGrid(x, y, width, _compact);
            return true;
        }

        private bool FnRemoveGrid(uint x, uint y, uint width)
        {
            _skyGrid.RemoveGrid(x, y, width, _compact);
            return true;
        }

        private bool FnEyeball(uint id, uint b, uint c)
        {
            // set 'result' to frame no. pointing to foster, according to table used
            // eg. FN_eyeball (id_eye_90_table);

            var eyeTable = new UShortAccess(_skyCompact.FetchCptRaw((ushort)id), 0);
            Compact cpt = _skyCompact.FetchCpt(ID_BLUE_FOSTER);

            int x = cpt.Core.xcood; // 168 < x < 416
            x -= 168;
            x >>= 3;

            int y = cpt.Core.ycood; // 256 < y < 296
            y -= 256;
            y <<= 2;

            _scriptVariables[RESULT] = (uint)(eyeTable[x + y] + S91);
            return true;
        }

        private bool FnRestoreGame(uint a, uint b, uint c)
        {
            Control.DoLoadSavePanel();
            return false;
        }

        private bool FnRestartGame(uint a, uint b, uint c)
        {
            Control.RestartGame();
            return false;
        }

        private bool FnNewSwingSeq(uint a, uint b, uint c)
        {
            // only certain files work on pc. (huh?! something we should take care of?)
            if ((a == 85) || (a == 106) || (a == 75) || (a == 15))
            {
                _skyScreen.StartSequenceItem((ushort)a);
            }
            else
            {
                // TODO: Debug(1, "fnNewSwingSeq: ignored seq %d", a);
            }
            return true;
        }

        private bool FnWaitSwingEnd(uint a, uint b, uint c)
        {
            _skyScreen.WaitForSequence();
            return true;
        }

        private bool FnSkipIntroCode(uint a, uint b, uint c)
        {
            SystemVars.Instance.PastIntro = true;
            return true;
        }

        private bool FnBlankScreen(uint a, uint b, uint c)
        {
            _skyScreen.ClearScreen();
            return true;
        }

        private bool FnPrintCredit(uint a, uint b, uint c)
        {
            DisplayedText creditText = _skyText.LowTextManager(a, 240, 0, 248, true);
            Compact credCompact = _skyCompact.FetchCpt(creditText.CompactNum);
            credCompact.Core.xcood = 168;
            if ((a == 558) && (c == 215))
                credCompact.Core.ycood = 211;
            else
                credCompact.Core.ycood = (ushort)c;
            _scriptVariables[RESULT] = creditText.CompactNum;
            return true;
        }

        private bool FnLookAt(uint a, uint b, uint c)
        {
            DisplayedText textInfo = _skyText.LowTextManager(a, 240, 0, 248, true);
            Compact textCpt = _skyCompact.FetchCpt(textInfo.CompactNum);
            textCpt.Core.xcood = 168;
            textCpt.Core.ycood = (ushort)c;

            _skyScreen.Recreate();
            _skyScreen.SpriteEngine();
            _skyScreen.Flip();

            FnNoHuman(0, 0, 0);
            _skyMouse.LockMouse();

            _skyMouse.WaitMouseNotPressed(800);

            _skyMouse.UnlockMouse();
            FnAddHuman(0, 0, 0);
            textCpt.Core.status = 0;

            return true;
        }

        private bool FnLincTextModule(uint textPos, uint textNo, uint buttonAction)
        {
            ushort cnt;
            if ((buttonAction & 0x8000) != 0)
                for (cnt = LINC_DIGIT_0; cnt <= LINC_DIGIT_9; cnt++)
                    _scriptVariables[cnt] = 0;
            buttonAction &= 0x7FFF;
            if (buttonAction < 10)
                _scriptVariables[(int)(LINC_DIGIT_0 + buttonAction)] = textNo;

            DisplayedText text = _skyText.LowTextManager(textNo, 220, 0, 215, false);

            Compact textCpt = _skyCompact.FetchCpt(text.CompactNum);

            if (textPos < 20)
            { // line number (for text)
                textCpt.Core.xcood = 152;
                textCpt.Core.ycood = (ushort)(textPos * 13 + 170);
            }
            else if (textPos > 20)
            { // x coordinate (for numbers)
                textCpt.Core.xcood = (ushort)textPos;
                textCpt.Core.ycood = 214;
            }
            else
            {
                //TODO:  warning("::fnLincTextModule: textPos == 20");
            }
            textCpt.Core.getToFlag = (ushort)textNo;
            return true;
        }

        private bool FnTextKill2(uint a, uint b, uint c)
        {
            // Kill all text items

            uint id = FIRST_TEXT_COMPACT;

            for (int i = 10; i > 0; i--)
            {
                Compact cpt = _skyCompact.FetchCpt((ushort)id);
                cpt.Core.status = 0;
                id++;
            }
            return true;
        }

        private bool FnSetFont(uint font, uint b, uint c)
        {
            _skyText.FnSetFont(font);
            return true;
        }

        private bool FnStartFx(uint sound, uint b, uint c)
        {
            _skySound.FnStartFx(sound, (byte)(b & 1));
            return true;
        }

        private bool FnStopFx(uint a, uint b, uint c)
        {
            _skySound.FnStopFx();
            return true;
        }

        private bool FnStartMusic(uint a, uint b, uint c)
        {
            if (!SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MUS_OFF))
                _skyMusic.StartMusic((ushort)a);
            SystemVars.Instance.CurrentMusic = (ushort)a;
            return true;
        }

        private bool FnStopMusic(uint a, uint b, uint c)
        {
            _skyMusic.StartMusic(0);
            SystemVars.Instance.CurrentMusic = 0;
            return true;
        }

        private bool FnFadeDown(uint a, uint b, uint c)
        {
            _skyScreen.FnFadeDown(a);
            return true;
        }

        private bool FnFadeUp(uint a, uint b, uint c)
        {
            SystemVars.Instance.CurrentPalette = a;
            _skyScreen.FnFadeUp(a, b);
            return true;
        }

        private bool FnQuitToDos(uint a, uint b, uint c)
        {
            SkyEngine.QuitGame();
            return false;
        }

        private bool FnPauseFx(uint a, uint b, uint c)
        {
            _skySound.FnPauseFx();
            return true;
        }

        private bool FnUnPauseFx(uint a, uint b, uint c)
        {
            _skySound.FnUnPauseFx();
            return true;
        }

        private bool FnPrintf(uint a, uint b, uint c)
        {
            // TODO: debug("fnPrintf(%d, %d, %d)", a, b, c);
            return true;
        }
    }
}
