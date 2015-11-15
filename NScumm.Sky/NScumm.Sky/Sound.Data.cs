
namespace NScumm.Sky
{
    partial class Sound
    {
        private static readonly Sfx FxNull = new Sfx(
            0,
            0,
            new[]
            {
                new Room(200, 127, 127),
                new Room(255, 0, 0)
            });

        private static readonly Sfx FxLevel3Ping = new Sfx(
            1,
            0,
            new[]
            {
                new Room(28, 63, 63),
                new Room(29, 63, 63),
                new Room(31, 63, 63),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxFactorySound = new Sfx(
            1,
            SfxfSave,
            new[]
            {
                new Room(255, 30, 30)
            });

        private static readonly Sfx FxCrowbarPlaster = new Sfx(
            1,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxMasonryFall = new Sfx(
            1,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxPriseBrick = new Sfx(
            2,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxRopeCreak = new Sfx(
            2,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxPing = new Sfx(
            3,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxForceFireDoor = new Sfx(
            3,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxBrickHitFoster = new Sfx(
            3,
            10 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxBrickHitPlank = new Sfx(
            3,
            8 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxRm3LiftMoving = new Sfx(
            4,
            SfxfSave,
            new[]
            {
                new Room(3, 127, 127),
                new Room(2, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxWeld = new Sfx(
            4,
            0,
            new[]
            {
                new Room(15, 127, 127),
                new Room(7, 127, 127),
                new Room(6, 60, 60),
                new Room(12, 60, 60),
                new Room(13, 60, 60),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxWeld12 = new Sfx(
            4,
            0,
            new[]
            {
                new Room(12, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxSprayOnSkin = new Sfx(
            4,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxPlankVibrating = new Sfx(
            4,
            6 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxPressBang = new Sfx(
            5,
            0,
            new[]
            {
                new Room(0, 50, 100),
                new Room(255, 0, 0)
            });

        private static readonly Sfx FxSpannerClunk = new Sfx(
            5,
            0,
            new[]
            {
                new Room(255, 127, 127),
            }
            );

        private static readonly Sfx FxBreakCrystals = new Sfx(
            5,
            0,
            new[]
            {
                new Room(96, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxPressHiss = new Sfx(
            6,
            0,
            new[]
            {
                new Room(0, 40, 40),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxOpenDoor = new Sfx(
            6,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxOpenLambDoor = new Sfx(
            6,
            0,
            new[]
            {
                new Room(20, 127, 127),
                new Room(21, 127, 127),
                new Room(255, 0, 0)
            });

        private static readonly Sfx FxSplash = new Sfx(
            6,
            22 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxDisintegrate = new Sfx(
            7,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxBuzzer = new Sfx(
            7,
            4 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxLathe = new Sfx(
            7,
            SfxfSave,
            new[]
            {
                new Room(4, 60, 60),
                new Room(2, 20, 20),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxHitCrowbarBrick = new Sfx(
            7,
            9 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxHelloHelga = new Sfx(
            8,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxStatueOnArmor = new Sfx(
            8,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxLiftAlarm = new Sfx(
            8,
            SfxfSave,
            new[]
            {
                new Room(2, 63, 63),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxDropCrowbar = new Sfx(
            8,
            5 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxByeeHelga = new Sfx(
            9,
            3 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxShedDoorCreak = new Sfx(
            10,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxExplosion = new Sfx(
            10,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxFireCrackleInPit = new Sfx(
            9,
            SfxfSave,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxRemoveBarGrill = new Sfx(
            10,
            7 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxGrillCreak = new Sfx(
            10,
            43 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxSteam1 = new Sfx(
            11,
            SfxfSave,
            new[]
            {
                new Room(18, 20, 20),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxSteam2 = new Sfx(
            11,
            SfxfSave,
            new[]
            {
                new Room(18, 63, 63),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxSteam3 = new Sfx(
            11,
            SfxfSave,
            new[]
            {
                new Room(18, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxCrowbarWooden = new Sfx(
            11,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxHelmetDown3 = new Sfx(
            11,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxGuardFall = new Sfx(
            11,
            4,
            new[]
            {
                new Room(255, 127, 127),
            });

#if Undefined
static const Sfx fx_furnace = new Sfx(
	11,
	0,
	{
		{ 3,90,90 },
		{ 255,0,0 },
	}
};
#endif

        private static readonly Sfx FxFallThruBox = new Sfx(
            12,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxLazer = new Sfx(
            12,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxScanner = new Sfx(
            12,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxHelmetUp3 = new Sfx(
            12,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxLiquidBubble = new Sfx(
            12,
            SfxfSave,
            new[]
            {
                new Room(80, 127, 127),
                new Room(72, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxLiquidDrip = new Sfx(
            13,
            6 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxGooDrip = new Sfx(
            13,
            5 + SfxfStartDelay,
            new[]
    {
        new Room(255, 127, 127)
    });

        private static readonly Sfx FxCompBleeps = new Sfx(
            13,
            0,
            new[]
            {
                new Room(255, 127, 127)
                });

        private static readonly Sfx FxUseCrowbarGrill = new Sfx(
            13,
            34 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxHelmetGrind = new Sfx(
            14,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxLiftMoving = new Sfx(
            14,
            SfxfSave,
            new[]
            {
                new Room(7, 127, 127),
                new Room(29, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxUseSecateurs = new Sfx(
            14,
            18 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxHitJoey1 = new Sfx(
            14,
            7 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxHitJoey2 = new Sfx(
            14,
            13 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxDaniPhoneRing = new Sfx(
            15,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxSc74PodDown = new Sfx(
            15,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxPhone = new Sfx(
            15,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx Fx25Weld = new Sfx(
            15,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxLiftOpen7 = new Sfx(
            15,
            0,
            new[]
            {
                new Room(7, 127, 127),
                new Room(255, 0, 0)
            });

        private static readonly Sfx FxLiftClose7 = new Sfx(
            16,
            0,
            new[]
            {
                new Room(7, 127, 127),
                new Room(255, 0, 0)
            });

        private static readonly Sfx FxS2Helmet = new Sfx(
            16,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxHissInNitrogen = new Sfx(
            16,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx FxDogYapIndoors = new Sfx(
            16,
            0,
            new[]
            {
                new Room(38, 127, 127),
                new Room(255, 0, 0)
            });

        private static readonly Sfx FxDogYapOutdoors = new Sfx(
            16,
            0,
            new[]
            {
                new Room(31, 127, 127),
                new Room(30, 40, 40),
                new Room(32, 40, 40),
                new Room(33, 40, 40),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxLockerCreakOpen = new Sfx(
            17,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxBigTentGurgle = new Sfx(
            17,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxWindHowl = new Sfx(
            17,
            SfxfSave,
            new[]
            {
                new Room(1, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxLiftOpen29 = new Sfx(
            17,
            0,
            new[]
            {
                new Room(29, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxLiftArrive7 = new Sfx(
            17,
            0,
            new[]
            {
                new Room(7, 63, 63),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxLiftClose29 = new Sfx(
            18,
            0,
            new[]
            {
                new Room(29, 127, 127),
                new Room(28, 127, 127),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxShaftIndustrialNoise = new Sfx(
            18,
            SfxfSave,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxGallDrop = new Sfx(
            18,
            29 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxDoorSlamUnder = new Sfx(
            19,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxReichsFish = new Sfx(
            19,
            SfxfSave,
            new[]
            {
                new Room(255, 60, 60),
            });

        private static readonly Sfx FxJudgesGavel1 = new Sfx(
            19,
            13 + SfxfStartDelay,
            new[]
            {
                new Room(255, 60, 60),
            });

        private static readonly Sfx FxJudgesGavel2 = new Sfx(
            19,
            16 + SfxfStartDelay,
            new[]
            {
                new Room(255, 90, 90),
            });

        private static readonly Sfx FxJudgesGavel3 = new Sfx(
            19,
            19 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxWind3 = new Sfx(
            20,
            SfxfSave,
            new[]
            {
                new Room(255, 60, 60),
            });

        private static readonly Sfx FxFactSensor = new Sfx(
            20,
            SfxfSave,
            new[]
            {
                new Room(255, 60, 60),
            });

        private static readonly Sfx FxMediStabGall = new Sfx(
            20,
            17 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxComputer3 = new Sfx(
            21,
            SfxfSave,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxTimberCracking = new Sfx(
            21,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxAnchorFall = new Sfx(
            22,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxElevator4 = new Sfx(
            22,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxStarTrek2 = new Sfx(
            22,
            SfxfSave,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxLiftClosing = new Sfx(
            23,
            0,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxHeartbeat = new Sfx(
            23,
            11 + SfxfStartDelay,
            new[]
            {
                new Room(67, 60, 60),
                new Room(68, 60, 60),
                new Room(69, 60, 60),
                new Room(77, 20, 20),
                new Room(78, 50, 50),
                new Room(79, 70, 70),
                new Room(80, 127, 127),
                new Room(81, 60, 60),
                new Room(255, 0, 0),
            });

        private static readonly Sfx FxPosKey = new Sfx(
            25,
            2 + SfxfStartDelay,
            new[]
            {
                new Room(255, 127, 127),
            });

        private static readonly Sfx FxNegKey = new Sfx(
            26,
            2 + SfxfStartDelay,
            new[]
            {
                new Room(255, 100, 100),
            });

        private static readonly Sfx FxOrificeSwallowDrip = new Sfx(
            28,
            0,
            new[]
            {
                new Room(255, 127, 127)
            });

        private static readonly Sfx[] MusicList = {
    FxPressBang, // 256 banging of the press
	FxPressHiss, // 257 hissing press
	FxWindHowl, // 258 howling wind
	FxSpannerClunk, // 259 spanner in works
	FxReichsFish, // 260 Reichs fish
	FxExplosion, // 261 panel blows open
	FxWind3, // 262 single steam
	FxOpenDoor, // 263 general open door
	FxOpenLambDoor, // 264 lamb door opens
	FxCompBleeps, // 265 scanner bleeps
	FxHelmetDown3, // 266
	FxHelmetUp3, // 267
	FxHelmetGrind, // 268
	FxLiftClose29, // 269 rm 29 lift closes
	FxLiftOpen29, // 270 rm 29 lift opens
	FxComputer3, // 271 rm 29 lift arrives
	FxLevel3Ping, // 272 background noise in room 4
	FxLiftAlarm, // 273 loader alarm
	FxNull, // 274 furnace room background noise
	FxRm3LiftMoving, // 275 lift moving in room 3
	FxLathe, // 276 jobsworth lathe
	FxFactorySound, // 277 factory background sound
	FxWeld, // 278 do some welding
	FxLiftClose7, // 279 rm 7 lift closes
	FxLiftOpen7, // 280 rm 7 lift opens
	FxLiftArrive7, // 281 rm 7 lift arrives
	FxLiftMoving, // 282 lift moving
	FxScanner, // 283 scanner operating
	FxForceFireDoor, // 284 Force fire door open
	FxNull, // 285 General door creak
	FxPhone, // 286 telephone
	FxLazer, // 287 lazer
	FxLazer, // 288 lazer
	FxAnchorFall, // 289 electric   ;not used on amiga
	FxWeld12, // 290 welding in room 12 (not joey)
	FxHelloHelga, // 291 helga appears
	FxByeeHelga, // 292 helga disapears
	FxNull, // 293 smash through window               ;doesn't exist
	FxPosKey, // 294
	FxNegKey, // 295
	FxS2Helmet, // 296 ;helmet down section 2
	FxS2Helmet, // 297 ;  "      up    "    "
	FxLiftArrive7, // 298 ;security door room 7
	FxNull, // 299
	FxRopeCreak, // 300
	FxCrowbarWooden, // 301
	FxFallThruBox, // 302
	FxUseCrowbarGrill, // 303
	FxUseSecateurs, // 304
	FxGrillCreak, // 305
	FxTimberCracking, // 306
	FxMasonryFall, // 307
	FxMasonryFall, // 308
	FxCrowbarPlaster, // 309
	FxPriseBrick, // 310
	FxBrickHitFoster, // 311
	FxSprayOnSkin, // 312
	FxHitCrowbarBrick, // 313
	FxDropCrowbar, // 314
	FxFireCrackleInPit, // 315
	FxRemoveBarGrill, // 316
	FxLiquidBubble, // 317
	FxLiquidDrip, // 318
	FxGuardFall, // 319
	FxSc74PodDown, // 320
	FxHissInNitrogen, // 321
	FxNull, // 322
	FxHitJoey1, // 323
	FxHitJoey2, // 324
	FxMediStabGall, // 325
	FxGallDrop, // 326
	FxNull, // 327
	FxNull, // 328
	FxNull, // 329
	FxBigTentGurgle, // 330
	FxNull, // 331
	FxOrificeSwallowDrip, // 332
	FxBrickHitPlank, // 333
	FxGooDrip, // 334
	FxPlankVibrating, // 335
	FxSplash, // 336
	FxBuzzer, // 337
	FxShedDoorCreak, // 338
	FxDogYapOutdoors, // 339
	FxDaniPhoneRing, // 340
	FxLockerCreakOpen, // 341
	FxJudgesGavel1, // 342
	FxDogYapIndoors, // 343
	FxBrickHitPlank, // 344
	FxBrickHitPlank, // 345
	FxShaftIndustrialNoise, // 346
	FxJudgesGavel2, // 347
	FxJudgesGavel3, // 348
	FxElevator4, // 349
	FxLiftClosing, // 350
	FxNull, // 351
	FxNull, // 352
	FxSc74PodDown, // 353
	FxNull, // 354
	FxNull, // 355
	FxHeartbeat, // 356
	FxStarTrek2, // 357
	FxNull, // 358
	FxNull, // 359
	FxNull, // 350
	FxNull, // 361
	FxNull, // 362
	FxNull, // 363
	FxNull, // 364
	FxNull, // 365
	FxBreakCrystals, // 366
	FxDisintegrate, // 367
	FxStatueOnArmor, // 368
	FxNull, // 369
	FxNull, // 360
	FxPing, // 371
	FxNull, // 372
	FxDoorSlamUnder, // 373
	FxNull, // 374
	FxNull, // 375
	FxNull, // 376
	FxNull, // 377
	FxNull, // 378
	FxNull, // 379
	FxSteam1, // 380
	FxSteam2, // 381
	FxSteam2, // 382
	FxSteam3, // 383
	FxNull, // 384
	FxNull, // 385
	FxFactSensor, // 386            Sensor in Potts' room
	FxNull, // 387
	FxNull, // 388
	FxNull, // 389
	FxNull, // 390
	FxNull, // 391
	FxNull, // 392
	FxNull, // 393
	Fx25Weld // 394            my anchor weld bodge
};
    }
}
