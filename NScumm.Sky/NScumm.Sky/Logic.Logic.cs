using System;

namespace NScumm.Sky
{
    internal partial class Logic
    {
        private void SetupLogicTable()
        {
            _logicTable = new Action[] {
                Nop,
                LogicScript,	 // 1  script processor
		        AutoRoute,	 // 2  Make a route
		        ArAnim,	 // 3  Follow a route
		        ArTurn,	 // 4  Mega turns araound
		        Alt,		 // 5  Set up new get-to script
		        Anim,	 // 6  Follow a sequence
		        Turn,	 // 7  Mega turning
		        Cursor,	 // 8  id tracks the pointer
		        Talk,	 // 9  count down and animate
		        Listen,	 // 10 player waits for talking id
		        Stopped,	 // 11 wait for id to move
		        Choose,	 // 12 wait for player to click
		        Frames,	 // 13 animate just frames
		        Pause,	 // 14 Count down to 0 and go
		        WaitSync,	 // 15 Set to l_script when sync!=0
		        SimpleAnim,	 // 16 Module anim without x,y's
	        };
        }

        private void SimpleAnim()
        {
            // follow an animation sequence module whilst ignoring the coordinate data

            var grafixProg = _skyCompact.GetGrafixPtr(_compact);

            // *grafix_prog: command
            while (grafixProg.Value != 0)
            {
                _compact.Core.grafixProgPos += 3;
                if (grafixProg.Value != SEND_SYNC)
                {
                    grafixProg.Offset += 2;
                    grafixProg.Offset += 2; // skip coordinates

                    // *grafix_prog: frame
                    if (grafixProg.Value >= 64)
                        _compact.Core.frame = grafixProg.Value;
                    else
                        _compact.Core.frame = (ushort)(grafixProg.Value + _compact.Core.offset);

                    return;
                }

                grafixProg.Offset += 2;
                // *grafix_prog: id to sync
                Compact compact2 = _skyCompact.FetchCpt(grafixProg.Value);
                grafixProg.Offset += 2;

                // *grafix_prog: sync
                compact2.Core.sync = grafixProg.Value;
                grafixProg.Offset += 2;
            }

            _compact.Core.downFlag = 0; // return 'ok' to script
            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void WaitSync()
        {
            // checks c_sync, when its non 0
            // the id is put back into script mode
            // use this instead of loops in the script

            if (_compact.Core.sync == 0)
                return;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void Pause()
        {
            if (--_compact.Core.flag != 0)
                return;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void Frames()
        {
            if (_compact.Core.sync == 0)
                SimpleAnim();
            else
            {
                _compact.Core.downFlag = 0; // return 'ok' to script
                _compact.Core.logic = L_SCRIPT;
                LogicScript();
            }
        }

        private void Choose()
        {
            // Remain in this mode until player selects some text
            if (_scriptVariables[THE_CHOSEN_ONE] == 0)
                return;

            FnNoHuman(0, 0, 0); // kill mouse again

            SystemVars.Instance.SystemFlags &= ~SystemFlags.Choosing; // restore save/restore

            _compact.Core.logic = L_SCRIPT; // and continue script
            LogicScript();
        }

        private void Stopped()
        {
            // waiting for another mega to move or give-up trying
            //
            // this mode will always be set up from a special script
            // that will be one level higher than the script we
            // would wish to restart from

            Compact cpt = _skyCompact.FetchCpt(_compact.Core.waitingFor);

            if (cpt != null)
                if (cpt.Core.mood == 0 && Collide(cpt))
                    return;

            // we are free, continue processing the script

            // restart script one level below
            SkyCompact.GetSub(_compact, _compact.Core.mode - 2).Field = 0;
            _compact.Core.waitingFor = 0xffff;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void Listen()
        {
            // Stay in this mode until id in getToFlag leaves L_TALK mode

            Compact cpt = _skyCompact.FetchCpt(_compact.Core.flag);

            if (cpt.Core.logic == L_TALK)
                return;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void Talk()
        {
            // first count through the frames
            // just frames - nothing tweeky
            // the speech finishes when the timer runs out &
            // not when the animation finishes
            // this routine is very task specific

            // TODO: Check for mouse clicking

            // Are we allowed to click

            if (_skyMouse.WasClicked)
                for (int i = 0; i < ClickTable.Length; i++)
                    if (ClickTable[i] == (ushort)_scriptVariables[CUR_ID])
                    {
                        if (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.AllowSpeech) && !_skySound.SpeechFinished)
                            _skySound.StopSpeech();
                        if ((_compact.Core.spTextId > 0) &&
                            (_compact.Core.spTextId < 0xFFFF))
                        {

                            _skyCompact.FetchCpt(_compact.Core.spTextId).Core.status = 0;
                        }
                        if (_skyCompact.GetGrafixPtr(_compact) != null)
                        {
                            _compact.Core.frame = _compact.Core.getToFlag; // set character to stand
                            _compact.Core.grafixProgId = 0;
                        }

                        _compact.Core.logic = L_SCRIPT;
                        LogicScript();
                        return;
                    }

            // If speech is allowed then check for it to finish before finishing animations

            if ((_compact.Core.spTextId == 0xFFFF) && // is this a voc file?
                _skySound.SpeechFinished)
            { // finished?

                _compact.Core.logic = L_SCRIPT; // restart character control

                if (_skyCompact.GetGrafixPtr(_compact) != null)
                {
                    _compact.Core.frame = _compact.Core.getToFlag; // set character to stand
                    _compact.Core.grafixProgId = 0;
                }

                LogicScript();
                return;
            }

            var graphixProg = _skyCompact.GetGrafixPtr(_compact);
            if (graphixProg != null)
            {
                if ((graphixProg[0] != 0) && ((_compact.Core.spTime != 3) || !_skySound.SpeechFinished))
                {
                    // we will force the animation to finish 3 game cycles
                    // before the speech actually finishes - because it looks good.

                    _compact.Core.frame = (ushort)(graphixProg[2] + _compact.Core.offset);
                    graphixProg.Offset += 3 * 2;
                    _compact.Core.grafixProgPos += 3;
                }
                else
                {
                    // we ran out of frames or finished speech, let actor stand still.
                    _compact.Core.frame = _compact.Core.getToFlag;
                    _compact.Core.grafixProgId = 0;
                }
            }

            if (_skySound.SpeechFinished) _compact.Core.spTime--;

            if (_compact.Core.spTime == 0)
            {

                // ok, speech has finished

                if (_compact.Core.spTextId != 0)
                {
                    Compact cpt = _skyCompact.FetchCpt(_compact.Core.spTextId); // get text id to kill
                    cpt.Core.status = 0; // kill the text
                }

                _compact.Core.logic = L_SCRIPT;
                LogicScript();
            }
        }

        private void Cursor()
        {
            _skyText.LogicCursor(_compact, _skyMouse.MouseX, _skyMouse.MouseY);
        }

        private void Turn()
        {
            var turnData = new UShortAccess(_skyCompact.FetchCptRaw(_compact.Core.turnProgId), _compact.Core.turnProgPos * 2);
            if (turnData[0] != 0)
            {
                _compact.Core.frame = turnData[0];
                _compact.Core.turnProgPos++;
                return;
            }

            // turn_to_script:
            _compact.Core.arAnimIndex = 0;
            _compact.Core.logic = L_SCRIPT;

            LogicScript();
        }

        private void Anim()
        {
            // Follow an animation sequence
            var grafixProg = _skyCompact.GetGrafixPtr(_compact);

            while (grafixProg.Value != 0)
            {
                _compact.Core.grafixProgPos += 3; // all types are 3 words.
                if (grafixProg.Value == LF_START_FX)
                { // do fx
                    grafixProg.Offset += 2;
                    ushort sound = grafixProg.Value; grafixProg.Offset += 2;
                    ushort volume = grafixProg.Value; grafixProg.Offset += 2;

                    // channel 0
                    FnStartFx(sound, 0, volume);
                }
                else if (grafixProg.Value >= LF_START_FX)
                { // do sync
                    grafixProg.Offset += 2;

                    Compact cpt = _skyCompact.FetchCpt(grafixProg.Value); grafixProg.Offset += 2;

                    cpt.Core.sync = grafixProg.Value; grafixProg.Offset += 2;
                }
                else
                { // put coordinates and frame in
                    _compact.Core.xcood = grafixProg.Value; grafixProg.Offset += 2;
                    _compact.Core.ycood = grafixProg.Value; grafixProg.Offset += 2;

                    _compact.Core.frame = (ushort)(grafixProg.Value | _compact.Core.offset); grafixProg.Offset += 2;
                    return;
                }
            }

            _compact.Core.downFlag = 0;
            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void Alt()
        {
            // change the current script
            _compact.Core.logic = L_SCRIPT;
            SkyCompact.GetSub(_compact, _compact.Core.mode).Field = _compact.Core.alt;
            SkyCompact.GetSub(_compact, _compact.Core.mode + 2).Field = 0;
            LogicScript();
        }

        private void ArTurn()
        {
            var turnData = new UShortAccess(_skyCompact.FetchCptRaw(_compact.Core.turnProgId), _compact.Core.turnProgPos * 2);
            _compact.Core.frame = turnData[0];
            turnData.Offset += 2;
            _compact.Core.turnProgPos++;

            if (turnData[0] == 0)
            { // turn done?
              // Back to ar mode
                _compact.Core.arAnimIndex = 0;
                _compact.Core.logic = L_AR_ANIM;
            }
        }

        private void ArAnim()
        {
            // Follow a route
            // Mega should be in getToMode

            // only check collisions on character boundaries
            if (((_compact.Core.xcood & 7) != 0) || ((_compact.Core.ycood & 7) != 0))
            {
                MainAnim();
                return;
            }

            // On character boundary. Have we been told to wait?
            // if not - are WE colliding?

            if (_compact.Core.waitingFor == 0xffff)
            { // 1st cycle of re-route does not require collision checks
                MainAnim();
                return;
            }

            if (_compact.Core.waitingFor != 0)
            {
                // ok, we've been told we've hit someone
                // we will wait until we are no longer colliding
                // with them. here we check to see if we are (still) colliding.
                // if we are then run the stop script. if not clear the flag
                // and continue.

                // remember - this could be the first ar cycle for some time,
                // we might have been told to wait months ago. if we are
                // waiting for one person then another hits us then
                // c_waiting_for will be replaced by the new mega - this is
                // fine because the later collision will almost certainly
                // take longer to clear than the earlier one.

                if (Collide(_skyCompact.FetchCpt(_compact.Core.waitingFor)))
                {
                    StopAndWait();
                    return;
                }

                // we are not in fact hitting this person so clr & continue
                // it must have registered some time ago

                _compact.Core.waitingFor = 0; // clear id flag
            }

            // ok, our turn to check for collisions

            var logicList = new UShortAccess(_skyCompact.FetchCptRaw((ushort)_scriptVariables[LOGIC_LIST_NO]), 0);
            ushort id;

            while ((id = logicList[0]) != 0)
            { // get an id

                logicList.Offset += 2;
                if (id == 0xffff)
                { // address change?
                    logicList = new UShortAccess(_skyCompact.FetchCptRaw(logicList[0]), 0); // get new logic list
                    continue;
                }

                if (id == (ushort)(_scriptVariables[CUR_ID] & 0xffff)) // is it us?
                    continue;

                _scriptVariables[HIT_ID] = id; // save target id for any possible c_mini_bump
                var cpt = _skyCompact.FetchCpt(id);

                if ((cpt.Core.status & (1 << ST_COLLISION_BIT)) == 0) // can it collide?
                    continue;

                if (cpt.Core.screen != _compact.Core.screen) // is it on our screen?
                    continue;

                if (Collide(cpt))
                { // check for a hit
                  // ok, we've hit a mega
                  // is it moving... or something else?

                    if (cpt.Core.logic != L_AR_ANIM)
                    { // check for following route
                      // it is doing something else
                      // we restart our get-to script
                      // first tell it to wait for us - in case it starts moving
                      // ( *it may have already hit us and stopped to wait )

                        _compact.Core.waitingFor = 0xffff; // effect 1 cycle collision skip
                                                           // tell it it is waiting for us
                        cpt.Core.waitingFor = (ushort)(_scriptVariables[CUR_ID] & 0xffff);
                        // restart current script
                        SkyCompact.GetSub(_compact, _compact.Core.mode + 2).Field = 0;
                        _compact.Core.logic = L_SCRIPT;
                        LogicScript();
                        return;
                    }

                    Script(_compact.Core.miniBump, 0);
                    return;
                }
            }

            // ok, there was no collisions
            // now check for interaction request
            // *note: the interaction is always set up as an action script

            if (_compact.Core.request != 0)
            {
                _compact.Core.mode = C_ACTION_MODE; // put into action mode
                _compact.Core.actionSub = _compact.Core.request;
                _compact.Core.actionSub_off = 0;
                _compact.Core.request = 0; // trash request
                _compact.Core.logic = L_SCRIPT;
                LogicScript();
                return;
            }

            // any flag? - or any change?
            // if change then re-run the current script, which must be
            // a position independent get-to		 ----

            if (_compact.Core.atWatch == 0)
            { // any flag set?
                MainAnim();
                return;
            }

            // ok, there is an at watch - see if it's changed

            if (_compact.Core.atWas == _scriptVariables[_compact.Core.atWatch / 4])
            { // still the same?
                MainAnim();
                return;
            }

            // changed so restart the current script
            // *not suitable for base initiated ARing
            SkyCompact.GetSub(_compact, _compact.Core.mode + 2).Field = 0;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void AutoRoute()
        {
            _compact.Core.downFlag = _skyAutoRoute.DoAutoRoute(_compact);
            if ((_compact.Core.downFlag == 2) && _skyCompact.CptIsId(_compact, (ushort)CptIds.Joey) &&
               (_compact.Core.mode == 0) && (_compact.Core.baseSub == JOEY_OUT_OF_LIFT))
            {
                // workaround for script bug #1064113. Details unclear...
                _compact.Core.downFlag = 0;
            }
            if (_compact.Core.downFlag != 1)
            { // route ok
                _compact.Core.grafixProgId = _compact.Core.animScratchId;
                _compact.Core.grafixProgPos = 0;
            }

            _compact.Core.logic = L_SCRIPT; // continue the script

            LogicScript();
        }

        /// <summary>
        /// This function is basicly a wrapper around the real script engine. It runs
        /// the script engine until a script has finished.
        /// </summary>
        private void LogicScript()
        {
            // Process the current mega's script
            // If the script finishes then drop back a level

            for (;;)
            {
                ushort mode = _compact.Core.mode; // get pointer to current script
                var scriptNo = SkyCompact.GetSub(_compact, mode);
                var offset = SkyCompact.GetSub(_compact, (ushort)(mode + 2));

                offset.Field = Script(scriptNo.Field, offset.Field);

                if (offset.Field == 0) // script finished
                    _compact.Core.mode -= 4;
                else if (_compact.Core.mode == mode)
                    return;
            }
        }

        private void Nop()
        {
        }

        private static readonly ushort[] ClickTable = {
            ID_FOSTER,
            ID_JOEY,
            ID_JOBS,
            ID_LAMB,
            ID_ANITA,
            ID_SON,
            ID_DAD,
            ID_MONITOR,
            ID_SHADES,
            MINI_SS,
            FULL_SS,
            ID_FOREMAN,
            ID_RADMAN,
            ID_GALLAGER_BEL,
            ID_BURKE,
            ID_BODY,
            ID_HOLO,
            ID_TREVOR,
            ID_ANCHOR,
            ID_WRECK_GUARD,
            ID_SKORL_GUARD,

	        // BASE LEVEL
	        ID_SC30_HENRI,
            ID_SC31_GUARD,
            ID_SC32_VINCENT,
            ID_SC32_GARDENER,
            ID_SC32_BUZZER,
            ID_SC36_BABS,
            ID_SC36_BARMAN,
            ID_SC36_COLSTON,
            ID_SC36_GALLAGHER,
            ID_SC36_JUKEBOX,
            ID_DANIELLE,
            ID_SC42_JUDGE,
            ID_SC42_CLERK,
            ID_SC42_PROSECUTION,
            ID_SC42_JOBSWORTH,

	        // UNDERWORLD
	        ID_MEDI,
            ID_WITNESS,
            ID_GALLAGHER,
            ID_KEN,
            ID_SC76_ANDROID_2,
            ID_SC76_ANDROID_3,
            ID_SC81_FATHER,
            ID_SC82_JOBSWORTH,

	        // LINC WORLD
	        ID_HOLOGRAM_B,
            12289
        };
    }
}
