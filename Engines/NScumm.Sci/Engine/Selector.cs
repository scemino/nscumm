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

namespace NScumm.Sci.Engine
{
    internal class SelectorCache
    {
        // Statically defined selectors, (almost the) same in all SCI versions
        public int _info_;    ///< Removed in SCI3
        public int y;
        public int x;
        public int view, loop, cel; ///< Description of a specific image
        public int underBits; ///< Used by the graphics subroutines to store backupped BG pic data
        public int nsTop, nsLeft, nsBottom, nsRight; ///< View boundaries ('now seen')
        public int lsTop, lsLeft, lsBottom, lsRight; ///< Used by Animate() subfunctions and scroll list controls
        public int signal; ///< Used by Animate() to control a view's behavior
        public int illegalBits; ///< Used by CanBeHere
        public int brTop, brLeft, brBottom, brRight; ///< Bounding Rectangle
        // name, key, time
        public int text; ///< Used by controls
        public int elements; ///< Used by SetSynonyms()
        // color, back
        public int mode; ///< Used by text controls (-> DrawControl())
        // style
        public int state, font, type;///< Used by controls
        // window
        public int cursor; ///< Used by EditControl
        public int max; ///< Used by EditControl, removed in SCI3
        public int mark; //< Used by list controls (script internal, is needed by us for the QfG import rooms)
        public int sort; //< Used by list controls (script internal, is needed by us for QfG3 import room)
                              // who
        public int message; ///< Used by GetEvent
        // edit
        public int play; ///< Play function (first function to be called)
        public int number;
        public int handle;    ///< Replaced by nodePtr in SCI1+
        public int nodePtr;   ///< Replaces handle in SCI1+
        public int client; ///< The object that wants to be moved
        public int dx, dy; ///< Deltas
        public int b_movCnt, b_i1, b_i2, b_di, b_xAxis, b_incr; ///< Various Bresenham vars
        public int xStep, yStep; ///< BR adjustments
        public int xLast, yLast; ///< BR last position of client
        public int moveSpeed; ///< Used for DoBresen
        public int canBeHere; ///< Funcselector: Checks for movement validity in SCI0
        public int heading, mover; ///< Used in DoAvoider
        public int doit; ///< Called (!) by the Animate() system call
        public int isBlocked, looper; ///< Used in DoAvoider
        public int priority;
        public int modifiers; ///< Used by GetEvent
        public int replay; ///< Replay function
        // setPri, at, next, done, width
        public int wordFail, syntaxFail; ///< Used by Parse()
        // semanticFail, pragmaFail
        // said
        public int claimed; ///< Used generally by the event mechanism
        // value, save, restore, title, button, icon, draw
        public int delete; ///< Called by Animate() to dispose a view object
        public int z;

        // SCI1+ static selectors
        public int parseLang;
        public int printLang; ///< Used for i18n
        public int subtitleLang;
        public int size;
        public int points; ///< Used by AvoidPath()
        public int palette;   ///< Used by the SCI0-SCI1.1 animate code, unused in SCI2-SCI2.1, removed in SCI3
        public int dataInc;   ///< Used to sync music with animations, removed in SCI3
        // handle (in SCI1)
        public int min; ///< SMPTE time format
        public int sec;
        public int frame;
        public int vol;
        public int pri;
        // perform
        public int moveDone;  ///< used for DoBresen

        // SCI1 selectors which have been moved a bit in SCI1.1, but otherwise static
        public int cantBeHere; ///< Checks for movement avoidance in SCI1+. Replaces canBeHere
        public int topString; ///< SCI1 scroll lists use this instead of lsTop. Removed in SCI3
        public int flags;

        // SCI1+ audio sync related selectors, not static. They're used for lip syncing in
        // CD talkie games
        public int syncCue; ///< Used by DoSync()
        public int syncTime;

        // SCI1.1 specific selectors
        public int scaleSignal; //< Used by kAnimate() for cel scaling (SCI1.1+)
        public int scaleX, scaleY;    ///< SCI1.1 view scaling
        public int maxScale;      ///< SCI1.1 view scaling, limit for cel, when using global scaling
        public int vanishingX;    ///< SCI1.1 view scaling, used by global scaling
        public int vanishingY;    ///< SCI1.1 view scaling, used by global scaling

        // Used for auto detection purposes
        public int overlay;   ///< Used to determine if a game is using old gfx functions or not

        // SCI1.1 Mac icon bar selectors
        public int iconIndex; ///< Used to index icon bar objects
        public int select;

# if ENABLE_SCI32
        public int data; // Used by Array()/String()
        public int picture; // Used to hold the picture ID for SCI32 pictures
        public int bitmap; // Used to hold the text bitmap for SCI32 texts

        public int plane;
        public int top;
        public int left;
        public int bottom;
        public int right;
        public int resX;
        public int resY;

        public int fore;
        public int back;
        public int skip;
        public int dimmed;

        public int fixPriority;
        public int mirrored;
        public int visible;

        public int useInsetRect;
        public int inTop, inLeft, inBottom, inRight;
#endif
    };
}
