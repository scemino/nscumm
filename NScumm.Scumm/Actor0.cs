//
//  Actor0.cs
//
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
using NScumm.Core.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    class Actor0: Actor2
    {
        public Point CurrentWalkTo  { get; set; }

        public Point NewWalkTo { get; set; }

        public byte CostCommandNew { get; set; }

        public byte CostCommand { get; set; }

        public ActorV0MiscFlags MiscFlags { get; set; }

        public byte Speaking { get; set; }

        byte _walkCountModulo;

        public bool NewWalkBoxEntered { get; set; }

        byte _walkDirX;
        byte _walkDirY;

        byte _walkYCountGreaterThanXCount;
        byte _walkXCount;
        byte _walkXCountInc;
        byte _walkYCount;
        byte _walkYCountInc;

        byte _walkMaxXYCountInc;

        Point _tmp_Pos;
        Point _tmp_Dest;
        byte _tmp_WalkBox;
        bool _tmp_NewWalkBoxEntered;

        public sbyte AnimFrameRepeat { get; set; }

        public sbyte[] LimbFrameRepeatNew { get; private set; }

        public sbyte[] LimbFrameRepeat { get; private set; }

        public bool[] LimbFlipped { get; private set; }

        public Actor0(ScummEngine scumm, byte id)
            : base(scumm, id)
        {
            LimbFrameRepeatNew = new sbyte[8];
            LimbFrameRepeat = new sbyte[8];
            LimbFlipped = new bool[8];
        }

        public override void Init(int mode)
        {
            base.Init(mode);

            if (Number != 0)
            {
                switch (_scumm.Game.Culture.TwoLetterISOLanguageName)
                {
                    case "de":
                        Name = System.Text.Encoding.UTF8.GetBytes(v0ActorNames_German[Number - 1]);
                        break;
                    default:
                        Name = System.Text.Encoding.UTF8.GetBytes(v0ActorNames_English[Number - 1]);
                        break;
                }
            }

            CostCommandNew = 0xFF;
            CostCommand = 0xFF;
            MiscFlags = 0;
            Speaking = 0;

            _walkCountModulo = 0;
            NewWalkBoxEntered = false;
            _walkDirX = 0;
            _walkDirY = 0;
            _walkYCountGreaterThanXCount = 0;
            _walkXCount = 0;
            _walkXCountInc = 0;
            _walkYCount = 0;
            _walkYCountInc = 0;
            _walkMaxXYCountInc = 0;

            _tmp_WalkBox = 0;
            _tmp_NewWalkBoxEntered = false;

            AnimFrameRepeat = 0;
            for (int i = 0; i < 8; ++i)
            {
                LimbFrameRepeatNew[i] = 0;
                LimbFrameRepeat[i] = 0;
                LimbFlipped[i] = false;
            }

            if (_scumm.Game.Features.HasFlag(GameFeatures.Demo))
            {
                Sound = v0ActorDemoTalk[Number];
            }
            else
            {
                Sound = v0ActorTalk[Number];
            }
        }

        public override void SaveOrLoad(Serializer serializer)
        {
            base.SaveOrLoad(serializer);

            var actorEntries = new[]
            {
                LoadAndSaveEntry.Create(reader => CostCommand = reader.ReadByte(), writer => writer.WriteByte(CostCommand), 84),
                LoadAndSaveEntry.Create(reader => MiscFlags = (ActorV0MiscFlags)reader.ReadByte(), writer => writer.WriteByte((byte)MiscFlags), 84),
                LoadAndSaveEntry.Create(reader => Speaking = reader.ReadByte(), writer => writer.WriteByte(Speaking), 84),
                LoadAndSaveEntry.Create(reader => AnimFrameRepeat = reader.ReadSByte(), writer => writer.WriteByte(AnimFrameRepeat), 89),
                LoadAndSaveEntry.Create(reader => LimbFrameRepeatNew = reader.ReadSBytes(8), writer => writer.WriteSBytes(LimbFrameRepeatNew, 8), 89),
                LoadAndSaveEntry.Create(reader => LimbFrameRepeat = reader.ReadSBytes(8), writer => writer.WriteSBytes(LimbFrameRepeat, 8), 90),
                LoadAndSaveEntry.Create(reader =>
                    {
                        CurrentWalkTo = new Point(reader.ReadInt16(), reader.ReadInt16());
                    }, writer =>
                    {
                        writer.WriteInt16(CurrentWalkTo.X);
                        writer.WriteInt16(CurrentWalkTo.Y);
                    }, 97),
                LoadAndSaveEntry.Create(reader =>
                    {
                        NewWalkTo = new Point(reader.ReadInt16(), reader.ReadInt16());
                    }, writer =>
                    {
                        writer.WriteInt16(NewWalkTo.X);
                        writer.WriteInt16(NewWalkTo.Y);
                    }, 97),
                LoadAndSaveEntry.Create(reader => _walkCountModulo = reader.ReadByte(), writer => writer.WriteByte(_walkCountModulo), 97),
                LoadAndSaveEntry.Create(reader => _walkDirX = reader.ReadByte(), writer => writer.WriteByte(_walkDirX), 97),
                LoadAndSaveEntry.Create(reader => _walkDirY = reader.ReadByte(), writer => writer.WriteByte(_walkDirY), 97),
                LoadAndSaveEntry.Create(reader => _walkYCountGreaterThanXCount = reader.ReadByte(), writer => writer.WriteByte(_walkYCountGreaterThanXCount), 97),
                LoadAndSaveEntry.Create(reader => _walkXCount = reader.ReadByte(), writer => writer.WriteByte(_walkXCount), 97),
                LoadAndSaveEntry.Create(reader => _walkXCountInc = reader.ReadByte(), writer => writer.WriteByte(_walkXCountInc), 97),
                LoadAndSaveEntry.Create(reader => _walkYCount = reader.ReadByte(), writer => writer.WriteByte(_walkYCount), 97),
                LoadAndSaveEntry.Create(reader => _walkYCountInc = reader.ReadByte(), writer => writer.WriteByte(_walkYCountInc), 97),
                LoadAndSaveEntry.Create(reader => _walkMaxXYCountInc = reader.ReadByte(), writer => writer.WriteByte(_walkMaxXYCountInc), 97)
            };

            actorEntries.ForEach(e => e.Execute(serializer));
        }

        void AnimateActor(int anim)
        {
            int dir = -1;

            switch (anim)
            {
                case 0x00:
                case 0x04:
                    dir = 0;
                    break;

                case 0x01:
                case 0x05:
                    dir = 1;
                    break;

                case 0x02:
                case 0x06:
                    dir = 2;
                    break;

                case 0x03:
                case 0x07:
                    dir = 3;
                    break;
            }

            if (IsInCurrentRoom)
            {
                CostCommandNew = (byte)anim;
                _scumm.CostumeLoader.CostumeDecodeData(this, 0, 0);

                if (dir == -1)
                    return;

                Facing = (ushort)ScummMath.NormalizeAngle(ScummHelper.OldDirToNewDir(dir));
            }
            else
            {
                if (anim > 4 && anim <= 7)
                    Facing = (ushort)ScummMath.NormalizeAngle(ScummHelper.OldDirToNewDir(dir));
            }
        }

        public override void AnimateCostume()
        {
            SpeakCheck();

            if (_scumm.CostumeLoader.IncreaseAnims(this) != 0)
                NeedRedraw = true;
        }

        void SpeakCheck()
        {
            if ((Sound & 0x80) != 0)
                return;

            int cmd = ScummHelper.NewDirToOldDir(Facing);

            if ((Speaking & 0x80) != 0)
                cmd += 0x0C;
            else
                cmd += 0x10;

            AnimFrameRepeat = -1;
            AnimateActor(cmd);
        }

        public void LimbFrameCheck(int limb)
        {
            if (Cost.Frame[limb] == 0xFFFF)
                return;

            if (Cost.Start[limb] == Cost.Frame[limb])
                return;

            // 0x25A4
            Cost.Start[limb] = Cost.Frame[limb];

            LimbFrameRepeat[limb] = LimbFrameRepeatNew[limb];

            // 0x25C3
            Cost.Active[limb] = ((CostumeLoader0)_scumm.CostumeLoader).GetFrame(this, limb);
            Cost.Curpos[limb] = 0;

            NeedRedraw = true;
        }

        public override void SetDirection(int direction)
        {
            int dir = ScummHelper.NewDirToOldDir(direction);
            int res = 0;

            switch (dir)
            {
                case 0:
                    res = 4;    // Left
                    break;

                case 1:
                    res = 5;    // Right
                    break;

                case 2:
                    res = 6;    // Face Camera
                    break;

                default:
                    res = 7;    // Face Away
                    break;
            }

            AnimFrameRepeat = -1;
            AnimateActor(res);
        }

        public override void StartAnimActor(int frame)
        {
            if (frame == TalkStartFrame)
            {
                if ((Sound & 0x40) != 0)
                    return;

                Speaking = 1;
                return;
            }

            if (frame == TalkStopFrame)
            {
                Speaking = 0;
                return;
            }

            if (frame == StandFrame)
                SetDirection(Facing);
        }

        bool CalcWalkDistances()
        {
            _walkDirX = 0;
            _walkDirY = 0;
            _walkYCountGreaterThanXCount = 0;
            ushort A = 0;

            if (CurrentWalkTo.X >= _tmp_Dest.X)
            {
                A = (ushort)(CurrentWalkTo.X - _tmp_Dest.X);
                _walkDirX = 1;
            }
            else
            {
                A = (ushort)(_tmp_Dest.X - CurrentWalkTo.X);
            }

            _walkXCountInc = (byte)A;

            if (CurrentWalkTo.Y >= _tmp_Dest.Y)
            {
                A = (ushort)(CurrentWalkTo.Y - _tmp_Dest.Y);
                _walkDirY = 1;
            }
            else
            {
                A = (ushort)(_tmp_Dest.Y - CurrentWalkTo.Y);
            }

            _walkYCountInc = (byte)A;
            if (_walkXCountInc == 0 && _walkYCountInc == 0)
                return true;

            if (_walkXCountInc <= _walkYCountInc)
                _walkYCountGreaterThanXCount = 1;

            // 2FCC
            A = _walkXCountInc;
            if (A <= _walkYCountInc)
                A = _walkYCountInc;

            _walkMaxXYCountInc = (byte)A;
            _walkXCount = _walkXCountInc;
            _walkYCount = _walkYCountInc;
            _walkCountModulo = _walkMaxXYCountInc;

            return false;
        }

        enum WalkCommand
        {
            None,
            L2A33,
            _2A0A,
            _2A9A,
            L2C36,
            L2CA3
        }

        public override void Walk()
        {
            var cmd = WalkCommand.None;
            ActorSetWalkTo();

            NeedRedraw = true;
            do
            {
                switch (cmd)
                {
                    case WalkCommand.None:
                        {
                            if (NewWalkTo != CurrentWalkTo)
                            {
                                CurrentWalkTo = NewWalkTo;
                                cmd = WalkCommand.L2A33;
                            }
                            else
                            {
                                cmd = WalkCommand._2A0A;
                            }
                        }
                        break;
                    case WalkCommand._2A0A:
                        {
                            if ((Moving & (MoveFlags)0x7F) != MoveFlags.NewLeg)
                            {
                                if (NewWalkTo == RealPosition)
                                    return;
                            }
                            cmd = WalkCommand._2A9A;
                        }
                        break;
                    case WalkCommand.L2A33:
                        {
                            Moving &= (MoveFlags)0xF0;
                            _tmp_Dest = RealPosition;

                            var tmp = CalcWalkDistances() ? 1 : 0;
                            Moving &= (MoveFlags)0xF0;
                            Moving |= (MoveFlags)tmp;

                            if (_walkYCountGreaterThanXCount == 0)
                            {
                                if (_walkDirX != 0)
                                {
                                    _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * 1, Actor2.V12_Y_MULTIPLIER * 0, false);
                                }
                                else
                                {
                                    _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * -1, Actor2.V12_Y_MULTIPLIER * 0, false);
                                }
                            }
                            else
                            {
                                if (_walkDirY != 0)
                                {
                                    _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * 0, Actor2.V12_Y_MULTIPLIER * 1, false);
                                }
                                else
                                {
                                    _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * 0, Actor2.V12_Y_MULTIPLIER * -1, false);
                                }
                            }

                            DirectionUpdate();

                            if ((Moving & MoveFlags.Frozen) != 0)
                                return;

                            AnimateActor(ScummHelper.NewDirToOldDir(Facing));

                            cmd = WalkCommand._2A9A;
                        }
                        break;

                    case WalkCommand._2A9A:
                        {
                            if (Moving == MoveFlags.InLeg)
                                return;

                            if ((Moving & (MoveFlags)0x0F) == MoveFlags.NewLeg)
                            {
                                StopActorMoving();
                                return;
                            }

                            // 2AAD
                            if (Moving.HasFlag(MoveFlags.Frozen))
                            {
                                DirectionUpdate();

                                if (Moving.HasFlag(MoveFlags.Frozen))
                                    return;

                                AnimateActor(ScummHelper.NewDirToOldDir(Facing));
                            }

                            if ((Moving & (MoveFlags)0x0F) == (MoveFlags)3)
                            {
                                cmd = WalkCommand.L2C36;
                                break;
                            }

                            // 2ADA
                            if ((Moving & (MoveFlags)0x0F) == MoveFlags.Turn)
                            {
                                cmd = WalkCommand.L2CA3;
                                break;
                            }

                            if ((Moving & (MoveFlags)0x0F) == 0)
                            {
                                // 2AE8
                                byte A = ActorWalkX();

                                if (A == 1)
                                {
                                    A = ActorWalkY();
                                    if (A == 1)
                                    {
                                        Moving &= (MoveFlags)0xF0;
                                        Moving |= (MoveFlags)A;
                                    }
                                    else
                                    {
                                        if (A == 4)
                                            StopActorMoving();
                                    }

                                    return;
                                }
                                else
                                {
                                    // 2B0C
                                    if (A == 3)
                                    {
                                        Moving &= (MoveFlags)0xF0;
                                        Moving |= (MoveFlags)A;

                                        if (_walkDirY != 0)
                                        {
                                            _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * 0, Actor2.V12_Y_MULTIPLIER * 1, false);
                                        }
                                        else
                                        {
                                            _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * 0, Actor2.V12_Y_MULTIPLIER * -1, false);
                                        }

                                        DirectionUpdate();
                                        AnimateActor(ScummHelper.NewDirToOldDir(Facing));
                                        cmd = WalkCommand.L2C36;
                                        break;
                                    }
                                    else
                                    {
                                        // 2B39
                                        A = ActorWalkY();
                                        if (A != 4)
                                            return;

                                        Moving &= (MoveFlags)0xF0;
                                        Moving |= (MoveFlags)A;

                                        if (_walkDirX != 0)
                                        {
                                            _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * 1, Actor2.V12_Y_MULTIPLIER * 0, false);
                                        }
                                        else
                                        {
                                            _targetFacing = (ushort)ScummMath.GetAngleFromPos(Actor2.V12_X_MULTIPLIER * -1, Actor2.V12_Y_MULTIPLIER * 0, false);
                                        }

                                        DirectionUpdate();
                                        AnimateActor(ScummHelper.NewDirToOldDir(Facing));
                                        cmd = WalkCommand.L2CA3;
                                    }
                                }
                            }
                        }
                        break;
                    case WalkCommand.L2C36:
                        {
                            SetTmpFromActor();
                            if (_walkDirX == 0)
                            {
                                RealPosition = new Point(RealPosition.X - 1, RealPosition.Y);
                            }
                            else
                            {
                                RealPosition = new Point(RealPosition.X + 1, RealPosition.Y);
                            }

                            // 2C51
                            if (UpdateWalkbox() != InvalidBox)
                            {
                                SetActorFromTmp();
                                cmd = WalkCommand.L2A33;
                                break;
                            }

                            SetActorFromTmp();

                            if (CurrentWalkTo.Y == _tmp_Dest.Y)
                            {
                                StopActorMoving();
                                return;
                            }

                            if (_walkDirY == 0)
                            {
                                _tmp_Dest = new Point(_tmp_Dest.X, _tmp_Dest.Y - 1);
                            }
                            else
                            {
                                _tmp_Dest = new Point(_tmp_Dest.X, _tmp_Dest.Y + 1);
                            }

                            SetTmpFromActor();

                            byte A = (byte)UpdateWalkbox();
                            if (A == 0xFF)
                            {
                                SetActorFromTmp();
                                StopActorMoving();
                                return;
                            }
                            // 2C98: Yes, an exact copy of what just occured.. the original does this, so im doing it...
                            //       Just to keep me sane when going over it :)
                            if (A == 0xFF)
                            {
                                SetActorFromTmp();
                                StopActorMoving();
                                return;
                            }
                        }
                        return;
                    case WalkCommand.L2CA3:
                        SetTmpFromActor();
                        if (_walkDirY == 0)
                        {
                            RealPosition = new Point(RealPosition.X, RealPosition.Y - 1);
                        }
                        else
                        {
                            RealPosition = new Point(RealPosition.X, RealPosition.Y + 1);
                        }

                        if (UpdateWalkbox() == InvalidBox)
                        {
                            // 2CC7
                            SetActorFromTmp();
                            if (CurrentWalkTo.X == _tmp_Dest.X)
                            {
                                StopActorMoving();
                                return;
                            }

                            if (_walkDirX == 0)
                            {
                                _tmp_Dest = new Point(_tmp_Dest.X - 1, _tmp_Dest.Y);
                            }
                            else
                            {
                                _tmp_Dest = new Point(_tmp_Dest.X + 1, _tmp_Dest.Y);
                            }
                            SetTmpFromActor();

                            if (UpdateWalkbox() == InvalidBox)
                            {
                                SetActorFromTmp();
                                StopActorMoving();
                            }
                            return;
                        }
                        else
                        {
                            SetActorFromTmp();
                            cmd = WalkCommand.L2A33;
                        }
                        break;
                }

            } while(cmd != WalkCommand.None);
        }

        byte ActorWalkX()
        {
            byte A = _walkXCount;
            A += _walkXCountInc;
            if (A >= _walkCountModulo)
            {
                if (_walkDirX == 0)
                {
                    _tmp_Dest = new Point(_tmp_Dest.X - 1, _tmp_Dest.Y);
                }
                else
                {
                    _tmp_Dest = new Point(_tmp_Dest.X + 1, _tmp_Dest.Y);
                }

                A -= _walkCountModulo;
            }
            // 2EAC
            _walkXCount = A;
            SetTmpFromActor();
            if (UpdateWalkbox() == InvalidBox)
            {
                // 2EB9
                SetActorFromTmp();

                return 3;
            } 
            // 2EBF
            if (_tmp_Dest.X == CurrentWalkTo.X)
                return 1;

            return 0;
        }

        byte ActorWalkY()
        {
            byte A = _walkYCount;
            A += _walkYCountInc;
            if (A >= _walkCountModulo)
            {
                if (_walkDirY == 0)
                {
                    _tmp_Dest = new Point(_tmp_Dest.X, _tmp_Dest.Y - 1);
                }
                else
                {
                    _tmp_Dest = new Point(_tmp_Dest.X, _tmp_Dest.Y + 1);
                }

                A -= _walkCountModulo;
            }
            // 2EEB
            _walkYCount = A;
            SetTmpFromActor();
            if (UpdateWalkbox() == InvalidBox)
            {
                // 2EF8
                SetActorFromTmp();
                return 4;
            } 
            // 2EFE
            if (_walkYCountInc != 0)
            {
                if (_walkYCountInc == 0xFF)
                {
                    SetActorFromTmp();
                    return 4;
                }
            }
            // 2F0D
            if (CurrentWalkTo.Y == _tmp_Dest.Y)
                return 1;

            return 0;
        }

        int UpdateWalkbox()
        {
            if (_scumm.CheckXYInBoxBounds(Walkbox, RealPosition))
                return 0;

            int numBoxes = _scumm.GetNumBoxes() - 1;
            for (var i = 0; i <= numBoxes; i++)
            {
                if (_scumm.CheckXYInBoxBounds(i, RealPosition))
                {
                    if (_walkdata.CurBox == i)
                    {
                        SetBox((byte)i);
                        DirectionUpdate();

                        NewWalkBoxEntered = true;
                        return i;
                    }
                }
            }

            return InvalidBox;
        }

        void DirectionUpdate()
        {

            int nextFacing = UpdateActorDirection(true);
            if (Facing != nextFacing)
            {
                // 2A89
                SetDirection(nextFacing);

                // Still need to turn?
                if (Facing != _targetFacing)
                {
                    Moving |= MoveFlags.Frozen;
                    return;
                }
            }

            Moving &= ~MoveFlags.Frozen;
        }

        void SetTmpFromActor()
        {
            _tmp_Pos = RealPosition;
            RealPosition = _tmp_Dest;
            _tmp_WalkBox = Walkbox;
            _tmp_NewWalkBoxEntered = NewWalkBoxEntered;
        }

        void SetActorFromTmp()
        {
            RealPosition = _tmp_Pos;
            _tmp_Dest = _tmp_Pos;
            Walkbox = _tmp_WalkBox;
            NewWalkBoxEntered = _tmp_NewWalkBoxEntered;
        }

        void ActorSetWalkTo()
        {
            if (!NewWalkBoxEntered)
                return;

            NewWalkBoxEntered = false;

            int nextBox = ((ScummEngine0)_scumm).WalkboxFindTarget(this, _walkdata.DestBox, _walkdata.Dest);
            if (nextBox != InvalidBox)
            {
                _walkdata.CurBox = (byte)nextBox;
            }
        }

        static readonly byte[] v0ActorDemoTalk =
            {
                0x00,
                0x06, // Syd
                0x06, // Razor
                0x06, // Dave
                0x06, // Michael
                0x06, // Bernard
                0x06, // Wendy
                0x00, // Jeff
                0x46, // Radiation Suit
                0x06, // Dr Fred
                0x06, // Nurse Edna
                0x06, // Weird Ed
                0x06, // Dead Cousin Ted
                0xE2, // Purple Tentacle
                0xE2, // Green Tentacle
                0x06, // Meteor police
                0xC0, // Meteor
                0x06, // Mark Eteer
                0x06, // Talkshow Host
                0x00, // Plant
                0xC0, // Meteor Radiation
                0xC0, // Edsel (small, outro)
                0x00, // Meteor (small, intro)
                0x06, // Sandy (Lab)
                0x06, // Sandy (Cut-Scene)
            };

        static readonly byte[] v0ActorTalk =
            {
                0x00,
                0x06, // Syd
                0x06, // Razor
                0x06, // Dave
                0x06, // Michael
                0x06, // Bernard
                0x06, // Wendy
                0x00, // Jeff
                0x46, // Radiation Suit
                0x06, // Dr Fred
                0x06, // Nurse Edna
                0x06, // Weird Ed
                0x06, // Dead Cousin Ted
                0xFF, // Purple Tentacle
                0xFF, // Green Tentacle
                0x06, // Meteor police
                0xC0, // Meteor
                0x06, // Mark Eteer
                0x06, // Talkshow Host
                0x00, // Plant
                0xC0, // Meteor Radiation
                0xC0, // Edsel (small, outro)
                0x00, // Meteor (small, intro)
                0x06, // Sandy (Lab)
                0x06, // Sandy (Cut-Scene)
            };

        static readonly string[] v0ActorNames_English =
            {
                "Syd",
                "Razor",
                "Dave",
                "Michael",
                "Bernard",
                "Wendy",
                "Jeff",
                "", // Radiation Suit
                "Dr Fred",
                "Nurse Edna",
                "Weird Ed",
                "Dead Cousin Ted",
                "Purple Tentacle",
                "Green Tentacle",
                "", // Meteor Police
                "Meteor",
                "", // Mark Eteer
                "", // Talkshow Host
                "Plant",
                "", // Meteor Radiation
                "", // Edsel (small, outro)
                "", // Meteor (small, intro)
                "Sandy", // (Lab)
                "", // Sandy (Cut-Scene)
            };

        static string[] v0ActorNames_German =
            {
                "Syd",
                "Razor",
                "Dave",
                "Michael",
                "Bernard",
                "Wendy",
                "Jeff",
                "",
                "Dr.Fred",
                "Schwester Edna",
                "Weird Ed",
                "Ted",
                "Lila Tentakel",
                "Gr<nes Tentakel",
                "",
                "Meteor",
                "",
                "",
                "Pflanze",
                "",
                "",
                "",
                "Sandy",
                "",
            };
    }
}

