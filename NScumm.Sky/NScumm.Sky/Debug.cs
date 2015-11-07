using NScumm.Core;

namespace NScumm.Sky
{
    internal class Debug : IEnableTrace
    {
        private static Debug _instance;

        public static Debug Instance
        {
            get { return _instance ?? new Debug(); }
        }

        public void Logic(uint logic)
        {
            Write("LOGIC: {0}", LogicTableNames[logic]);
        }

        public void Mcode(uint mcode, uint a, uint b, uint c)
        {
            Write("MCODE: {0}({1}, {2}, {3})", Mcodes[mcode], a, b, c);
        }

        public void Write(string format, params object[] args)
        {
            this.Trace().Write("Sky", format, args);
        }

        private static readonly string[] Mcodes = {
            "fn_cache_chip",
            "fn_cache_fast",
            "fn_draw_screen",
            "fn_ar",
            "fn_ar_animate",
            "fn_idle",
            "fn_interact",
            "fn_start_sub",
            "fn_they_start_sub",
            "fn_assign_base",
            "fn_disk_mouse",
            "fn_normal_mouse",
            "fn_blank_mouse",
            "fn_cross_mouse",
            "fn_cursor_right",
            "fn_cursor_left",
            "fn_cursor_down",
            "fn_open_hand",
            "fn_close_hand",
            "fn_get_to",
            "fn_set_to_stand",
            "fn_turn_to",
            "fn_arrived",
            "fn_leaving",
            "fn_set_alternate",
            "fn_alt_set_alternate",
            "fn_kill_id",
            "fn_no_human",
            "fn_add_human",
            "fn_add_buttons",
            "fn_no_buttons",
            "fn_set_stop",
            "fn_clear_stop",
            "fn_pointer_text",
            "fn_quit",
            "fn_speak_me",
            "fn_speak_me_dir",
            "fn_speak_wait",
            "fn_speak_wait_dir",
            "fn_chooser",
            "fn_highlight",
            "fn_text_kill",
            "fn_stop_mode",
            "fn_we_wait",
            "fn_send_sync",
            "fn_send_fast_sync",
            "fn_send_request",
            "fn_clear_request",
            "fn_check_request",
            "fn_start_menu",
            "fn_unhighlight",
            "fn_face_id",
            "fn_foreground",
            "fn_background",
            "fn_new_background",
            "fn_sort",
            "fn_no_sprite_engine",
            "fn_no_sprites_a6",
            "fn_reset_id",
            "fn_toggle_grid",
            "fn_pause",
            "fn_run_anim_mod",
            "fn_simple_mod",
            "fn_run_frames",
            "fn_await_sync",
            "fn_inc_mega_set",
            "fn_dec_mega_set",
            "fn_set_mega_set",
            "fn_move_items",
            "fn_new_list",
            "fn_ask_this",
            "fn_random",
            "fn_person_here",
            "fn_toggle_mouse",
            "fn_mouse_on",
            "fn_mouse_off",
            "fn_fetch_x",
            "fn_fetch_y",
            "fn_test_list",
            "fn_fetch_place",
            "fn_custom_joey",
            "fn_set_palette",
            "fn_text_module",
            "fn_change_name",
            "fn_mini_load",
            "fn_flush_buffers",
            "fn_flush_chip",
            "fn_save_coods",
            "fn_plot_grid",
            "fn_remove_grid",
            "fn_eyeball",
            "fn_cursor_up",
            "fn_leave_section",
            "fn_enter_section",
            "fn_restore_game",
            "fn_restart_game",
            "fn_new_swing_seq",
            "fn_wait_swing_end",
            "fn_skip_intro_code",
            "fn_blank_screen",
            "fn_print_credit",
            "fn_look_at",
            "fn_linc_text_module",
            "fn_text_kill2",
            "fn_set_font",
            "fn_start_fx",
            "fn_stop_fx",
            "fn_start_music",
            "fn_stop_music",
            "fn_fade_down",
            "fn_fade_up",
            "fn_quit_to_dos",
            "fn_pause_fx",
            "fn_un_pause_fx",
            "fn_printf"
        };

        static readonly string[] LogicTableNames = {
            "return",
            "Logic::script",
            "Logic::auto_route",
            "Logic::ar_anim",
            "Logic::ar_turn",
            "Logic::alt",
            "Logic::anim",
            "Logic::turn",
            "Logic::cursor",
            "Logic::talk",
            "Logic::listen",
            "Logic::stopped",
            "Logic::choose",
            "Logic::frames",
            "Logic::pause",
            "Logic::wait_sync",
            "Logic::simple_anim"
        };
    }
}
