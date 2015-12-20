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

namespace NScumm.Sword1
{
    partial class Sound
    {
        static readonly FxDef[] _fxList = {
		// 0
        new FxDef(
            new byte[] {0,0,0},						// sampleId
		    0,						// type				(FX_LOOP, FX_RANDOM or FX_SPOT)
		    0,						// delay			(random chance for FX_RANDOM sound fx)
		    new []{							// roomVolList
			    new []{0,0,0},		// {roomNo,leftVol,rightVol}
		    }),
	    //------------------------
	    // 1 Newton's cradle. Anim=NEWTON.
	    new FxDef(
            FX_NEWTON,       // sampleId
            FX_SPOT,			// type
		    7,						// delay (or random chance)
            new []{							// roomVolList
			    new []{45,4,2},		// {roomNo,leftVol,rightVol}
			    new []{0,0,0},		// NULL-TERMINATOR
		    }),
	//------------------------
	// 2
	new FxDef(
        FX_TRAFFIC2,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new [] {					// roomVolList
			new []{1,12,12},	// {roomNo,leftVol,rightVol}
			new []{2,1,1},
            new []{3,1,1},
            new []{4,13,13},
            new []{5,1,1},
            new []{8,7,7},
            new []{0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 3
	new FxDef(
        FX_HORN1,			// sampleId
		FX_RANDOM,		// type
		1200,					// delay (or random chance)
		new [] {							// roomVolList
			new [] {1,3,3},		// {roomNo,leftVol,rightVol}
			new [] {3,1,1},
            new [] {4,1,1},
            new [] {5,2,2},
            new [] {8,4,4},
            new [] {18,2,3},
            new [] {0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 4
	new FxDef(
        FX_HORN2,			// sampleId
		FX_RANDOM,		// type
		1200,					// delay (or random chance)
		new [] {							// roomVolList
			new [] {1,4,4},		// {roomNo,leftVol,rightVol}
			new [] {3,2,2},
            new [] {4,3,3},
            new [] {5,2,2},
            new [] {8,4,4},
            new [] {18,1,1},
            new [] {0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 5
	new FxDef(
        FX_HORN3,			// sampleId
		FX_RANDOM,		// type
		1200,					// delay (or random chance)
		new []{							// roomVolList
			new []{1,4,4},		// {roomNo,leftVol,rightVol}
			new []{2,4,4},
            new []{3,2,2},
            new []{4,3,3},
            new []{5,2,2},
            new []{8,4,4},
            new []{18,1,1},
        }),
    //------------------------
    // 6
    new FxDef(
        FX_CAMERA1,		// sampleId
		FX_SPOT,			// type
		25,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 7
	new FxDef(
        FX_CAMERA2,		// sampleId
		FX_SPOT,			// type
		25,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 8
	new FxDef(
        FX_CAMERA3,		// sampleId
		FX_SPOT,			// type
		25,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    //------------------------
    // 9
        new FxDef(
        FX_SWATER1,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{7,12,12},	// {roomNo,leftVol,rightVol}
			new []{6,12,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 10 Mad dogs in Spain, triggered by George going around the corner in the villa hall.
	// In 56 and 57, the dogs will continue barking after George has either been ejected or sneaked up stairs
	// for a couple of loops before stopping.
	new FxDef(
        FX_DOGS56,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{60,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0}			// NULL-TERMINATOR
		}),
	//------------------------
	// 11
	new FxDef(
        FX_DRIP1,			// sampleId
		FX_RANDOM,		// type
		20,						// delay (or random chance)
        new []{							// roomVolList
			new []{7,15,15},	// {roomNo,leftVol,rightVol}
			new []{6,8,8},		// {roomNo,leftVol,rightVol}
			new[] { 0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 12
	new FxDef(
        FX_DRIP2,			// sampleId
		FX_RANDOM,		// type
		30,						// delay (or random chance)
        new []{							// roomVolList
			new []{7,15,15},	// {roomNo,leftVol,rightVol}
			new []{6,8,8},		// {roomNo,leftVol,rightVol}
			new[] { 0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 13
	new FxDef(
        FX_DRIP3,			// sampleId
		FX_RANDOM,		// type
		40,						// delay (or random chance)
		new []{							// roomVolList
			new []{7,15,15},	// {roomNo,leftVol,rightVol}
			new []{6,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 14
	new FxDef(
        FX_TWEET1,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    //------------------------
    // 15
        new FxDef(
        FX_TWEET2,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
	//------------------------
	// 16
	 new FxDef(
        FX_TWEET3,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 17
	 new FxDef(
        FX_TWEET4,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 18
	 new FxDef(
        FX_TWEET5,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
    //------------------------
    // 19 Tied to large bird flying up screen anim
        new FxDef(
        FX_CAW1,			// sampleId
		FX_SPOT,			// type
		20,						// delay (or random chance)
		new []{							// roomVolList
			new []{1,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	//------------------------
	// 20 George picking the canopy up: GEOCAN
	 new FxDef(
        FX_CANUP,			// sampleId
		FX_SPOT,			// type
		5,						// delay (or random chance) *
		new []{							// roomVolList
			new []{1,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 21 George dropping the canopy: GEOCAN
	 new FxDef(
        FX_CANDO,			// sampleId
		FX_SPOT,			// type
		52,						// delay (or random chance) *
		new []{							// roomVolList
			new []{1,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 22 George dusts himself down: GEOCAN
	new FxDef(
        FX_DUST,			// sampleId
		FX_SPOT,			// type
		58,						// delay (or random chance) *
		new []{							// roomVolList
			new []{1,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
    //------------------------
    // 23 George picks up the paper and opens it: GEOPAP
        new FxDef(
        FX_PAP1,			// sampleId
		FX_SPOT,			// type
		23,						// delay (or random chance) *
		new []{							// roomVolList
			new []{1,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 24 George puts the paper away: GEOPAP2
	new FxDef(
        FX_PAP2,			// sampleId
		FX_SPOT,			// type
		3,						// delay (or random chance) *
		new []{							// roomVolList
			new []{1,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 25 George gives the paper away: GEOWRK8
    new FxDef(
        FX_PAP3,			// sampleId
		FX_SPOT,			// type
		13,						// delay (or random chance) *
		new []{							// roomVolList
			new []{4,14,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 26 Workman examines paper: WRKOPN - it's now just WRKPPR
	new FxDef(
        FX_PAP4,			// sampleId
		FX_SPOT,			// type
		15,						// delay (or random chance) *
        new []{							// roomVolList
			new []{4,14,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 27 Workman puts paper down: WRKOPN (REVERSED) - now just WRKCLM
	new FxDef(
        FX_PAP5,			// sampleId
		FX_SPOT,			// type
		2,						// delay (or random chance)*
        new []{							// roomVolList
			new []{4,14,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 28 Pickaxe sound 1:, Screen 4 - WRKDIG
	new FxDef(
        FX_PICK1,			// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{4,10,10},
            new []{0,0,0}			// NULL-TERMINATOR
		}),
	//------------------------
	// 29 Pickaxe sound 2:, Screen 4 - WRKDIG
	new FxDef(
        FX_PICK2,			// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
        new []{							// roomVolList
			new []{4,10,10},
           new [] {0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	//------------------------
	// 30 Pickaxe sound 3:, Screen 4 - WRKDIG
	new FxDef(
        FX_PICK3,			// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
        new []{							// roomVolList
			new []{4,10,10},
            new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 31 Pickaxe sound 4:, Screen 4 - WRKDIG
	new FxDef(
        FX_PICK4,			// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
        new []{                           // roomVolList
           new[] { 4,10,10},
           new[] { 0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 32 Shorting light: FLICKER
	new FxDef(
        FX_LIGHT,			// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{3,15,15},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 33 Cat leaps out of bin and runs: CATJMP!
	new FxDef(
        FX_CAT,				// sampleId
		FX_SPOT,			// type
		20,						// delay (or random chance) *
        new []{							// roomVolList
			new []{2,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    

	//------------------------
	// 34 George rocks plastic crate: GEOCRT
	new FxDef(
        FX_CRATE,			// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance) *
		new []{							// roomVolList
			new []{2,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 35 George tries to climb drainpipe: GEOCLM02
	new FxDef(
        FX_DRAIN,			// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{2,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 36 George removes manhole cover: GEOMAN8
	new FxDef(
        FX_HOLE,			// sampleId
		FX_SPOT,			// type
		19,						// delay (or random chance) ?
		new []{							// roomVolList
			new []{2,12,11},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 37 Brandy bottle put down: CHNDRN
	new FxDef(
        FX_BOTDN,			// sampleId
		FX_SPOT,			// type
		43,						// delay (or random chance) *
	new []  {							// roomVolList
			new []{3,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 38 Brandy bottle picked up: GEOBOT3
	new FxDef(
        FX_BOTUP,			// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{3,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 39 Chantelle gulps on brandy: CHNDRN
	new FxDef(
        FX_GULP,			// sampleId
		FX_SPOT,			// type
		23,						// delay (or random chance) *
		new []{							// roomVolList
			new []{3,4,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 40 Chantelle picked up off the floor: GEOCHN
	new FxDef(
        FX_PIKUP,			// sampleId
		FX_SPOT,			// type
		28,						// delay (or random chance) *
		new []{							// roomVolList
			new []{3,11,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}),
    
	//------------------------
	// 41 George searches Plantard's body: GEOCPS
	new FxDef(
        FX_BODY,			// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance) *
		new []{							// roomVolList
			new []{3,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 42 Moue cocks handgun. MOUENT
	new FxDef(
        FX_PISTOL,		// sampleId
		FX_SPOT,			// type
		23,						// delay (or random chance) *
		new []{							// roomVolList
            new []{4,4,7},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 43 George rummages in toolbox: GEOTBX
	new FxDef(
        FX_TBOX,			// sampleId
		FX_SPOT,			// type
		12,						// delay (or random chance) *
		new []{							// roomVolList
			new []{4,12,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 44 rat squeak 1
	new FxDef(
        FX_RAT1,			// sampleId
		FX_RANDOM,		// type
		193,					// delay (or random chance)
		new []{							// roomVolList
			new []{6,5,7},		// {roomNo,leftVol,rightVol}
			new []{7,5,3},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 45 rat squeak 2
	new FxDef(
        FX_RAT2,			// sampleId
		FX_RANDOM,		// type
		201,					// delay (or random chance)
		new []{							// roomVolList
			new []{6,3,5},		// {roomNo,leftVol,rightVol}
			new []{7,4,6},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 46 George climbs down ladder:
	new FxDef(
        FX_LADD1,			// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{6,10,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 47 Rushing water loop
	new FxDef(
        FX_SWATER3,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{6,10,11},	// {roomNo,leftVol,rightVol}
			new []{7,12,11},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 48 Left hand bin being opened: GEOCAT?
	new FxDef(
        FX_BIN3,			// sampleId
		FX_SPOT,			// type
		12,						// delay (or random chance)
        new []{							// roomVolList
			new []{2,12,11},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 49 Middle bin being opened: GEOBIN
	new FxDef(
        FX_BIN2,			// sampleId
		FX_SPOT,			// type
		12,						// delay (or random chance)
        new []{							// roomVolList
			new []{2,11,11},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 50 Right hand bin being opened: GEOLID?
	new FxDef(
        FX_BIN1,			// sampleId
		FX_SPOT,			// type
		12,						// delay (or random chance)
		new []{							// roomVolList
			new []{2,10,11},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 51 Passing car sound
	new FxDef(
        FX_CARS,			// sampleId
		FX_RANDOM,		// type
		120,					// delay (or random chance)
		new []{							// roomVolList
			new []{10,8,1},
            new []{12,7,7},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 52 Passing car sound
	new FxDef(
        FX_FIESTA,		// sampleId
		FX_RANDOM,		// type
		127,					// delay (or random chance)
		new []{							// roomVolList
			new []{10,8,1},
            new []{12,7,7},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 53 Passing car sound
	new FxDef(
        FX_CARLTON ,	// sampleId
		FX_RANDOM,		// type
		119,					// delay (or random chance)
		new []{							// roomVolList
			new []{10,8,1},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 54 Bird
	new FxDef(
        FX_BIRD,			// sampleId
		FX_RANDOM,		// type
		500,					// delay (or random chance)
		new []{							// roomVolList
			new []{9,10,10},	// {roomNo,leftVol,rightVol}
			new []{10,2,1},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 55 George tries the door: GEOTRY
	new FxDef(
        FX_DOORTRY,		// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance)
		new []{							// roomVolList
			new []{9,9,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 56 George opens the door: GEODOOR9
	new FxDef(
        FX_FLATDOOR,	// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{9,9,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 57 George picks the 'phone up: GEOPHN10
	new FxDef(
        FX_FONEUP,		// sampleId
		FX_SPOT,			// type
		15,						// delay (or random chance)
		new []{							// roomVolList
			new []{10,9,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 58 George puts the 'phone down: GEPDWN10
	new FxDef(
        FX_FONEDN,		// sampleId
		FX_SPOT,			// type
		4,						// delay (or random chance)
		new []{							// roomVolList
			new []{10,9,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 59 Albert opens the door: ALBOPEN
	new FxDef(
        FX_ALBOP,			// sampleId
		FX_SPOT,			// type
		13,						// delay (or random chance)
		new []{							// roomVolList
			new []{5,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 60 Albert closes the door: ALBCLOSE
	new FxDef(
        FX_ALBCLO,		// sampleId
		FX_SPOT,			// type
		20,						// delay (or random chance)
		new []{							// roomVolList
            new []{5,9,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 61 George enter Nico's flat. GEOENT10
	new FxDef(
        FX_NICOPEN,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		new []{							// roomVolList
			new []{10,7,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 62 George leaves Nico's. GEOLVS10
	new FxDef(
        FX_NICLOSE,     // sampleId
        FX_SPOT,			// type
		13,						// delay (or random chance)
        new []{							// roomVolList
			new []{10,7,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 63 Another bird for the street.
	new FxDef(
        FX_BIRD2,           // sampleId
        FX_RANDOM,		// type
		500,					// WAS 15 (TOO LATE)
        new []{							// roomVolList
			new []{9,10,10},	// {roomNo,leftVol,rightVol}
			new []{10,2,1},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 64 George sits in the chair: GEOCHR
	new FxDef(
        FX_GEOCHAIR,    // sampleId
        FX_SPOT,			// type
		14,						// delay (or random chance)
        new []{							// roomVolList
			new []{10,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 65 George sits on the couch: GEOCCH
	new FxDef(
        FX_GEOCCH,      // sampleId
        FX_SPOT,			// type
		14,						// delay (or random chance)
        new []{							// roomVolList
			new []{10,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 66 George gets up from the chair:  GEOCHR9
	new FxDef(
        FX_GEOCHR9,     // sampleId
        FX_SPOT,			// type
		5,						// delay (or random chance)
        new []{							// roomVolList
			new []{10,3,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 67 George is electrocuted: COSSHK
	new FxDef(
        FX_SHOCK1,      // sampleId
        FX_SPOT,			// type
		19,						// delay (or random chance)
        new []{							// roomVolList
			new []{11,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 68 George plays record: GEOWIND
	new FxDef(
        FX_GRAMOFON,    // sampleId
        FX_SPOT,			// type
		0,						// delay (or random chance)
        new []{							// roomVolList
			new []{11,11,13},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 69 George is frisked: GORFRK
	new FxDef(
        FX_FRISK,           // sampleId
        FX_SPOT,			// type
		6,						// delay (or random chance)
        new []{							// roomVolList
			new []{12,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 70 Traffic sound
	new FxDef(
        FX_TRAFFIC3,    // sampleId
        FX_LOOP,			// type
		0,						// delay (or random chance)
        new []{							// roomVolList
			new []{11,5,4},
            new []{12,1,1},
            new []{16,4,4},
            new []{18,2,3},
            new []{46,4,3},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 71 Latvian reading: LATRDS
	new FxDef(
        FX_PAPER6,      // sampleId
        FX_SPOT,			// type
		8,						// delay (or random chance)
        new []{							// roomVolList
			new []{13,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 72 Deskbell
	new FxDef(
        FX_DESKBELL,    // sampleId
        FX_SPOT,			// type
		0,						// delay (or random chance)
        new []{							// roomVolList
			new []{13,10,8},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 73 George picks up hotel 'phone: GEOTEL
	new FxDef(
        FX_PHONEUP2,    // sampleId
        FX_SPOT,			// type
		10,						// delay (or random chance)
        new []{							// roomVolList
			new []{13,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 74 George puts down hotel 'phone: GEOTEL9
	new FxDef(
        FX_PHONEDN2,    // sampleId
        FX_SPOT,			// type
		10,						// delay (or random chance)
        new []{							// roomVolList
			new []{13,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 75 George tries doors in corridor: GEODOR
	new FxDef(
        FX_TRYDOR14,    // sampleId
        FX_SPOT,			// type
		10,						// delay (or random chance)
        new []{							// roomVolList
			new []{14,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 76 George opens bedside cabinet: BEDDOR
	new FxDef(
        FX_CABOPEN,     // sampleId
        FX_SPOT,			// type
		11,						// delay (or random chance)
        new []{							// roomVolList
			new []{15,10,14},	// {roomNo,leftVol,rightVol}
			new []{17,10,14},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 77 George closes bedside cabinet: BEDDOR (reversed)
	new FxDef(
        FX_CABCLOSE,    // sampleId
        FX_SPOT,			// type
		5,						// delay (or random chance)
        new []{							// roomVolList
			new []{15,10,14},	// {roomNo,leftVol,rightVol}
			new []{17,10,14},
            new[] { 0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 78 George opens the window: WINDOW
	new FxDef(
        FX_WINDOPEN,    // sampleId
        FX_SPOT,			// type
		19,						// delay (or random chance)
        new []{							// roomVolList
			new []{15,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 79 George goes right along the ledge: GEOIRW
	new FxDef(
        FX_LEDGE1,      // sampleId
        FX_SPOT,			// type
		1,						// delay (or random chance)
        new []{							// roomVolList
			new []{16,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 80 George goes left along the ledge: GEOILW
	new FxDef(
        FX_LEDGE2,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		new []{							// roomVolList
			new []{16,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 81 Pigeon noises
	new FxDef(
        FX_COO,				// sampleId
		FX_RANDOM,		// type
		80,						// delay (or random chance)
		new []{							// roomVolList
			new []{16,7,9},		// {roomNo,leftVol,rightVol}
			new []{46,5,4},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 82 Pigeon noises
	new FxDef(
        FX_COO2,			// sampleId
		FX_RANDOM,		// type
		60,						// delay (or random chance)
		new []{							// roomVolList
			new []{15,3,4},		// {roomNo,leftVol,rightVol}
			new []{16,8,5},		// {roomNo,leftVol,rightVol}
			new []{17,3,4},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 83 George picks up and opens case: GEOBFC
	new FxDef(
        FX_BRIEFON,		// sampleId
		FX_SPOT,			// type
		16,						// delay (or random chance)
		new []{							// roomVolList
			new []{17,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 84 George closes and puts down case: GEOBFC (reversed)
	new FxDef(
        FX_BRIEFOFF,	// sampleId
		FX_SPOT,			// type
		12,						// delay (or random chance)
		new []{							// roomVolList
			new []{17,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 85 George gets into wardrobe. GEOWRB2 Attention, James. This is new as of 15/7/96
	new FxDef(
        FX_WARDIN,		// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance)
		new []{							// roomVolList
			new []{17,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 86 George gets out of wardrobe. GEOWRB2  (Reversed). Attention, James. This is new as of 15/7/96
	new FxDef(
        FX_WARDOUT,		// sampleId
		FX_SPOT,			// type
		41,						// delay (or random chance)
		new []{							// roomVolList
			new []{17,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 87 George jumps in through window: GEOWIN2
	new FxDef(
        FX_JUMPIN,		// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance)
		new []{							// roomVolList
			new []{15,8,10},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 88 George climbs in: GEOWIN2/GEOWIN8
	new FxDef(
        FX_CLIMBIN,		// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{17,8,16},	// {roomNo,leftVol,rightVol}
			new []{15,8,16},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 89 George climbs out: GEOWIN1/GEOWIN9
	new FxDef(
        FX_CLIMBOUT,	// sampleId
		FX_SPOT,			// type
		17,						// delay (or random chance)
		new []{							// roomVolList
			new []{17,9,10},	// {roomNo,leftVol,rightVol}
			new []{15,9,10},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 90 George picks the 'phone up: GEOTEL18
	new FxDef(
        FX_FONEUP,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{18,4,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 91 George puts the 'phone down: GEOTL18A
	new FxDef(
        FX_FONEDN,		// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance)
		new []{							// roomVolList
			new []{18,4,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 92 George tries to get keys. GEOKEY
	new FxDef(
        FX_KEY13,			// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance)
		new []{							// roomVolList
			new []{13,3,2},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 93 George manages to get keys. GEOKEY13
	new FxDef(
        FX_KEY13,			// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance)
		new []{							// roomVolList
			new []{13,3,2},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 94 George electrocutes Maguire: MAGSHK
	new FxDef(
        FX_SHOCK2,		// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance)
		new []{							// roomVolList
			new []{19,9,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 95 George opens dray door : GEOTRP8
	new FxDef(
        FX_TRAPOPEN,	// sampleId
		FX_SPOT,			// type
		20,						// delay (or random chance)
		new []{							// roomVolList
            new []{19,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 96 George breaks switch : Which anim?
	new FxDef(
        FX_SWITCH19,	// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
            new []{19,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 97 Leary pulls pint: LESPMP
	new FxDef(
        FX_PULLPINT,	// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance)
		new []{							// roomVolList
			new []{20,10,8},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 98 Glasswasher fuse blows (and the glass washer grinds to a halt)
	new FxDef(
        FX_FUSE20,		// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
            new []{20,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 99 Fitz leaps to his feet: FTZSTD
	new FxDef(
        FX_FITZUP,		// sampleId
		FX_SPOT,			// type
		5,						// delay (or random chance)
        new []{							// roomVolList
			new []{20,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 100 Fitz runs for it: FTZRUN
	new FxDef(
        FX_FITZRUN,		// sampleId
		FX_SPOT,			// type
		15,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{20,12,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 101 George pulls lever: GEOLVR & GEOLVR26
	new FxDef(
        FX_LEVER,			// sampleId
		FX_SPOT,			// type
		26,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{21,8,10},	// {roomNo,leftVol,rightVol}
			 new []{26,8,10},
             new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 102 George pulls lever: GEOLVR8 & GEOLVR08
	new FxDef(
        FX_LEVER2,		// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{21,8,10},	// {roomNo,leftVol,rightVol}
			 new []{26,8,10},
             new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 103 George opens tap: No idea what the anim is
	new FxDef(
        FX_TAP,				// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{21,8,8},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 104 George closes tap: No idea what this anim is either
	new FxDef(
        FX_TAP2,			// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{21,8,8},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 105 Bar flap: FLPOPN
	new FxDef(
        FX_BARFLAP,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{20,6,6},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 106 Farmer leaves: FRMWLK
	new FxDef(
        FX_FARMERGO,	// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{22,6,9},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 107 George climbs haystack: GEOCLM
	new FxDef(
        FX_CLIMBHAY,	// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{22,14,14},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 108 George drives sewer key into wall: GEOKEY23
	new FxDef(
        FX_KEYSTEP,		// sampleId
		FX_SPOT,			// type
		39,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{23,8,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 109 George climbs over wall: GEOCLM23
	new FxDef(
        FX_CASTLWAL,	// sampleId
		FX_SPOT,			// type
		17,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{23,8,8},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 110 George falls from wall: GEOTRY23
	new FxDef(
        FX_CLIMBFAL,	// sampleId
		FX_SPOT,			// type
		43,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{23,12,12},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 111 Goat chewing: GOTEAT
	new FxDef(
        FX_GOATCHEW,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{24,10,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 112 George moves plough: GEOPLW
	new FxDef(
        FX_PLOUGH,		// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{24,10,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 113 George drops slab: STNFALL
	new FxDef(
        FX_SLABFALL,	// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{25,10,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 114 George picks up slab: GEOSTN8
	new FxDef(
        FX_SLABUP,		// sampleId
		FX_SPOT,			// type
		29,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{25,10,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 115 Secret door opens: ALTOPN
	new FxDef(
        FX_SECDOR25,	// sampleId
		FX_SPOT,			// type
		17,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{25,10,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 116 George wrings out cloth: GEOTWL25
	new FxDef(
        FX_WRING,			// sampleId
		FX_SPOT,			// type
		24,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{25,10,10},	// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 117 Rat running across barrels: RATJMP
	new FxDef(
        FX_RAT3A,			// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{26,8,5},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 118 Rat running across barrels: RATJMP
	new FxDef(
        FX_RAT3B,			// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{26,7,6},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 119 Rat running across barrels: RATJMP
	new FxDef(
        FX_RAT3C,			// sampleId
		FX_SPOT,			// type
		26,						// delay (or random chance)
		 new []{							// roomVolList
			 new []{26,8,8},		// {roomNo,leftVol,rightVol}
			 new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 120 Irish bird song 1:
	new FxDef(
        FX_EIRBIRD1,	// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance)
		new []{							// roomVolList
			new []{19,6,8},		// {roomNo,leftVol,rightVol}
			new []{21,2,3},
            new []{22,8,5},
            new []{23,6,5},
            new []{24,8,8},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 121 Irish bird song 2:
	new FxDef(
        FX_EIRBIRD2,	// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance)
		new []{							// roomVolList
			new []{19,8,6},		// {roomNo,leftVol,rightVol}
			new []{21,2,3},
            new []{22,6,8},
            new []{23,5,5},
            new []{24,8,8},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 122 Irish bird song 3:
	new FxDef(
        FX_EIRBIRD3,	// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance)
		new []{							// roomVolList
			new []{19,8,8},		// {roomNo,leftVol,rightVol}
			new []{21,3,4},
            new []{22,8,8},
            new []{23,5,6},
            new []{24,6,8},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 123 Rat 3D:
	new FxDef(
        FX_RAT3D,			// sampleId
		FX_RANDOM,		// type
		600,					// delay (or random chance)
		new []{							// roomVolList
			new []{26,2,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 124 Wind atop the battlements
	new FxDef(
        FX_WIND,			// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{23,6,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 125 Glasswasher in the pub (Room 20) *JEL* Stops after fuse blows and starts when george fixes it.
	new FxDef(
        FX_WASHER,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{20,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 126 Running tap in the cellar: (Room 21) *JEL* Only when the tap is on.
	new FxDef(
        FX_CELTAP,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{21,3,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 127 Lopez's hose. Basically a loop but stops when George cuts the water supply. Replaces MUTTER1.
	new FxDef(
        FX_HOSE57,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{57,3,1},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 128 Lopez's hose being switched off. Anim GARD05. Replaces MUTTER2.
	new FxDef(
        FX_HOSE57B,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		new []{							// roomVolList
			new []{57,3,2},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 129 Nejo bouncing the ball off the door. NEJ8
	new FxDef(
        FX_BALLPLAY,	// sampleId
		FX_SPOT,			// type
		13,						// delay (or random chance)
		new []{							// roomVolList
			new []{45,5,1},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 130 Cricket loop for Syrian desert Only audible in 55 when the cave door is open.
	new FxDef(
        FX_CRICKET,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,8,8},		// {roomNo,leftVol,rightVol}
			new []{55,3,5},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 131 Display case shatters: GEOTOTB
	new FxDef(
        FX_SMASHGLA,	// sampleId
		FX_SPOT,			// type
		35,						// delay (or random chance)
		new []{							// roomVolList
			new []{29,16,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 132 Burglar alarm: Once the case is smashed (see 131)
	new FxDef(
        FX_ALARM,			// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{28,12,12},	// {roomNo,leftVol,rightVol}
			new []{29,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 133 Guido fires: GUIGUN
	new FxDef(
        FX_GUN1,			// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance)
		new []{							// roomVolList
			new []{29,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 134 Guido knocked down: NICPUS1
	new FxDef(
        FX_GUI_HIT,		// sampleId
		FX_SPOT,			// type
		40,						// delay (or random chance)
		new []{							// roomVolList
			new []{29,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 135 Museum exterior ambience
	new FxDef(
        FX_MUESEXT,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{27,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 136 Cat gets nowty: CAT3
	new FxDef(
        FX_STALLCAT,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		new []{							// roomVolList
			new []{45,10,6},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 137 Cat gets very nowty: CAT5
	new FxDef(
        FX_CATHIT,		// sampleId
		FX_SPOT,			// type
		4,						// delay (or random chance)
        new []{							// roomVolList
			new []{45,10,6},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 138 Desert wind: Only audible in 55 when the cave door is open.
	new FxDef(
        FX_SYRIWIND,	// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance)
		new []{							// roomVolList
			new []{54,10,10},	// {roomNo,leftVol,rightVol}
			new []{55,5,7},
           new [] {0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 139 Bell on Nejo's stall: GEOSYR7
	new FxDef(
        FX_STALLBEL,	// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance)
		new []{							// roomVolList
			new []{45,10,8},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 140 George electrocutes Khan: GEOSYR40
	new FxDef(
        FX_SHOCK3,		// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 141 George thumps Khan: GEOSYR40
	new FxDef(
        FX_THUMP1,		// sampleId
		FX_SPOT,			// type
		22,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 142 Khan hits the floor: KHS9
	new FxDef(
        FX_KHANDOWN,	// sampleId
		FX_SPOT,			// type
		24,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 143 Hospital ambience
	new FxDef(
        FX_HOSPNOIS,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{32,6,4},		// {roomNo,leftVol,rightVol}
			new []{33,7,7},
            new []{34,3,4},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 144 Mr Shiny switched on: DOMPLG (Start FX_SHINY)
	new FxDef(
        FX_SHINYON,		// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{33,12,14},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 145 Mr Shiny running
	new FxDef(
        FX_SHINY,			// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{32,4,3},		// {roomNo,leftVol,rightVol}
			new []{33,12,14},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 146 Mr Shiny switched off: GEOPLG33 (Turn off FX_SHINY at the same time)
	new FxDef(
        FX_SHINYOFF,	// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{33,12,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
   ),
	//------------------------
	// 147 Benoir takes blood pressure: BENBP1 or BENBP2
	new FxDef(
        FX_BLOODPRE,	// sampleId
		FX_SPOT,			// type
		39,						// delay (or random chance)
		new []{							// roomVolList
			new []{34,14,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 148 George takes blood pressure: GEOBP1 or GEOBP2
	new FxDef(
        FX_BLOODPRE,	// sampleId
		FX_SPOT,			// type
		62,						// delay (or random chance)
		new []{							// roomVolList
			new []{34,14,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 149 Goat baas as it attacks: GOTCR and GOTCL
	new FxDef(
        FX_GOATBAA,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		new []{							// roomVolList
			new []{24,12,12},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 150 Goat peeved at being trapped: GOTPLW (I'd advise triggering this anim randomly if you haven't done that)
	new FxDef(
        FX_GOATDOH,		// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance)
		new []{							// roomVolList
			new []{24,7,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 151 George triggers the Irish secret door: GEOPUT
	new FxDef(
        FX_TRIGER25,	// sampleId
		FX_SPOT,			// type
		35,						// delay (or random chance)
		new []{							// roomVolList
			new []{25,6,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 152 George winds up gramophone: GEOWIND
	new FxDef(
        FX_WINDUP11,	// sampleId
		FX_SPOT,			// type
		16,						// delay (or random chance)
		new []{							// roomVolList
			new []{11,7,7},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 153 Marib ambience
	new FxDef(
        FX_MARIB,			// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{45,7,7},		// {roomNo,leftVol,rightVol}
			new []{47,5,5},
            new []{50,5,4},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 154 Statuette breaks: STA2
	new FxDef(
        FX_STATBREK,	// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{45,7,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 155 George opens toilet door: CUBDOR50
	new FxDef(
        FX_CUBDOR,		// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance)
		new []{							// roomVolList
			new []{50,6,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 156 Crowd goes, "Ooh!": CRO36APP
	new FxDef(
        FX_OOH,				// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance)
		new []{							// roomVolList
			new []{36,6,7},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 157 Phone rings: When Nico calls back in room 41. Loops until the guard answers it.
	new FxDef(
        FX_PHONCALL,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{41,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 158 Phone picked up in 41: GUA41ANS
	new FxDef(
        FX_FONEUP41,	// sampleId
		FX_SPOT,			// type
		18,						// delay (or random chance)
		new []{							// roomVolList
			new []{41,5,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 159 George turns thermostat: GEO41THE (another dummy). Also used on the reverse.
	new FxDef(
        FX_THERMO1,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance)
		new []{							// roomVolList
			new []{41,6,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 160 Low echoing rumble of large church
	new FxDef(
        FX_CHURCHFX,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance)
		new []{							// roomVolList
			new []{38,5,5},		// {roomNo,leftVol,rightVol}
			new []{48,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 161 George drys hand: GEO43HAN
	new FxDef(
        FX_DRIER1,		// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance)
		new []{							// roomVolList
			new []{43,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 162 George jumps in through window: GEOWIN8
	new FxDef(
        FX_JUMPIN,		// sampleId
		FX_SPOT,			// type
		49,						// delay (or random chance)
		new []{							// roomVolList
			new []{17,8,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 163 Khan fires: KHS12
	new FxDef(
        FX_SHOTKHAN,	// sampleId
		FX_SPOT,			// type
		30,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 164 Khan fires: KHS5
	new FxDef(
        FX_SHOTKHAN,	// sampleId
		FX_SPOT,			// type
		5,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 165 George falls: GEOSYR37
	new FxDef(
        FX_GEOFAL54,	// sampleId
		FX_SPOT,			// type
		25,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 166 George falls after going for the gun (GEOSYR42)
	new FxDef(
        FX_GEOFAL54,	// sampleId
		FX_SPOT,			// type
		46,						// delay (or random chance)
		new []{							// roomVolList
			new []{54,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 167 Pickaxe sound 5: Screen 1 - WRKDIG01
	new FxDef(
        FX_PICK5,			// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{1,3,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 168 George climbs ladder in 7: GEOASC07
	new FxDef(
        FX_SEWLADU7,	// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance) *
		new []{							// roomVolList
			new []{7,8,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 169 George picks keys up in Alamut: GEOKEYS1
	new FxDef(
        FX_KEYS49,		// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{49,8,7},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 170 George puts down keys up in Alamut: GEOKEYS2
	new FxDef(
        FX_KEYS49,		// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance) *
		new []{							// roomVolList
			new []{49,8,7},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 171 George unlocks toilet door: GEOSYR43
	new FxDef(
        FX_UNLOCK49,	// sampleId
		FX_SPOT,			// type
		16,						// delay (or random chance) *
		new []{							// roomVolList
			new []{49,6,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 172 George breaks the toilet chain. GEOSYR48
	new FxDef(
        FX_WCCHAIN,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance) *
		new []{							// roomVolList
			new []{50,6,7},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 173 George breaks the branch of the cliff edge tree. GEOSYR20
	new FxDef(
        FX_BREKSTIK,	// sampleId
		FX_SPOT,			// type
		16,						// delay (or random chance) *
		new []{							// roomVolList
			new []{54,6,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 174 George climbs down the cliff face. GEOSYR23
	new FxDef(
        FX_CLIMBDWN,	// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance) *
		new []{							// roomVolList
			new []{54,6,7},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 175 George pulls ring:  GEOSYR26
	new FxDef(
        FX_RINGPULL,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{54,7,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 176 Bull's Head door opens: SECDOR
	new FxDef(
        FX_SECDOR54,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{54,7,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 177 Inside Bull's Head door opens: DOOR55 (and its reverse).
	new FxDef(
        FX_SECDOR55,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{55,4,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 178 Ayub opens door. AYU1
	new FxDef(
        FX_AYUBDOOR,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{45,8,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 179 George knocks at the door in location 4: GEONOK followed by reverse of GEONOK
	new FxDef(
        FX_KNOKKNOK,	// sampleId
		FX_SPOT,			// type
		13,						// delay (or random chance) *
		new []{							// roomVolList
			new []{4,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 180 George knocks at the door in location 5: GEONOK05
	new FxDef(
        FX_KNOKKNOK,	// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance) *
		new []{							// roomVolList
			new []{5,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 181 Those pesky Irish birds turn up in Spain, too.
	new FxDef(
        FX_SPNBIRD1,	// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance) *
		new []{							// roomVolList
			new []{57,1,4},
            new []{58,8,4},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 182 Those pesky Irish birds turn up in Spain, too.
	new FxDef(
        FX_SPNBIRD2,	// sampleId
		FX_RANDOM,		// type
		697,					// delay (or random chance) *
		new []{							// roomVolList
			new []{57,4,8},		// {roomNo,leftVol,rightVol}
			new []{58,4,1},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 183 The secret door in the well: SECDOR61 anim
	new FxDef(
        FX_SECDOR61,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{61,4,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 184 Spanish countryside ambience
	new FxDef(
        FX_SPAIN,			// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{57,1,2},		//
			new []{58,2,2},		//
			new []{60,1,1},		//
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 185 Spanish well ambience
	new FxDef(
        FX_WELLDRIP,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{61,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 186 Fish falls on George's head: GEOTOT29
	new FxDef(
        FX_FISHFALL,	// sampleId
		FX_SPOT,			// type
		60,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,10,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 187 Hospital exterior ambience
	new FxDef(
        FX_HOSPEXT,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{31,3,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 188 Hospital exterior gravel footstep #1
	new FxDef(
        FX_GRAVEL1,		// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{31,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 189 Hospital exterior gravel footstep #2
	new FxDef(
        FX_GRAVEL2,		// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{31,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 190 George opens sarcophagus: GEOSAR
	new FxDef(
        FX_SARCO28A,	// sampleId
		FX_SPOT,			// type
		26,						// delay (or random chance) *
		new []{							// roomVolList
			new []{28,6,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 191 George closes sarcophagus: GEOSAR2
	new FxDef(
        FX_SARCO28B,	// sampleId
		FX_SPOT,			// type
		24,						// delay (or random chance) *
		new []{							// roomVolList
			new []{28,3,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 192 Guard opens sarcophagus: MUSOPN
	new FxDef(
        FX_SARCO28C,	// sampleId
		FX_SPOT,			// type
		14,						// delay (or random chance) *
		new []{							// roomVolList
			new []{28,3,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 193 George peeks out of sarcophagus: GEOPEEK
	new FxDef(
        FX_SARCO29,		// sampleId
		FX_SPOT,			// type
		4,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,5,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 194 The rope drops into the room: ROPE29
	new FxDef(
        FX_ROPEDOWN,	// sampleId
		FX_SPOT,			// type
		3,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,3,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 195 George pushes the totem pole: GEOTOT29
	new FxDef(
        FX_TOTEM29A,	// sampleId
		FX_SPOT,			// type
		30,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 196 George pushes the totem pole over: GEOTOTB
	new FxDef(
        FX_TOTEM29B,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 197 George rocks the totem pole in museum hours: TOTEM28
	new FxDef(
        FX_TOTEM28A,	// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance) *
		new []{							// roomVolList
			new []{28,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 198 Ambient sound for Montfaucon Square
	new FxDef(
        FX_MONTAMB,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{36,6,6},		// {roomNo,leftVol,rightVol}
			new []{40,6,6},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 199 Ambient sound churchyard.
	new FxDef(
        FX_WIND71,		// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance) *
		new []{							// roomVolList
			new []{71,10,10},	// {roomNo,leftVol,rightVol}
			new []{72,7,7},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 200 Owl cry #1 in churchyard
	new FxDef(
        FX_OWL71A,		// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance) *
		new []{							// roomVolList
			new []{71,8,8},		// {roomNo,leftVol,rightVol}
			new []{72,6,4},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 201 Owl cry #2 in churchyard
	new FxDef(
        FX_OWL71B,		// sampleId
		FX_RANDOM,		// type
		1080,					// delay (or random chance) *
		new []{							// roomVolList
			new []{71,8,8},		// {roomNo,leftVol,rightVol}
			new []{72,7,6},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 202 Air conditioner in the museum
	new FxDef(
        FX_AIRCON28,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{28,6,6},		// {roomNo,leftVol,rightVol}
			new []{29,3,3},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 203 George breaks the handle off in the church tower. GEOWND72
	new FxDef(
        FX_COG72A,		// sampleId
		FX_SPOT,			// type
		5,						// delay (or random chance) *
		new []{							// roomVolList
			new []{72,10,10},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 204 Countess' room ambience
	new FxDef(
        FX_AMBIEN56,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{56,3,2},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 205 Musical effect for George drinking beer. GEODRN20
	new FxDef(
        FX_DRINK,			// sampleId
		FX_SPOT,			// type
		17,						// delay (or random chance) *
		new []{							// roomVolList
			new []{20,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 206 Torch thrown through the air. GEOTHROW
	new FxDef(
        FX_TORCH73,		// sampleId
		FX_SPOT,			// type
		14,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 207 Internal train ambience.
	new FxDef(
        FX_TRAININT,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{63,3,3},		// {roomNo,leftVol,rightVol}
			new []{65,2,2},
            new []{66,2,2},
            new []{67,2,2},
            new []{69,2,2},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 208 Countess' clock. PENDULUM. Note: Trigger the sound effect on alternate runs of the pendulum animation.
	new FxDef(
        FX_PENDULUM,	// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance) *
		new []{							// roomVolList
			new []{56,2,2},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 209 Compartment door.  DOOR65
	new FxDef(
        FX_DOOR65,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{65,3,3},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 210 Opening window. GEOOPN1
	new FxDef(
        FX_WINDOW66,	// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance) *
		new []{							// roomVolList
			new []{66,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 211 Wind rip by the open window. Triggered at the end of effect 210.
	new FxDef(
        FX_WIND66,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{66,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 212 George electrocutes himself on the pantograph. Fool.  GEOSHK64
	new FxDef(
        FX_SHOCK63,		// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{63,12,14},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 213 The train brakes violently. GEOSTP69
	new FxDef(
        FX_BRAKES,		// sampleId
		FX_SPOT,			// type
		13,						// delay (or random chance) *
		new []{							// roomVolList
			new []{69,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 214 The train ticks over. From the end of BRAKE.
	new FxDef(
        FX_TICK69,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{69,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 215 Eklund shoot Khan.  FIGHT69
	new FxDef(
        FX_EKSHOOT,		// sampleId
		FX_SPOT,			// type
		120,					// delay (or random chance) *
		new []{							// roomVolList
			new []{69,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 216 Eklund shoots George. GEODIE69
	new FxDef(
        FX_EKSHOOT,		// sampleId
		FX_SPOT,			// type
		21,						// delay (or random chance) *
		new []{							// roomVolList
			new []{69,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 217 Khan pulls the door open. FIGHT69
	new FxDef(
        FX_DOOR69,		// sampleId
		FX_SPOT,			// type
		42,						// delay (or random chance) *
		new []{							// roomVolList
			new []{69,8,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 218 Wind shriek. Loops from the end of DOOR69 wav to the beginning of BRAKES.
	new FxDef(
        FX_WIND66,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{69,8,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 219 Brakes releasing pressure. Only after BRAKE has been run.
	new FxDef(
        FX_PNEUMO69,	// sampleId
		FX_RANDOM,		// type
		720,					// delay (or random chance) *
		new []{							// roomVolList
			new []{69,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 220 External train sound. Played while George is on the top of the train.
	new FxDef(
        FX_TRAINEXT,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{63,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 221 The passing train. FIGHT69
	new FxDef(
        FX_TRNPASS,		// sampleId
		FX_SPOT,			// type
		102,					// delay (or random chance) *
		new []{							// roomVolList
			new []{69,4,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 222 George descends into sewer. GEODESO6
	new FxDef(
        FX_LADD2,			// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{6,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 223 George ascends into alley. GEOASC06
	new FxDef(
        FX_LADD3,			// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance) *
		new []{							// roomVolList
			new []{6,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 224 George replaces manhole cover. GEOMAN9
	new FxDef(
        FX_COVERON2,	// sampleId
		FX_SPOT,			// type
		19,						// delay (or random chance) *
		new []{							// roomVolList
			new []{2,12,11},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 225 Montfaucon sewer ambience.
	new FxDef(
        FX_AMBIEN37,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,5,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 226 George's winning smile. GEOJMP72.
	new FxDef(
        FX_PING,			// sampleId
		FX_SPOT,			// type
		26,						// delay (or random chance) *
		new []{							// roomVolList
			new []{72,10,14},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 227 George starts to open the manhole. GEO36KNE
	new FxDef(
        FX_MANOP36,		// sampleId
		FX_SPOT,			// type
		19,						// delay (or random chance) *
		new []{							// roomVolList
			new []{36,4,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 228 George opens the manhole. GEO36OPE
	new FxDef(
        FX_PULLUP36,	// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{36,4,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 229 George replaces the manhole cover. GEO36CLO
	new FxDef(
        FX_REPLCE36,	// sampleId
		FX_SPOT,			// type
		20,						// delay (or random chance) *
		new []{							// roomVolList
			new []{36,4,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 230 George knocks at righthand arch. GEO37TA3
	new FxDef(
        FX_KNOCK37,		// sampleId
		FX_SPOT,			// type
		20,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,6,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 231 George knocks at middle or lefthand arch. GEO37TA1 or GEO37TA2.
	new FxDef(
        FX_KNOCK37B,	// sampleId
		FX_SPOT,			// type
		20,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,4,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 232 George winds the chain down  HOO37LBO
	new FxDef(
        FX_CHAIN37,		// sampleId
		FX_SPOT,			// type
		14,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,6,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
	}
    ),
	//------------------------
	// 233 George winds the chain up.  HOO37LBO (In reverse)
	new FxDef(
        FX_CHAIN37B,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,6,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 234 George breaks hole in door. GEO37TA4
	new FxDef(
        FX_HOLE37,		// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,6,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 235 Plaster door collapses. DOR37COL
	new FxDef(
        FX_DOOR37,		// sampleId
		FX_SPOT,			// type
		23,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,8,15},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 236 Barge winch. GEO37TUL (If it runs more than once, trigger the effect on frame one. Incidentally, this is a reversible so the effect must launch on frame one of the .cdr version as well. )
	new FxDef(
        FX_WINCH37,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{37,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 237 George places chess piece. GEOSPA17
	new FxDef(
        FX_CHESS,			// sampleId
		FX_SPOT,			// type
		23,						// delay (or random chance) *
		new []{							// roomVolList
			new []{59,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 238 Piano loop for the upstairs hotel corridor.
	new FxDef(
        FX_PIANO14,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{14,2,2},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 239 Door opens in church tower. PANEL72
	new FxDef(
        FX_SECDOR72,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
        new []{							// roomVolList
			new []{72,8,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 240 George rummages through debris. Tied to the end of the whichever crouch is used. Use either this one or RUMMAGE2 alternatively or randomly. Same kind of schtick as the pick axe noises, I suppose.
	new FxDef(
        FX_RUMMAGE1,	// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{72,8,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 241 George rummages through debris. See above for notes.
	new FxDef(
        FX_RUMMAGE2,	// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{72,8,6},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 242 Gust of wind in the graveyard.
	new FxDef(
        FX_GUST71,		// sampleId
		FX_RANDOM,		// type
		1080,					// delay (or random chance) *
		new []{							// roomVolList
			new []{71,3,3},		// {roomNo,leftVol,rightVol}
			new []{72,2,1},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 243 Violin ambience for Ireland.
	new FxDef(
        FX_VIOLIN19,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{19,3,3},		// {roomNo,leftVol,rightVol}
			new []{21,2,2},
            new []{26,2,2},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 244 Footstep #1 for underground locations. Same schtick as for 188 and 189.
	new FxDef(
        FX_SEWSTEP1,	// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{6,8,8},		// {roomNo,leftVol,rightVol}
			new []{7,8,8},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 245 Footstep #2 for underground locations. Same schtick as for 188 and 189.
	new FxDef(
        FX_SEWSTEP2,	// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{6,16,16},	// {roomNo,leftVol,rightVol}
			new []{7,16,16},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 246 Nico's carabiner as she descends into the museum. NICPUS1
	new FxDef(
        FX_CARABINE,	// sampleId
		FX_SPOT,			// type
		4,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 247 Rosso is shot (with a piece of field artillery).  ROSSHOT
	new FxDef(
        FX_GUN79,			// sampleId
		FX_SPOT,			// type
		2,						// delay (or random chance) *
		new []{							// roomVolList
			new []{79,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 248 George is hit by the thrown stilletto. GEODIE1
	new FxDef(
        FX_DAGGER1,		// sampleId
		FX_SPOT,			// type
		2,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 249 George is hit by the thrown stilletto after walking forward. GEODIE2
	new FxDef(
        FX_DAGGER1,		// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 250 Can hits the well water. The cue is in GAR2SC57.TXT immediately after the line, "over: Lopez threw the can away. It seemed to fall an awfully long way."
	new FxDef(
        FX_CANFALL,		// sampleId
		FX_SPOT,			// type
		4,						// delay (or random chance) *
		new []{							// roomVolList
			new []{57,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 251 Mad, fizzing damp and ancient gunpowder after the application of a torch.
	new FxDef(
        FX_GUNPOWDR,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 252 Maguire whistling. MAGSLK. Plays while Maguire is idling, stops abruptly when he does something else.
	new FxDef(
        FX_WHISTLE,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{19,2,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 253  George is hit by the goat. GEOHITR and GEOHITL.
	new FxDef(
        FX_GEOGOAT,		// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{24,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 254 Manager says, "Hello". MAN2
	new FxDef(
        FX_MANG1,			// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance) *
		new []{							// roomVolList
			new []{49,6,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 255 Manager says, Don't go in there!" MAN3
	new FxDef(
        FX_MANG2,			// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{49,6,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 256 Manager says, "Here are the keys." MAN4
	new FxDef(
        FX_MANG3,			// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance) *
		new []{							// roomVolList
			new []{49,6,5},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 257 George pulls the lion's tooth. GEOSPA26
	new FxDef(
        FX_TOOTHPUL,	// sampleId
		FX_SPOT,			// type
		19,						// delay (or random chance) *
		new []{							// roomVolList
			new []{61,8,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 258 George escapes the lion.  LION1
	new FxDef(
        FX_LIONFALL,	// sampleId
		FX_SPOT,			// type
		7,						// delay (or random chance) *
		new []{							// roomVolList
			new []{61,8,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 259 George gets flattened. LION2
	new FxDef(
        FX_LIONFAL2,	// sampleId
		FX_SPOT,			// type
		4,						// delay (or random chance) *
		new []{							// roomVolList
			new []{61,8,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 260 Rosso dies. ROSSFALL
	new FxDef(
        FX_ROSSODIE,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{74,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 261 Eklund chokes George. FIGHT79
	new FxDef(
        FX_CHOKE1,		// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{79,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 262 Eklund chokes George some more. FIGHT79
	new FxDef(
        FX_CHOKE2,		// sampleId
		FX_SPOT,			// type
		54,						// delay (or random chance) *
		new []{							// roomVolList
			new []{79,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 263 Eklund dies. FIGHT79
	new FxDef(
        FX_FIGHT2,		// sampleId
		FX_SPOT,			// type
		44,					// delay (or random chance) *
		new []{							// roomVolList
			new []{79,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 264 George hears museum break-in. GEOSUR29
	new FxDef(
        FX_DOOR29,		// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance) *
		new []{							// roomVolList
			new []{94,14,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 265 George hits the floor having been shot. GEODED.
	new FxDef(
        FX_GDROP29,		// sampleId
		FX_SPOT,			// type
		27,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,10,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 266 George hits the floor having been stunned. GEOFISH
	new FxDef(
        FX_GDROP29,		// sampleId
		FX_SPOT,			// type
		27,						// delay (or random chance) *
		new []{							// roomVolList
			new []{29,10,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 267 Fitz being knocked down as heard from inside the pub. Triggered from the script, I think. This is just a stopgap until Hackenbacker do the full version for the Smacker, then I'll sample the requisite bit and put it in here.
	new FxDef(
        FX_FITZHIT,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{20,16,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 268 Gendarme shoots lock off. GENSHOT
	new FxDef(
        FX_GUN34,			// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{34,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 269 ECG alarm, Marquet in trouble. Start looping imeediately before George says, "Thanks, Bunny".
	// Incidentally, James, please switch off Mr Shiney permanently when George first gets into Marquet's room. He gets in the way when they're figuring out that Eklund's an imposter.
	new FxDef(
        FX_PULSE2,		// sampleId
		FX_LOOP,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{30,16,16},	// {roomNo,leftVol,rightVol}
			new []{34,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 270 ECG alarm, Marquet dead. Switch off the previous effect and replace with this immediately before the gendarme says, "Stand back, messieurs."
	new FxDef(
        FX_PULSE3,		// sampleId
		FX_LOOP,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{30,16,16},	// {roomNo,leftVol,rightVol}
			new []{34,13,13},
            new []{35,13,13},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 271 Door closing. GEOENT15
	new FxDef(
        FX_DORCLOSE,	// sampleId
		FX_SPOT,			// type
		4,						// delay (or random chance) *
		new []{							// roomVolList
			new []{15,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 272 Cupboard opening. GEOCOT
	new FxDef(
        FX_CUPBOPEN,	// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance) *
		new []{							// roomVolList
			new []{33,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 273 Cupboard closing. GEOCOT
	new FxDef(
        FX_CUPBCLOS,	// sampleId
		FX_SPOT,			// type
		33,						// delay (or random chance) *
		new []{							// roomVolList
			new []{33,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 274 Closing door when George leaves hotel room. GEOLVS15 and GEODOR17 (they're identical).
	new FxDef(
        FX_DORCLOSE,	// sampleId
		FX_SPOT,			// type
		44,						// delay (or random chance) *
		new []{							// roomVolList
			new []{15,12,12},	// {roomNo,leftVol,rightVol}
			new []{17,12,12},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 275 Closing door when George leaves the pub. DOROPN20 (Reversed)
	new FxDef(
        FX_DORCLOSE20,// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{20,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 276 Nico call for a cab.  NICPHN10
	new FxDef(
        FX_PHONICO1,	// sampleId
		FX_SPOT,			// type
		15,						// delay (or random chance) *
		new []{							// roomVolList
			new []{10,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 277 Nico puts down the phone. NICDWN10
	new FxDef(
        FX_FONEDN,		// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance) *
		new []{							// roomVolList
			new []{10,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 278 Painter puts down the phone. PAI41HAN
	new FxDef(
        FX_FONEDN41,	// sampleId
		FX_SPOT,			// type
		5,						// delay (or random chance) *
		new []{							// roomVolList
			new []{41,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 279 Mechanical hum of heating system in the dig lobby.
	new FxDef(
        FX_AIRCON41,	// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{41,6,6},		// {roomNo,leftVol,rightVol}
			new []{43,8,8},
            new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	//------------------------
	// 280 The Sword is Reforged (Grandmaster gets zapped) GMPOWER
	new FxDef(
        FX_REFORGE1,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{78,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 281 The Sword is Reforged (G&N gawp at the spectacle) There's no anim I know of to tie it to unless the flickering blue light is one.
	new FxDef(
        FX_REFORGE2,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{75,12,12},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 282 The Sword is Reforged (We watch over G&N's heads as the Grandmaster gets zapped) GMWRIT74
	new FxDef(
        FX_REFORGE2,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{74,14,14},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 283 The Sword is Reforged (Grandmaster finishes being zapped) GMWRITH
	new FxDef(
        FX_REFORGE4,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{78,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 284 Baphomet Cavern Ambience
	new FxDef(
        FX_BAPHAMB,		// sampleId
		FX_LOOP,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{74,6,8},		// {roomNo,leftVol,rightVol}
			new []{75,7,8},		// {roomNo,leftVol,rightVol}
			new []{76,8,8},		// {roomNo,leftVol,rightVol}
			new []{77,8,8},		// {roomNo,leftVol,rightVol}
			new []{78,8,8},		// {roomNo,leftVol,rightVol}
			new []{79,7,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 285 Duane's Happy-Snappy Camera. XDNEPHO3 and XDNEPHO5.
	new FxDef(
        FX_CAMERA45,	// sampleId
		FX_SPOT,			// type
		30,						// delay (or random chance) *
		new []{							// roomVolList
			new []{45,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 286 Grand Master strikes the floor with his cane. GMENTER
	new FxDef(
        FX_STAFF,			// sampleId
		FX_SPOT,			// type
		28,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 287 George descends ladder in 7: GEOASC07 (Reversed) This used to be handled by effect #46 but it didn't fit at all.
	new FxDef(
        FX_SEWLADD7,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{7,8,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 288 Sam kicks the recalcitrant Mr. Shiny. DOMKIK
	new FxDef(
        FX_KIKSHINY,	// sampleId
		FX_SPOT,			// type
		16,						// delay (or random chance) *
		new []{							// roomVolList
			new []{33,9,9},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 289 Gust of wind outside bombed cafe. LVSFLY
	new FxDef(
        FX_LVSFLY,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{1,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 290 Ron's disgusting No.1 Sneeze. Either this or the next effect (randomly chosen) is used for the following animations, RONSNZ & RONSNZ2
	new FxDef(
        FX_SNEEZE1,		// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{20,10,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 291 Ron's disgusting No.2 Sneeze. Either this or the previous effect (randomly chosen) is used for the following animations, RONSNZ & RONSNZ2
	new FxDef(
        FX_SNEEZE2,		// sampleId
		FX_SPOT,			// type
		11,						// delay (or random chance) *
		new []{							// roomVolList
			new []{20,10,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 292 Dripping tap in the pub cellar. TAPDRP
	new FxDef(
        FX_DRIPIRE,		// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{21,4,4},		// {roomNo,leftVol,rightVol}
			new []{26,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 293 Dripping tap in the pub cellar. TAPDRP
	new FxDef(
        FX_DRIPIRE2,	// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{21,4,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 294 Dripping tap in the excavation toilet. (see WATER43 - but it's looped anyway, not triggered with anim)
	new FxDef(
        FX_TAPDRIP,		// sampleId
		FX_SPOT,			// type
		6,						// delay (or random chance) *
		new []{							// roomVolList
			new []{43,8,8},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 295 George closes the mausoleum window. GEOSPA23
	new FxDef(
        FX_WINDOW59,	// sampleId
		FX_SPOT,			// type
		24,						// delay (or random chance) *
		new []{							// roomVolList
			new []{59,10,8},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 296 George opens the mausoleum window, the feebleminded loon. GEOSPA23 reversed.
	new FxDef(
        FX_WINDOW59,	// sampleId
		FX_SPOT,			// type
		14,						// delay (or random chance) *
		new []{							// roomVolList
			new []{59,10,8},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 297	When George & Nico hear chanting from sc73
	new FxDef(
        FX_CHANT,			// sampleId
		FX_SPOT,			// type
		10,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,2,4},		// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 298	EKFIGHT
	new FxDef(
        FX_FIGHT1,		// sampleId
		FX_SPOT,			// type
		31,						// delay (or random chance) *
		new []{							// roomVolList
			new []{74,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 299 Small van passes, left to right. CARA9 and CARC9
	new FxDef(
        FX_LITEVEHR,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{9,16,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 300 Small van passes, right to left to right. CARB9
	new FxDef(
        FX_LITEVEHL,	// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{9,16,10},	// {roomNo,leftVol,rightVol}
			new [] { 0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 301 Truck passes, left to right. TRUCKA9 and TRUCKB9
	new FxDef(
        FX_HVYVEHR,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{9,14,10},	// {roomNo,leftVol,rightVol}
			new [] { 0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 302 Truck passes, right to left. TRUCKC9
	new FxDef(
        FX_HVYVEHL,		// sampleId
		FX_SPOT,			// type
		1,						// delay (or random chance) *
		new []{							// roomVolList
			new []{9,14,10},	// {roomNo,leftVol,rightVol}
			new [] { 0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 303 With anim FIGHT69
	new FxDef(
        FX_FIGHT69,		// sampleId
		FX_SPOT,			// type
		78,						// delay (or random chance) *
		new []{							// roomVolList
			new []{69,12,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 304 With anim GEODIE1 in sc73
	new FxDef(
        FX_GDROP73,		// sampleId
		FX_SPOT,			// type
		14,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,12,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 305 With anim GEODIE2 in sc73
	new FxDef(
        FX_GDROP73,		// sampleId
		FX_SPOT,			// type
		21,						// delay (or random chance) *
		new []{							// roomVolList
			new []{73,12,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 306 With anim GEODES25
	new FxDef(
        FX_LADDWN25,	// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{25,12,8},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 307 With anim GEOASC25
	new FxDef(
        FX_LADDUP25,	// sampleId
		FX_SPOT,			// type
		8,						// delay (or random chance) *
		new []{							// roomVolList
			new []{25,12,8},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 308 With anim GKSWORD in sc76
	new FxDef(
        FX_GKSWORD,		// sampleId
		FX_SPOT,			// type
		9,						// delay (or random chance) *
		new []{							// roomVolList
			new []{76,10,10},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 309 With anim GEO36KNE in sc36
	new FxDef(
        FX_KEYIN,			// sampleId
		FX_SPOT,			// type
		18,						// delay (or random chance) *
		new []{							// roomVolList
			new []{36,14,14},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 310 With anim GEO36ENT in sc36
	new FxDef(
        FX_COVDWN,		// sampleId
		FX_SPOT,			// type
		85,						// delay (or random chance) *
		new []{							// roomVolList
			new []{36,14,14},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    ),
	//------------------------
	// 311 With anim SECDOR59 in sc59
	new FxDef(
        FX_SECDOR59,	// sampleId
		FX_SPOT,			// type
		0,						// delay (or random chance) *
		new []{							// roomVolList
			new []{59,16,16},	// {roomNo,leftVol,rightVol}
			new []{0,0,0},		// NULL-TERMINATOR
		}
    )
    //------------------------
};
    }
}
