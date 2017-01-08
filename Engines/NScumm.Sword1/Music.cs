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
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using System;
using System.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sword1
{
    class MusicHandle : IAudioStream
    {
        // This means fading takes 3 seconds.
        private const int FADE_LENGTH = 3;

        private Stream _file;
        private int _fading;
        private int _fadeSamples;
        private IAudioStream _audioSource;
        private readonly short[] _data;

        public MusicHandle()
        {
            _data = new short[4096];
        }

        void IDisposable.Dispose()
        {
        }

        public virtual int ReadBuffer(Ptr<short> buffer, int numSamples)
        {
            int totalSamples = 0;
            var bufferPos = 0;
            if (_audioSource == null)
                return 0;
            int expectedSamples = numSamples;
            while (expectedSamples > 0 && _audioSource != null)
            {
                // _audioSource becomes NULL if we reach EOF and aren't looping
                var len = Math.Min(_data.Length, expectedSamples);
                int samplesReturned = _audioSource.ReadBuffer(_data, len);
                Array.Copy(_data, 0, buffer.Data, buffer.Offset + bufferPos, samplesReturned);
                bufferPos += samplesReturned;
                totalSamples += samplesReturned;
                expectedSamples -= samplesReturned;
                if ((expectedSamples > 0) && _audioSource.IsEndOfData)
                {
                    Debug(2, "Music reached EOF");
                    Stop();
                }
            }
            // buffer was filled, now do the fading (if necessary)
            int samplePos = 0;
            while ((_fading > 0) && (samplePos < totalSamples))
            {
                // fade down
                --_fading;
                buffer[samplePos] = (short) ((buffer[samplePos] * _fading) / _fadeSamples);
                samplePos++;
                if (_fading == 0)
                {
                    Stop();
                    // clear the rest of the buffer
                    Array.Clear(buffer.Data, buffer.Offset + samplePos, (totalSamples - samplePos));
                    return samplePos;
                }
            }
            while ((_fading < 0) && (samplePos < totalSamples))
            {
                // fade up
                buffer[samplePos] = (short) (-(buffer[samplePos] * --_fading) / _fadeSamples);
                if (_fading <= -_fadeSamples)
                    _fading = 0;
            }
            return totalSamples;
        }

        public bool Play(string filename, bool loop)
        {
            Stop();

            // FIXME: How about using AudioStream::openStreamFile instead of the code below?
            // I.e.:
            //_audioSource = Audio::AudioStream::openStreamFile(fileBase, 0, 0, loop ? 0 : 1);

            if (ServiceLocator.AudioManager == null) return false;

            IRewindableAudioStream stream = ServiceLocator.AudioManager.MakeStream(filename);

            if (stream == null)
                return false;

            _audioSource = new LoopingAudioStream(stream, loop ? 0 : 1);

            FadeUp();
            return true;
        }

        public bool PlayPsx(ushort id, bool loop)
        {
            Stop();

            Stream stream;
            if (_file == null)
            {
                stream = Engine.OpenFileRead("tunes.dat");
                if (stream == null)
                    return false;
                _file = stream;
            }

            stream = Engine.OpenFileRead("tunes.tab");
            if (stream == null)
                return false;

            uint offset;
            int size;
            using (var tableFile = stream)
            {
                var br = new BinaryReader(tableFile);
                tableFile.Seek((id - 1) * 8, SeekOrigin.Begin);
                offset = br.ReadUInt32() * 0x800;
                size = br.ReadInt32();
            }

            // Because of broken tunes.dat/tab in psx demo, also check that tune offset is
            // not over file size
            if ((size != 0) && (size != int.MaxValue) && ((int) (offset + size) <= _file.Length))
            {
                _file.Seek(offset, SeekOrigin.Begin);
                var br = new BinaryReader(_file);
                var ms = new MemoryStream(br.ReadBytes(size));
                _audioSource = new LoopingAudioStream(new XAStream(ms, 11025), loop ? 0 : 1);
                FadeUp();
            }
            else
            {
                _audioSource = null;
                return false;
            }

            return true;
        }

        public void Stop()
        {
            if (_audioSource != null)
            {
                _audioSource.Dispose();
                _audioSource = null;
            }
            if (_file != null)
            {
                _file.Dispose();
                _file = null;
            }
            _fading = 0;
        }

        public void FadeUp()
        {
            if (IsStreaming)
            {
                if (_fading > 0)
                    _fading = -_fading;
                else if (_fading == 0)
                    _fading = -1;
                _fadeSamples = FADE_LENGTH * Rate;
            }
        }

        public void FadeDown()
        {
            if (IsStreaming)
            {
                if (_fading < 0)
                    _fading = -_fading;
                else if (_fading == 0)
                    _fading = FADE_LENGTH * Rate;
                _fadeSamples = FADE_LENGTH * Rate;
            }
        }

        public bool IsStreaming => _audioSource != null && !_audioSource.IsEndOfStream;

        public int Fading => _fading;

        public bool IsEndOfData => !IsStreaming;

        public bool IsEndOfStream => false;

        public bool IsStereo => _audioSource?.IsStereo ?? false;

        public int Rate => _audioSource?.Rate ?? 11025;
    }

    internal class Music : IAudioStream
    {
        private readonly IMixer _mixer;
        private ushort _volumeL;
        private ushort _volumeR;
        private readonly int _sampleRate;
        private readonly object _gate = new object();
        private readonly MusicHandle[] _handles;
        private readonly IRateConverter[] _converter = new IRateConverter[2];
        private readonly SoundHandle _soundHandle;

        public bool IsStereo => true;

        public bool IsEndOfData => false;

        public bool IsEndOfStream => false;

        public int Rate => _sampleRate;

        public Music(IMixer mixer)
        {
            _mixer = mixer;
            _sampleRate = mixer.OutputRate;
            _volumeL = _volumeR = 192;
            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
            _handles = new MusicHandle[2];
            for (int i = 0; i < _handles.Length; i++)
            {
                _handles[i] = new MusicHandle();
            }
        }

        public void Dispose()
        {
            _mixer.StopHandle(_soundHandle);
        }

        public int ReadBuffer(Ptr<short> buffer, int numSamples)
        {
            Mix(buffer, numSamples);
            return numSamples;
        }

        public void StartMusic(int tuneId, int loopFlag)
        {
            if (!string.IsNullOrEmpty(_tuneList[tuneId]))
            {
                int newStream = 0;
                lock (_gate)
                {
                    if (_handles[0].IsStreaming && _handles[1].IsStreaming)
                    {
                        int streamToStop;
                        // Both streams playing - one must be forced to stop.
                        if (_handles[0].Fading == 0 && _handles[1].Fading == 0)
                        {
                            // None of them are fading. Shouldn't happen,
                            // so it doesn't matter which one we pick.
                            streamToStop = 0;
                        }
                        else if (_handles[0].Fading != 0 && _handles[1].Fading == 0)
                        {
                            // Stream 0 is fading, so pick that one.
                            streamToStop = 0;
                        }
                        else if (_handles[0].Fading == 0 && _handles[1].Fading != 0)
                        {
                            // Stream 1 is fading, so pick that one.
                            streamToStop = 1;
                        }
                        else
                        {
                            // Both streams are fading. Pick the one that
                            // is closest to silent.
                            if (Math.Abs(_handles[0].Fading) < Math.Abs(_handles[1].Fading))
                                streamToStop = 0;
                            else
                                streamToStop = 1;
                        }
                        _handles[streamToStop].Stop();
                    }
                    if (_handles[0].IsStreaming)
                    {
                        _handles[0].FadeDown();
                        newStream = 1;
                    }
                    else if (_handles[1].IsStreaming)
                    {
                        _handles[1].FadeDown();
                        newStream = 0;
                    }
                    _converter[newStream] = null;
                }

                /* The handle will load the music file now. It can take a while, so unlock
                   the mutex before, to have the soundthread playing normally.
                   As the corresponding _converter is NULL, the handle will be ignored by the playing thread */
                if (SystemVars.Platform == Core.IO.Platform.PSX)
                {
                    if (_handles[newStream].PlayPsx((ushort) tuneId, loopFlag != 0))
                    {
                        lock (_gate)
                        {
                            _converter[newStream] = RateHelper.MakeRateConverter(_handles[newStream].Rate,
                                _mixer.OutputRate, _handles[newStream].IsStereo, false);
                        }
                    }
                }
                else if (_handles[newStream].Play(_tuneList[tuneId], loopFlag != 0))
                {
                    lock (_gate)
                    {
                        _converter[newStream] = RateHelper.MakeRateConverter(_handles[newStream].Rate, _mixer.OutputRate,
                            _handles[newStream].IsStereo, false);
                    }
                }
                else
                {
                    if (tuneId != 81) // file 81 was apparently removed from BS.
                        Warning($"Can't find music file {_tuneList[tuneId]}");
                }
            }
            else
            {
                lock (_gate)
                {
                    if (_handles[0].IsStreaming)
                        _handles[0].FadeDown();
                    if (_handles[1].IsStreaming)
                        _handles[1].FadeDown();
                }
            }
        }

        public void FadeDown()
        {
            lock (_gate)
            {
                for (int i = 0; i < _handles.Length; i++)
                    if (_handles[i].IsStreaming)
                        _handles[i].FadeDown();
            }
        }

        public void GiveVolume(out byte volL, out byte volR)
        {
            volL = (byte) _volumeL;
            volR = (byte) _volumeR;
        }

        public void SetVolume(byte volL, byte volR)
        {
            _volumeL = volL;
            _volumeR = volR;
        }

        private void Mix(Ptr<short> buf, int len)
        {
            lock (_gate)
            {
                Array.Clear(buf.Data, buf.Offset, buf.Data.Length - buf.Offset);
                for (int i = 0; i < _handles.Length; i++)
                    if (_handles[i].IsStreaming && _converter[i] != null)
                        _converter[i].Flow(_handles[i], buf, len, _volumeL, _volumeR);
            }
        }

        static readonly string[] _tuneList =
        {
            "", // 0	SPARE
            "1m2", // DONE 1	George picks up the newspaper
            "1m3", // DONE 2	In the alley for the first time
            "1m4", // DONE 3	Alleycat surprises George
            "1m6", // DONE 4	George fails to remove manhole cover. Even numbered attempts
            "1m7", // !!!! 5	George fails to remove manhole cover. Odd numbered attempts
            "1m8", // DONE 6	George leaves alley
            "1m9", // DONE	7	George enters cafe for the first time
            "1m10", // DONE 8	Waitress
            "1m11", // DONE 9	Lying doctor

            "1m12", // DONE 10	Truthful George
            "1m13", // DONE 11	Yes, drink brandy
            "1m14", // DONE 12	Yes, he's dead (Maybe 1m14a)
            "1m15", // DONE 13	From, "...clown entered?"
            "1m16", // DONE 14	From, "How did the old man behave?"
            "1m17", // DONE 15	Salah-eh-Din
            "1m18", // DONE 16	From, "Stay here, mademoiselle"
            "1m19", // DONE 17	Leaving the cafe
            "1m20", // DONE 18	Stick-up on Moue's gun
            "1m21", // DONE 19	From, "Stop that, monsieur"

            "1m22", // DONE 20	From, "If you can"
            "1m23", // DONE 21	From, "Yeah,...clown"
            "1m24", // DONE 22	From, he claimed to be a doctor
            "1m25", // DONE 23	First time George meets Nico
            "1m26", // DONE 24	From, "Oh God, him again." (Read notes on list)
            "1m27", // DONE 25	From, "He's inside"
            "1m28", // DONE 26	From, "You speak very good English"
            "1m29", // DONE 27	From, "Why won't you tell me about this clown?"
            "1m28a", // DONE 28	Costumed killers from, "How did Plantard get your name?"
            "1m30", // DONE 29	From, "I really did see the clown" when talking to Moue at cafe doorway

            "1m31", // DONE 30	From, "I found this (paper) in the street" (talking to Moue)
            "1m32", // DONE 31	From, "What's the difference?"
            "1m34", // DONE 32	Roadworker "Did you see a clown?"
            "1m35", // DONE 33	Worker re: explosion, "I guess not"
            "2m1", // DONE 34	From, "What about the waitress?"
            "2m2", // DONE 35	From, "Did you see the old guy with the briefcase?"
            "2m4", // DONE 36	"Would you like to read my newspaper?" (2M3 is at position 144)
            "2m5", // DONE 37	From, "Ah, what's this Saleh-eh-Din?"
            "2m6", // DONE 38	From, "It was a battered old tool box".
            "2m7", // DONE 39	George "borrows" the lifting key

            "2m8", // DONE 40	From 'phone page. Call Nico
            "2m9", // DONE 41	Leaving the workman's tent
            "2m10", // DONE 42	Use lifting keys on manhole
            "2m11", // DONE 43	Into sewer no.1 from George on his knees (Alternative: 2m12)
            "2m12", // DONE 44	Into sewer (alternative to 2m11)
            "2m13", // DONE 45	George bends to pick up the red nose
            "2m14", // DONE 46	Paper tissue, "It was a soggy..."
            "2m15", // DONE 47	Cloth, as George picks it up. (Alternative: 2m16)
            "2m16", // !!!! 48	Alternative cloth music
            "2m17", // DONE 49	George climbs out of sewer.

            "2m18", // DONE 50	From, "The man I chased..."
            "2m19", // DONE 51	Spooky music for sewers.
            "2m20", // DONE 52	"She isn't hurt, is she?"
            "2m21", // DONE 53	Click on material icon
            "2m22", // DONE 54	Spooky music for sewers.
            "2m23", // DONE 55	From, "So you don't want to hear my experiences in the desert?"
            "2m24", // DONE 56	On the material icon with Albert (suit icon instead, because material icon done)
            "2m25", // DONE 57	After "What was on the label?" i.e. the 'phone number.
            "2m26",
            // DONE 58	Leaving yard, after, "I hope you catch that killer soon." Also for the Musee Crune icon on the map (5M7).
            "2m27",
            // DONE 59	As George starts to 'phone Todryk.  (Repeated every time he calls him). Also, when the aeroport is clicked on (5M21).

            "2m28", // DONE 60	Todryk conversation after, "Truth and justice"
            "2m29",
            // DONE 61	'Phoning Nico from the roadworks. Also, 'phoning her from Ireland, ideally looping and fading on finish (6M10).
            "2m30", // DONE 62	First time on Paris map
            "2m31", // DONE 63	Click on Rue d'Alfred Jarry
            "2m32", // DONE 64	From, "Will you tell me my fortune?"
            "2m33", // DONE 65	After "Can you really tell my future?"
            "1m28", // DONE 66	"What about the tall yellow ones?" Copy from 1M28.
            "2m24", // DONE 67	Material Icon. Copy from 2M24
            "2m6", // DONE 68	Exit icon on "See you later". Copy from 2M6.
            "1m25", // DONE 69	On opening the front foor to Nico's. Copy from 1M25. .

            "2m38", // DONE 70	Victim 1: From, "Tell me more about the clown's previous victims."
            "2m39", // DONE 71	Victim 2: After, "What about the clown's second victim?"
            "2m40", // DONE 72	Victim 3: On clown icon for 3rd victim.
            "3m1", // DONE 73	George passes Nico the nose.
            "3m2", // DONE 74	With Nico. From, "I found a piece of material..."
            "3m3", // DONE 75	After George says, "... or clowns?"
            "3m4", // DONE 76	After, "Did you live with your father?"
            "1m28", // DONE 77	After, "Do you have a boyfriend?". Copy from 1M28.
            "2m26", // DONE 78	After, "Good idea" (about going to costumier's). Copy from 2M26.
            "3m7", // DONE 79	On costumier's icon on map.

            "3m8", // DONE 80	Costumier's, after, "Come in, welcome."
            "3m9", // DONE 81	On entering costumier's on later visits
            "3m10", // DONE 82	After, "A description, perhaps."
            "2m13", // DONE 83	Red nose icon at costumier's. Copy 2M13.
            "3m12", // DONE 84	Tissue icon. Also, after Nico's "No, I write it (the magazine) 5M19.
            "3m13", // DONE 85	Photo icon over, "Do you recognize this man?"
            "3m14", // DONE 86	Exit icon, over, "Thanks for your help, buddy."
            "2m9", // DONE 87	Clicking on police station on the map.
            "3m17", // DONE 88	Police station on, "I've tracked down the clown's movements."
            "3m18", // DONE 89	"One moment, m'sieur," as Moue turns.

            "3m19", // DONE 90	G. on Rosso. "If he was trying to impress me..."
            "3m20", // DONE 91	G. thinks, "He looked at me as if I'd farted."
            "3m21", // DONE 92	Over Rosso's, "I've washed my hands of the whole affair."
            "3m22", // DONE 93	Played over, "So long, inspector."
            "3m24", // DONE 94	Conversation with Todrk, "He bought a suit from you, remember?"
            "3m26", // !!!! 95	This piece is a problem. Don't worry about it for present.
            "3m27",
            // DONE 96	George to Nico (in the flat): "Have you found out anymore?" [about the murders? or about the templars? JEL]
            "2m26", // DONE 97	After, "Don't worry, I will." on leaving Nico's.
            "3m29", // DONE 98	Ubu icon on the map.
            "3m30",
            // DONE 99	G and Flap. After, "I love the clowns. Don't you?" AND "after "Not if you see me first" (3M31)

            "3m32", // DONE 100	Source music for Lady Piermont.
            "3m33", // DONE 101	More music for Lady P.
            "2m13", // DONE 102 Red Nose music Copy 2M13
            "4m3", // DONE 103	On photo, "Do you recognize the man in this photograph"
            "4m4", // DONE 104	With Lady P. After, "Hi there, ma'am."
            "4m5", // DONE 105	After, "I think the word you're looking for is...dick"
            "4m6", // DONE 106	After, "English arrogance might do the trick." Also for "More English arrogance" (4M27)
            "4m8", // !!!! 107	As George grabs key.
            "4m9", // DONE 108	Room 21, on "Maybe it wasn't the right room"
            "4m10", // DONE 109	On coming into 21 on subsequent occasions.

            "4m11", // DONE 110 As George steps upto window.
            "4m12", // DONE 111	Alternative times he steps up to the window.
            "4m13", // DONE 112	In Moerlin's room
            "4m14", // DONE 113	Sees "Moerlin" on the Stairs
            "4m15", // DONE 114	George closing wardrobe door aftre Moerlin's gone.
            "4m17", // DONE 115	After, "take your mind off the inquest"
            "4m18", // DONE 116	"It was just as I'd imagined."
            "4m19", // DONE 117	Show photo to Lady P
            "4m20", // DONE 118	Lady P is "shocked" after the name "Khan".
            "4m21", // DONE 119	After, "A bundle of papers, perhaps".

            "4m22", // DONE 120	After, "Plantard's briefcase"
            "4m24", // DONE 121	On fade to black as George leaves the hotel (prior to being searched)
            "4m25", // DONE 122	After, "I break your fingers"
            "4m28", // DONE 123	After clerk says, "Voila, m'sieur. Le manuscript..."
            "4m29", // DONE 124	Onto the window sill after getting the manuscript.
            "4m31", // DONE 125	Searched after he's dumped the manuscript in the alleyway.
            "4m32", // DONE 126	Recovering the manuscript in the alley, "If the manuscript was..."
            "5m1", // DONE 127	The manuscript, just after, "It's worth enough to kill for."
            "5m2", // !SMK 128 The Templars after, "...over 800 years ago."
            "5m3", // DONE 129	After, "Let's take another look at that manuscript"

            "5m4", // DONE 130	On "Knight with a crystal ball" icon
            "5m5", // DONE 131	On Nico's, "Patience"
            "5m6",
            // DONE 132	After "I'm sure it will come in useful" when George leaves. Also, George leaving Nico after, "Keep me informed if you find anything new" (5M20). + "just take care of yourself"
            "5m8", // DONE 133	Entering the museum for the first time on the fade.
            "5m9", // DONE 134	George with guard after, "park their cars." Guard saying "No, no, no"
            "5m10",
            // DONE 135	Incidental looking around the museum music. + fading from map to museum street, when lobineau is in museum
            "5m11",
            // DONE 136	From "In the case was a spindly tripod, blackened with age and pitted with rust...". George answers Tripod ((?)That's what the cue list says). Also 5M15 and 5M16)
            "5m12", // DONE 137	More looking around music.
            "5m13", // DONE 138	Opening the mummy case.
            "5m14", // DONE 139	High above me was a window

            "5m17", // DONE 140	"As I reached toward the display case" (5M18 is in slot 165)
            "5m22", // !SMK 141	From Ireland on the Europe map.
            "5m23", // !!!! 142	IN front of the pub, searching.
            "5m24", // DONE 143	Cheeky Maguire, "Wait 'til I get back"
            "2m3", // DONE 144	Before, "Did anybody at the village work at the dig?" Loop and fade.
            "6m1", // DONE 145	After, "You know something ... not telling me, don't you?"
            "6m2", // DONE 146	On, "Mister, I seen it with my own eyes."
            "6m3",
            // DONE 147	After, "Did you get to see the ghost" + On George's, "As soon as I saw the flickering torches..." in SCR_SC73.txt.
            "6m4", // DONE 148	"the bloody place is haunted", just after G's "rational explanation... the castle"
            "6m5",
            // DONE 149	Pub fiddler 1. Please programme stops between numbers - about 20" and a longer one every four or five minutes.

            "6m6", // DONE 150	Pub fiddler 2.
            "6m7", // DONE 151	Pub fiddler 3.
            "6m8", // DONE 152	Pub fiddler 4.
            "6m12", // DONE 153	Exit pub (as door opens). Copy from 2M6.
            "2m6", // DONE 154	Going to the castle, start on the path blackout.
            "5m1", // DONE 155	On, "Where was the Templar preceptory?" Copy 5M1
            "6m15", // DONE 156	"On, "Do you mind if I climb up your hay stack..."
            "7m1", // DONE 157	On plastic box, "It was a featureless..."
            "7m2", // DONE 158	On tapping the plastic box with the sewer key
            "7m4", // !!!! 159	"Shame on you, Patrick!" Fitzgerald was at the dig

            "7m5", // !!!! 160	On the icon that leads to, "Maguire says that he saw you at the dig"
            "7m6", // !!!! 161	On "The man from Paris"
            "7m7", // !!!! 162	On, "I wish I'd never heard of..."
            "7m8", // DONE 163	Exit pub
            "7m11", // DONE 164	George picks up gem.
            "7m14", // DONE 165	On George's icon, "the driver of the Ferrari..."
            "7m15", // DONE 166	After George, "His name is Sean Fitzgerald"
            "5m18", // DONE 167	Leaving museum after discovering tripod.
            "6m11",
            // !!!! 168	With Fitz. On G's, "Did you work at...?". This is triggered here and on each subsequent question, fading at the end.
            "7m17", // DONE 169	"You don't have to demolish the haystack"

            "7m18", // DONE 170	George begins to climb the haystack.
            "7m19",
            // DONE 171	Alternative climbing haystack music. These two tracks can be rotated with an ascent with FX only).
            "7m20", // DONE 172	Attempting to get over the wall.
            "7m21", // DONE 173	Descending the haystack
            "7m22", // !!!! 174	Useful general purpose walking about music.
            "7m23", // DONE 175	"Plastic cover" The exposed box, LB and RB.
            "7m28", // !!!! 176	"No return"
            "7m30",
            // !!!! 177	Picking up drink music (This will definitely clash with the fiddle music. We'll use it for something else). *
            "7m31", // !!!! 178	Showing the landlord the electrician's ID.
            "7m32", // !!!! 179	Stealing the wire (Probable clash again) *

            "7m33", // DONE 180	On fade to black before going down into dark cellar.
            "7m34", // DONE 181	On opening the grate, as George says, "I lifted the..." Khan's entrance.
            "8m1", // DONE 182	Going down into the light cellar, starting as he goes through bar door.
            "8m2", // DONE 183	General cellar music on, "It was an empty carton".
            "8m4", // !!!! 184	Trying to get the bar towel. On, "The man's arm lay across..." *
            "8m7", // DONE 185	Squeeze towel into drain. On, "Silly boy..."
            "8m10", // DONE 186	Entering the castle as he places his foot on the tool embedded into the wall.
            "8m11", // DONE 187	On, "Hey, billy." Goat confrontation music.
            "8m12", // DONE 188	First butt from goat at moment of impact.
            "8m13", // DONE 189	On examining the plough.

            "8m14", // DONE 190	Second butt from goat.
            "8m15", // DONE 191	Third butt from goat.
            "8m16", // DONE 192	All subsequent butts, alternating with no music.
            "8m18",
            // DONE 193	Poking around in the excavation music. I'd trigger it as he starts to descend the ladder into the dig.
            "8m19", // DONE 194	"There was a pattern..." The five holes.
            "8m20", // DONE 195	George actually touches the stone. Cooling carving (?)
            "8m21", // DONE 196	"As I swung the stone upright" coming in on "Upright"
            "8m22", // DONE 197	"The sack contained"
            "8m24", // DONE 198	Down wall. As screen goes black. George over wall to haystack.
            "8m26", // DONE 199	Wetting the towel

            "8m28",
            // DONE 200	Wetting plaster. As George reaches for the towel prior to wringing water onto the plaster.
            "8m29", // DONE 201	Mould in "The hardened plaster..."
            "8m30", // DONE 202	Entering castle. As George steps in.
            "8m31", // DONE 203	After George, "Hardly - he was dead." in nico_scr.txt
            "8m37", // !!!! 204	Talking to Lobineau about the Templars. 5M2Keep looping and fade at the end.
            //					The problem is that it's an enormous sample and will have to be reduced in volume.
            //					I suggest forgetting about this one for the time being.
            //					If there's room when the rest of the game's in, then I'll re-record it more quietly and call it 8M37, okay?
            "8m38", // DONE 205	"A female friend"
            "8m39", // DONE 206	"Public toilet"
            "8m40", // DONE 207	When George asks, "Where was the site at Montfaucon?" (to Lobineau, I suppose)
            "8m41", // DONE 208	On matchbox icon. "Does this matchbook mean anything to you?"
            "9m1", // DONE 209	On George, "It was the king of France" in ross_scr.txt

            "9m2", // DONE 210	George, "Why do you get wound up...?" in ross_scr.txt
            "9m3", // DONE 211	Ever heard of a guy called Marquet? Jacques Marquet?
            "9m5", // DONE 212	On fade up at the hospital drive
            "9m6", // DONE 213	On fade up inside the hospital
            "9m7", // DONE 214	With Eva talking about Marquet. Before, "I'm conducting a private investigation."
            "9m8", // DONE 215	With Eva, showing her the ID card.
            "9m9", // DONE 216	With Eva, second NURSE click, "If nurse Grendel is that bad..."
            "9m10",
            // DONE 217	Saying goodbye to Eva on the conversation where he discovers Marquet's location + on fade up on Sam's screen after being kicked off the ward
            "9m11", // DONE 218	Talking to Sam. On, "Oh - hiya!" + first click on MR_SHINY
            "9m13", // DONE 219	When George drinks from the cooler.

            "9m14", // DONE 220	To Grendel, third MARQUET click. On "Do you know who paid for Marquet's room?"
            "9m15", // DONE 221	To Grendel on first CLOWN click, "Do you have any clowns on the ward?"
            "9m17", // DONE 222	When George pulls Shiny's plug the first time, on "As I tugged the plug..."
            "9m18", // DONE 223	On subsequent plug tuggings if George has failed to get the white coat.
            "9m19", // DONE 224	With the consultant, on "Excuse me, sir..."
            "9m20",
            // DONE 225	Talking to Grendel. Launch immediately after she gives him the long metal box and "a stunning smile"
            "9m21",
            // DONE 226	On Eric's, "Doctor!" when George is trying to get by for the first time, i.e. ward_stop_flag==0.
            "9m22",
            // DONE 227	On Eric's, "Oh, Doctor!" when George is trying to get by for the second time, i.e. ward_stop_flag==1.
            "9m23",
            // DONE 228	On Eric's, "You haven't finished taking my blood pressure!" when George is trying to get by for the third+ time, i.e. ward_stop_flag>1.
            "9m24", // DONE 229	Giving the pressure gauge to Benoir, on, "Here, take this pressure gauge."

            "9m25", // DONE 230	With Benoir, suggesting he use the gauge on the nurse. On, "Use it on Nurse Grendel."
            "10m1", // DONE 231	Immediately after Marquet's, "Well, what are you waiting for? Get it over with!"
            "10m2",
            // DONE 232	When George pulls open the sarcophagus lid prior to his successful hiding before the raid.
            "10m3", // DONE 233	On fade to black as George spies on the Neo-Templars.
            "10m4", // DONE 234	On second peer through the hole at the "Templars"
            "11m1", // DONE 235	On clicking on the Marib button.
            "11m3", // DONE 236	Loop in the Club Alamut, alternating with...
            "11m4", // DONE 237	Loop in the Club Alamut.
            "11m7", // DONE 238	When the door in the Bull's Mouth closes on George.
            "11m8", // DONE 239	When the door opens to reveal Khan, immediately after, "You!" in KHAN_55.TXT.

            "11m9", // !SMK 240	Over the "Going to the Bull's Head" smacker. Probably.
            "12m1", // DONE 241	Clicking on the Spain icon from the aeroport. (AFTER CHANGING CD!)
            "11m2", // DONE 242	Loop in the marketplace of Marib.
            "spm2", // DONE 243	On fade up in the Countess' room for the first time.
            "spm3",
            // DONE 244	At the end of VAS1SC56, triggered immediately before the Countess says, "Senor Stobbart, if I find that you are wasting my time..."
            "spm4", // DONE 245	Immediately before Lopez enters the mausoleum with the chess set.
            "spm5",
            // DONE 246	(This is actually 5m2 copied for CD2) Played through the chess puzzle. Ideally, when it finsishes, give it a couple of seconds and then launch 12m1. When that finishes,  a couple of seconds and then back to this and so on and so forth.
            "spm6", // DONE 247	On fade up from completing the chess puzzle. The climax is now "spm6b"
            "scm1", // DONE 248	This is used whenever George goes out of a carriage and onto the corridor.
            "scm2", // DONE 249	As George climbs out of the train window.

            "scm3", // DONE 250	As George lands inside the guard's van.
            "scm4", // DONE 251	On Khan's death speech, "A noble foe..."
            "scm5", // DONE 252	George to Khan. On, "You're talking in riddles!"
            "scm6", // DONE 253	Before, "He's dead"
            "scm7", // DONE 254	Kissing Nico. After, "Where do you think you're going?"
            "scm8", // DONE 255	In the churchyard after Nico's, "I rather hope it did"
            "scm11", // DONE 256	Click on the opened secret door.
            "rm3a", // DONE 257	Immediately they fade up in the great cave.
            "rm3b", // DONE 258	The scene change immediately after Eklund says, "If you wish to live much longer..."
            "scm16",
            // DONE 259	The big end sequence from when the torch hits the gunpowder. Cross fade with the shortened version that fits on the Smacker.

            "scm1b", // DONE 260	When George passes the trigger point toward the back of the train and he sees Guido.
            "spm6b", // DONE 261 The climax of "spm6", which should coincide with the Countess holding up the chalice.
            "marquet", // DONE 262 Starts at the fade down when George is asked to leave Jacques' room
            "rm4",
            // DONE 263 "On crystal stand icon. As George walks to the center of the cavern." I'd do this on the first LMB on the stump.
            "rm5",
            // DONE 264 "On icon. As George places the crystal on top of the stand." When the player places the gem luggage on the emplaced tripod.
            "rm6",
            // DONE 265 "Chalice reflection. On icon as George places Chalice on floor of Church" i.e. the mosaic in the Baphomet dig. It's over thirty seconds long so it had best start running when the chalice luggage is placed on the mosaic so it runs through the big screen of the reflection.
            "rm7",
            // DONE 266 "Burning candle. On icon as George sets about lighting candle." One minute forty-eight, this one. I've no idea how long the burning candle Smacker is but the cue description seems to indicate it should run from the moment the burning tissue successfully lights the candle, i.e. the window is shut.
            "rm8",
            // DONE 267 "Down well. George descends into circus trap well." I think the circus reference refers to the lion. Run it from the moment that George gets off the rope and has a look around. Run it once only.

            "rm3c",
            // DONE 268 On the scene change to the Grand Master standing between the pillars as the earth power whomps him.
            "rm3d",
            // DONE 269 ONe the scene change after the Grand Master says, "George, we have watched you..." This one might need a bit of fiddling to get it to match to the fisticuffs.
        };
    }
}