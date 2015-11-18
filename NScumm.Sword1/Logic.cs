using System;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    enum ScriptVariableNames
    {
        RETURN_VALUE = 0,
        RETURN_VALUE_2,
        RETURN_VALUE_3,
        RETURN_VALUE_4,
        DEFAULT_ICON_TEXT,
        MENU_LOOKING,
        TOP_MENU_DISABLED,
        GEORGE_DOING_REST_ANIM,
        GEORGE_WALKING,
        ADVISOR_188_FLAG,
        MEGA_ON_GRID,
        REROUTE_GEORGE,
        WALK_FLAG,
        WALK_ATTEMPT,
        TARGET_X,
        TARGET_Y,
        DISTANCE_APART,
        ID_LOW_FLOOR,
        NEW_SCREEN,
        CUR_ID,
        MOUSE_STATUS,
        PALETTE,
        NEW_PALETTE,
        MOUSE_X,
        MOUSE_Y,
        SPECIAL_ITEM,
        CLICK_ID,
        MOUSE_BUTTON,
        BUTTON,
        BOTH_BUTTONS, // not used anymore
        SAFE_X,
        SAFE_Y,
        CHANGE_X,
        CHANGE_Y,
        CHANGE_PLACE,
        CHANGE_DIR,
        CHANGE_STANCE,
        SCROLL_FLAG,
        SCROLL_OFFSET_X,
        SCROLL_OFFSET_Y,
        MAX_SCROLL_OFFSET_X,
        MAX_SCROLL_OFFSET_Y,
        FEET_X,
        FEET_Y,
        SECOND_ITEM, //SECOND_ICON,
        SUBJECT_CHOSEN,
        IN_SUBJECT,
        DEBUG_FLAG_1,
        DEBUG_FLAG_2,
        DEBUG_FLAG_3,
        FIRST_WATCH,
        GEORGE_ALLOWED_REST_ANIMS,
        CURRENT_MUSIC,
        TESTLINENO,
        LASTLINENO,
        WANTPREVIOUSLINE,
        PLAYINGDEMO,
        TEMP_FLAG,
        PHOTOS_FLAG,
        PHONE_FLOOR_FLAG,
        PHONE_ROOM_FLAG,
        BENOIR_FLAG,
        GUARD_FLAG,
        MOUE_DOOR_FLAG,
        CANOPY_FLAG,
        GOT_NEWSPAPER_FLAG,
        DEMO_NICO_FLAG,
        NICO_TARGET,
        NICO_DIR,
        BEEN_TO_ALLEY,
        DUSTBIN_FLAG,
        DUSTBIN_2_FLAG,
        TRIED_MANHOLE_FLAG,
        MANHOLE_FLAG,
        DRAINPIPE_FLAG,
        OPENED_MANHOLE_2_BEFORE,
        SEARCHED_PLANTARD_FLAG,
        ENTERED_CAFE_ONCE,
        BOTTLE_3_FLAG,
        TOOLBOX_4_FLAG,
        CALL_ALB_FLAG,
        CALL_ALBERT_FLAG,
        GOT_NOSE_FLAG,
        GOT_MATERIAL_FLAG,
        GOT_TISSUE_FLAG,
        RAILING_7_FLAG,
        SEEN_FLOWERS_FLAG,
        SEEN_DRESS_SHOP_FLAG,
        DOOR_9_FLAG,
        PHONE_10_FLAG,
        MANUSCRIPT_ON_TABLE_10_FLAG,
        DOG_TURD_FLAG,
        PIERMONT_AT_PIANO_FLAG,
        GOT_KEY_FLAG,
        USED_HOTEL_KEY_ONCE,
        WINDOW_15_OPEN,
        CLIMBED_OUT_15_FLAG,
        WINDOW_16_FLAG,
        HOTEL_ASSASSIN_BEEN,
        WARDROBE_17_OPEN,
        SEARCHED_TROUSERS_17,
        ENTERED_17_FLAG,
        WINDOW_27_FLAG,
        CASE_1_LOCKED_FLAG,
        CASE_2_LOCKED_FLAG,
        CASE_3_LOCKED_FLAG,
        CASE_4_LOCKED_FLAG,
        SEEN_ARMOR_28_FLAG,
        CLOSED_WINDOW_28_FLAG,
        WINDOW_28_FLAG,
        WINDOW_DRAUGHT_FLAG,
        SEEN_WINDOW_28_FLAG,
        FACING_WINDOW_FLAG,
        CLOSING_WINDOW_FLAG,
        SARCOPHAGUS_FLAG,
        ENTERED_MUSEUM_28_FLAG,
        SARCOPHAGUS_DOOR_29_OPEN,
        AMBULANCE_31_FLAG,
        CONSULTANT_HERE,
        SEEN_MR_SHINY_FLAG,
        SEEN_CUPBOARD_FLAG,
        PLUG_33_UNPLUGGED,
        SAM_RETURNING,
        PULLED_PLUG_33,
        PULSE_34_FLAG,
        DOOR_34_OPEN,
        MARQUET_AWAKE_FLAG,
        JUGGLER_FLAG,
        JUGGLE_FLAG,
        CROWD_FLAG,
        MANHOLE_36_FLAG,
        DOOR_37_FLAG,
        IN_BOAT_FLAG,
        GOT_HOOK_FLAG,
        HOOK_FLAG,
        STEPS_38_FLAG,
        TRIPOD_PUZZLE_FLAG,
        SOAP_43_FLAG,
        SEEN_WASHBASIN_43,
        HOSPITAL_FLAG,
        SEEN_PARIS_MAP,
        PHONE_SCREEN_FLAG,
        PHONE_PLACE_FLAG,
        SEAN_DEAD,
        SPAIN_VISIT,
        WET_BEER_TOWEL_TIMER,
        BEER_TOWEL_BEEN_WET,
        NICO_SCOT_SCREEN,
        NICO_AT_PANEL_72,
        NICO_POSITION_71,
        SEEN_DRAIN_19,
        SEEN_MENU_19,
        PUB_TRAP_DOOR,
        ASSASSIN_EIRE_DONE,
        BAR_TOWEL_TAKEN,
        GLASS_WASH_FLAG,
        PUB_DOOR_FLAG,
        PUB_FLAP_FLAG,
        DOYLE_DRINKING,
        RON_SNEEZING,
        FUSE_WIRE_TAKEN,
        FUSE_WIRE_ON_TABLE,
        GLASS_20_FLAG,
        MAGUIRE_PUB_DONE,
        PINT_LEVEL_FLAG,
        GEM_21_TAKEN,
        MAGUIRE_CEL_DONE,
        TORCH_21_TAKEN,
        BEEN_UP_HAYBAILS,
        LIFTING_KEYS_IN_HOLE_23,
        SEEN_STEPS_SEQUENCE,
        SEEN_GOAT_24,
        FLEECY_TANGLED,
        FLEECY_STUCK,
        FLEECY_BACKING_OFF,
        SEEN_LADDER_SEQUENCE,
        BUTT_COUNT_24,
        KEYSTONE_FLAG,
        PANEL_25_MOVED,
        SACK_25_FLAG,
        SAND_FLAG,
        SEEN_HOLES_25,
        REPLICA_IN_CAVITY,
        SEEN_RAT_26,
        ENTERED_CELLAR_BEFORE,
        CAT_ON_SHELF,
        CAT_RAN_OFF,
        CAT_TIMER,
        STATUETTE_FLAG,
        SEEN_TOP_SHELF_45,
        DUANE_TARGET,
        AYUB_OPENING_DOOR,
        GEORGE_TALKING_TO_PEARL,
        CARPET_DOOR_47_OPEN,
        TOILET_KEYS_ON_BAR,
        EXPLAINED_RETURNING_KEYS,
        DOOR_49_OPEN,
        TOILET_CHAIN_50_TAKEN,
        TOWEL_DISPENSER_50_OPEN,
        TOWEL_50_TAKEN,
        CUBICLE_DOOR_50_OPEN,
        DOOR_50_OPEN,
        MAX_ITERATION,
        ITERATION,
        STICK_54_FLAG,
        TOWEL_IN_CRACK_54,
        CAVE_54_OPEN,
        GUN_54_FLAG,
        KHAN_54_HERE,
        DOOR_55_OPEN,
        READ_INSCRIPTION_55,
        SEEN_STATUE_55,
        VISITED_COUNTESS_56_AGAIN,
        CHALICE_56_GIVEN,
        CHESS_PIECE_56_GIVEN,
        GARDENER_57_HERE,
        PRESSURE_GAUGE_57_FLAG,
        FOUND_WELL_57,
        DOOR_58_OPEN,
        COUNTESS_58_HERE,
        GARDENER_58_HERE,
        COUNTESS_59_HERE,
        BIBLE_59_FLAG,
        WINDOW_59_SHUT,
        CHALICE_59_TAKEN,
        SECRET_DOOR_59_OPEN,
        HOLDING_SNUFFER,
        TISSUE_ON_SNUFFER,
        TISSUE_59_CHARRED,
        TISSUE_59_BURNING,
        CANDLE_59_BURNT,
        LECTERN_CANDLES_59_LIT,
        TISSUE_FLAME_59_ON,
        GARDENER_60_POSITION,
        GARDENER_60_CHECKING_DOGS,
        DOGS_DISTURBED,
        MIRROR_60_TAKEN,
        SEEN_LEFT_ROCKFALL_61,
        LION_HEAD_FALLING,
        LION_FANG_FLAG,
        DOOR_61_FLAG,
        GEORGE_HOLDING_PIECE,
        CHESS_SQUARE_1_FLAG,
        CHESS_SQUARE_2_FLAG,
        CHESS_SQUARE_3_FLAG,
        CHESS_SQUARE_4_FLAG,
        CHESS_SQUARE_5_FLAG,
        DOOR_ONE_63_OPEN,
        DOOR_TWO_63_OPEN,
        DOOR_THREE_63_OPEN,
        GEORGE_ON_ROOF,
        SEEN_EKLUND_63,
        DOOR_65_OPEN,
        DOOR_67_OPEN,
        WINDOW_66_OPEN,
        SEQUENCE_69_FLAG,
        SC69_TIMER,
        LEFT_TREE_POINTER_71_FLAG,
        RIGHT_TREE_POINTER_71_FLAG,
        RUBBLE_72_FLAG,
        MACHINERY_HANDLE_FLAG,
        MACHINERY_COG_FLAG,
        DEMON_RB_FLAG,
        DEMON_LB_FLAG,
        DEMON_COGS_FLAG,
        DEMON_PIPE_FLAG,
        DEMON_NOSE_FLAG,
        DEMON_LEFT_COG_FLAG,
        DEMON_RIGHT_COG_FLAG,
        PANEL_72_FLAG,
        SEEN_CRYPT_73,
        SEEN_GUNPOWDER_73,
        GUIDO_73_HERE,
        NICO_POSITION_73,
        ALBERT_ANNOYED_FLAG,
        ALBERT_BRIEFCASE_FLAG,
        ALBERT_BUZZER_FLAG,
        ALBERT_CDT_FLAG,
        ALBERT_CHANTELLE_FLAG,
        ALBERT_CHAT_FLAG,
        ALBERT_CLOWN_FLAG,
        ALBERT_JACKET_FLAG,
        ALBERT_KEYS_FLAG,
        ALBERT_NOSE_FLAG,
        ALBERT_PLANTARD_FLAG,
        ALBERT_POLICE_FLAG,
        ALBERT_POS_FLAG,
        ALBERT_TALK_FLAG,
        ALBERT_TISSUE_FLAG,
        ALBERT_TEXT,
        ALBERT_INFO_FLAG,
        ARTO_BULL_FLAG,
        ARTO_BRUSH_FLAG,
        ARTO_IRRITATION_FLAG,
        ARTO_KLAUSNER_FLAG,
        ARTO_LOOM_FLAG,
        ARTO_OBJECT_FLAG,
        ARTO_PHRASE_FLAG,
        ARTO_TEXT,
        ASSASSIN_BOOK_FLAG,
        ASSASSIN_BULL_FLAG,
        ASSASSIN_CHURCH_FLAG,
        ASSASSIN_EIRE_TEXT,
        ASSASSIN_SWORD_FLAG,
        ASSASSIN_TEMPLAR_FLAG,
        ASSASSIN_TEXT,
        AYUB_BULL_FLAG,
        AYUB_KLAUSNER_FLAG,
        AYUB_LOOM_FLAG,
        AYUB_ULTAR_FLAG,
        AYUB_TEXT,
        BASHER_BEER_FLAG,
        BASHER_COMPLAIN_FLAG,
        BASHER_EKLUND_FLAG,
        BASHER_HELP_FLAG,
        BASHER_NICO_FLAG,
        BASHER_STOP_FLAG,
        BASHER_WEASEL_FLAG,
        BASHER_WINDOW_FLAG,
        BASHER_TEXT,
        BENOIR_BUZZER_FLAG,
        BENOIR_GAUGE_FLAG,
        BENOIR_MARQUET_FLAG,
        BENOIR_NURSE_FLAG,
        BENOIR_RENEE_FLAG,
        BENOIR_TEXT,
        CARPET_TEXT,
        CARPET_OBJECT_FLAG,
        CHANTELLE_BRIEFCASE_FLAG,
        CHANTELLE_CLOWN_FLAG,
        CHANTELLE_DOCTOR_FLAG,
        CHANTELLE_EYE_FLAG,
        CHANTELLE_FAINT_FLAG,
        CHANTELLE_NEWSPAPER_FLAG,
        CHANTELLE_PLANTARD_FLAG,
        CHANTELLE_TEXT,
        CHANTELLE_WAKE_COUNTER,
        CLERK_ASSASSIN_FLAG,
        CLERK_BUZZER_FLAG,
        CLERK_CLOWN_FLAG,
        CLERK_ENOUGH_FLAG,
        CLERK_HKEY_FLAG,
        CLERK_KEY_FLAG,
        CLERK_KEY_STOP_FLAG,
        CLERK_NOSE_FLAG,
        CLERK_PASS_FLAG,
        CLERK_PHOTO_FLAG,
        CLERK_PIERMONT_FLAG,
        CLERK_PLANTARD_FLAG,
        CLERK_POLITE_FLAG,
        CLERK_SAFE_FLAG,
        CLERK_TEMPLAR_FLAG,
        CLERK_TEXT,
        CLERK_TISSUE_FLAG,
        CLERK_WEASEL_FLAG,
        CONSULT_CHALICE_FLAG,
        CONSULT_GAUGE_FLAG,
        CONSULT_GEM_FLAG,
        CONSULT_LIFTKEY_FLAG,
        CONSULT_MARQUET_FLAG,
        CONSULT_NOSE_FLAG,
        CONSULT_PHOTO_FLAG,
        CONSULT_TEXT,
        CONSULT_TISSUE_FLAG,
        COSTUMIER_BALL_FLAG,
        COSTUMIER_BUZZER_FLAG,
        COSTUMIER_CLOWN_FLAG,
        COSTUMIER_PHOTO_FLAG,
        COSTUMIER_PLANTARD_FLAG,
        COSTUMIER_TISSUE_FLAG,
        COSTUMIER_TEXT,
        DOYLE_BEER_FLAG,
        DOYLE_BUZZER_FLAG,
        DOYLE_CASTLE_FLAG,
        DOYLE_DIG_FLAG,
        DOYLE_FLASHLIGHT_FLAG,
        DOYLE_GEM_FLAG,
        DOYLE_JEWEL_FLAG,
        DOYLE_JOB_FLAG,
        DOYLE_KEYS_FLAG,
        DOYLE_LEPRECHAUN_FLAG,
        DOYLE_NOSE_FLAG,
        DOYLE_PEAGRAM_FLAG,
        DOYLE_PHOTOGRAPH_FLAG,
        DOYLE_SEAN_FLAG,
        DOYLE_TEMPLAR_FLAG,
        DOYLE_TEXT,
        DOYLE_TISSUE_FLAG,
        DOYLE_TOWEL_FLAG,
        DUANE_ARTO_FLAG,
        DUANE_BULL_FLAG,
        DUANE_CLEVE_FLAG,
        DUANE_DUANE_FLAG,
        DUANE_PEARL_FLAG,
        DUANE_PHOTO_FLAG,
        DUANE_KEYS_FLAG,
        DUANE_MANUSCRIPT_FLAG,
        DUANE_NEJO_FLAG,
        DUANE_PHRASE_FLAG,
        DUANE_QUEEN_FLAG,
        DUANE_STATUETTE_FLAG,
        DUANE_TEMPLAR_FLAG,
        DUANE_TEXT,
        DUANE_ULTAR_FLAG,
        ERIC_MARQUET_FLAG,
        ERIC_NURSE_FLAG,
        ERIC_PHOTO_FLAG,
        EVA_CLOWN_FLAG,
        EVA_LENS_FLAG,
        EVA_MARQUET_FLAG,
        EVA_MOB_FLAG,
        EVA_NURSE_FLAG,
        EVA_TEXT,
        FARMER_BEER_FLAG,
        FARMER_BOOK_FLAG,
        FARMER_BUZZER_FLAG,
        FARMER_CAR_FLAG,
        FARMER_CASTLE_FLAG,
        FARMER_FLASHLIGHT_FLAG,
        FARMER_GEM_FLAG,
        FARMER_GHOST_FLAG,
        FARMER_LAST_STRAW,
        FARMER_LIFTKEYS_FLAG,
        FARMER_MOVED_FLAG,
        FARMER_NOSE_FLAG,
        FARMER_PASS_FLAG,
        FARMER_PEAGRAM_FLAG,
        FARMER_PHOTO_FLAG,
        FARMER_SEAN_FLAG,
        FARMER_TEMPLAR_FLAG,
        FARMER_TEXT,
        FARMER_TISSUE_FLAG,
        FARMER_WIRE_FLAG,
        FLEECY_TEXT,
        FLOWER_FLOWER_FLAG,
        FLOWER_FORTUNE_FLAG,
        FLOWER_GAUGE_FLAG,
        FLOWER_GEM_FLAG,
        FLOWER_LIFTKEYS_FLAG,
        FLOWER_NICO_FLAG,
        FLOWER_PASS_FLAG,
        FLOWER_PHOTO_FLAG,
        FLOWER_TEXT,
        GARD_ATTEMPT,
        GARD_BY_WELL,
        GARDENER_COUNTESS_FLAG,
        GARDENER_CHALICE_FLAG,
        GARDENER_FLOPPO_FLAG,
        GARDENER_GOODBYE_FLAG,
        GARDENER_HOSE_FLAG,
        GARDENER_IRRITATION,
        GARDENER_SPEECH_FLAG,
        GARDENER_TEMPLAR_FLAG,
        GARDENER_TEXT,
        GATEKEEPER_TALK_FLAG,
        GATEKEEPER_CDT_FLAG,
        GMASTER_TALK_FLAG,
        GMASTER_CDT_FLAG,
        GENDARME_CARD_FLAG,
        GENDARME_CLOWN_FLAG,
        GENDARME_MOUE_FLAG,
        GENDARME_NOSE_FLAG,
        GEND_PAPER_FLAG,
        GENDARME_PHOTO_FLAG,
        GENDARME_ROSSO_FLAG,
        GENDARME_TEXT,
        GENDARME_TISSUE_FLAG,
        GENDARME_WEASEL_FLAG,
        GOINFRE_ALARM_FLAG,
        GOINFRE_EXHIBIT_FLAG,
        GOINFRE_GEM_FLAG,
        GOINFRE_KEYS_FLAG,
        GOINFRE_LOBINEAU_FLAG,
        GOINFRE_MS_FLAG,
        GOINFRE_SARCOPHAGUS_FLAG,
        GOINFRE_SCOLD_FLAG,
        GOINFRE_TEMPLAR_FLAG,
        GOINFRE_TEXT,
        GOINFRE_TISSUE_FLAG,
        GOINFRE_TRIPOD_FLAG,
        GOINFRE_WINDOW_FLAG,
        GORILLA_CLOWN_FLAG,
        GORILLA_KHAN_FLAG,
        GORILLA_PASS_FLAG,
        GORILLA_PLANTARD_FLAG,
        GORILLA_SEARCH_FLAG,
        GORILLA_TEXT,
        GORILLA_TISSUE_FLAG,
        GORILLA_WEASEL_FLAG,
        HOSCOP_ALERT_FLAG,
        HOSCOP_MARQUET_FLAG,
        HOSCOP_MOB_FLAG,
        HOSCOP_TEXT,
        JUGGLER_JUGGLER_FLAG,
        JUGGLER_TEMPLAR_FLAG,
        JUGGLER_GEM_FLAG,
        JUGGLER_TEXT,
        KHAN_SUBJECT_FLAG,
        KHAN_PREAMBLE_FLAG,
        LATVIAN_CLOWN_FLAG,
        LATVIAN_EYE_FLAG,
        LATVIAN_LIFTKEYS_FLAG,
        LATVIAN_MATCHBOOK_FLAG,
        LATVIAN_MS_FLAG,
        LATVIAN_NOSE_FLAG,
        LATVIAN_PHOTO_FLAG,
        LATVIAN_PLANTARD_FLAG,
        LATVIAN_TEXT,
        LEARY_BEER_FLAG,
        LEARY_BUZZER_FLAG,
        LEARY_CASTLE_FLAG,
        LEARY_CLOWN_FLAG,
        LEARY_FISH_FLAG,
        LEARY_FLAP_FLAG,
        LEARY_FLAPALERT_FLAG,
        LEARY_KEYS_FLAG,
        LEARY_NOSE_FLAG,
        LEARY_PASS_FLAG,
        LEARY_PEAGRAM_FLAG,
        LEARY_PHONE_FLAG,
        LEARY_PHOTO_FLAG,
        LEARY_PLASTER_FLAG,
        LEARY_PLUG_FLAG,
        LEARY_SEAN_FLAG,
        LEARY_SNARE_FLAG,
        LEARY_TEMPLAR_FLAG,
        LEARY_TEXT,
        LEARY_TISSUE_FLAG,
        LEARY_TOWEL_FLAG,
        LEARY_WASHER_FLAG,
        LEARY_WILD_FLAG,
        LEARY_WIRE_FLAG,
        LOBINEAU_ARTEFACT_FLAG,
        LOBINEAU_BALL_FLAG,
        LOBINEAU_BEL_FLAG,
        LOBINEAU_GEM_FLAG,
        LOBINEAU_HASH_FLAG,
        LOBINEAU_KEYS_FLAG,
        LOBINEAU_MANUSCRIPT_FLAG,
        LOBINEAU_MATCHBOOK_FLAG,
        LOBINEAU_MONTFAUCON_FLAG,
        LOBINEAU_NICO_FLAG,
        LOBINEAU_PANTS_FLAG,
        LOBINEAU_PEAGRAM_FLAG,
        LOBINEAU_STATUE_FLAG,
        LOBINEAU_SYRIA_FLAG,
        LOBINEAU_TEMPLAR_FLAG,
        LOBINEAU_TEXT,
        LOBINEAU_TRIPOD_FLAG,
        MAGUIRE_CAR_FLAG,
        MAGUIRE_CASTLE_FLAG,
        MAGUIRE_CDT_FLAG,
        MAGUIRE_CLOWN_FLAG,
        MAGUIRE_COP_FLAG,
        MAGUIRE_DIG_FLAG,
        MAGUIRE_GEM_FLAG,
        MAGUIRE_GHOST_FLAG,
        MAGUIRE_JEWEL_FLAG,
        MAGUIRE_KEYS_FLAG,
        MAGUIRE_LEPRECHAUN_FLAG,
        MAGUIRE_NOSE_FLAG,
        MAGUIRE_PEAGRAM_FLAG,
        MAGUIRE_SEAN_FLAG,
        MAGUIRE_SHOCK_FLAG,
        MAGUIRE_TALK_FLAG,
        MAGUIRE_TEXT,
        MAGUIRE_WIRE_FLAG,
        MANAGER_TEXT,
        MANAGER_BRUSH_FLAG,
        MANAGER_SPEECH_FLAG,
        MOUE_BALL_FLAG,
        MOUE_BRIEFCASE_FLAG,
        MOUE_CARD_FLAG,
        MOUE_CDT_FLAG,
        MOUE_CLOWN_FLAG,
        MOUE_EYE_FLAG,
        MOUE_FETCH_FLAG,
        MOUE_HASH_FLAG,
        MOUE_KEY_FLAG,
        MOUE_MARQUET_FLAG,
        MOUE_MATCHBOOK_FLAG,
        MOUE_MATERIAL_FLAG,
        MOUE_MOB_FLAG,
        MOUE_NEWSPAPER_FLAG,
        MOUE_NICO_FLAG,
        MOUE_NOSE_FLAG,
        MOUE_PHOTO_FLAG,
        MOUE_PLANTARD_FLAG,
        MOUE_ROSSO_FLAG,
        MOUE_STOP_FLAG,
        MOUE_TALK_FLAG,
        MOUE_TEXT,
        MOUE_TISSUE_FLAG,
        NEJO_ARTO_FLAG,
        NEJO_AYUB_FLAG,
        NEJO_BALL_FLAG,
        NEJO_BALL_TALK,
        NEJO_BULL_FLAG,
        NEJO_CAT_FLAG,
        NEJO_CHALICE_FLAG,
        NEJO_DOLLAR_FLAG,
        NEJO_GOODBYE_FLAG,
        NEJO_HENDERSONS_FLAG,
        NEJO_LOOM_FLAG,
        NEJO_NEJO_FLAG,
        NEJO_PHRASE_FLAG,
        NEJO_PLASTER_FLAG,
        NEJO_PRESSURE_GAUGE_FLAG,
        NEJO_STALL_FLAG,
        NEJO_STATUE_FLAG,
        NEJO_TEMPLAR_FLAG,
        NEJO_TEXT,
        NEJO_ULTAR_FLAG,
        NICO_ALBERT_FLAG,
        NICO_ASSASSIN_FLAG,
        NICO_BALL_FLAG,
        NICO_BRIEFCASE_FLAG,
        NICO_BULL_FLAG,
        NICO_BUZZER_FLAG,
        NICO_CHALICE_FLAG,
        NICO_CDT_FLAG,
        NICO_CLOWN_FLAG,
        NICO_EKLUND_FLAG,
        NICO_GAUGE_FLAG,
        NICO_GEM_FLAG,
        NICO_GOODBYE_FLAG,
        NICO_GUIDO_FLAG,
        NICO_HASH_FLAG,
        NICO_IRELAND_FLAG,
        NICO_KNIGHT_FLAG,
        NICO_LIFTKEYS_FLAG,
        NICO_LENS_FLAG,
        NICO_LOBINEAU_FLAG,
        NICO_MANUSCRIPT_FLAG,
        NICO_MARQUET_FLAG,
        NICO_MATCHBOOK_FLAG,
        NICO_MATERIAL_FLAG,
        NICO_NEWSPAPER_FLAG,
        NICO_NICO_FLAG,
        NICO_NOSE_FLAG,
        NICO_PASS_FLAG,
        NICO_PEAGRAM_FLAG,
        NICO_PLANTARD_FLAG,
        NICO_PLASTER_FLAG,
        NICO_PHOTO_FLAG,
        NICO_PHONE_TEXT,
        NICO_POS_FLAG,
        NICO_QUEEN_FLAG,
        NICO_RINGING_BACK_FLAG,
        NICO_ROSSO_FLAG,
        NICO_SEWER_FLAG,
        NICO_SPAIN_FLAG,
        NICO_SYRIA_FLAG,
        NICO_TALK_FLAG,
        NICO_TEMPLAR_FLAG,
        NICO_TEXT,
        NICO_TISSUE_FLAG,
        NICO_TRAIN_FLAG,
        NICO_TRIPOD_FLAG,
        NICO_WEAVER_FLAG,
        NIC_BAG_TALK_FLAG,
        NIC_BAG_CDT_FLAG,
        NICO_LEAVING_CAFE_SCREEN,
        NURSE_BENOIR_FLAG,
        NURSE_CLOWN_FLAG,
        NURSE_GAUGE_FLAG,
        NURSE_MARQUET_FLAG,
        NURSE_INTERRUPTION_FLAG,
        NURSE_TEXT,
        OBRIEN_BUZZER_FLAG,
        OBRIEN_CASTLE_FLAG,
        OBRIEN_FLASHLIGHT_FLAG,
        OBRIEN_GEM_FLAG,
        OBRIEN_JEWEL_FLAG,
        OBRIEN_JOB_FLAG,
        OBRIEN_KEYS_FLAG,
        OBRIEN_LEARY_FLAG,
        OBRIEN_MAGUIRE_FLAG,
        OBRIEN_NOSE_FLAG,
        OBRIEN_PEAGRAM_FLAG,
        OBRIEN_SEAN_FLAG,
        OBRIEN_TEMPLAR_FLAG,
        OBRIEN_TEXT,
        OBRIEN_TISSUE_FLAG,
        OBRIEN_TOWEL_FLAG,
        OLD_NOSE_FLAG,
        OLD_PHOTO_FLAG,
        OLD_LIFT_FLAG,
        OLD_BUZZER_FLAG,
        PAINTER_DIG_FLAG,
        PAINTER_DISTRACTION_FLAG,
        PAINTER_PAINTER_FLAG,
        PAINTER_TEMPLAR_FLAG,
        PAINTER_CONTROL_FLAG,
        PAINTER_TEXT,
        PEARL_AKRON_FLAG,
        PEARL_ARTO_FLAG,
        PEARL_BULL_FLAG,
        PEARL_DUANE_FLAG,
        PEARL_NEJO_FLAG,
        PEARL_PEARL_FLAG,
        PEARL_PHRASE_FLAG,
        PEARL_POEMS_FLAG,
        PEARL_STATUE_FLAG,
        PEARL_TEMPLAR_FLAG,
        PEARL_TEXT,
        PEARL_ULTAR_FLAG,
        PEARL_TALK_FLAG,
        PEARL_CDT_FLAG,
        PEARL_STALL_FLAG,
        PEARL_WEAVER_FLAG,
        PIERMONT_ASSASSIN_FLAG,
        PIERMONT_BUZZER_FLAG,
        PIERMONT_CLOWN_FLAG,
        PIERMONT_GEM_FLAG,
        PIERMONT_HKEY_FLAG,
        PIERMONT_KEY_FLAG,
        PIERMONT_KEY_ALERT_FLAG,
        PIERMONT_MS_FLAG,
        PIERMONT_NOSE_FLAG,
        PIERMONT_PASS_FLAG,
        PIERMONT_PHOTO_FLAG,
        PIERMONT_PIERMONT_FLAG,
        PIERMONT_TEMPLAR_FLAG,
        PIERMONT_TEXT,
        PIERMONT_TISSUE_FLAG,
        PIERMONT_WEASEL_FLAG,
        PRIEST_TEXT,
        PRIEST_CHALICE_FLAG,
        PRIEST_CHALICE2_FLAG,
        PRIEST_TEMPLAR_FLAG,
        PRIEST_PRIEST_FLAG,
        PRIEST_WINDO1_FLAG,
        PRIEST_WINDO2_FLAG,
        PRIEST_WINDO3_FLAG,
        RENEE_MARQUET_FLAG,
        RENEE_PHOTO_FLAG,
        RENEE_RENEE_FLAG,
        RENEE_TEXT,
        RON_ALERT_FLAG,
        RON_BEER_FLAG,
        RON_CASTLE_FLAG,
        RON_DIG_FLAG,
        RON_FLASHLIGHT_FLAG,
        RON_GHOST_FLAG,
        RON_NOSE_FLAG,
        RON_PASS_FLAG,
        RON_PEAGRAM_FLAG,
        RON_PHOTO_FLAG,
        RON_POLICE_FLAG,
        RON_SEAN_FLAG,
        RON_SNARE_FLAG,
        RON_STOP_FLAG,
        RON_TEXT,
        RON_UPSET_FLAG,
        ROSSO_CDT_FLAG,
        ROSSO_CLOWN_FLAG,
        ROSSO_DOCTOR_FLAG,
        ROSSO_FORTUNE_FLAG,
        ROSSO_GEM_FLAG,
        ROSSO_MARQUET_FLAG,
        ROSSO_MATCHBOOK_FLAG,
        ROSSO_MOUE_FLAG,
        ROSSO_OPINION_FLAG,
        ROSSO_PASS_FLAG,
        ROSSO_PEAGRAM_FLAG,
        ROSSO_PHOTO_FLAG,
        ROSSO_PLANTARD_FLAG,
        ROSSO_ROSSO_FLAG,
        ROSSO_TALK_FLAG,
        ROSSO_TEMPLAR_FLAG,
        ROSSO_TEXT,
        ROSSO_THUGS_FLAG,
        ROZZER_36_FLAG,
        ROZZER_JUGGLER_FLAG,
        ROZZER_MANHOLE_FLAG,
        ROZZER_PLASTER_FLAG,
        ROZZER_ROZZER_FLAG,
        ROZZER_TEMPLAR_FLAG,
        ROZZER_TEXT,
        SAM_BREAKDOWN_FLAG,
        SAM_BUZZER_FLAG,
        SAM_CUPBOARD_FLAG,
        SAM_GEM_FLAG,
        SAM_MARQUET_FLAG,
        SAM_MATCHBOOK_FLAG,
        SAM_MOB_FLAG,
        SAM_NOSE_FLAG,
        SAM_NURSE_FLAG,
        SAM_PHOTO_FLAG,
        SAM_PLASTER_FLAG,
        SAM_SHINY_FLAG,
        SAM_SOCKET_FLAG,
        SAM_STOP_FLAG,
        SAM_TEXT,
        SEAN_ASSASSIN_FLAG,
        SEAN_BEER_FLAG,
        SEAN_CASTLE_FLAG,
        SEAN_DIG_FLAG,
        SEAN_GEM_FLAG,
        SEAN_LKEYS_FLAG,
        SEAN_NOSE_FLAG,
        SEAN_OPINION,
        SEAN_PACKAGE_FLAG,
        SEAN_PEAGRAM_FLAG,
        SEAN_SELF_FLAG,
        SEAN_SNAP_FLAG,
        SEAN_TEXT,
        STATUE_GUARD_CONTROL_FLAG,
        STATUE_GUARD_FLAG,
        STATUE_GUARD_GUARD_FLAG,
        STATUE_GUARD_KEY,
        GUARD_GLOVE_FLAG,
        STATUE_GUARD_TEMPLAR_FLAG,
        STATUE_GUARD_THERMO_FLAG,
        STATUE_GUARD_TEXT,
        STATUE_GUARD_TALK_FLAG,
        STATUE_GUARD_CDT_FLAG,
        TCLERK_PIERMONT_FLAG,
        TNIC_ENQUIRY_FLAG,
        TODRYK_CLOWN_FLAG,
        TODRYK_EYE_FLAG,
        TODRYK_GEORGE_FLAG,
        TODRYK_OPINION_FLAG,
        TODRYK_PHOTO_FLAG,
        TODRYK_PLANTARD_FLAG,
        TODRYK_ROSSO_FLAG,
        TODRYK_TEXT,
        ULTAR_ARTO_FLAG,
        ULTAR_BALL_FLAG,
        ULTAR_BULL_FLAG,
        ULTAR_BUZZER_FLAG,
        ULTAR_CHALICE_FLAG,
        ULTAR_CLUB_FLAG,
        ULTAR_DOLLARS_FLAG,
        ULTAR_GOODBYE_FLAG,
        ULTAR_HENDERSONS_FLAG,
        ULTAR_KLAUSNER_FLAG,
        ULTAR_LAB_PASS_FLAG,
        ULTAR_LIFTING_KEYS_FLAG,
        ULTAR_LOOM_FLAG,
        ULTAR_NEJO_FLAG,
        ULTAR_PHOTOGRAPH_FLAG,
        ULTAR_PHRASE_FLAG,
        ULTAR_PRESSURE_GAUGE_FLAG,
        ULTAR_RED_NOSE_FLAG,
        ULTAR_SIGN_FLAG,
        ULTAR_STATUETTE_FLAG,
        ULTAR_STATUETTE_PAINT_FLAG,
        ULTAR_TISSUE_FLAG,
        ULTAR_TEMPLAR_FLAG,
        ULTAR_TAXI_FLAG,
        ULTAR_TOILET_BRUSH_FLAG,
        ULTAR_TOILET_CHAIN_FLAG,
        ULTAR_TOILET_KEY_FLAG,
        ULTAR_TOWEL_FLAG,
        ULTAR_PLASTER_FLAG,
        ULTAR_TEXT,
        COUNTESS_56A_SUBJECT_FLAG,
        COUNTESS_56A_GOODBYE_FLAG,
        COUNTESS_56B_GOODBYE_FLAG,
        COUNTESS_TALK_FLAG,
        COUNTESS_CDT_FLAG,
        VAS_BALL_FLAG,
        VAS_COUNTESS_FLAG,
        VAS_GOODBYE_FLAG,
        VAS_KEY_FLAG,
        VAS_PHOTO_FLAG,
        VAS_TALK,
        VAS_TEXT,
        VAS_TEXT_TOGGLE,
        VAS_TEMPLAR_FLAG,
        VAS_CURSE_FLAG,
        VAS_PCHALICE_FLAG,
        GEORGE59A,
        VAIL_TEXT,
        VAIL_TALK_FLAG,
        VAIL_CDT_FLAG,
        WEASEL_CLOWN_FLAG,
        WEASEL_KHAN_FLAG,
        WEASEL_GUIDO_FLAG,
        WEASEL_PLANTARD_FLAG,
        WEASEL_ROSSO_FLAG,
        WEASEL_STOP_FLAG,
        WEASEL_TEXT,
        WORKMAN_CLOWN_FLAG,
        WORKMAN_COP_FLAG,
        WORKMAN_PHONE_ALERT_FLAG,
        WORKMAN_PLANTARD_FLAG,
        WORKMAN_ROSSO_CARD,
        WORKMAN_STOP_FLAG,
        WORKMAN_TOOL_FLAG,
        WORKMAN_TOOLBOX_FLAG,
        WORKMAN_TEXT,
        GEORGE_TALK_FLAG,
        GEORGE_CDT_FLAG,
        CHOOSER_COUNT_FLAG,
        HURRY_FLAG,
        IRELAND_FLAG,
        IRELAND_MAP_FLAG,
        KNOWS_PEAGRAM_FLAG,
        KNOWS_PHILIP_FLAG,
        MANUSCRIPT_FLAG,
        OBJECT_HELD,
        OBJECT_ICON,
        OBJECT_TALK,
        PARIS_FLAG,
        RESPONSERECEIVED,
        SCENE_FLAG,
        SCREEN,
        SCORE_FLAG,
        SCOTLAND_MAP_FLAG,
        SPAIN_MAP_FLAG,
        SYRIA_FLAG,
        TALK_FLAG,
        WEIRD_ZONE,
        TARGET_MEGA,
        CHURCH_ARRIVAL_FLAG,
        SHH_ALERT_FLAG,
        AEROPORT_ADDRESS_FLAG,
        CHANTELLE_BRANDY_FLAG,
        CHURCH_FLAG,
        CHOOSE_GAUGE_FLAG,
        CLERK_AT_DESK_FLAG,
        CONSULTANT_STOP_FLAG,
        COSTUMES_ADDRESS_FLAG,
        COSTUMES_PHONE_FLAG,
        FOUND_WARD_FLAG,
        GEORGE_POS_FLAG,
        GOT_BENOIR_FLAG,
        HOLE_FLAG,
        HOSPITAL_ADDRESS_FLAG,
        HOSPITAL_VISIT_FLAG,
        HOS_POS_FLAG,
        HOTEL_ADDRESS_FLAG,
        IRELAND_ALERT_FLAG,
        KEY_ALERT_FLAG,
        KEYRING_FLAG,
        KEY_TALK,
        KNOWS_MOERLIN_FLAG,
        LENS_FLAG,
        MACDEVITTS_PHONE_FLAG,
        MANUSCRIPT_ALERT_FLAG,
        MANUSCRIPT_VIEW_FLAG,
        MEETING_FLAG,
        MESSAGE_FLAG,
        MONTFACN_ADDRESS_FLAG,
        MONTFAUCON_CONTROL_FLAG,
        MUSEUM_ADDRESS_FLAG,
        MUSEUM_CLOSING_FLAG,
        MUSEUM_PHONE_FLAG,
        NERVAL_ADDRESS_FLAG,
        NICO_ADDRESS_FLAG,
        NICO_APT_FLAG,
        NICO_DOOR_FLAG,
        NICO_GONE_HOME_FLAG,
        NICO_PHONE_FLAG,
        NICO_VISIT_FLAG,
        NURSE_TELEPHONE_FLAG,
        PAINT_TALK,
        PAINTPOT_FLAG,
        PARIS_STATUE_FLAG,
        PHONE_CHECK,
        PHONE_REQUEST,
        POLICE_ADDRESS_FLAG,
        POLICE_PHONE_FLAG,
        POLISHER_PLUG_FLAG,
        POS_FLAG,
        RADIO_ALERT_FLAG,
        READ_NEWSPAPER,
        READ_NOSE_FLAG,
        SARCOPHAGUS_ALERT_FLAG,
        SC28_COIN_FLAG,
        SC28_POTTERY_FLAG,
        SC48_SCROLL_FLAG,
        SEEN_BRIEFCASE_FLAG,
        SEEN_DOOR22_FLAG,
        SEEN_KEY_FLAG,
        SEEN_MANHOLE_FLAG,
        SEEN_PLANTARD_FLAG,
        SEEN_REGISTER_FLAG,
        SEEN_SEWERS_FLAG,
        SEEN_TRIPOD_FLAG,
        SEWER_EXIT_FLAG,
        SKIP_TALK,
        SOAP_FLAG,
        ERIC_TEXT,
        TAILOR_PHONE_FLAG,
        THERMO_FLAG,
        TOILET_TALK,
        TOMB_FLAG,
        TORCH_ALERT_FLAG,
        TOTEM_ALERT_FLAG,
        TRIPOD_FLAG,
        TRIPOD_ALERT_FLAG,
        TRIPOD_STOLEN_FLAG,
        WARD_STOP_FLAG,
        WHITE_COAT_FLAG,
        WINDOW_ALERT_FLAG,
        WORKMAN_GONE_FLAG,
        CLIMBING_CART_FLAG,
        FIDDLER_TEXT,
        PEAGRAM_GONE_FLAG,
        PINT_FLAG,
        PUB_ELEC_FLAG,
        PUB_INTERRUPTION_FLAG,
        PUB_TAP_FLAG,
        SEEN_GOAT_FLAG,
        SYRIA_BOOK_FLAG,
        SEEN_BRUSH_FLAG,
        SEEN_STATUE_FLAG,
        SYRIA_DEAD_FLAG,
        SYRIA_NICHE_FLAG,
        ARMOR_HIDE_FLAG,
        CANDLE59_FLAG,
        CANDLE_BURNT,
        CHALICE_FLAG,
        CHESSET_FLAG,
        CHESSBOARD_FLAG,
        DOOR_REVEALED,
        DOWSE_FLAG,
        GEORGE_POSITION,
        GEORGE_SAFE,
        GEORGE_WELL_FLAG,
        HAZEL_FLAG,
        INTRO_FLAG,
        LION_FANG,
        LOGS_56_FLAG,
        MARY_FLAG,
        MIRROR_HINT,
        ROCKFALL_1,
        ROCKFALL_2,
        SECOND_CURSE_FLAG,
        SPAIN_CODA,
        TOMBS59_FLAG,
        ASSASSIN_KILLED_FLAG,
        AXE_ALERT_FLAG,
        DOOR_SC69_ALERT_FLAG,
        DOOR_SC65_FLAG,
        EKLUND_KILLED,
        FINALE_OPTION_FLAG,
        NICO_GONE_FLAG,
        NICO_TIED_FLAG,
        PIPE_ALERT_FLAG,
        SEEN_GUIDO_63,
        END_SCENE,
        MASTER_39_TALK_FLAG,
        MASTER_39_CDT_FLAG,
        COLONEL_TALK_FLAG,
        COLONEL_CDT_FLAG,
        EXEC_TALK_FLAG,
        EXEC_CDT_FLAG,
        CIVIL_TALK_FLAG,
        CIVIL_CDT_FLAG,
        LATVIAN_39_TALK_FLAG,
        LATVIAN_39_CDT_FLAG,
        EKLUND_39_TALK_FLAG,
        EKLUND_39_CDT_FLAG,
        CAFE_BOMBED,
        BLIND_ALLEY,
        CAFE_INTERIOR,
        ROAD_WORKS,
        COURT_YARD,
        SEWER_ONE,
        SEWER_TWO,
        CAFE_REPAIRED,
        APT_STREET,
        APT_NICO,
        COSTUME_SHOP,
        HOTEL_STREET,
        HOTEL_DESK,
        HOTEL_CORRIDOR,
        HOTEL_EMPTY,
        HOTEL_LEDGE,
        HOTEL_ASSASSIN,
        GENDARMERIE,
        IRELAND_STREET,
        MACDEVITTS,
        PUB_CELLAR,
        CASTLE_GATE,
        CASTLE_HAY_TOP,
        CASTLE_YARD,
        CASTLE_DIG,
        CELLAR_DARK,
        MUSEUM_STREET,
        MUSEUM_ONE,
        MUSEUM_TWO,
        MUSEUM_HIDING,
        HOSPITAL_STREET,
        HOSPITAL_DESK,
        HOSPITAL_CORRIDOR,
        HOSPITAL_WARD,
        HOSPITAL_JACQUES,
        MONTFAUCON,
        CATACOMB_SEWER,
        CATACOMB_ROOM,
        CATACOMB_MEETING,
        EXCAVATION_EXT,
        EXCAVATION_LOBBY,
        EXCAVATION_DIG,
        EXCAVATION_TOILET,
        EXCAVATION_SECRET,
        TEMPLAR_CHURCH,
        SYRIA_STALL,
        SYRIA_CARPET,
        SYRIA_CLUB,
        SYRIA_TOILET,
        BULL_CLIFF,
        BULL_INTERIOR,
        MAUSOLEUM_EXT,
        SPAIN_DRIVE,
        SPAIN_GARDEN,
        MAUSOLEUM_INT,
        SPAIN_RECEPTION,
        SPAIN_WELL,
        SPAIN_SECRET,
        TRAIN_ONE,
        TRAIN_TWO,
        COMPT_ONE,
        COMPT_TWO,
        COMPT_THREE,
        COMPT_FOUR,
        TRAIN_GUARD,
        CHURCHYARD,
        CHURCH_TOWER,
        CRYPT,
        SECRET_CRYPT,
        POCKET_1,
        POCKET_2,
        POCKET_3,
        POCKET_4,
        POCKET_5,
        POCKET_6,
        POCKET_7,
        POCKET_8,
        POCKET_9,
        POCKET_10,
        POCKET_11,
        POCKET_12,
        POCKET_13,
        POCKET_14,
        POCKET_15,
        POCKET_16,
        POCKET_17,
        POCKET_18,
        POCKET_19,
        POCKET_20,
        POCKET_21,
        POCKET_22,
        POCKET_23,
        POCKET_24,
        POCKET_25,
        POCKET_26,
        POCKET_27,
        POCKET_28,
        POCKET_29,
        POCKET_30,
        POCKET_31,
        POCKET_32,
        POCKET_33,
        POCKET_34,
        POCKET_35,
        POCKET_36,
        POCKET_37,
        POCKET_38,
        POCKET_39,
        POCKET_40,
        POCKET_41,
        POCKET_42,
        POCKET_43,
        POCKET_44,
        POCKET_45,
        POCKET_46,
        POCKET_47,
        POCKET_48,
        POCKET_49,
        POCKET_50,
        POCKET_51,
        POCKET_52
    };

    enum HelperScripts
    {
        HELP_IRELAND = 0,
        HELP_SYRIA,
        HELP_SPAIN,
        HELP_NIGHTTRAIN,
        HELP_SCOTLAND,
        HELP_WHITECOAT,
        HELP_SPAIN2
    }

    internal class Logic
    {
        const int NON_ZERO_SCRIPT_VARS = 95;
        const int NUM_SCRIPT_VARS = 1179;

        const int SCRIPT_CONT = 1;
        const int SCRIPT_STOP = 0;

        public const int SAM = 2162689;
        public const int PLAYER = 8388608;
        public const int GEORGE = 8388608;
        public const int NICO = 8454144;
        public const int BENOIR = 8585216;
        public const int ROSSO = 8716288;
        public const int DUANE = 8781824;
        public const int MOUE = 9502720;
        public const int ALBERT = 9568256;

        public const int SAND_25 = 1638407;
        public const int HOLDING_REPLICA_25 = 1638408;
        public const int GMASTER_79 = 5177345;
        public const int SCR_std_off = (0 * 0x10000 + 6);
        public const int SCR_exit0 = (0 * 0x10000 + 7);
        public const int SCR_exit1 = (0 * 0x10000 + 8);
        public const int SCR_exit2 = (0 * 0x10000 + 9);
        public const int SCR_exit3 = (0 * 0x10000 + 10);
        public const int SCR_exit4 = (0 * 0x10000 + 11);
        public const int SCR_exit5 = (0 * 0x10000 + 12);
        public const int SCR_exit6 = (0 * 0x10000 + 13);
        public const int SCR_exit7 = (0 * 0x10000 + 14);
        public const int SCR_exit8 = (0 * 0x10000 + 15);
        public const int SCR_exit9 = (0 * 0x10000 + 16);
        public const int LEFT_SCROLL_POINTER = 8388610;
        public const int RIGHT_SCROLL_POINTER = 8388611;
        public const int FLOOR_63 = 4128768;
        public const int ROOF_63 = 4128779;
        public const int GUARD_ROOF_63 = 4128781;
        public const int LEFT_TREE_POINTER_71 = 4653058;
        public const int RIGHT_TREE_POINTER_71 = 4653059;
        public const int SCR_menu_look = (0 * 0x10000 + 24);
        public const int SCR_icon_combine_script = (0 * 0x10000 + 25);

        public const int STAT_MOUSE = 1;
        public const int STAT_LOGIC = 2;
        public const int STAT_EVENTS = 4;
        public const int STAT_FORE = 8;
        public const int STAT_BACK = 16;
        public const int STAT_SORT = 32;
        public const int STAT_SHRINK = 64;
        public const int STAT_BOOKMARK = 128;
        public const int STAT_TALK_WAIT = 256;
        public const int STAT_OVERRIDE = 512;

        private SwordEngine _vm;
        private IMixer _mixer;
        public static uint[] ScriptVars = new uint[NUM_SCRIPT_VARS];

        private static readonly uint[,] _scriptVarInit = new uint[NON_ZERO_SCRIPT_VARS, 2]
        {
            {42, 448}, {43, 378}, {51, 1}, {92, 1}, {147, 71}, {201, 1},
            {209, 1}, {215, 1}, {242, 2}, {244, 1}, {246, 3}, {247, 1},
            {253, 1}, {297, 1}, {398, 1}, {508, 1}, {605, 1}, {606, 1},
            {701, 1}, {709, 1}, {773, 1}, {843, 1}, {907, 1}, {923, 1},
            {966, 1}, {988, 2}, {1058, 1}, {1059, 2}, {1060, 3}, {1061, 4},
            {1062, 5}, {1063, 6}, {1064, 7}, {1065, 8}, {1066, 9}, {1067, 10},
            {1068, 11}, {1069, 12}, {1070, 13}, {1071, 14}, {1072, 15}, {1073, 16},
            {1074, 17}, {1075, 18}, {1076, 19}, {1077, 20}, {1078, 21}, {1079, 22},
            {1080, 23}, {1081, 24}, {1082, 25}, {1083, 26}, {1084, 27}, {1085, 28},
            {1086, 29}, {1087, 30}, {1088, 31}, {1089, 32}, {1090, 33}, {1091, 34},
            {1092, 35}, {1093, 36}, {1094, 37}, {1095, 38}, {1096, 39}, {1097, 40},
            {1098, 41}, {1099, 42}, {1100, 43}, {1101, 44}, {1102, 48}, {1103, 45},
            {1104, 47}, {1105, 49}, {1106, 50}, {1107, 52}, {1108, 54}, {1109, 56},
            {1110, 57}, {1111, 58}, {1112, 59}, {1113, 60}, {1114, 61}, {1115, 62},
            {1116, 63}, {1117, 64}, {1118, 65}, {1119, 66}, {1120, 67}, {1121, 68},
            {1122, 69}, {1123, 71}, {1124, 72}, {1125, 73}, {1126, 74}
        };

        private bool _textRunning;
        private bool _speechRunning;
        private bool _speechFinished;

        private readonly byte[][] _startData =
        {
            StaticRes.g_startPos0.ToArray(),
            StaticRes.g_startPos1.ToArray()
        };

        private ObjectMan _objMan;
        private Music _music;

        public Logic(SwordEngine vm, ObjectMan objectMan, Music music, IMixer mixer, Sound sound)
        {
            _vm = vm;
            _objMan = objectMan;
            _music = music;
            _mixer = mixer;

            // TODO:
        }

        public void Initialize()
        {
            Array.Clear(ScriptVars, 0, NUM_SCRIPT_VARS);
            for (byte cnt = 0; cnt < NON_ZERO_SCRIPT_VARS; cnt++)
                ScriptVars[_scriptVarInit[cnt, 0]] = _scriptVarInit[cnt, 1];
            if (SystemVars.IsDemo)
                ScriptVars[(int)ScriptVariableNames.PLAYINGDEMO] = 1;

            // TODO:
            //_eventMan = new EventManager();
            //_textMan = new Text(_objMan, _resMan, SystemVars.Language == Language.BS1_CZECH);
            //_screen.UseTextManager(_textMan);
            _textRunning = _speechRunning = false;
            _speechFinished = true;
        }

        public void StartPositions(int pos)
        {
            bool spainVisit2 = false;
            if ((pos >= 956) && (pos <= 962))
            {
                spainVisit2 = true;
                pos -= 900;
            }
            if ((pos > 80) || (_startData[pos] == null))
                throw new InvalidOperationException($"Starting in Section {pos} is not supported");

            ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE] = StaticRes.STAND;
            ScriptVars[(int)ScriptVariableNames.GEORGE_CDT_FLAG] = Sword1Res.GEO_TLK_TABLE;

            RunStartScript(_startData[pos]);
            if (spainVisit2)
                RunStartScript(_helperData[(int)HelperScripts.HELP_SPAIN2]);

            if (pos == 0)
                pos = 1;
            var compact = _objMan.FetchObject(PLAYER);
            FnEnterSection(compact, PLAYER, pos, 0, 0, 0, 0, 0);    // (automatically opens the compact resource for that section)
            SystemVars.ControlPanelMode = ControlPanelMode.CP_NORMAL;
            SystemVars.WantFade = true;
        }

        private int FnEnterSection(SwordObject cpt, int id, int screen, int d, int e, int f, int z, int x)
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

        void RunStartScript(byte[] data)
        {
            // Here data is a static resource defined in staticres.cpp
            // It is always in little endian
            ushort varId = 0;
            byte fnId = 0;
            uint param1 = 0;
            int i = 0;
            while (data[i] != (int)StartPosOpcodes.opcSeqEnd)
            {
                switch ((StartPosOpcodes)data[i++])
                {
                    case StartPosOpcodes.opcCallFn:
                        fnId = data[i++];
                        param1 = data[i++];
                        StartPosCallFn(fnId, param1, 0, 0);
                        break;
                    case StartPosOpcodes.opcCallFnLong:
                        fnId = data[i++];
                        StartPosCallFn(fnId, data.ToUInt32(i), data.ToUInt32(i + 4), data.ToUInt32(i + 8));
                        i += 12;
                        break;
                    case StartPosOpcodes.opcSetVar8:
                        varId = data.ToUInt16(i);
                        ScriptVars[varId] = data[2];
                        i += 3;
                        break;
                    case StartPosOpcodes.opcSetVar16:
                        varId = data.ToUInt16(i);
                        ScriptVars[varId] = data.ToUInt32(i + 2);
                        i += 4;
                        break;
                    case StartPosOpcodes.opcSetVar32:
                        varId = data.ToUInt16(i);
                        ScriptVars[varId] = data.ToUInt32(i + 2);
                        i += 6;
                        break;
                    case StartPosOpcodes.opcGeorge:
                        ScriptVars[(int)ScriptVariableNames.CHANGE_X] = data.ToUInt16(i + 0);
                        ScriptVars[(int)ScriptVariableNames.CHANGE_Y] = data.ToUInt16(i + 2);
                        ScriptVars[(int)ScriptVariableNames.CHANGE_DIR] = data[4];
                        ScriptVars[(int)ScriptVariableNames.CHANGE_PLACE] = data.ToUInt24(i + 5);
                        i += 8;
                        break;
                    case StartPosOpcodes.opcRunStart:
                        data = _startData[data[i]];
                        break;
                    case StartPosOpcodes.opcRunHelper:
                        data = _helperData[data[i]];
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected opcode in StartScript");
                }
            }
        }

        private void StartPosCallFn(byte fnId, uint param1, uint param2, uint param3)
        {
            {
                //Object obj = NULL;
                switch ((StartPosOpcodes)fnId)
                {
                    case StartPosOpcodes.opcPlaySequence:
                        FnPlaySequence(null, 0, (int)param1, 0, 0, 0, 0, 0);
                        break;
                    //case StartPosOpcodes.opcAddObject:
                    //    FnAddObject(null, 0, param1, 0, 0, 0, 0, 0);
                    //    break;
                    //case StartPosOpcodes.opcRemoveObject:
                    //    FnRemoveObject(null, 0, param1, 0, 0, 0, 0, 0);
                    //    break;
                    //case StartPosOpcodes.opcMegaSet:
                    //    obj = _objMan.fetchObject(param1);
                    //    FnMegaSet(obj, param1, param2, param3, 0, 0, 0, 0);
                    //    break;
                    //case StartPosOpcodes.opcNoSprite:
                    //    obj = _objMan.fetchObject(param1);
                    //    FnNoSprite(obj, param1, param2, param3, 0, 0, 0, 0);
                    //    break;
                    default:
                        throw new InvalidOperationException($"Illegal fnCallfn argument {fnId}");
                }
            }
        }

        int FnPlaySequence(object cpt, int id, int sequenceId, int d, int e, int f, int z, int x)
        {
            // A cutscene usually (always?) means the room will change. In the
            // meantime, we don't want any looping sound effects still playing.
            // TODO:
            //_sound.quitScreen();

            var player = new MoviePlayer(_vm); //makeMoviePlayer(sequenceId, _vm, _textMan, _resMan, _system);
            //_screen.ClearScreen();
            player.Load(sequenceId);
            player.Play();
            return SCRIPT_CONT;
        }

        public void Engine()
        {
            throw new NotImplementedException();
            // TODO: debug(8, "\n\nNext logic cycle");
            //_eventMan.serviceGlobalEventList();

            //for (ushort sectCnt = 0; sectCnt < TOTAL_SECTIONS; sectCnt++)
            //{
            //    if (_objMan.SectionAlive(sectCnt))
            //    {
            //        uint numCpts = _objMan.fetchNoObjects(sectCnt);
            //        for (uint cptCnt = 0; cptCnt < numCpts; cptCnt++)
            //        {
            //            uint currentId = sectCnt * ITM_PER_SEC + cptCnt;
            //            Object compact = _objMan.FetchObject(currentId);

            //            if (compact.status & STAT_LOGIC)
            //            { // does the object want to be processed?
            //                if (compact.status & STAT_EVENTS)
            //                {
            //                    //subscribed to the global-event-switcher? and in logic mode
            //                    switch (compact.logic)
            //                    {
            //                        case LOGIC_pause_for_event:
            //                        case LOGIC_idle:
            //                        case LOGIC_AR_animate:
            //                            _eventMan.checkForEvent(compact);
            //                            break;
            //                    }
            //                }
            //                // TODO: debug(7, "Logic::engine: handling compact %d (%X)", currentId, currentId);
            //                ProcessLogic(compact, currentId);
            //                compact.sync = 0; // syncs are only available for 1 cycle.
            //            }

            //            if ((uint)compact.screen == _scriptVars[SCREEN])
            //            {
            //                if (compact.status & STAT_FORE)
            //                    _screen.addToGraphicList(0, currentId);
            //                if (compact.status & STAT_SORT)
            //                    _screen.addToGraphicList(1, currentId);
            //                if (compact.status & STAT_BACK)
            //                    _screen.addToGraphicList(2, currentId);

            //                if (compact.status & STAT_MOUSE)
            //                    _mouse.addToList(currentId, compact);
            //            }
            //        }
            //    }
            //}
        }

        public void UpdateScreenParams()
        {
            throw new NotImplementedException();
        }

        public void NewScreen(uint screen)
        {
            var compact = (SwordObject)_objMan.FetchObject(PLAYER);

            // work around script bug #911508
            if (((screen == 25) || (ScriptVars[(int)ScriptVariableNames.SCREEN] == 25)) && (ScriptVars[(int)ScriptVariableNames.SAND_FLAG] == 4))
            {
                var cpt = _objMan.FetchObject(Logic.SAND_25);
                var george = _objMan.FetchObject(PLAYER);
                if (george.place == HOLDING_REPLICA_25) // is george holding the replica in his hands?
                    FnFullSetFrame(cpt, SAND_25, Sword1Res.IMPFLRCDT, Sword1Res.IMPFLR, 0, 0, 0, 0); // empty impression in floor
                else
                    FnFullSetFrame(cpt, SAND_25, Sword1Res.IMPPLSCDT, Sword1Res.IMPPLS, 0, 0, 0, 0); // impression filled with plaster
            }

            // work around, at screen 69 in psx version TOP menu gets stuck at disabled, fix it at next screen (71)
            if ((screen == 71) && (SystemVars.Platform == Platform.PSX))
                ScriptVars[(int)ScriptVariableNames.TOP_MENU_DISABLED] = 0;

            if (SystemVars.JustRestoredGame != 0)
            { // if we've just restored a game - we want George to be exactly as saved
                FnAddHuman(null, 0, 0, 0, 0, 0, 0, 0);
                if (ScriptVars[(int)ScriptVariableNames.GEORGE_WALKING] != 0)
                { // except that if George was walking when we saveed the game
                    FnStandAt(compact, PLAYER, (int)ScriptVars[(int)ScriptVariableNames.CHANGE_X], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_Y], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_DIR], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE], 0, 0);
                    FnIdle(compact, PLAYER, 0, 0, 0, 0, 0, 0);
                    ScriptVars[(int)ScriptVariableNames.GEORGE_WALKING] = 0;
                }
                SystemVars.JustRestoredGame = 0;
                _music.StartMusic(ScriptVars[(int)ScriptVariableNames.CURRENT_MUSIC], 1);
            }
            else
            { // if we haven't just restored a game, set George to stand, etc
                compact.screen = (int)ScriptVars[(int)ScriptVariableNames.NEW_SCREEN]; //move the mega/player at this point between screens
                FnStandAt(compact, PLAYER, (int)ScriptVars[(int)ScriptVariableNames.CHANGE_X], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_Y], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_DIR], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE], 0, 0);
                FnChangeFloor(compact, PLAYER, ScriptVars[(int)ScriptVariableNames.CHANGE_PLACE], 0, 0, 0, 0, 0);
            }
        }

        private int FnChangeFloor(SwordObject cpt, int id, uint floor, int i, int i1, int i2, int i3, int i4)
        {
            cpt.place = (int)floor;
            var floorCpt = _objMan.FetchObject(floor);
            cpt.scale_a = floorCpt.scale_a;
            cpt.scale_b = floorCpt.scale_b;
            return SCRIPT_CONT;
        }

        private void FnIdle(SwordObject compact, int player, int i, int i1, int i2, int i3, int i4, int i5)
        {
            throw new NotImplementedException();
        }

        private int FnStandAt(SwordObject cpt, int id, int x, int y, int dir, int stance, int a, int b)
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
            return FnStand(cpt, id, dir, stance, 0, 0, 0, 0);
        }


        private int FnStand(SwordObject cpt, int id, int dir, int stance, int c, int d, int a, int b)
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

        private void FnAddHuman(object o, int i, int i1, int i2, int i3, int i4, int i5, int i6)
        {
            throw new NotImplementedException();
        }

        private void FnFullSetFrame(SwordObject cpt, int sand25, int impflrcdt, int impflr, int p4, int p5, int p6, int p7)
        {
            throw new NotImplementedException();
        }

        static readonly byte[][] _helperData = {
            StaticRes.g_genIreland.ToArray(),
            StaticRes.g_genSyria.ToArray(),
            StaticRes.g_genSpain.ToArray(),
            StaticRes.g_genNightTrain.ToArray(),
            StaticRes.g_genScotland.ToArray(),
            StaticRes.g_genWhiteCoat.ToArray(),
            StaticRes.g_genSpain.ToArray()
        };
    }

    class ArrayInt
    {
        private byte[] _data;
        private int _offset;
        private int _length;

        public int this[int index]
        {
            get { return _data.ToInt32(_offset + index << 2); }
            set { _data.WriteUInt32(_offset + index << 2, (uint)value); }
        }

        public ArrayInt(byte[] data, int offset, int length)
        {
            _data = data;
            _offset = offset;
            _length = length;
        }
    }

    class ScriptTree
    {         //this is a logic tree, used by OBJECTs
        const int TOTAL_script_levels = 5;

        //logic level
        public int script_level
        {
            get { return _data.ToInt32(_offset); }
            set { _data.WriteUInt32(_offset, (uint)value); }
        }
        public ArrayInt script_id { get; private set; }   //script id's (are unique to each level)
        public ArrayInt script_pc { get; private set; }   //pc of script for each (if script_manager)

        private byte[] _data;
        private int _offset;

        public ScriptTree(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;
            script_id = new ArrayInt(data, offset + 4, TOTAL_script_levels);
            script_pc = new ArrayInt(data, offset + 4 + 4 * TOTAL_script_levels, TOTAL_script_levels);
        }
    }

    class TalkOffset
    {
        public int x;
        public int y;
    }

    struct OEventSlot
    {         //receiving event list in the compact -
        public int o_event;        //array of these with O_TOTAL_EVENTS elements
        public int o_event_script;
    }

    class WalkData
    {
        public int frame;
        public int x;
        public int y;
        public int step;
        public int dir;
    }


    internal class SwordObject
    {
        const int O_TOTAL_EVENTS = 5;
        const int O_WALKANIM_SIZE = 600;         //max number of nodes in router output

        /// <summary>
        /// 0 broad description of type - object, floor, etc.
        /// </summary>
        public int type
        {
            get { return _data.ToInt32(_offset); }
            set { _data.WriteUInt32(_offset, (uint)value); }
        }

        // 4  bit flags for logic, graphics, mouse, etc.                
        public int status
        {
            get { return _data.ToInt32(_offset + 4); }
            set { _data.WriteUInt32(_offset + 4, (uint)value); }
        }
        // 8  logic type         
        public int logic
        {
            get { return _data.ToInt32(_offset + 8); }
            set { _data.WriteUInt32(_offset + 8, (uint)value); }
        }
        // 12 where is the mega character            
        public int place
        {
            get { return _data.ToInt32(_offset + 12); }
            set { _data.WriteUInt32(_offset + 12, (uint)value); }
        }
        public int down_flag
        {
            get { return _data.ToInt32(_offset + 16); }
            set { _data.WriteUInt32(_offset + 16, (uint)value); }
        }                // 16 pass back down with this - with C possibly both are unnecessary?
        public int target
        {
            get { return _data.ToInt32(_offset + 20); }
            set { _data.WriteUInt32(_offset + 20, (uint)value); }
        }                   // 20 target object for the GTM         *these are linked to script
        public int screen
        {
            get { return _data.ToInt32(_offset + 24); }
            set { _data.WriteUInt32(_offset + 24, (uint)value); }
        }                   // 24 physical screen/section
        public int frame
        {
            get { return _data.ToInt32(_offset + 28); }
            set { _data.WriteUInt32(_offset + 28, (uint)value); }
        }                    // 28 frame number &
        public int resource
        {
            get { return _data.ToInt32(_offset + 32); }
            set { _data.WriteUInt32(_offset + 32, (uint)value); }
        }                 // 32 id of spr file it comes from
        public int sync
        {
            get { return _data.ToInt32(_offset + 36); }
            set { _data.WriteUInt32(_offset + 36, (uint)value); }
        }                     // 36 receive sync here
        public int pause
        {
            get { return _data.ToInt32(_offset + 40); }
            set { _data.WriteUInt32(_offset + 40, (uint)value); }
        }                    // 40 logic_engine() pauses these cycles
        public int xcoord
        {
            get { return _data.ToInt32(_offset + 44); }
            set { _data.WriteUInt32(_offset + 44, (uint)value); }
        }                   // 44
        public int ycoord
        {
            get { return _data.ToInt32(_offset + 48); }
            set { _data.WriteUInt32(_offset + 48, (uint)value); }
        }                   // 48
        public int mouse_x1
        {
            get { return _data.ToInt32(_offset + 52); }
            set { _data.WriteUInt32(_offset + 52, (uint)value); }
        }                 // 52 top-left of mouse area is (x1,y1)
        public int mouse_y1
        {
            get { return _data.ToInt32(_offset + 56); }
            set { _data.WriteUInt32(_offset + 56, (uint)value); }
        }                 // 56
        public int mouse_x2
        {
            get { return _data.ToInt32(_offset + 60); }
            set { _data.WriteUInt32(_offset + 60, (uint)value); }
        }                 // 60 bottom-right of area is (x2,y2)   (these coords are inclusive)
        public int mouse_y2
        {
            get { return _data.ToInt32(_offset + 64); }
            set { _data.WriteUInt32(_offset + 64, (uint)value); }
        }                 // 64
        public int priority
        {
            get { return _data.ToInt32(_offset + 68); }
            set { _data.WriteUInt32(_offset + 68, (uint)value); }
        }                 // 68
        public int mouse_on
        {
            get { return _data.ToInt32(_offset + 72); }
            set { _data.WriteUInt32(_offset + 72, (uint)value); }
        }                 // 72
        public int mouse_off
        {
            get { return _data.ToInt32(_offset + 76); }
            set { _data.WriteUInt32(_offset + 76, (uint)value); }
        }                // 76
        public int mouse_click
        {
            get { return _data.ToInt32(_offset + 80); }
            set { _data.WriteUInt32(_offset + 80, (uint)value); }
        }              // 80
        public int interact
        {
            get { return _data.ToInt32(_offset + 84); }
            set { _data.WriteUInt32(_offset + 84, (uint)value); }
        }                 // 84
        public int get_to_script
        {
            get { return _data.ToInt32(_offset + 88); }
            set { _data.WriteUInt32(_offset + 88, (uint)value); }
        }            // 88
        public int scale_a
        {
            get { return _data.ToInt32(_offset + 92); }
            set { _data.WriteUInt32(_offset + 92, (uint)value); }
        }                  // 92 used by floors
        public int scale_b
        {
            get { return _data.ToInt32(_offset + 96); }
            set { _data.WriteUInt32(_offset + 96, (uint)value); }
        }                  // 96
        public int anim_x
        {
            get { return _data.ToInt32(_offset + 100); }
            set { _data.WriteUInt32(_offset + 100, (uint)value); }
        }                   // 100
        public int anim_y
        {
            get { return _data.ToInt32(_offset + 104); }
            set { _data.WriteUInt32(_offset + 104, (uint)value); }
        }                   // 104

        public ScriptTree tree { get; private set; }                // 108  size = 44 bytes
        public ScriptTree bookmark;            // 152  size = 44 bytes

        public int dir
        {
            get { return _data.ToInt32(_offset + 196); }
            set { _data.WriteUInt32(_offset + 196, (uint)value); }
        }                        // 196
        public int speech_pen
        {
            get { return _data.ToInt32(_offset + 200); }
            set { _data.WriteUInt32(_offset + 200, (uint)value); }
        }                 // 200
        public int speech_width
        {
            get { return _data.ToInt32(_offset + 204); }
            set { _data.WriteUInt32(_offset + 204, (uint)value); }
        }               // 204
        public int speech_time
        {
            get { return _data.ToInt32(_offset + 208); }
            set { _data.WriteUInt32(_offset + 208, (uint)value); }
        }                // 208
        public int text_id
        {
            get { return _data.ToInt32(_offset + 212); }
            set { _data.WriteUInt32(_offset + 212, (uint)value); }
        }                    // 212 working back from o_ins1
        public int tag
        {
            get { return _data.ToInt32(_offset + 216); }
            set { _data.WriteUInt32(_offset + 216, (uint)value); }
        }                        // 216
        public int anim_pc
        {
            get { return _data.ToInt32(_offset + 220); }
            set { _data.WriteUInt32(_offset + 220, (uint)value); }
        }                    // 220 position within an animation structure
        public int anim_resource
        {
            get { return _data.ToInt32(_offset + 224); }
            set { _data.WriteUInt32(_offset + 224, (uint)value); }
        }              // 224 cdt or anim table

        public int walk_pc
        {
            get { return _data.ToInt32(_offset + 228); }
            set { _data.WriteUInt32(_offset + 228, (uint)value); }
        }                      // 228

        public TalkOffset[] talk_table = new TalkOffset[6];         // 232  size = 6*8 bytes = 48

        public OEventSlot[] event_list = new OEventSlot[O_TOTAL_EVENTS];    // 280  size = 5*8 bytes = 40

        public int ins1
        {
            get { return _data.ToInt32(_offset + 320); }
            set { _data.WriteUInt32(_offset + 320, (uint)value); }
        }                      // 320
        public int ins2
        {
            get { return _data.ToInt32(_offset + 324); }
            set { _data.WriteUInt32(_offset + 324, (uint)value); }
        }                      // 324
        public int ins3
        {
            get { return _data.ToInt32(_offset + 328); }
            set { _data.WriteUInt32(_offset + 328, (uint)value); }
        }                      // 328

        public int mega_resource
        {
            get { return _data.ToInt32(_offset + 332); }
            set { _data.WriteUInt32(_offset + 332, (uint)value); }
        }                // 332
        public int walk_resource
        {
            get { return _data.ToInt32(_offset + 336); }
            set { _data.WriteUInt32(_offset + 336, (uint)value); }
        }                // 336

        public WalkData[] route = new WalkData[O_WALKANIM_SIZE];   // 340  size = 600*20 bytes = 12000

        private byte[] _data;
        private int _offset;

        public SwordObject(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;
            tree = new ScriptTree(data, offset + 108);
            bookmark = new ScriptTree(data, offset + 152);

            // TODO: talk_table, event_list, route
        }
        // mega size = 12340 bytes (+ 8 byte offset table + 20 byte header = 12368)


    }
}
