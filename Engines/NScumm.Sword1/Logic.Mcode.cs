using System;
using NScumm.Core;

namespace NScumm.Sword1
{
    internal class AnimSet
    {
        public const int Size = 8;

        public uint cdt
        {
            get { return Data.ToUInt32(Offset); }
            set { Data.WriteUInt32(Offset, value); }
        }

        public uint spr
        {
            get { return Data.ToUInt32(Offset + 4); }
            set { Data.WriteUInt32(Offset + 4, value); }
        }

        public AnimSet(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }

        public int Offset { get; }
        public byte[] Data { get; }
    }

    internal class AnimUnit
    {
        public const int Size = 12;

        public uint animX
        {
            get { return Data.ToUInt32(Offset); }
            set { Data.WriteUInt32(Offset, value); }
        }

        public uint animY
        {
            get { return Data.ToUInt32(Offset + 4); }
            set { Data.WriteUInt32(Offset + 4, value); }
        }

        public uint animFrame
        {
            get { return Data.ToUInt32(Offset + 8); }
            set { Data.WriteUInt32(Offset + 8, value); }
        }

        public AnimUnit(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }

        public int Offset { get; }
        public byte[] Data { get; }
    }

    internal partial class Logic
    {
        private Menu _menu;
        private byte _speechClickDelay;
        private Sound _sound;
        private Router _router;
        private Random _rnd = new Random(Environment.TickCount);

        private const int LAST_FRAME = 999;
        private const int INS_talk = 1;
        private const int LOOPED = 1;

        private int fnBackground(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {

            cpt.status &= ~(STAT_FORE | STAT_SORT);
            cpt.status |= STAT_BACK;
            return SCRIPT_CONT;
        }

        private int fnForeground(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status &= ~(STAT_BACK | STAT_SORT);
            cpt.status |= STAT_FORE;
            return SCRIPT_CONT;
        }

        private int fnSort(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status &= ~(STAT_BACK | STAT_FORE);
            cpt.status |= STAT_SORT;
            return SCRIPT_CONT;
        }

        private int fnNoSprite(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status &= ~(STAT_BACK | STAT_FORE | STAT_SORT);
            return SCRIPT_CONT;
        }

        private int fnMegaSet(SwordObject cpt, int id, int walk_data, int spr, int e, int f, int z, int x)
        {
            cpt.mega_resource = walk_data;
            cpt.walk_resource = spr;
            return SCRIPT_CONT;
        }

        private int fnAnim(SwordObject cpt, int id, int cdt, int spr, int e, int f, int z, int x)
        {

            if (cdt != 0 && (spr == 0))
            {
                var animTab = _resMan.OpenFetchRes((uint)cdt);
                var animOffset = Header.Size + cpt.dir * AnimSet.Size;
                var anim = new AnimSet(animTab, animOffset);
                cpt.anim_resource = (int)_resMan.ReadUInt32(anim.cdt);
                cpt.resource = (int)_resMan.ReadUInt32(anim.spr);
                _resMan.ResClose((uint)cdt);
            }
            else
            {
                cpt.anim_resource = cdt;
                cpt.resource = spr;
            }
            if ((cpt.anim_resource == 0) || (cpt.resource == 0))
                throw new InvalidOperationException($"fnAnim called width ({cdt}/{spr}) => ({cpt.anim_resource}/{cpt.resource})");

            var frameHead = new FrameHeader(_resMan.FetchFrame(_resMan.OpenFetchRes((uint)cpt.resource), 0));
            if (frameHead.offsetX != 0 || frameHead.offsetY != 0)
            { // boxed mega anim?
                cpt.status |= STAT_SHRINK;
                cpt.anim_x = cpt.xcoord; // set anim coords to 'feet' coords - only need to do this once
                cpt.anim_y = cpt.ycoord;
            }
            else
            {
                // Anim_driver sets anim coords to cdt coords for every frame of a loose anim
                cpt.status &= ~STAT_SHRINK;
            }
            _resMan.ResClose((uint)cpt.resource);

            cpt.logic = LOGIC_anim;
            cpt.anim_pc = 0;
            cpt.sync = 0;
            return SCRIPT_STOP;
        }

        private int fnSetFrame(SwordObject cpt, int id, int cdt, int spr, int frameNo, int f, int z, int x)
        {

            AnimUnit animPtr;

            var data = _resMan.OpenFetchRes((uint)cdt);
            var dataOffs = Header.Size;
            if (frameNo == LAST_FRAME)
                frameNo = (int)(_resMan.ReadUInt32(data.ToUInt32(dataOffs)) - 1);

            dataOffs += 4;
            animPtr = new AnimUnit(data, dataOffs + frameNo * AnimUnit.Size);

            cpt.anim_x = (int)_resMan.ReadUInt32(animPtr.animX);
            cpt.anim_y = (int)_resMan.ReadUInt32(animPtr.animY);
            cpt.frame = (int)_resMan.ReadUInt32(animPtr.animFrame);

            cpt.resource = spr;
            cpt.status &= ~STAT_SHRINK;
            _resMan.ResClose((uint)cdt);
            return SCRIPT_CONT;
        }

        private int fnFullAnim(SwordObject cpt, int id, int anim, int graphic, int e, int f, int z, int x)
        {
            cpt.logic = LOGIC_full_anim;

            cpt.anim_pc = 0;
            cpt.anim_resource = anim;
            cpt.resource = graphic;
            cpt.status &= ~STAT_SHRINK;
            cpt.sync = 0;
            return SCRIPT_STOP;
        }

        private int fnFullSetFrame(SwordObject cpt, int id, int cdt, int spr, int frameNo, int f, int z, int x)
        {
            var data = _resMan.OpenFetchRes((uint)cdt);
            var dataOff = Header.Size;

            if (frameNo == LAST_FRAME)
                frameNo = (int)(_resMan.ReadUInt32(data.ToUInt32(dataOff)) - 1);
            dataOff += 4;

            var animPtr = new AnimUnit(data, dataOff + AnimUnit.Size * frameNo);
            cpt.anim_x = cpt.xcoord = (int)_resMan.ReadUInt32(animPtr.animX);
            cpt.anim_y = cpt.ycoord = (int)_resMan.ReadUInt32(animPtr.animY);
            cpt.frame = (int)_resMan.ReadUInt32(animPtr.animFrame);

            cpt.resource = spr;
            cpt.status &= ~STAT_SHRINK;

            _resMan.ResClose((uint)cdt);
            return SCRIPT_CONT;
        }

        private int fnFadeDown(SwordObject cpt, int id, int speed, int d, int e, int f, int z, int x)
        {
            _screen.FadeDownPalette();
            return SCRIPT_CONT;
        }

        private int fnFadeUp(SwordObject cpt, int id, int speed, int d, int e, int f, int z, int x)
        {
            _screen.FadeUpPalette();
            return SCRIPT_CONT;
        }

        private int fnCheckFade(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.RETURN_VALUE] = (uint)(_screen.StillFading() ? 1 : 0);
            return SCRIPT_CONT;
        }

        private int fnSetSpritePalette(SwordObject cpt, int id, int spritePal, int d, int e, int f, int z, int x)
        {
            _screen.FnSetPalette(184, 72, (uint)spritePal, false);
            return SCRIPT_CONT;
        }

        private int fnSetWholePalette(SwordObject cpt, int id, int spritePal, int d, int e, int f, int z, int x)
        {
            _screen.FnSetPalette(0, 256, (uint)spritePal, false);
            return SCRIPT_CONT;
        }

        private int fnSetFadeTargetPalette(SwordObject cpt, int id, int spritePal, int d, int e, int f, int z, int x)
        {
            _screen.FnSetPalette(0, 184, (uint)spritePal, true);
            return SCRIPT_CONT;
        }

        private int fnSetPaletteToFade(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            SystemVars.WantFade = true;
            return SCRIPT_CONT;
        }

        private int fnSetPaletteToCut(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            SystemVars.WantFade = false;
            return SCRIPT_CONT;
        }

        private int fnPlaySequence(SwordObject cpt, int id, int sequenceId, int d, int e, int f, int z, int x)
        {
            // A cutscene usually (always?) means the room will change. In the
            // meantime, we don't want any looping sound effects still playing.
            _sound.QuitScreen();

            var player = new MoviePlayer(_vm,_textMan,_resMan);
            _screen.ClearScreen();
            player.Load(sequenceId);
            player.Play();

            return SCRIPT_CONT;
        }

        private int fnIdle(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.tree.script_level = 0; // force to level 0
            cpt.logic = LOGIC_idle;
            return SCRIPT_STOP;
        }

        private int fnPause(SwordObject cpt, int id, int pause, int d, int e, int f, int z, int x)
        {
            cpt.pause = pause;
            cpt.logic = LOGIC_pause;
            return SCRIPT_STOP;
        }

        private int fnPauseSeconds(SwordObject cpt, int id, int pause, int d, int e, int f, int z, int x)
        {
            cpt.pause = pause * SwordEngine.FRAME_RATE;
            cpt.logic = LOGIC_pause;
            return SCRIPT_STOP;
        }

        private int fnQuit(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.logic = LOGIC_quit;
            return SCRIPT_STOP;
        }

        private int fnKillId(SwordObject cpt, int id, int target, int d, int e, int f, int z, int x)
        {
            SwordObject targetObj = _objMan.FetchObject((uint)target);
            targetObj.status = 0;
            return SCRIPT_CONT;
        }

        private int fnSuicide(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status = 0;
            cpt.logic = LOGIC_quit;
            return SCRIPT_STOP;
        }

        private int fnNewScript(SwordObject cpt, int id, int script, int d, int e, int f, int z, int x)
        {
            cpt.logic = LOGIC_new_script;
            _newScript = (uint)script;
            return SCRIPT_STOP;
        }

        private int fnSubScript(SwordObject cpt, int id, int script, int d, int e, int f, int z, int x)
        {
            cpt.tree.script_level++;
            if (cpt.tree.script_level == ScriptTree.TOTAL_script_levels)
                throw new InvalidOperationException($"Compact {id}: script level exceeded in fnSubScript");
            cpt.tree.script_pc[cpt.tree.script_level] = script;
            cpt.tree.script_id[cpt.tree.script_level] = script;
            return SCRIPT_STOP;
        }

        private int fnRestartScript(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.logic = LOGIC_restart;
            return SCRIPT_STOP;
        }

        private int fnSetBookmark(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.bookmark.CopyFrom(cpt.tree);
            return SCRIPT_CONT;
        }

        private int fnGotoBookmark(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.logic = LOGIC_bookmark;
            return SCRIPT_STOP;
        }

        private int fnSendSync(SwordObject cpt, int id, int sendId, int syncValue, int e, int f, int z, int x)
        {
            SwordObject target = _objMan.FetchObject((uint)sendId);
            target.sync = syncValue;
            return SCRIPT_CONT;
        }

        private int fnWaitSync(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.logic = LOGIC_wait_for_sync;
            return SCRIPT_STOP;
        }

        private int cfnClickInteract(SwordObject cpt, int id, int target, int d, int e, int f, int z, int x)
        {
            SwordObject tar = _objMan.FetchObject((uint)target);
            cpt = _objMan.FetchObject(PLAYER);
            cpt.tree.script_level = 0;
            cpt.tree.script_pc[0] = tar.interact;
            cpt.tree.script_id[0] = tar.interact;
            cpt.logic = LOGIC_script;
            return SCRIPT_STOP;
        }

        private int cfnSetScript(SwordObject cpt, int id, int target, int script, int e, int f, int z, int x)
        {
            SwordObject tar = _objMan.FetchObject((uint)target);
            tar.tree.script_level = 0;
            tar.tree.script_pc[0] = script;
            tar.tree.script_id[0] = script;
            tar.logic = LOGIC_script;
            return SCRIPT_CONT;
        }

        private int cfnPresetScript(SwordObject cpt, int id, int target, int script, int e, int f, int z, int x)
        {
            SwordObject tar = _objMan.FetchObject((uint)target);
            tar.tree.script_level = 0;
            tar.tree.script_pc[0] = script;
            tar.tree.script_id[0] = script;
            if (tar.logic == LOGIC_idle)
                tar.logic = LOGIC_script;
            return SCRIPT_CONT;
        }

        private int fnInteract(SwordObject cpt, int id, int target, int d, int e, int f, int z, int x)
        {
            SwordObject tar = _objMan.FetchObject((uint)target);
            cpt.place = tar.place;

            SwordObject floorObject = _objMan.FetchObject((uint)tar.place);
            cpt.scale_a = floorObject.scale_a;
            cpt.scale_b = floorObject.scale_b;

            cpt.tree.script_level++;
            cpt.tree.script_pc[cpt.tree.script_level] = tar.interact;
            cpt.tree.script_id[cpt.tree.script_level] = tar.interact;

            return SCRIPT_STOP;
        }

        private int fnIssueEvent(SwordObject cpt, int id, int evt, int delay, int e, int f, int z, int x)
        {
            _eventMan.FnIssueEvent(cpt, id, evt, delay);
            return SCRIPT_CONT;
        }

        private int fnCheckForEvent(SwordObject cpt, int id, int pause, int d, int e, int f, int z, int x)
        {
            return _eventMan.FnCheckForEvent(cpt, id, pause);
        }

        private int fnWipeHands(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = 0;
            _mouse.SetLuggage(0, 0);
            _menu.Refresh(Menu.MENU_TOP);
            return SCRIPT_CONT;
        }

        private int fnISpeak(SwordObject cpt, int id, int cdt, int textNo, int spr, int f, int z, int x)
        {
            _speechClickDelay = 3;
            if (((textNo & ~1) == 0x3f0012) && (cdt == 0) && (spr == 0))
            {
                cdt = SwordRes.GEOSTDLCDT; // workaround for missing animation when examining
                spr = SwordRes.GEOSTDL;    // the conductor on the train roof
            }
            cpt.logic = LOGIC_speech;

            // first setup the talk animation
            if (cdt != 0 && (spr == 0))
            { // if 'cdt' is non-zero but 'spr' is zero - 'cdt' is an anim table tag
                var animTabData = _resMan.OpenFetchRes((uint)cdt);
                var anim = new AnimSet(animTabData, Header.Size + cpt.dir * AnimSet.Size);

                cpt.anim_resource = (int)_resMan.ReadUInt32(anim.cdt);
                if (anim.cdt != 0)
                    cpt.resource = (int)_resMan.ReadUInt32(anim.spr);
                _resMan.ResClose((uint)cdt);
            }
            else
            {
                cpt.anim_resource = cdt;
                if (cdt != 0)
                    cpt.resource = spr;
            }
            cpt.anim_pc = 0; // start anim from first frame
            if (cpt.anim_resource != 0)
            {
                if (cpt.resource == 0)
                    throw new InvalidOperationException($"ID {id}: Can't run anim with cdt={cdt}, spr={spr}");

                FrameHeader frameHead = new FrameHeader(_resMan.FetchFrame(_resMan.OpenFetchRes((uint)cpt.resource), 0));
                if (frameHead.offsetX != 0 && frameHead.offsetY != 0)
                { // is this a boxed mega?
                    cpt.status |= STAT_SHRINK;
                    cpt.anim_x = cpt.xcoord;
                    cpt.anim_y = cpt.ycoord;
                }
                else
                    cpt.status &= ~STAT_SHRINK;

                _resMan.ResClose((uint)cpt.resource);
            }
            if (SystemVars.PlaySpeech != 0)
                _speechRunning = _sound.StartSpeech((ushort)(textNo >> 16), (ushort)(textNo & 0xFFFF));
            else
                _speechRunning = false;
            _speechFinished = false;
            if (SystemVars.ShowText != 0 || (!_speechRunning))
            {
                _textRunning = true;

                var text = _objMan.LockText((uint) textNo);
                cpt.speech_time = GetTextLength(text) + 5;
                uint textCptId = _textMan.LowTextManager(text, cpt.speech_width, (byte)cpt.speech_pen);
                _objMan.UnlockText((uint) textNo);

                SwordObject textCpt = _objMan.FetchObject(textCptId);
                textCpt.screen = cpt.screen;
                textCpt.target = (int)textCptId;

                // the graphic is a property of Text, so we don't lock/unlock it.
                ushort textSpriteWidth = _resMan.ReadUInt16(new FrameHeader(_textMan.GiveSpriteData((byte)textCpt.target)).width);
                ushort textSpriteHeight = _resMan.ReadUInt16(new FrameHeader(_textMan.GiveSpriteData((byte)textCpt.target)).height);

                cpt.text_id = (int)textCptId;

                // now set text coords, above the player, usually

                const int TEXT_MARGIN = 3; // distance kept from edges of screen
                const int ABOVE_HEAD = 20; // distance kept above talking sprite
                ushort textX, textY;
                if (((id == GEORGE) || ((id == NICO) && (ScriptVars[(int)ScriptVariableNames.SCREEN] == 10))) && (cpt.anim_resource == 0))
                {
                    // if George is doing Voice-Over text (centered at the bottom of the screen)
                    textX = (ushort)(ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] + 128 + (640 / 2) - textSpriteWidth / 2);
                    textY = (ushort)(ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] + 128 + 400);
                }
                else
                {
                    if ((id == GEORGE) && (ScriptVars[(int)ScriptVariableNames.SCREEN] == 79))
                        textX = (ushort)cpt.mouse_x2; // move it off george's head
                    else
                        textX = (ushort)((cpt.mouse_x1 + cpt.mouse_x2) / 2 - textSpriteWidth / 2);

                    textY = (ushort)(cpt.mouse_y1 - textSpriteHeight - ABOVE_HEAD);
                }
                // now ensure text is within visible screen
                ushort textLeftMargin, textRightMargin, textTopMargin, textBottomMargin;
                textLeftMargin = (ushort)(Screen.SCREEN_LEFT_EDGE + TEXT_MARGIN + ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X]);
                textRightMargin = (ushort)(Screen.SCREEN_RIGHT_EDGE - TEXT_MARGIN + ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] - textSpriteWidth);
                textTopMargin = (ushort)(Screen.SCREEN_TOP_EDGE + TEXT_MARGIN + ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]);
                textBottomMargin = (ushort)(Screen.SCREEN_BOTTOM_EDGE - TEXT_MARGIN + ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] - textSpriteHeight);

                textCpt.anim_x = textCpt.xcoord = ScummHelper.Clip(textX, textLeftMargin, textRightMargin);
                textCpt.anim_y = textCpt.ycoord = ScummHelper.Clip(textY, textTopMargin, textBottomMargin);
            }
            return SCRIPT_STOP;
        }

        //send instructions to mega in conversation with player
        //the instruction is interpreted by the script mega_interact
        private int fnTheyDo(SwordObject cpt, int id, int tar, int instruc, int param1, int param2, int param3, int x)
        {
            SwordObject target;
            target = _objMan.FetchObject((uint)tar);
            target.down_flag = instruc; // instruction for the mega
            target.ins1 = param1;
            target.ins2 = param2;
            target.ins3 = param3;
            return SCRIPT_CONT;
        }

        //send an instruction to mega we're talking to and wait
        //until it has finished before returning to script
        private int fnTheyDoWeWait(SwordObject cpt, int id, int tar, int instruc, int param1, int param2, int param3, int x)
        {
            // workaround for scriptbug #928791: Freeze at hospital
            // in at least one game version, a script forgets to set sam_returning back to zero
            if ((tar == SAM) && (instruc == INS_talk) && (param2 == 2162856))
                ScriptVars[(int)ScriptVariableNames.SAM_RETURNING] = 0;
            SwordObject target = _objMan.FetchObject((uint)tar);
            target.down_flag = instruc; // instruction for the mega
            target.ins1 = param1;
            target.ins2 = param2;
            target.ins3 = param3;
            target.status &= ~STAT_TALK_WAIT;

            cpt.logic = LOGIC_wait_for_talk;
            cpt.down_flag = tar;
            return SCRIPT_STOP;
        }

        private int fnWeWait(SwordObject cpt, int id, int tar, int d, int e, int f, int z, int x)
        {
            SwordObject target = _objMan.FetchObject((uint)tar);
            target.status &= ~STAT_TALK_WAIT;

            cpt.logic = LOGIC_wait_for_talk;
            cpt.down_flag = tar;

            return SCRIPT_STOP;
        }

        private int fnChangeSpeechText(SwordObject cpt, int id, int tar, int width, int pen, int f, int z, int x)
        {
            SwordObject target = _objMan.FetchObject((uint)tar);
            target.speech_width = width;
            target.speech_pen = pen;
            return SCRIPT_STOP;
        }

        //mega_interact has received an instruction it does not understand -
        //The game is halted for debugging. Maybe we'll remove this later.
        private int fnTalkError(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            throw new InvalidOperationException($"fnTalkError for id {id}, instruction {cpt.down_flag}");
            return SCRIPT_STOP; // for compilers that don't support NORETURN
        }

        private int fnStartTalk(SwordObject cpt, int id, int target, int d, int e, int f, int z, int x)
        {
            cpt.down_flag = target;
            cpt.logic = LOGIC_start_talk;
            return SCRIPT_STOP;
        }

        private int fnCheckForTextLine(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.RETURN_VALUE] = _objMan.FnCheckForTextLine((uint) id);
            return SCRIPT_CONT;
        }

        private int fnAddTalkWaitStatusBit(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status |= STAT_TALK_WAIT;
            return SCRIPT_CONT;
        }

        private int fnRemoveTalkWaitStatusBit(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status &= ~STAT_TALK_WAIT;
            return SCRIPT_CONT;
        }

        private int fnNoHuman(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _mouse.FnNoHuman();
            return SCRIPT_CONT;
        }

        private int fnAddHuman(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _mouse.FnAddHuman();
            return SCRIPT_CONT;
        }

        private int fnBlankMouse(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _mouse.FnBlankMouse();
            return SCRIPT_CONT;
        }

        private int fnNormalMouse(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _mouse.FnNormalMouse();
            return SCRIPT_CONT;
        }

        private int fnLockMouse(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _mouse.FnLockMouse();
            return SCRIPT_CONT;
        }

        private int fnUnlockMouse(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _mouse.FnUnlockMouse();
            return SCRIPT_CONT;
        }

        private int fnSetMousePointer(SwordObject cpt, int id, int tag, int rate, int e, int f, int z, int x)
        {
            _mouse.SetPointer((uint) tag, (uint) rate);
            return SCRIPT_CONT;
        }

        private int fnSetMouseLuggage(SwordObject cpt, int id, int tag, int rate, int e, int f, int z, int x)
        {
            _mouse.SetLuggage((uint) tag, (uint) rate);
            return SCRIPT_CONT;
        }

        private int fnMouseOn(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status |= STAT_MOUSE;
            return SCRIPT_CONT;
        }

        private int fnMouseOff(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.status &= ~STAT_MOUSE;
            return SCRIPT_CONT;
        }

        private int fnChooser(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _menu.FnChooser(cpt);
            return SCRIPT_STOP;
        }

        private int fnEndChooser(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _menu.FnEndChooser();
            return SCRIPT_CONT;
        }

        private int fnStartMenu(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _menu.FnStartMenu();
            return SCRIPT_CONT;
        }

        private int fnEndMenu(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _menu.FnEndMenu();
            return SCRIPT_CONT;
        }

        private int cfnReleaseMenu(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            _menu.CfnReleaseMenu();
            return SCRIPT_STOP;
        }

        private int fnAddSubject(SwordObject cpt, int id, int sub, int d, int e, int f, int z, int x)
        {
            _menu.FnAddSubject(sub);
            return SCRIPT_CONT;
        }

        private int fnAddObject(SwordObject cpt, int id, int objectNo, int d, int e, int f, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.POCKET_1 + objectNo - 1] = 1; // basically means: carrying object objectNo = true;
            return SCRIPT_CONT;
        }

        private int fnRemoveObject(SwordObject cpt, int id, int objectNo, int d, int e, int f, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.POCKET_1 + objectNo - 1] = 0;
            return SCRIPT_CONT;
        }

        private int fnEnterSection(SwordObject cpt, int id, int screen, int d, int e, int f, int z, int x)
        {
            if (screen >= ObjectMan.TOTAL_SECTIONS)
                throw new InvalidOperationException($"mega {id} tried entering section {screen}");

            /* if (cpt.o_type == TYPE_PLAYER)
               ^= this was the original condition from the game sourcecode.
               not sure why it doesn't work*/
            if (id == PLAYER)
                ScriptVars[(int)ScriptVariableNames.NEW_SCREEN] = (uint)screen;
            else
                cpt.screen = screen; // move the mega
            _objMan.MegaEntering((ushort)screen);
            return SCRIPT_CONT;
        }

        private int fnLeaveSection(SwordObject cpt, int id, int oldScreen, int d, int e, int f, int z, int x)
        {
            if (oldScreen >= ObjectMan.TOTAL_SECTIONS)
                throw new InvalidOperationException($"mega {id} leaving section {oldScreen}");
            _objMan.MegaLeaving((ushort) oldScreen, id);
            return SCRIPT_CONT;
        }

        private int fnChangeFloor(SwordObject cpt, int id, int floor, int d, int e, int f, int z, int x)
        {
            cpt.place = floor;
            SwordObject floorCpt = _objMan.FetchObject((uint)floor);
            cpt.scale_a = floorCpt.scale_a;
            cpt.scale_b = floorCpt.scale_b;
            return SCRIPT_CONT;
        }

        private int fnWalk(SwordObject cpt, int id, int x, int y, int dir, int stance, int a, int b)
        {
            if (stance > 0)
                dir = 9;
            cpt.walk_pc = 0;
            cpt.route[1].frame = 512; // end of sequence
            if (id == PLAYER)
                _router.SetPlayerTarget(x, y, dir, stance);

            int routeRes = _router.RouteFinder(id, cpt, x, y, dir);

            if (id == PLAYER)
            {
                if ((routeRes == 1) || (routeRes == 2))
                {
                    ScriptVars[(int)ScriptVariableNames.MEGA_ON_GRID] = 0;
                    ScriptVars[(int)ScriptVariableNames.REROUTE_GEORGE] = 0;
                }
            }
            if ((routeRes == 1) || (routeRes == 2))
            {
                cpt.down_flag = 1; // 1 means okay.
                                   // if both mouse buttons were pressed on an exit => skip george's walk
                if ((id == GEORGE) && (_mouse.TestEvent() == Mouse.MOUSE_BOTH_BUTTONS))
                {
                    int target = (int)ScriptVars[(int)ScriptVariableNames.CLICK_ID];
                    // exceptions: compacts that use hand pointers but are not actually exits
                    if ((target != LEFT_SCROLL_POINTER) && (target != RIGHT_SCROLL_POINTER) &&
                            (target != FLOOR_63) && (target != ROOF_63) && (target != GUARD_ROOF_63) &&
                            (target != LEFT_TREE_POINTER_71) && (target != RIGHT_TREE_POINTER_71))
                    {

                        target = _objMan.FetchObject(ScriptVars[(int)ScriptVariableNames.CLICK_ID]).mouse_on;
                        if ((target >= SCR_exit0) && (target <= SCR_exit9))
                        {
                            fnStandAt(cpt, id, x, y, dir, stance, 0, 0);
                            return SCRIPT_STOP;
                        }
                    }
                }
                cpt.logic = LOGIC_AR_animate;
                return SCRIPT_STOP;
            }
            else if (routeRes == 3)
                cpt.down_flag = 1; // pretend it was successful
            else
                cpt.down_flag = 0; // 0 means error

            return SCRIPT_CONT;
        }

        private int fnTurn(SwordObject cpt, int id, int dir, int stance, int c, int d, int a, int b)
        {
            if (stance > 0)
                dir = 9;
            int route = _router.RouteFinder(id, cpt, cpt.xcoord, cpt.ycoord, dir);

            if (route != 0)
                cpt.down_flag = 1;       //1 means ok
            else
                cpt.down_flag = 0;       //0 means error

            cpt.logic = LOGIC_AR_animate;
            cpt.walk_pc = 0;                     //reset

            return SCRIPT_STOP;
        }

        private int fnStand(SwordObject cpt, int id, int dir, int stance, int c, int d, int a, int b)
        {
            if ((dir < 0) || (dir > 8))
            {
                // TODO: warning("fnStand:: invalid direction %d", dir);
                return SCRIPT_CONT;
            }
            if (dir == 8)
                dir = cpt.dir;
            cpt.resource = cpt.walk_resource;
            cpt.status |= STAT_SHRINK;
            cpt.anim_x = cpt.xcoord;
            cpt.anim_y = cpt.ycoord;
            cpt.frame = 96 + dir;
            cpt.dir = dir;
            return SCRIPT_STOP;
        }

        private int fnStandAt(SwordObject cpt, int id, int x, int y, int dir, int stance, int a, int b)
        {
            if ((dir < 0) || (dir > 8))
            {
                // TODO: warning("fnStandAt:: invalid direction %d", dir);
                return SCRIPT_CONT;
            }
            if (dir == 8)
                dir = cpt.dir;
            cpt.xcoord = x;
            cpt.ycoord = y;
            return fnStand(cpt, id, dir, stance, 0, 0, 0, 0);
        }

        private int fnFace(SwordObject cpt, int id, int targetId, int b, int c, int d, int a, int z)
        {
            SwordObject target = _objMan.FetchObject((uint)targetId);
            int x, y;
            if ((target.type == Screen.TYPE_MEGA) || (target.type == Screen.TYPE_PLAYER))
            {
                x = target.xcoord;
                y = target.ycoord;
            }
            else
            {
                x = (target.mouse_x1 + target.mouse_x2) / 2;
                y = target.mouse_y2;
            }
            int megaTarDir = WhatTarget(cpt.xcoord, cpt.ycoord, x, y);
            fnTurn(cpt, id, megaTarDir, 0, 0, 0, 0, 0);
            return SCRIPT_STOP;
        }

        private const int DIAGONALX = 36;
        private const int DIAGONALY = 8;

        private int WhatTarget(int startX, int startY, int destX, int destY)
        {
            int tar_dir;
            //setting up
            int deltaX = destX - startX;
            int deltaY = destY - startY;
            int signX = (deltaX > 0)?1:0;
            int signY = (deltaY > 0) ? 1 : 0;
            int slope;

            if ((Math.Abs(deltaY) * DIAGONALX) < (Math.Abs(deltaX) * DIAGONALY / 2))
                slope = 0;// its flat
            else if ((Math.Abs(deltaY) * DIAGONALX / 2) > (Math.Abs(deltaX) * DIAGONALY))
                slope = 2;// its vertical
            else
                slope = 1;// its diagonal

            if (slope == 0)
            { //flat
                if (signX == 1) // going right
                    tar_dir = 2;
                else
                    tar_dir = 6;
            }
            else if (slope == 2)
            { //vertical
                if (signY == 1) // going down
                    tar_dir = 4;
                else
                    tar_dir = 0;
            }
            else if (signX == 1)
            { //right diagonal
                if (signY == 1) // going down
                    tar_dir = 3;
                else
                    tar_dir = 1;
            }
            else
            { //left diagonal
                if (signY == 1) // going down
                    tar_dir = 5;
                else
                    tar_dir = 7;
            }
            return tar_dir;
        }

        private int fnFaceXy(SwordObject cpt, int id, int x, int y, int c, int d, int a, int b)
        {
            int megaTarDir = WhatTarget(cpt.xcoord, cpt.ycoord, x, y);
            fnTurn(cpt, id, megaTarDir, 0, 0, 0, 0, 0);
            return SCRIPT_STOP;
        }

        private int fnIsFacing(SwordObject cpt, int id, int targetId, int b, int c, int d, int a, int z)
        {
            SwordObject target = _objMan.FetchObject((uint)targetId);
            int x, y, dir;
            if ((target.type == Screen.TYPE_MEGA) || (target.type == Screen.TYPE_PLAYER))
            {
                x = target.xcoord;
                y = target.ycoord;
                dir = target.dir;
            }
            else
                throw new InvalidOperationException("fnIsFacing:: Target isn't a mega");

            int lookDir = WhatTarget(x, y, cpt.xcoord, cpt.ycoord);
            lookDir -= dir;
            lookDir = Math.Abs(lookDir);

            if (lookDir > 4)
                lookDir = 8 - lookDir;

            ScriptVars[(int)ScriptVariableNames.RETURN_VALUE] = (uint)lookDir;
            return SCRIPT_STOP;
        }

        private int fnGetTo(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            SwordObject place = _objMan.FetchObject((uint)cpt.place);

            cpt.tree.script_level++;
            cpt.tree.script_pc[cpt.tree.script_level] = place.get_to_script;
            cpt.tree.script_id[cpt.tree.script_level] = place.get_to_script;
            return SCRIPT_STOP;
        }

        private int fnGetToError(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            // TODO: debug(1, "fnGetToError: compact %d at place %d no get-to for target %d, click_id %d\n", id, cpt.o_place, cpt.o_target, ScriptVars[(int) ScriptVariableNames.CLICK_ID]);
            return SCRIPT_CONT;
        }

        private int fnRandom(SwordObject compact, int id, int min, int max, int e, int f, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.RETURN_VALUE] = (uint)_rnd.Next(min, max);
            return SCRIPT_CONT;
        }

        private int fnGetPos(SwordObject cpt, int id, int targetId, int b, int c, int d, int z, int x)
        {
            SwordObject target = _objMan.FetchObject((uint)targetId);
            if ((target.type == Screen.TYPE_MEGA) || (target.type == Screen.TYPE_PLAYER))
            {
                ScriptVars[(int)ScriptVariableNames.RETURN_VALUE] = (uint)target.xcoord;
                ScriptVars[(int)ScriptVariableNames.RETURN_VALUE_2] = (uint)target.ycoord;
            }
            else
            {
                ScriptVars[(int)ScriptVariableNames.RETURN_VALUE] = (uint)((target.mouse_x1 + target.mouse_x2) / 2);
                ScriptVars[(int)ScriptVariableNames.RETURN_VALUE_2] = (uint)target.mouse_y2;
            }
            ScriptVars[(int)ScriptVariableNames.RETURN_VALUE_3] = (uint)target.dir;

            int megaSeperation;
            if (targetId == DUANE)
                megaSeperation = 70; // George & Duane stand with feet 70 pixels apart when at full scale
            else if (targetId == BENOIR)
                megaSeperation = 61; // George & Benoir
            else
                megaSeperation = 42; // George & Nico/Goinfre stand with feet 42 pixels apart when at full scale

            if ((target.status & STAT_SHRINK) != 0)
            {
                int scale = (target.scale_a * target.ycoord + target.scale_b) / 256;
                ScriptVars[(int)ScriptVariableNames.RETURN_VALUE_4] = (uint)((megaSeperation * scale) / 256);
            }
            else
                ScriptVars[(int)ScriptVariableNames.RETURN_VALUE_4] = (uint)megaSeperation;
            return SCRIPT_CONT;
        }

        private int fnGetGamepadXy(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            // playstation only
            return SCRIPT_CONT;
        }

        private int fnPlayFx(SwordObject cpt, int id, int fxNo, int b, int c, int d, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.RETURN_VALUE] = _sound.AddToQueue(fxNo);
            return SCRIPT_CONT;
        }

        private int fnStopFx(SwordObject cpt, int id, int fxNo, int b, int c, int d, int z, int x)
        {
            _sound.FnStopFx(fxNo);
            //_sound.removeFromQueue(fxNo);
            return SCRIPT_CONT;
        }

        private int fnPlayMusic(SwordObject cpt, int id, int tuneId, int loopFlag, int c, int d, int z, int x)
        {
            if (tuneId == 153)
                return SCRIPT_CONT;
            if (loopFlag == LOOPED)
                ScriptVars[(int)ScriptVariableNames.CURRENT_MUSIC] = (uint)tuneId; // so it gets restarted when saving & reloading
            else
                ScriptVars[(int)ScriptVariableNames.CURRENT_MUSIC] = 0;

            _music.StartMusic(tuneId, loopFlag);
            return SCRIPT_CONT;
        }

        private int fnStopMusic(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            ScriptVars[(int)ScriptVariableNames.CURRENT_MUSIC] = 0;
            _music.FadeDown();
            return SCRIPT_CONT;
        }

        private int fnInnerSpace(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            throw new InvalidOperationException("fnInnerSpace() not working");
            return SCRIPT_STOP; // for compilers that don't support NORETURN
        }

        private int fnSetScreen(SwordObject cpt, int id, int target, int screen, int c, int d, int z, int x)
        {
            _objMan.FetchObject((uint)target).screen = screen;
            return SCRIPT_CONT;
        }

        private int fnPreload(SwordObject cpt, int id, int resId, int b, int c, int d, int z, int x)
        {
            _resMan.ResOpen((uint)resId);
            _resMan.ResClose((uint)resId);
            return SCRIPT_CONT;
        }

        private int fnCheckCD(SwordObject cpt, int id, int screen, int b, int c, int d, int z, int x)
        {
            // only a dummy, here.
            // the check is done in the mainloop
            return SCRIPT_CONT;
        }

        private int fnRestartGame(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            SystemVars.ForceRestart = true;
            cpt.logic = LOGIC_quit;
            return SCRIPT_STOP;
        }

        private int fnQuitGame(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            if (SystemVars.IsDemo)
            {
                // TODO:GUI::MessageDialog dialog(_("This is the end of the Broken Sword 1 Demo"), _("OK"), NULL);
                //dialog.runModal();
                SwordEngine.ShouldQuit = true;
            }
            else
                throw new InvalidOperationException("fnQuitGame() called");
            return fnQuit(cpt, id, 0, 0, 0, 0, 0, 0);
        }

        private int fnDeathScreen(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {

            if (ScriptVars[(int)ScriptVariableNames.FINALE_OPTION_FLAG] == 4) // successful end of game!
                SystemVars.ControlPanelMode = ControlPanelMode.CP_THEEND;
            else
                SystemVars.ControlPanelMode = ControlPanelMode.CP_DEATHSCREEN;

            cpt.logic = LOGIC_quit;
            return SCRIPT_STOP;
        }

        private int fnSetParallax(SwordObject cpt, int id, int screen, int resId, int c, int d, int z, int x)
        {
            _screen.FnSetParallax((uint) screen, (uint) resId);
            return SCRIPT_CONT;
        }

        private int fnTdebug(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            // TODO: debug(1, "Script TDebug id %d code %d, %d", id, a, b);
            return SCRIPT_CONT;
        }

        private int fnRedFlash(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            _screen.FnFlash(Screen.FLASH_RED);
            return SCRIPT_CONT;
        }

        private int fnBlueFlash(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            _screen.FnFlash(Screen.FLASH_BLUE);
            return SCRIPT_CONT;
        }

        private int fnYellow(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            _screen.FnFlash(Screen.BORDER_YELLOW);
            return SCRIPT_CONT;
        }

        private int fnGreen(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            _screen.FnFlash(Screen.BORDER_GREEN);
            return SCRIPT_CONT;
        }

        private int fnPurple(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            _screen.FnFlash(Screen.BORDER_PURPLE);
            return SCRIPT_CONT;
        }

        private int fnBlack(SwordObject cpt, int id, int a, int b, int c, int d, int z, int x)
        {
            _screen.FnFlash(Screen.BORDER_BLACK);
            return SCRIPT_CONT;
        }
    }
}
