using NScumm.Core;

namespace NScumm.Sword1
{
    internal class SwordObject
    {
        private const int O_TOTAL_EVENTS = 5;
        private const int O_WALKANIM_SIZE = 600;         //max number of nodes in router output

        /// <summary>
        /// 0 broad description of type - object, floor, etc.
        /// </summary>
        public int type
        {
            get { return Data.ToInt32(Offset); }
            set { Data.WriteUInt32(Offset, (uint)value); }
        }

        // 4  bit flags for logic, graphics, mouse, etc.                
        public int status
        {
            get { return Data.ToInt32(Offset + 4); }
            set { Data.WriteUInt32(Offset + 4, (uint)value); }
        }
        // 8  logic type         
        public int logic
        {
            get { return Data.ToInt32(Offset + 8); }
            set { Data.WriteUInt32(Offset + 8, (uint)value); }
        }
        // 12 where is the mega character            
        public int place
        {
            get { return Data.ToInt32(Offset + 12); }
            set { Data.WriteUInt32(Offset + 12, (uint)value); }
        }
        public int down_flag
        {
            get { return Data.ToInt32(Offset + 16); }
            set { Data.WriteUInt32(Offset + 16, (uint)value); }
        }                // 16 pass back down with this - with C possibly both are unnecessary?
        public int target
        {
            get { return Data.ToInt32(Offset + 20); }
            set { Data.WriteUInt32(Offset + 20, (uint)value); }
        }                   // 20 target object for the GTM         *these are linked to script
        public int screen
        {
            get { return Data.ToInt32(Offset + 24); }
            set { Data.WriteUInt32(Offset + 24, (uint)value); }
        }                   // 24 physical screen/section
        public int frame
        {
            get { return Data.ToInt32(Offset + 28); }
            set { Data.WriteUInt32(Offset + 28, (uint)value); }
        }                    // 28 frame number &
        public int resource
        {
            get { return Data.ToInt32(Offset + 32); }
            set { Data.WriteUInt32(Offset + 32, (uint)value); }
        }                 // 32 id of spr file it comes from
        public int sync
        {
            get { return Data.ToInt32(Offset + 36); }
            set { Data.WriteUInt32(Offset + 36, (uint)value); }
        }                     // 36 receive sync here
        public int pause
        {
            get { return Data.ToInt32(Offset + 40); }
            set { Data.WriteUInt32(Offset + 40, (uint)value); }
        }                    // 40 logic_engine() pauses these cycles
        public int xcoord
        {
            get { return Data.ToInt32(Offset + 44); }
            set { Data.WriteUInt32(Offset + 44, (uint)value); }
        }                   // 44
        public int ycoord
        {
            get { return Data.ToInt32(Offset + 48); }
            set { Data.WriteUInt32(Offset + 48, (uint)value); }
        }                   // 48
        public int mouse_x1
        {
            get { return Data.ToInt32(Offset + 52); }
            set { Data.WriteUInt32(Offset + 52, (uint)value); }
        }                 // 52 top-left of mouse area is (x1,y1)
        public int mouse_y1
        {
            get { return Data.ToInt32(Offset + 56); }
            set { Data.WriteUInt32(Offset + 56, (uint)value); }
        }                 // 56
        public int mouse_x2
        {
            get { return Data.ToInt32(Offset + 60); }
            set { Data.WriteUInt32(Offset + 60, (uint)value); }
        }                 // 60 bottom-right of area is (x2,y2)   (these coords are inclusive)
        public int mouse_y2
        {
            get { return Data.ToInt32(Offset + 64); }
            set { Data.WriteUInt32(Offset + 64, (uint)value); }
        }                 // 64
        public int priority
        {
            get { return Data.ToInt32(Offset + 68); }
            set { Data.WriteUInt32(Offset + 68, (uint)value); }
        }                 // 68
        public int mouse_on
        {
            get { return Data.ToInt32(Offset + 72); }
            set { Data.WriteUInt32(Offset + 72, (uint)value); }
        }                 // 72
        public int mouse_off
        {
            get { return Data.ToInt32(Offset + 76); }
            set { Data.WriteUInt32(Offset + 76, (uint)value); }
        }                // 76
        public int mouse_click
        {
            get { return Data.ToInt32(Offset + 80); }
            set { Data.WriteUInt32(Offset + 80, (uint)value); }
        }              // 80
        public int interact
        {
            get { return Data.ToInt32(Offset + 84); }
            set { Data.WriteUInt32(Offset + 84, (uint)value); }
        }                 // 84
        public int get_to_script
        {
            get { return Data.ToInt32(Offset + 88); }
            set { Data.WriteUInt32(Offset + 88, (uint)value); }
        }            // 88
        public int scale_a
        {
            get { return Data.ToInt32(Offset + 92); }
            set { Data.WriteUInt32(Offset + 92, (uint)value); }
        }                  // 92 used by floors
        public int scale_b
        {
            get { return Data.ToInt32(Offset + 96); }
            set { Data.WriteUInt32(Offset + 96, (uint)value); }
        }                  // 96
        public int anim_x
        {
            get { return Data.ToInt32(Offset + 100); }
            set { Data.WriteUInt32(Offset + 100, (uint)value); }
        }                   // 100
        public int anim_y
        {
            get { return Data.ToInt32(Offset + 104); }
            set { Data.WriteUInt32(Offset + 104, (uint)value); }
        }                   // 104

        public ScriptTree tree { get; private set; }                // 108  size = 44 bytes
        public ScriptTree bookmark;            // 152  size = 44 bytes

        public int dir
        {
            get { return Data.ToInt32(Offset + 196); }
            set { Data.WriteUInt32(Offset + 196, (uint)value); }
        }                        // 196
        public int speech_pen
        {
            get { return Data.ToInt32(Offset + 200); }
            set { Data.WriteUInt32(Offset + 200, (uint)value); }
        }                 // 200
        public int speech_width
        {
            get { return Data.ToInt32(Offset + 204); }
            set { Data.WriteUInt32(Offset + 204, (uint)value); }
        }               // 204
        public int speech_time
        {
            get { return Data.ToInt32(Offset + 208); }
            set { Data.WriteUInt32(Offset + 208, (uint)value); }
        }                // 208
        public int text_id
        {
            get { return Data.ToInt32(Offset + 212); }
            set { Data.WriteUInt32(Offset + 212, (uint)value); }
        }                    // 212 working back from o_ins1
        public int tag
        {
            get { return Data.ToInt32(Offset + 216); }
            set { Data.WriteUInt32(Offset + 216, (uint)value); }
        }                        // 216
        public int anim_pc
        {
            get { return Data.ToInt32(Offset + 220); }
            set { Data.WriteUInt32(Offset + 220, (uint)value); }
        }                    // 220 position within an animation structure
        public int anim_resource
        {
            get { return Data.ToInt32(Offset + 224); }
            set { Data.WriteUInt32(Offset + 224, (uint)value); }
        }              // 224 cdt or anim table

        public int walk_pc
        {
            get { return Data.ToInt32(Offset + 228); }
            set { Data.WriteUInt32(Offset + 228, (uint)value); }
        }                      // 228

        public TalkOffset[] talk_table { get; } // 232  size = 6*8 bytes = 48

        public OEventSlot[] event_list { get; }   // 280  size = 5*8 bytes = 40

        public int ins1
        {
            get { return Data.ToInt32(Offset + 320); }
            set { Data.WriteUInt32(Offset + 320, (uint)value); }
        }                      // 320
        public int ins2
        {
            get { return Data.ToInt32(Offset + 324); }
            set { Data.WriteUInt32(Offset + 324, (uint)value); }
        }                      // 324
        public int ins3
        {
            get { return Data.ToInt32(Offset + 328); }
            set { Data.WriteUInt32(Offset + 328, (uint)value); }
        }                      // 328

        public int mega_resource
        {
            get { return Data.ToInt32(Offset + 332); }
            set { Data.WriteUInt32(Offset + 332, (uint)value); }
        }                // 332
        public int walk_resource
        {
            get { return Data.ToInt32(Offset + 336); }
            set { Data.WriteUInt32(Offset + 336, (uint)value); }
        }                // 336

        public WalkData[] route { get; }   // 340  size = 600*20 bytes = 12000

        public byte[] Data { get; }
        public int Offset { get; }

        public SwordObject(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
            tree = new ScriptTree(data, offset + 108);
            bookmark = new ScriptTree(data, offset + 152);
            talk_table = new TalkOffset[6];
            for (int i = 0; i < 6; i++)
            {
                talk_table[i] = new TalkOffset(data, offset + 232 + i * 8);
            }
            event_list = new OEventSlot[O_TOTAL_EVENTS];
            for (int i = 0; i < O_TOTAL_EVENTS; i++)
            {
                event_list[i] = new OEventSlot(data, offset + 280 + i * 8);
            }
            route = new WalkData[O_WALKANIM_SIZE];
            for (int i = 0; i < O_WALKANIM_SIZE; i++)
            {
                route[i] = new WalkData(data, offset + 340 + i * WalkData.Size);
            }
        }
        // mega size = 12340 bytes (+ 8 byte offset table + 20 byte header = 12368)


    }
}