namespace NScumm.Sword1
{
    public static class MenuObject
    {
        public const int textDesc = 0;
        public const int bigIconRes = 1;
        public const int bigIconFrame = 2;
        public const int luggageIconRes = 3;
        public const int useScript = 4;
    }

    enum MenuState
    {
        MENU_CLOSED,
        MENU_CLOSING,
        MENU_OPENING,
        MENU_OPEN
    }

    internal class Menu
    {
        const int TOTAL_pockets = 52;
        public const int MENU_TOP = 0;
        public const int MENU_BOT = 1;
        const int BASE_SUBJECT = 256;

        private Screen _screen;
        private Mouse _mouse;
        private MenuState _subjectBarStatus;
        private MenuState _objectBarStatus;
        private uint _inMenu;
        private uint[] _menuList = new uint[TOTAL_pockets];
        MenuIcon[] _objects = new MenuIcon[TOTAL_pockets];
        sbyte _fadeObject;
        MenuIcon[] _subjects = new MenuIcon[16];
        uint[] _subjectBar = new uint[16];
        private sbyte _fadeSubject;

        static readonly byte[] _fadeEffectTop = {
            1, 7, 5, 3, 2, 4, 6, 0,
            3, 1, 7, 5, 4, 6, 0, 2,
            5, 3, 1, 7, 6, 0, 2, 4,
            7, 5, 3, 1, 0, 2, 4, 6,
            7, 5, 3, 1, 0, 2, 4, 6,
            5, 3, 1, 7, 6, 0, 2, 4,
            3, 1, 7, 5, 4, 6, 0, 2,
            1, 7, 5, 3, 2, 4, 6, 0
        };

        static readonly byte[] _fadeEffectBottom = {
            7, 6, 5, 4, 3, 2, 1, 0,
            0, 7, 6, 5, 4, 3, 2, 1,
            1, 0, 7, 6, 5, 4, 3, 2,
            2, 1, 0, 7, 6, 5, 4, 3,
            3, 2, 1, 0, 7, 6, 5, 4,
            4, 3, 2, 1, 0, 7, 6, 5,
            5, 4, 3, 2, 1, 0, 7, 6,
            6, 5, 4, 3, 2, 1, 0, 7
        };

        public Menu(Screen screen, Mouse mouse)
        {
            _screen = screen;
            _mouse = mouse;
            _subjectBarStatus = MenuState.MENU_CLOSED;
            _objectBarStatus = MenuState.MENU_CLOSED;
        }

        public void Refresh(byte menuType)
        {
            uint i;

            if (menuType == MENU_TOP)
            {
                if (_objectBarStatus == MenuState.MENU_OPENING || _objectBarStatus == MenuState.MENU_CLOSING)
                {
                    for (i = 0; i < 16; i++)
                    {
                        if (_objects[i] != null)
                            _objects[i].Draw(_fadeEffectTop, _fadeObject);
                        else
                            _screen.ShowFrame((ushort)(i * 40), 0, 0xffffffff, 0, _fadeEffectTop, _fadeObject);
                    }
                }
                if (_objectBarStatus == MenuState.MENU_OPENING)
                {
                    if (_fadeObject < 8)
                        _fadeObject++;
                    else
                        _objectBarStatus = MenuState.MENU_OPEN;
                }
                else if (_objectBarStatus == MenuState.MENU_CLOSING)
                {
                    if (_fadeObject > 0)
                        _fadeObject--;
                    else
                    {
                        for (i = 0; i < _inMenu; i++)
                        {
                            _objects[i] = null;
                        }
                        _objectBarStatus = MenuState.MENU_CLOSED;
                    }
                }
            }
            else
            {
                if (_subjectBarStatus == MenuState.MENU_OPENING || _subjectBarStatus == MenuState.MENU_CLOSING)
                {
                    for (i = 0; i < 16; i++)
                    {
                        if (_subjects[i] != null)
                            _subjects[i].Draw(_fadeEffectBottom, _fadeSubject);
                        else
                            _screen.ShowFrame((ushort)(i * 40), 440, 0xffffffff, 0, _fadeEffectBottom, _fadeSubject);
                    }
                }
                if (_subjectBarStatus == MenuState.MENU_OPENING)
                {
                    if (_fadeSubject < 8)
                        _fadeSubject++;
                    else
                        _subjectBarStatus = MenuState.MENU_OPEN;
                }
                else if (_subjectBarStatus == MenuState.MENU_CLOSING)
                {
                    if (_fadeSubject > 0)
                        _fadeSubject--;
                    else
                    {
                        for (i = 0; i < Logic.ScriptVars[(int)ScriptVariableNames.IN_SUBJECT]; i++)
                        {
                            _subjects[i] = null;
                        }
                        _subjectBarStatus = MenuState.MENU_CLOSED;
                    }
                }
            }
        }

        public void FnChooser(SwordObject compact)
        {
            Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = 0;
            _mouse.SetLuggage(0, 0);
            BuildSubjects();
            compact.logic = Logic.LOGIC_choose;
            _mouse.ControlPanel(true); // so the mouse cursor will be shown.
            _subjectBarStatus = MenuState.MENU_OPENING;
        }

        private void BuildSubjects()
        {
            for (var cnt = 0; cnt < 16; cnt++)
                _subjects[cnt] = null;

            for (var cnt = 0; cnt < Logic.ScriptVars[(int)ScriptVariableNames.IN_SUBJECT]; cnt++)
            {
                uint res = (uint)_subjectList[(_subjectBar[cnt] & 65535) - BASE_SUBJECT, 0];
                uint frame = (uint)_subjectList[(_subjectBar[cnt] & 65535) - BASE_SUBJECT, 1];
                _subjects[cnt] = new MenuIcon(MENU_BOT, (byte)cnt, res, frame, _screen);
                if (Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] != 0)
                    _subjects[cnt].SetSelect(_subjectBar[cnt] == Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD]);
                else
                    _subjects[cnt].SetSelect(true);
            }
        }

        public void FnEndChooser()
        {
            Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = 0;
            _subjectBarStatus = MenuState.MENU_CLOSING;
            _objectBarStatus = MenuState.MENU_CLOSING;
            _mouse.ControlPanel(false);
            _mouse.SetLuggage(0, 0);
        }

        public void FnStartMenu()
        {
            Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = 0; // icon no longer selected
            Logic.ScriptVars[(int)ScriptVariableNames.SECOND_ITEM] = 0; // second icon no longer selected (after using one on another)
            Logic.ScriptVars[(int)ScriptVariableNames.MENU_LOOKING] = 0; // no longer 'looking at' an icon
            BuildMenu();
            ShowMenu(MENU_TOP);
        }

        private void ShowMenu(int menuType)
        {
            if (menuType == MENU_TOP)
            {
                if (_objectBarStatus == MenuState.MENU_OPEN)
                {
                    for (var cnt = 0; cnt < 16; cnt++)
                    {
                        if (_objects[cnt] != null)
                            _objects[cnt].Draw();
                        else
                            _screen.ShowFrame((ushort)(cnt * 40), 0, 0xffffffff, 0);
                    }
                }
                else if (_objectBarStatus == MenuState.MENU_CLOSED)
                {
                    _objectBarStatus = MenuState.MENU_OPENING;
                    _fadeObject = 0;
                }
                else if (_objectBarStatus == MenuState.MENU_CLOSING)
                    _objectBarStatus = MenuState.MENU_OPENING;
            }
        }

        private void BuildMenu()
        {
            for (var cnt = 0; cnt < _inMenu; cnt++)
                _objects[cnt] = null;

            _inMenu = 0;
            for (var pocketNo = 0; pocketNo < TOTAL_pockets; pocketNo++)
            {
                if (Logic.ScriptVars[(int)(ScriptVariableNames.POCKET_1 + pocketNo)] != 0)
                {
                    _menuList[_inMenu] = (uint)(pocketNo + 1);
                    _inMenu++;
                }
            }

            for (var menuSlot = 0; menuSlot < _inMenu; menuSlot++)
            {
                _objects[menuSlot] = new MenuIcon(MENU_TOP, (byte)menuSlot, (uint) _objectDefs[_menuList[menuSlot],MenuObject.bigIconRes], (uint) _objectDefs[_menuList[menuSlot], MenuObject.bigIconFrame], _screen);
                uint objHeld = Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD];

                // check highlighting
                if (Logic.ScriptVars[(int)ScriptVariableNames.MENU_LOOKING] != 0 || _subjectBarStatus == MenuState.MENU_OPEN)
                { // either we're in the chooser or we're doing a 'LOOK AT'
                    if ((objHeld == 0) || (objHeld == _menuList[menuSlot]))
                        _objects[menuSlot].SetSelect(true);
                }
                else if (Logic.ScriptVars[(int)ScriptVariableNames.SECOND_ITEM] != 0)
                { // clicked luggage onto 2nd icon - we need to color-highlight the 2 relevant icons & grey out the rest
                    if ((_menuList[menuSlot] == objHeld) || (_menuList[menuSlot] == Logic.ScriptVars[(int)ScriptVariableNames.SECOND_ITEM]))
                        _objects[menuSlot].SetSelect(true);
                }
                else
                { // this object is selected - ie. GREYED OUT
                    if (objHeld != _menuList[menuSlot])
                        _objects[menuSlot].SetSelect(true);
                }
            }
        }

        public void FnEndMenu()
        {
            if (_objectBarStatus != MenuState.MENU_CLOSED)
                _objectBarStatus = MenuState.MENU_CLOSING;
        }

        public void CfnReleaseMenu()
        {
            _objectBarStatus = MenuState.MENU_CLOSING;
        }

        public void FnAddSubject(int sub)
        {
            _subjectBar[Logic.ScriptVars[(int)ScriptVariableNames.IN_SUBJECT]] = (uint)sub;
            Logic.ScriptVars[(int)ScriptVariableNames.IN_SUBJECT]++;
        }

        public void CheckTopMenu()
        {
            if (_objectBarStatus == MenuState.MENU_OPEN)
                CheckMenuClick(MENU_TOP);
        }

        private byte CheckMenuClick(int menuType)
        {
            ushort mouseEvent = _mouse.TestEvent();
            if (mouseEvent == 0)
                return 0;
            ushort x, y;
            _mouse.GiveCoords(out x, out y);
            if (_subjectBarStatus == MenuState.MENU_OPEN)
            {
                // Conversation mode. Icons are highlighted on mouse-down, but
                // the actual response is made on mouse-up.
                if (menuType == MENU_BOT)
                {
                    if (Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] != 0 && ((mouseEvent & Mouse.BS1L_BUTTON_UP) != 0))
                    {
                        for (var cnt = 0; cnt < Logic.ScriptVars[(int)ScriptVariableNames.IN_SUBJECT]; cnt++)
                        {
                            if (_subjectBar[cnt] == Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD])
                                return (byte)(cnt + 1);
                        }
                    }
                    else if ((mouseEvent & Mouse.BS1L_BUTTON_DOWN) != 0)
                    {
                        for (var cnt = 0; cnt < Logic.ScriptVars[(int)ScriptVariableNames.IN_SUBJECT]; cnt++)
                        {
                            if (_subjects[cnt].WasClicked(x, y))
                            {
                                Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = _subjectBar[cnt];
                                RefreshMenus();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] != 0 && ((mouseEvent & Mouse.BS1L_BUTTON_UP) != 0))
                    {
                        for (var cnt = 0; cnt < _inMenu; cnt++)
                        {
                            if (_menuList[cnt] == Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD])
                                return (byte)(cnt + 1);
                        }
                    }
                    else if ((mouseEvent & Mouse.BS1L_BUTTON_DOWN) != 0)
                    {
                        for (var cnt = 0; cnt < _inMenu; cnt++)
                        {
                            if (_objects[cnt].WasClicked(x, y))
                            {
                                Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = _menuList[cnt];
                                RefreshMenus();
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // Normal use, i.e. inventory. Things happen on mouse-down.
                if (menuType == MENU_TOP)
                {
                    for (var cnt = 0; cnt < _inMenu; cnt++)
                    {
                        if (_objects[cnt].WasClicked(x, y))
                        {
                            if ((mouseEvent & Mouse.BS1R_BUTTON_DOWN) != 0)
                            { // looking at item
                                Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = _menuList[cnt];
                                Logic.ScriptVars[(int)ScriptVariableNames.MENU_LOOKING] = 1;
                                Logic.ScriptVars[(int)ScriptVariableNames.DEFAULT_ICON_TEXT] = (uint)_objectDefs[_menuList[cnt], MenuObject.textDesc];
                            }
                            else if ((mouseEvent & Mouse.BS1L_BUTTON_DOWN) != 0)
                            {
                                if (Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] != 0)
                                {
                                    if (Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] == _menuList[cnt])
                                    {
                                        _mouse.SetLuggage(0, 0);
                                        Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = 0; // reselected => deselect it
                                    }
                                    else
                                    { // the player is clicking another item on this one.
                                      // run its use-script, if there is one
                                        Logic.ScriptVars[(int)ScriptVariableNames.SECOND_ITEM] = _menuList[cnt];
                                        _mouse.SetLuggage(0, 0);
                                    }
                                }
                                else
                                {
                                    Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] = _menuList[cnt];
                                    _mouse.SetLuggage((uint) _objectDefs[_menuList[cnt], MenuObject.luggageIconRes], 0);
                                }
                            }
                            RefreshMenus();
                            break;
                        }
                    }
                }
            }
            return 0;
        }

        private void RefreshMenus()
        {
            if (_objectBarStatus == MenuState.MENU_OPEN)
            {
                BuildMenu();
                for (var cnt = 0; cnt < 16; cnt++)
                {
                    if (_objects[cnt] != null)
                        _objects[cnt].Draw();
                    else
                        _screen.ShowFrame((ushort)(cnt * 40), 0, 0xffffffff, 0);
                }
            }
            if (_subjectBarStatus == MenuState.MENU_OPEN)
            {
                BuildSubjects();
                for (var cnt = 0; cnt < 16; cnt++)
                {
                    if (_subjects[cnt] != null)
                        _subjects[cnt].Draw();
                    else
                        _screen.ShowFrame((ushort)(cnt * 40), 440, 0xffffffff, 0);
                }
            }
        }

        public int LogicChooser(SwordObject compact)
        {
            byte objSelected = 0;
            if (_objectBarStatus == MenuState.MENU_OPEN)
                objSelected = CheckMenuClick(MENU_TOP);
            if (objSelected == 0)
                objSelected = CheckMenuClick(MENU_BOT);
            if (objSelected != 0)
            {
                compact.logic = Logic.LOGIC_script;
                return 1;
            }
            return 0;
        }

        static readonly int[,] _subjectList = {
            {	// 256
		        0,								// subject_res
		        0									// subject_frame
	        },
            {	// 257
		        ICON_BEER,				// subject_res
		        0									// subject_frame
	        },
            {	// 258
		        ICON_CASTLE,			// subject_res
		        0									// subject_frame
	        },
            { // 259
		        ICON_YES,					// subject_res
		        0									// subject_frame
	        },
            { // 260
		        ICON_NO,					// subject_res
		        0									// subject_frame
	        },
            { // 261
		        ICON_PEAGRAM,			// subject_res
		        0									// subject_frame
	        },
            { // 262
		        ICON_DIG,					// subject_res
		        0									// subject_frame
	        },
            { // 263
		        ICON_SEAN,				// subject_res
		        0									// subject_frame
	        },
            { // 264
		        ICON_GEM,					// subject_res
		        0									// subject_frame
	        },
            { // 265
		        ICON_TEMPLARS,		// subject_res
		        0									// subject_frame
	        },
            { // 266
		        ICON_LEPRECHAUN,	// subject_res
		        0									// subject_frame
	        },
            { // 267
		        ICON_GOODBYE,			// subject_res
		        0									// subject_frame
	        },
            { // 268
		        ICON_GEORGE,			// subject_res
		        0									// subject_frame
	        },
            { // 269
		        ICON_ROSSO,				// subject_res
		        0									// subject_frame
	        },
            { // 270
		        ICON_GHOST,				// subject_res
		        0									// subject_frame
	        },
            { // 271
		        ICON_CLOWN,				// subject_res
		        0									// subject_frame
	        },
            { // 272
		        ICON_CAR,					// subject_res
		        0									// subject_frame
	        },
            { // 273
		        ICON_MOUE,				// subject_res
		        0									// subject_frame
	        },
            { // 274
		        ICON_NICO,				// subject_res
		        0									// subject_frame
	        },
            { // 275
		        ICON_MOB,					// subject_res
		        0									// subject_frame
	        },
            { // 276
		        ICON_CHANTELLE,		// subject_res
		        0									// subject_frame
	        },
            { // 277
		        ICON_PLANTARD,		// subject_res
		        0									// subject_frame
	        },
            { // 278
		        ICON_JACKET,			// subject_res
		        0									// subject_frame
	        },
            { // 279
		        ICON_BRIEFCASE,		// subject_res
		        0									// subject_frame
	        },
            {
                0, //ICON_GLASS,	// subject_res
		        0									// subject_frame
	        },
            { // 281
		        ICON_GLASS_EYE,		// subject_res
		        0									// subject_frame
	        },
            { // 282
		        ICON_BULL,				// subject_res
		        0									// subject_frame
	        },
            { // 283
		        ICON_KLAUSNER,		// subject_res
		        0									// subject_frame
	        },
            { // 284
		        0, //ICON_LOOM,		// subject_res
		        0									// subject_frame
	        },
            { // 285
		        ICON_ULTAR,				// subject_res
		        0									// subject_frame
	        },
            { // 286
		        ICON_PHONE,				// subject_res
		        0									// subject_frame
	        },
            { // 287
		        ICON_PHOTOGRAPH,	// subject_res
		        0									// subject_frame
	        },
            { // 288
		        ICON_CREST,				// subject_res
		        0									// subject_frame
	        },
            { // 289
		        ICON_LOBINEAU,		// subject_res
		        0									// subject_frame
	        },
            { // 290
		        ICON_BOOK,				// subject_res
		        0									// subject_frame
	        },
            { // 291 (SNARE)
		        ICON_FUSE_WIRE,		// subject_res
		        0									// subject_frame
	        },
            { // 292
		        ICON_LEARY,				// subject_res
		        0									// subject_frame
	        },
            { // 293
		        ICON_LADDER,			// subject_res
		        0									// subject_frame
	        },
            { // 294
		        ICON_GOAT,				// subject_res
		        0									// subject_frame
	        },
            { // 295
		        ICON_TOOLBOX,			// subject_res
		        0									// subject_frame
	        },
            { // 296
		        ICON_PACKAGE,			// subject_res
		        0									// subject_frame
	        },
            { // 297
		        ICON_FISH,				// subject_res
		        0									// subject_frame
	        },
            { // 298
		        ICON_NEJO,				// subject_res
		        0									// subject_frame
	        },
            { // 299
		        ICON_CAT,					// subject_res
		        0									// subject_frame
	        },
            { // 300
		        ICON_AYUB,				// subject_res
		        0									// subject_frame
	        },
            { // 301
		        ICON_STATUETTE,		// subject_res
		        0									// subject_frame
	        },
            { // 302
		        ICON_NEJO_STALL,	// subject_res
		        0									// subject_frame
	        },
            { // 303
		        ICON_TEMPLARS,		// subject_res
		        0									// subject_frame
	        },
            { // 304
		        ICON_ARTO,				// subject_res
		        0									// subject_frame
	        },
            { // 305
		        ICON_HENDERSONS,	// subject_res
		        0									// subject_frame
	        },
            { // 306
		        ICON_CLUB,				// subject_res
		        0									// subject_frame
	        },
            { // 307
		        ICON_SIGN,				// subject_res
		        0									// subject_frame
	        },
            { // 308
		        ICON_TAXI,				// subject_res
		        0									// subject_frame
	        },
            { // 309
		        ICON_BULLS_HEAD,	// subject_res
		        0									// subject_frame
	        },
            { // 310
		        ICON_DUANE,				// subject_res
		        0									// subject_frame
	        },
            { // 311
		        ICON_PEARL,				// subject_res
		        0									// subject_frame
	        },
            { // 312
		        ICON_TRUTH,				// subject_res
		        0									// subject_frame
	        },
            { // 313
		        ICON_LIE,					// subject_res
		        0									// subject_frame
	        },
            { // 314
		        0, //ICON_SUBJECT,// subject_res
		        0									// subject_frame
	        },
            { // 315 KEY
		        ICON_HOTEL_KEY,		// subject_res
		        0									// subject_frame
	        },
            { // 316
		        ICON_FLOWERS,			// subject_res
		        0									// subject_frame
	        },
            { // 317
		        ICON_BUST,				// subject_res
		        0									// subject_frame
	        },
            { // 318
		        ICON_MANUSCRIPT,	// subject_res
		        0									// subject_frame
	        },
            { // 319
		        ICON_WEASEL,			// subject_res
		        0									// subject_frame
	        },
            { // 320
		        ICON_BANANA,			// subject_res
		        0									// subject_frame
	        },
            { // 321
		        ICON_WEAVER,			// subject_res
		        0									// subject_frame
	        },
            { // 322
		        ICON_KNIGHT,			// subject_res
		        0									// subject_frame
	        },
            { // 323
		        ICON_QUEEN,				// subject_res
		        0									// subject_frame
	        },
            { // 324
		        ICON_PIERMONT,		// subject_res
		        0									// subject_frame
	        },
            { // 325
		        ICON_BALL,				// subject_res
		        0									// subject_frame
	        },
            { // 326
		        ICON_COUNTESS,		// subject_res
		        0									// subject_frame
	        },
            { // 327
		        ICON_MARQUET,			// subject_res
		        0									// subject_frame
	        },
            { // 328
		        ICON_SAFE,				// subject_res
		        0									// subject_frame
	        },
            { // 329
		        ICON_COINS,				// subject_res
		        0									// subject_frame
	        },
            { // 330
		        ICON_CHESS_SET,		// subject_res
		        0									// subject_frame
	        },
            { // 331
		        ICON_TOMB,				// subject_res
		        0									// subject_frame
	        },
            { // 332
		        ICON_CANDLE,			// subject_res
		        0									// subject_frame
	        },
            { // 333
		        ICON_MARY,				// subject_res
		        0									// subject_frame
	        },
            { // 334
		        ICON_CHESSBOARD,	// subject_res
		        0									// subject_frame
	        },
            { // 335
		        ICON_TRIPOD,			// subject_res
		        0									// subject_frame
	        },
            { // 336
		        ICON_POTS,				// subject_res
		        0									// subject_frame
	        },
            { // 337
		        ICON_ALARM,				// subject_res
		        0									// subject_frame
	        },
            { // 338
		        ICON_BAPHOMET,		// subject_res
		        0									// subject_frame
	        },
            { // 339
		        ICON_PHRASE,			// subject_res
		        0									// subject_frame
	        },
            { // 340
		        ICON_POLISHED_CHALICE,	// subject_res
		        0												// subject_frame
	        },
            { // 341
		        ICON_NURSE,				// subject_res
		        0									// subject_frame
	        },
            { // 342
		        ICON_WELL,				// subject_res
		        0									// subject_frame
	        },
            { // 343
		        ICON_WELL2,				// subject_res
		        0									// subject_frame
	        },
            { // 344
		        ICON_HAZEL_WAND,	// subject_res
		        0									// subject_frame
	        },
            { // 345
		        ICON_CHALICE,			// subject_res
		        0									// subject_frame
	        },
            { // 346
		        ICON_MR_SHINY,		// subject_res
		        0									// subject_frame
	        },
            { // 347
		        ICON_PHOTOGRAPH,	// subject_res
		        0									// subject_frame
	        },
            { // 348
		        ICON_PHILIPPE,		// subject_res
		        0									// subject_frame
	        },
            { // 349
		        ICON_ERIC,				// subject_res
		        0									// subject_frame
	        },
            { // 350
		        ICON_ROZZER,			// subject_res
		        0									// subject_frame
	        },
            { // 351
		        ICON_JUGGLER,			// subject_res
		        0									// subject_frame
	        },
            { // 352
		        ICON_PRIEST,			// subject_res
		        0									// subject_frame
	        },
            { // 353
		        ICON_WINDOW,			// subject_res
		        0									// subject_frame
	        },
            { // 354
		        ICON_SCROLL,			// subject_res
		        0									// subject_frame
	        },
            { // 355
		        ICON_PRESSURE_GAUGE,	// subject_res
		        0											// subject_frame
	        },
            { // 356
		        ICON_RENEE,				// subject_res
		        0									// subject_frame
	        },
            { // 357
		        ICON_CHURCH,			// subject_res
		        0									// subject_frame
	        },
            { // 358
		        ICON_EKLUND,			// subject_res
		        0									// subject_frame
	        },
            { // 359
		        ICON_FORTUNE,			// subject_res
		        0									// subject_frame
	        },
            { // 360
		        ICON_PAINTER,			// subject_res
		        0									// subject_frame
	        },
            { // 361
		        0, //ICON_SWORD,	// subject_res
		        0									// subject_frame
	        },
            { // 362
		        ICON_GUARD,				// subject_res
		        0									// subject_frame
	        },
            { // 363
		        ICON_THERMOSTAT,	// subject_res
		        0									// subject_frame
	        },
            { // 364
		        ICON_TOILET,			// subject_res
		        0									// subject_frame
	        },
            { // 365
		        ICON_MONTFAUCON,	// subject_res
		        0									// subject_frame
	        },
            { // 366
		        ICON_ASSASSIN,		// subject_res
		        0									// subject_frame
	        },
            { // 367
		        ICON_HASH,				// subject_res
		        0									// subject_frame
	        },
            { // 368
		        ICON_DOG,					// subject_res
		        0									// subject_frame
	        },
            { // 369
		        ICON_AYUB,				// subject_res
		        0									// subject_frame
	        },
            { // 370
		        ICON_LENS,				// subject_res
		        0									// subject_frame
	        },
            { // 371
		        ICON_RED_NOSE,		// subject_res
		        0									// subject_frame
	        },
            { // 372
		        0,								// subject_res
		        0									// subject_frame
	        },
            { // 373
		        0,								// subject_res
		        0									// subject_frame
	        },
            { // 374
		        0,								// subject_res
		        0									// subject_frame
	        }
        };

        public static readonly int[,] _objectDefs = {
            {	// 0 can't use
		        0, 0, 0, 0, 0
            },
            {	// 1 NEWSPAPER
		        menu_newspaper,							// text_desc
		        ICON_NEWSPAPER,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_NEWSPAPER,							// luggage_icon_res
		        SCR_icon_combine_script,		// use_script
	        },
            {	// 2 HAZEL_WAND
		        menu_hazel_wand,						// text_desc
		        ICON_HAZEL_WAND,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_HAZEL_WAND,						// luggage_icon_res
		        SCR_icon_combine_script,		// use_script
	        },
            {	// 3 BEER_TOWEL
		        0,													// text_desc - SEE MENU.SCR
		        ICON_BEER_TOWEL,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_BEER_TOWEL,						// luggage_icon_res
		        SCR_icon_combine_script,		// use_script
	        },
            {	// 4 HOTEL_KEY
		        menu_hotel_key,							// text_desc
		        ICON_HOTEL_KEY,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_HOTEL_KEY,							// luggage_icon_res
		        SCR_icon_combine_script,		// use_script
	        },
            {	// 5 BALL
		        menu_ball,									// text_desc
		        ICON_BALL,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_BALL,									// luggage_icon_res
		        SCR_icon_combine_script,		// use_script
	        },
            {	// 6 STATUETTE
		        menu_statuette,							// text_desc
		        ICON_STATUETTE,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_STATUETTE,							// luggage_icon_res
		        SCR_icon_combine_script,		// use_script
	        },
            {	// 7 RED_NOSE
		        0,													// text_desc - SEE MENU.SCR
		        ICON_RED_NOSE,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_RED_NOSE,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 8 POLISHED_CHALICE
		        menu_polished_chalice,			// text_desc
		        ICON_POLISHED_CHALICE,			// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_POLISHED_CHALICE,			// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 9 DOLLAR_BILL
		        menu_dollar_bill,						// text_desc
		        ICON_DOLLAR_BILL,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_DOLLAR_BILL,						// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 10 PHOTO
		        menu_photograph,						// text_desc
		        ICON_PHOTOGRAPH,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_PHOTOGRAPH,						// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 11 FLASHLIGHT
		        menu_flashlight,						// text_desc
		        ICON_FLASHLIGHT,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_FLASHLIGHT,						// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 12 FUSE_WIRE
		        menu_fuse_wire,							// text_desc
		        ICON_FUSE_WIRE,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_FUSE_WIRE,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 13 GEM
		        menu_gem,										// text_desc
		        ICON_GEM,										// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_GEM,										// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 14 STATUETTE_PAINT
		        menu_statuette_paint,				// text_desc
		        ICON_STATUETTE_PAINT,				// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_STATUETTE_PAINT,				// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 15 STICK
		        menu_stick,									// text_desc
		        ICON_STICK,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_STICK,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 16 EXCAV_KEY
		        menu_excav_key,							// text_desc
		        ICON_EXCAV_KEY,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_EXCAV_KEY,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 17 LAB_PASS
		        menu_lab_pass,							// text_desc
		        ICON_LAB_PASS,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_LAB_PASS,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 18 LIFTING_KEYS
		        menu_lifting_keys,					// text_desc
		        ICON_LIFTING_KEYS,					// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_LIFTING_KEYS,					// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 19 MANUSCRIPT
		        menu_manuscript,						// text_desc
		        ICON_MANUSCRIPT,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_MANUSCRIPT,						// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 20 MATCH_BOOK
		        menu_match_book,						// text_desc
		        ICON_MATCHBOOK,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_MATCHBOOK,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 21 SUIT_MATERIAL
		        menu_suit_material,					// text_desc
		        ICON_SUIT_MATERIAL,					// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_SUIT_MATERIAL,					// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 22 STICK_TOWEL
		        menu_stick_towel,						// text_desc
		        ICON_STICK_TOWEL,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_STICK_TOWEL,						// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 23 PLASTER
		        menu_plaster,								// text_desc
		        ICON_PLASTER,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_PLASTER,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 24 PRESSURE_GAUGE
		        menu_pressure_gauge,				// text_desc
		        ICON_PRESSURE_GAUGE,				// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_PRESSURE_GAUGE,				// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 25 RAILWAY_TICKET
		        menu_railway_ticket,				// text_desc
		        ICON_RAILWAY_TICKET,				// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_RAILWAY_TICKET,				// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 26 BUZZER
		        menu_buzzer,								// text_desc
		        ICON_BUZZER,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_BUZZER,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 27 ROSSO_CARD
		        menu_rosso_card,						// text_desc
		        ICON_ROSSO_CARD,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_ROSSO_CARD,						// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 28 TOILET_KEY
		        menu_toilet_key,						// text_desc
		        ICON_TOILET_KEY,						// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TOILET_KEY,						// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 29 SOAP
		        menu_soap,									// text_desc
		        ICON_SOAP,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_SOAP,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 30 STONE_KEY
		        menu_stone_key,							// text_desc
		        ICON_STONE_KEY,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_STONE_KEY,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 31 CHALICE
		        menu_chalice,								// text_desc
		        ICON_CHALICE,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_CHALICE,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 32 TISSUE
		        menu_tissue,								// text_desc
		        ICON_TISSUE,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TISSUE,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 33 TOILET_BRUSH
		        menu_toilet_brush,					// text_desc
		        ICON_TOILET_BRUSH,					// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TOILET_BRUSH,					// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 34 TOILET_CHAIN
		        menu_toilet_chain,					// text_desc
		        ICON_TOILET_CHAIN,					// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TOILET_CHAIN,					// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 35 TOWEL
		        menu_towel,									// text_desc
		        ICON_TOWEL,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TOWEL,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 36 TRIPOD
		        menu_tripod,								// text_desc
		        ICON_TRIPOD,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TRIPOD,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 37 LENS
		        menu_lens,									// text_desc
		        ICON_LENS,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_LENS,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 38 MIRROR
		        menu_mirror,								// text_desc
		        ICON_MIRROR,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_MIRROR,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 39 TOWEL_CUT
		        menu_towel_cut,							// text_desc
		        ICON_TOWEL_CUT,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TOWEL_CUT,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 40 BIBLE
		        menu_bible,									// text_desc
		        ICON_BIBLE,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_BIBLE,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 41 TISSUE_CHARRED
		        menu_tissue_charred,				// text_desc
		        ICON_TISSUE_CHARRED,				// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_TISSUE_CHARRED,				// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 42 FALSE_KEY
		        menu_false_key,							// text_desc
		        ICON_FALSE_KEY,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_FALSE_KEY,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 43 PAINTED_KEY - looks identical to excav key, so uses that icon & luggage
		        menu_painted_key,						// text_desc
		        ICON_EXCAV_KEY,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_EXCAV_KEY,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 44 KEYRING
		        0,													// text_desc - SEE MENU.SCR
		        ICON_KEYRING,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_KEYRING,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 45 SOAP_IMP
		        menu_soap_imp,							// text_desc
		        ICON_SOAP_IMP,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_SOAP_IMP,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 46 SOAP_PLAS
		        menu_soap_plas,							// text_desc
		        ICON_SOAP_PLAS,							// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_SOAP_PLAS,							// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 47 COG_1 - the larger cog with spindle attached
		        menu_cog_1,									// text_desc
		        ICON_COG_1,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_COG_1,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 48 COG_2 - the smaller cog, found in the rubble
		        menu_cog_2,									// text_desc
		        ICON_COG_2,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_COG_2,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 49 HANDLE
		        menu_handle,								// text_desc
		        ICON_HANDLE,								// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_HANDLE,								// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 50 COIN
		        menu_coin,									// text_desc
		        ICON_COIN,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_COIN,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 51 BIRO
		        menu_biro,									// text_desc
		        ICON_BIRO,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_BIRO,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        },
            {	// 52 PIPE
		        menu_pipe,									// text_desc
		        ICON_PIPE,									// big_icon_res
		        0,													// big_icon_frame
		        SwordRes.LUGG_PIPE,									// luggage_icon_res
		        SCR_icon_combine_script,			// use_script
	        }
        };

        const int SCR_icon_combine_script = (0 * 0x10000 + 25);

        // 54 entities in TXTs, 54 in datafiles.
        // object_icons
        const int ICON_LEFT_ARROW = 0x04030000;
        const int ICON_RIGHT_ARROW = 0x04030001;
        const int ICON_NEWSPAPER = 0x04030002;
        const int ICON_HAZEL_WAND = 0x04030003;
        const int ICON_BEER_TOWEL = 0x04030004;
        const int ICON_HOTEL_KEY = 0x04030005;
        const int ICON_BALL = 0x04030006;
        const int ICON_STATUETTE = 0x04030007;
        const int ICON_RED_NOSE = 0x04030008;
        const int ICON_POLISHED_CHALICE = 0x04030009;
        const int ICON_DOLLAR_BILL = 0x0403000A;
        const int ICON_PHOTOGRAPH = 0x0403000B;
        const int ICON_FLASHLIGHT = 0x0403000C;
        const int ICON_FUSE_WIRE = 0x0403000D;
        const int ICON_GEM = 0x0403000E;
        const int ICON_STATUETTE_PAINT = 0x0403000F;
        const int ICON_STICK = 0x04030010;
        const int ICON_EXCAV_KEY = 0x04030011;
        const int ICON_LAB_PASS = 0x04030012;
        const int ICON_LIFTING_KEYS = 0x04030013;
        const int ICON_MANUSCRIPT = 0x04030014;
        const int ICON_MATCHBOOK = 0x04030015;
        const int ICON_SUIT_MATERIAL = 0x04030016;
        const int ICON_STICK_TOWEL = 0x04030017;
        const int ICON_PLASTER = 0x04030018;
        const int ICON_PRESSURE_GAUGE = 0x04030019;
        const int ICON_RAILWAY_TICKET = 0x0403001A;
        const int ICON_BUZZER = 0x0403001B;
        const int ICON_ROSSO_CARD = 0x0403001C;
        const int ICON_TOILET_KEY = 0x0403001D;
        const int ICON_SOAP = 0x0403001E;
        const int ICON_STONE_KEY = 0x0403001F;
        const int ICON_CHALICE = 0x04030020;
        const int ICON_TISSUE = 0x04030021;
        const int ICON_TOILET_BRUSH = 0x04030022;
        const int ICON_TOILET_CHAIN = 0x04030023;
        const int ICON_TOWEL = 0x04030024;
        const int ICON_TRIPOD = 0x04030025;
        const int ICON_LENS = 0x04030026;
        const int ICON_MIRROR = 0x04030027;
        const int ICON_TOWEL_CUT = 0x04030028;
        const int ICON_BIBLE = 0x04030029;
        const int ICON_TISSUE_CHARRED = 0x0403002A;
        const int ICON_FALSE_KEY = 0x0403002B;
        const int ICON_KEYRING = 0x0403002C;
        const int ICON_SOAP_IMP = 0x0403002D;
        const int ICON_SOAP_PLAS = 0x0403002E;
        const int ICON_COG_1 = 0x0403002F;
        const int ICON_COG_2 = 0x04030030;
        const int ICON_HANDLE = 0x04030031;
        const int ICON_COIN = 0x04030032;
        const int ICON_BIRO = 0x04030033;
        const int ICON_PIPE = 0x04030034;
        // 53 entities in TXTs, 53 in dataf;iles.
        // subject_icons                   ;
        const int ICON_ALARM = 0x04040000;
        const int ICON_ARTO = 0x04040001;
        const int ICON_ASSASSIN = 0x04040002;
        const int ICON_AYUB = 0x04040003;
        const int ICON_BANANA = 0x04040004;
        const int ICON_BAPHOMET = 0x04040005;
        const int ICON_BEER = 0x04040006;
        const int ICON_BOOK = 0x04040007;
        const int ICON_BRIEFCASE = 0x04040008;
        const int ICON_BULL = 0x04040009;
        const int ICON_BULLS_HEAD = 0x0404000A;
        const int ICON_BUST = 0x0404000B;
        const int ICON_CANDLE = 0x0404000C;
        const int ICON_CAR = 0x0404000D;
        const int ICON_CASTLE = 0x0404000E;
        const int ICON_CAT = 0x0404000F;
        const int ICON_CHANTELLE = 0x04040010;
        const int ICON_CHESSBOARD = 0x04040011;
        const int ICON_CHESS_SET = 0x04040012;
        const int ICON_CHURCH = 0x04040013;
        const int ICON_CLOWN = 0x04040014;
        const int ICON_CLUB = 0x04040015;
        const int ICON_COINS = 0x04040016;
        const int ICON_COUNTESS = 0x04040017;
        const int ICON_CREST = 0x04040018;
        const int ICON_DIG = 0x04040019;
        const int ICON_DOG = 0x0404001A;
        const int ICON_DUANE = 0x0404001B;
        const int ICON_EKLUND = 0x0404001C;
        const int ICON_ERIC = 0x0404001D;
        const int ICON_FISH = 0x0404001E;
        const int ICON_FLOWERS = 0x0404001F;
        const int ICON_FORTUNE = 0x04040020;
        const int ICON_GEORGE = 0x04040021;
        const int ICON_GHOST = 0x04040022;
        const int ICON_GLASS_EYE = 0x04040023;
        const int ICON_GOAT = 0x04040024;
        const int ICON_GOODBYE = 0x04040025;
        const int ICON_GUARD = 0x04040026;
        const int ICON_HASH = 0x04040027;
        const int ICON_HENDERSONS = 0x04040028;
        const int ICON_JACKET = 0x04040029;
        const int ICON_JUGGLER = 0x0404002A;
        const int ICON_KLAUSNER = 0x0404002B;
        const int ICON_KNIGHT = 0x0404002C;
        const int ICON_LADDER = 0x0404002D;
        const int ICON_LEARY = 0x0404002E;
        const int ICON_LEPRECHAUN = 0x0404002F;
        const int ICON_LIE = 0x04040030;
        const int ICON_LOBINEAU = 0x04040031;
        const int ICON_MARQUET = 0x04040032;
        const int ICON_MARY = 0x04040033;
        const int ICON_MOB = 0x04040034;
        const int ICON_MONTFAUCON = 0x04040035;
        const int ICON_MOUE = 0x04040036;
        const int ICON_MR_SHINY = 0x04040037;
        const int ICON_NEJO = 0x04040038;
        const int ICON_NEJO_STALL = 0x04040039;
        const int ICON_NICO = 0x0404003A;
        const int ICON_NO = 0x0404003B;
        const int ICON_NURSE = 0x0404003C;
        const int ICON_PACKAGE = 0x0404003D;
        const int ICON_PAINTER = 0x0404003E;
        const int ICON_PEAGRAM = 0x0404003F;
        const int ICON_PEARL = 0x04040040;
        const int ICON_PHILIPPE = 0x04040041;
        const int ICON_PHONE = 0x04040042;
        const int ICON_PHRASE = 0x04040043;
        const int ICON_PIERMONT = 0x04040044;
        const int ICON_PLANTARD = 0x04040045;
        const int ICON_POTS = 0x04040046;
        const int ICON_PRIEST = 0x04040047;
        const int ICON_QUEEN = 0x04040048;
        const int ICON_RENEE = 0x04040049;
        const int ICON_ROSSO = 0x0404004A;
        const int ICON_ROZZER = 0x0404004B;
        const int ICON_SAFE = 0x0404004C;
        const int ICON_SCROLL = 0x0404004D;
        const int ICON_SEAN = 0x0404004E;
        const int ICON_SIGN = 0x0404004F;
        const int ICON_TAXI = 0x04040050;
        const int ICON_TEMPLARS = 0x04040051;
        const int ICON_THERMOSTAT = 0x04040052;
        const int ICON_TOILET = 0x04040053;
        const int ICON_TOMB = 0x04040054;
        const int ICON_TOOLBOX = 0x04040055;
        const int ICON_TRUTH = 0x04040056;
        const int ICON_ULTAR = 0x04040057;
        const int ICON_WEASEL = 0x04040058;
        const int ICON_WEAVER = 0x04040059;
        const int ICON_WELL = 0x0404005A;
        const int ICON_WELL2 = 0x0404005B;
        const int ICON_WINDOW = 0x0404005C;
        const int ICON_YES = 0x0404005D;

        const int menu_bible = 69;
        const int menu_newspaper = 1;
        const int menu_hazel_wand = 2;
        const int menu_beer_towel = 68;
        const int menu_beer_towel_wet = 4;
        const int menu_beer_towel_damp = 5;
        const int menu_beer_towel_dried = 6;
        const int menu_hotel_key = 7;
        const int menu_ball = 8;
        const int menu_statuette = 9;
        const int menu_red_nose_first = 10;
        const int menu_red_nose_second = 11;
        const int menu_polished_chalice = 12;
        const int menu_dollar_bill = 13;
        const int menu_photograph = 14;
        const int menu_keyring_first = 15;
        const int menu_keyring_second = 70;
        const int menu_keyring_third = 17;
        const int menu_fuse_wire = 18;
        const int menu_gem = 19;
        const int menu_statuette_paint = 20;
        const int menu_stick = 21;
        const int menu_excav_key = 71;
        const int menu_false_key = 72;
        const int menu_painted_key = 73;
        const int menu_lab_pass = 25;
        const int menu_lifting_keys = 26;
        const int menu_manuscript = 27;
        const int menu_match_book = 28;
        const int menu_suit_material = 29;
        const int menu_stick_towel = 30;
        const int menu_plaster = 31;
        const int menu_pressure_gauge = 32;
        const int menu_railway_ticket = 33;
        const int menu_buzzer = 74;
        const int menu_rosso_card = 75;
        const int menu_toilet_key = 36;
        const int menu_soap = 76;
        const int menu_soap_imp = 77;
        const int menu_soap_plas = 78;
        const int menu_stone_key = 79;
        const int menu_chalice = 41;
        const int menu_tissue = 42;
        const int menu_toilet_brush = 80;
        const int menu_toilet_chain = 44;
        const int menu_towel = 45;
        const int menu_tripod = 46;
        const int menu_lens = 81;
        const int menu_towel_cut = 48;
        const int menu_mirror = 82;
        const int menu_tissue_charred = 50;
        const int menu_cog_1 = 51;
        const int menu_cog_2 = 52;
        const int menu_handle = 83;
        const int menu_coin = 84;
        const int menu_biro = 55;
        const int menu_pipe = 56;
        const int menu_flashlight = 57;

    }
}