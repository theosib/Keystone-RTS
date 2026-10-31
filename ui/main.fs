# Keystone UI (render thread)
#
# Reaches the game only through send_command. Hears from it only through on :state.

set selection []
set drag_start nil
set placing nil
set place_turns 0
set state nil

ui_skin "ui/skin/hearth.fgs"

set_faction_color 1 0.08 0.26 1.0
set_faction_color 2 0.95 0.1 0.08
set_ghost_style 0.35 0.75 1.0 0.15 0.45

# How far the Steward's hands reach
set STEWARD_REACH 6.0
set LAY_MAX_CELLS 32
set BLOCK_TOOLS ["planks" "palisade" "flagstone" "dressed_stone" "gravel_path" "thatch"]

set ribbon_timber {ui.text "" =id "r_timber"}
set ribbon_stone {ui.text "" =id "r_stone"}
set ribbon_food {ui.text "" =id "r_food"}
set ribbon_hands {ui.text "" =id "r_hands"}
set ribbon_army {ui.text "" =id "r_army"}
set hud_hint {ui.text "" =id "hud_hint"}
set hud_fps {ui.text "" =id "hud_fps"}

ui_show {ui.window "##ribbon"
    =window_pos_x 8 =window_pos_y 8
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing]
    =children [
        {ui.text {loc "ui.epoch"}}
        {ui.same_line 0}
        ribbon_timber {ui.same_line 0}
        ribbon_stone {ui.same_line 0}
        ribbon_food {ui.same_line 0}
        ribbon_hands {ui.same_line 0}
        ribbon_army
        hud_hint
        hud_fps
    ]
} =immediate true

# What's under the cursor, or the crosshair when on foot. Details come from the game.
set inspect_title {ui.text "" =id "i_title"}
set inspect_detail {ui.text "" =id "i_detail"}
set inspect_panel nil
set inspect_block ""
set inspect_wait 0

fn hide_inspect [] do
    if (inspect_panel != nil) do
        ui_hide inspect_panel
        set inspect_panel nil
    end
end

fn show_inspect_panel [] do
    if (inspect_panel != nil) { return nil }
    let size {window_size}
    set inspect_panel {ui_show {ui.window "##inspect"
        =window_pos_x (size[0] - 8) =window_pos_y 8 =window_pivot_x 1.0
        =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing :no_inputs]
        =children [inspect_title inspect_detail]
    } =immediate true}
end

# timber_stack_3 is timber_stack: every fill stage shares a name
fn block_family [name] do
    let length {str_length name}
    if ((length > 2) and ({str_substr name (length - 2) 1} == "_")) {
        return {str_substr name 0 (length - 2)}
    }
    return name
end

fn inspect_lines [info] do
    let lines ""
    fn add [line] do
        if (lines == "") { set lines line } { set lines "{lines}\n{line}" }
    end

    if info.stale { return {loc "inspect.remembered"} }
    let building {info.get :building}
    if (building != nil) do
        let name {loc "building.{building}"}
        let built {info.get :built}
        if (built != nil) { add {loc "inspect.site" =name name =percent {floor built}} } { add name }
    end
    let stack {info.get :stack}
    if (stack != nil) {
        add {loc "inspect.stack" =count stack =cap info.stack_cap =kind {loc "kind.{info.stack_kind}"}}
    }
    let gives {info.get :gives}
    if (gives != nil) { add {loc "inspect.gives" =count gives =kind {loc "kind.{info.gives_kind}"}} }
    let owner {info.get :owner}
    if (owner != nil) {
        if (owner == 1) { add {loc "inspect.yours"} } { add {loc "inspect.rival"} }
    }
    return lines
end

fn show_inspect [info] do
    if ({info.get :hidden} == true) do
        hide_inspect
        return nil
    end
    set inspect_title.text {loc "block.{block_family inspect_block}"}
    set inspect_detail.text {inspect_lines info}
    show_inspect_panel
end

fn inspect_target [] do
    if (view_mode == :embodied) { return {pick_view} }
    if ((placing != nil) or {gui_has_mouse}) { return nil }
    let m {mouse_pos}
    return {pick_block m[0] m[1]}
end

fn update_inspect [] do
    set inspect_wait (inspect_wait - 1)
    if (inspect_wait > 0) { return nil }
    set inspect_wait 6

    let hit {inspect_target}
    if (hit == nil) do
        hide_inspect
        return nil
    end
    set inspect_block hit.block
    send_command {=name :inspect =x hit.x =y hit.y =z hit.z}
end

# ---- Work ---------------------------------------------------------------------
# Work anyone can take, unless units are selected: then it's theirs, each as a stack.

set work_title {ui.text "" =id "w_title"}
set work_body {ui.text "" =id "w_body"}
set work_window {ui.window "##work"
    =window_pos_x 0 =window_pos_y 130 =window_pivot_x 1.0
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing :no_inputs]
    =children [work_title {ui.separator} work_body]
}
ui_show work_window =immediate true
set unit_tasks nil
set tasks_wait 0

fn frame_text [frame] do
    let name ""
    let kind ""
    let building {frame.get :building}
    if (building != nil) { set name {loc "building.{building}"} }
    let material {frame.get :kind}
    if (material != nil) { set kind {loc "kind.{material}"} }
    return {loc "work.{frame.key}" =name name =kind kind =percent {frame.get :percent}}
end

fn queue_row_text [row] do
    if ((row.key == "train") or (row.key == "muster")) do
        let line {loc "work.muster" =name ""}
        if (row.key == "train") { set line {loc "work.train" =count row.count} }
        if (row.key == "muster") { set line {loc "work.muster" =name {loc "unit.{row.unit}"}} }
        if row.housed { return line }
        let no_room {loc "work.no_room"}
        return "{line} {no_room}"
    end
    let line {frame_text row}
    let needs {row.get :needs}
    if (needs != nil) do
        let kind {loc "kind.{needs}"}
        let extra {loc "work.needs" =kind kind}
        return "{line} {extra}"
    end
    if (row.hands > 0) do
        let extra {loc "work.hands" =count row.hands}
        return "{line} {extra}"
    end
    return line
end

fn queue_text [rows] do
    if (rows.length == 0) { return {loc "work.nothing"} }
    let text ""
    let number 1
    for row in rows do
        let line "{number}. {queue_row_text row}"
        if (text == "") { set text line } { set text "{text}\n{line}" }
        set number (number + 1)
    end
    return text
end

fn tasks_text [groups] do
    if (groups.length == 0) { return {loc "work.nothing"} }
    let text ""
    for group in groups do
        let head {loc "unit.{group.who}"}
        if (group.count > 1) { set head {loc "work.group" =count group.count =name head} }
        if (text == "") { set text head } { set text "{text}\n{head}" }
        let indent "  "
        for frame in group.stack do
            set text "{text}\n{indent}{frame_text frame}"
            set indent "{indent}  "
        end
    end
    return text
end

fn update_work_panel [] do
    if (selection.length > 0) do
        set tasks_wait (tasks_wait - 1)
        if (tasks_wait <= 0) do
            set tasks_wait 12
            send_command {=name :tasks =units selection}
        end
        if (unit_tasks != nil) do
            set work_title.text {loc "work.title.units"}
            set work_body.text {tasks_text unit_tasks}
        end
    elif (state != nil) do
        set unit_tasks nil
        set tasks_wait 0
        set work_title.text {loc "work.title.queue"}
        set work_body.text {queue_text state.queue}
    end
end

# ---- Blocks -------------------------------------------------------------------
# One choice of block, for two ways of using it. On foot the Steward places it by hand;
# overhead, a drag marks a run of it for Hands to build.

set tool_index 0
set laying false
set lay_start nil
set denied nil
set denied_frames 0
set tool_buttons []

fn tool_name [] { return BLOCK_TOOLS[tool_index] }

fn refresh_tools [] do
    let i 0
    while (i < BLOCK_TOOLS.length) do
        let button tool_buttons[i]
        let tool BLOCK_TOOLS[i]
        let name {loc "block.{tool}"}
        let shown (i + 1)
        let label "{shown} {name}"
        if ((i == tool_index) and (laying or (view_mode == :embodied))) { set label "[{label}]" }
        set button.label label
        set i (i + 1)
    end
end

fn choose_tool [index] do
    set tool_index index
    if (view_mode != :embodied) do
        set laying true
        set placing nil
        set lay_start nil
    end
    refresh_tools
end

fn stop_laying [] do
    set laying false
    set lay_start nil
    refresh_tools
end

fn tool_button [index] do
    let button {ui.button "" =on_click fn [] do
        choose_tool index
    end}
    tool_buttons.push button
    return button
end

fn tool_children [] do
    let children [{ui.text {loc "ui.blocks"}}]
    let i 0
    while (i < BLOCK_TOOLS.length) do
        children.push {ui.same_line 0}
        children.push {tool_button i}
        set i (i + 1)
    end
    return children
end

set tool_window {ui.window "##tools"
    =window_pos_x 0 =window_pos_y 0 =window_pivot_x 0.5 =window_pivot_y 1.0
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing]
    =children {tool_children}
}
ui_show tool_window =immediate true
refresh_tools

fn begin_lay [] do
    let hit {pick_ground mouse_x mouse_y}
    if (hit != nil) { set lay_start [hit.x hit.y hit.z] }
end

fn finish_lay [] do
    if (lay_start == nil) { return nil }
    let hit {pick_ground mouse_x mouse_y}
    if (hit != nil) {
        send_command {=name :lay =block {to_sym {tool_name}} =x0 lay_start[0] =z0 lay_start[2] =x1 hit.x =z1 hit.z}
    }
    set lay_start nil
end

fn draw_laying [] do
    if ((not laying) or (view_mode == :embodied)) { return nil }
    let m {mouse_pos}
    let hit {pick_ground m[0] m[1]}
    if (hit == nil) { return nil }
    let start lay_start
    if (start == nil) { set start [hit.x hit.y hit.z] }
    let dx (hit.x - start[0])
    let dz (hit.z - start[2])
    if ({abs dx} >= {abs dz}) { set dz 0 } { set dx 0 }
    let reach (LAY_MAX_CELLS - 1)
    set dx {clamp dx (0 - reach) reach}
    set dz {clamp dz (0 - reach) reach}
    marker_box {min start[0] (start[0] + dx)} (start[1] + 1) {min start[2] (start[2] + dz)} ({abs dx} + 1) 1 ({abs dz} + 1) 0.35 0.75 1.0 0.28
end

fn steward_place_block [] do
    let hit {pick_view}
    if ((hit == nil) or (hit.distance > STEWARD_REACH)) { return nil }
    let x (hit.x + hit.nx)
    let y (hit.y + hit.ny)
    let z (hit.z + hit.nz)
    if {avatar_overlaps x y z} { return nil }
    send_command {=name :steward_place =x x =y y =z z =block {to_sym {tool_name}}}
end

fn draw_crosshair [] do
    let size {window_size}
    let cx (size[0] / 2)
    let cy (size[1] / 2)
    ui_rect (cx - 9) cy (cx + 9) cy 1.0 0.9 0.7 0.8
    ui_rect cx (cy - 9) cx (cy + 9) 1.0 0.9 0.7 0.8
end

fn start_placing [name] do
    set placing {to_sym name}
    if laying { stop_laying }
end

fn build_button [name] do
    return {ui.button {loc "building.{name}"} =on_click fn [] do
        start_placing name
    end}
end

fn muster_button [name] do
    return {ui.button {loc "unit.{name}"} =on_click fn [] do
        send_command {=name :muster =unit {to_sym name}}
    end}
end

ui_show {ui.window "##palette"
    =window_pos_x 8 =window_pos_y 110
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing]
    =children [
        {ui.button {loc "menu.open"} =on_click fn [] do
            open_menu
        end}
        {ui.text {loc "ui.palette"}}
        {build_button "shelter"}
        {build_button "forager_hut"}
        {build_button "sawpit"}
        {build_button "watchtower"}
        {build_button "palisade_wall"}
        {ui.separator}
        {ui.button {loc "ui.train_hand"} =on_click fn [] do
            send_command {=name :train}
        end}
        {ui.separator}
        {ui.text {loc "ui.muster"}}
        {muster_button "militia"}
        {muster_button "shortbow"}
        {muster_button "scout"}
    ]
} =immediate true

fn begin_drag [] do
    set drag_start [mouse_x mouse_y]
end

fn finish_drag [] do
    if (drag_start == nil) { return nil }
    let picked {pick_entities drag_start[0] drag_start[1] mouse_x mouse_y}
    set selection {picked.filter fn [id] ({entity_faction id} == 1)}
    set drag_start nil
end

fn order_move [] do
    if (selection.length == 0) { return nil }
    let hit {pick_ground mouse_x mouse_y}
    if (hit == nil) { return nil }
    send_command {=name :move =units selection =x hit.x =y (hit.y + 1) =z hit.z}
end

fn commit_placement [] do
    let hit {pick_ground mouse_x mouse_y}
    if (hit == nil) { return nil }
    send_command {=name :build =building placing =x hit.x =z hit.z =turns place_turns}
    set placing nil
end

# Embodied, the Steward works with their own hands
fn steward_click [] do
    let hit {pick_view}
    if ((hit == nil) or (hit.distance > STEWARD_REACH)) { return nil }
    send_command {=name :steward_break =x hit.x =y hit.y =z hit.z}
end

on :mouse do
    if (view_mode == :embodied) do
        if ((mouse_button == :left) and (mouse_event == :down)) { steward_click }
        if ((mouse_button == :right) and (mouse_event == :down)) { steward_place_block }
        return nil
    end
    if {gui_has_mouse} { return nil }
    if laying do
        if (mouse_button == :left) do
            if (mouse_event == :down) { begin_lay } { finish_lay }
        elif ((mouse_button == :right) and (mouse_event == :down)) do
            stop_laying
        end
        return nil
    end
    if (placing != nil) do
        if (mouse_event == :down) do
            if (mouse_button == :left) { commit_placement }
            if (mouse_button == :right) { set placing nil }
        end
        return nil
    end
    if (mouse_button == :left) do
        if (mouse_event == :down) { begin_drag } { finish_drag }
    elif ((mouse_button == :right) and (mouse_event == :down)) do
        order_move
    end
end

set menu_panel nil

fn close_menu [] do
    if (menu_panel != nil) do
        ui_hide menu_panel
        set menu_panel nil
    end
end

fn show_menu_window [children] do
    close_menu
    let size {window_size}
    set menu_panel {ui_show {ui.window "##menu"
        =window_pos_x ((size[0] / 2) - 120) =window_pos_y ((size[1] / 2) - 110)
        =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings]
        =children children
    } =immediate true}
end

fn open_new_game_confirm [] do
    show_menu_window [
        {ui.text {loc "menu.new_confirm"}}
        {ui.button {loc "menu.new_yes"} =on_click fn [] do
            new_game
        end}
        {ui.button {loc "menu.cancel"} =on_click fn [] do
            open_menu
        end}
    ]
end

fn open_menu [] do
    show_menu_window [
        {ui.button {loc "menu.resume"} =on_click fn [] do
            close_menu
        end}
        {ui.button {loc "menu.new"} =on_click fn [] do
            open_new_game_confirm
        end}
        {ui.button {loc "menu.quit"} =on_click fn [] do
            quit_game
        end}
    ]
end

# DESIGN 7.2: when a match ends, the world is still yours
set end_screen nil
set pacify_rivals false

fn show_end_screen [won] do
    let size {window_size}
    let title {loc "match.defeat"}
    let detail {loc "match.defeat.detail"}
    if won do
        set title {loc "match.victory"}
        set detail {loc "match.victory.detail"}
    end
    set end_screen {ui_show {ui.window "##end"
        =window_pos_x (size[0] / 2) =window_pos_y (size[1] / 2)
        =window_pivot_x 0.5 =window_pivot_y 0.5
        =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings]
        =children [
            {ui.text title}
            {ui.text detail}
            {ui.separator}
            {ui.checkbox {loc "match.pacify"} false =on_change fn [value] do
                set pacify_rivals value
            end}
            {ui.button {loc "match.continue"} =on_click fn [] do
                send_command {=name :continue =pacify pacify_rivals}
                ui_hide end_screen
            end}
            {ui.button {loc "match.exit"} =on_click fn [] do
                quit_game
            end}
        ]
    }}
end

on :state do
    let inspected {msg.get :inspect}
    if (inspected != nil) do
        show_inspect inspected
        return nil
    end
    let tasks {msg.get :tasks}
    if (tasks != nil) do
        set unit_tasks tasks
        return nil
    end
    let refused {msg.get :denied}
    if (refused != nil) do
        set denied refused
        set denied_frames 120
        return nil
    end
    if ((state == nil) and (not resumed)) {cam_focus msg.home[0] msg.home[1]}
    if ((msg.match == :over) and (end_screen == nil)) {
        show_end_screen (msg.winner == 1)
    }
    set state msg
end

on :action do
    match action
        :screenshot { request_screenshot "screenshots/keystone.png" }
        :turn { set place_turns ((place_turns + 1) % 4) }
        :cancel do
            set placing nil
            stop_laying
            close_menu
        end
        :tool_1 { choose_tool 0 }
        :tool_2 { choose_tool 1 }
        :tool_3 { choose_tool 2 }
        :tool_4 { choose_tool 3 }
        :tool_5 { choose_tool 4 }
        :tool_6 { choose_tool 5 }
        :quit { quit_game }
        _ { nil }
    end
end

fn draw_placement [] do
    if (placing == nil) { return nil }
    let m {mouse_pos}
    let hit {pick_block m[0] m[1]}
    if (hit == nil) { return nil }
    let size {blueprint_size placing}
    if (size == nil) { return nil }
    let sx size[0]
    let sz size[2]
    if ((place_turns % 2) == 1) do
        set sx size[2]
        set sz size[0]
    end
    marker_box (hit.x - (sx / 2)) (hit.y + 1) (hit.z - (sz / 2)) sx size[1] sz 0.35 0.75 1.0 0.28
end

fn draw_drag_box [a b] do
    ui_rect a[0] a[1] b[0] b[1] 0.4 0.8 1.0 0.9
end

set tools_view :tactical

on :frame do
    if (state != nil) do
        set ribbon_timber.text {loc "ui.stock.timber" =count state.stock.timber}
        set ribbon_stone.text {loc "ui.stock.stone" =count state.stock.stone}
        set ribbon_food.text {loc "ui.stock.provisions" =count state.stock.provisions}
        set ribbon_hands.text {loc "ui.stock.hands" =count state.hands =cap state.housing}
        set ribbon_army.text {loc "ui.army" =count state.army}
    end

    if (denied_frames > 0) { set denied_frames (denied_frames - 1) }

    if ((state != nil) and (state.capture > 0)) do
        set hud_hint.text {loc "ui.keystone_danger"}
    elif (denied_frames > 0) do
        set hud_hint.text {loc "ui.denied" =kind {loc "kind.{denied}"}}
    elif (view_mode == :embodied) do
        set hud_hint.text {loc "ui.hint.embodied"}
    elif laying do
        set hud_hint.text {loc "ui.hint.lay"}
    elif (placing != nil) do
        set hud_hint.text {loc "ui.hint.place"}
    elif (selection.length > 0) do
        set hud_hint.text {loc "ui.selected" =count selection.length}
    else do
        set hud_hint.text {loc "ui.hint.select"}
    end
    set hud_fps.text {loc "ui.fps" =fps {fps}}

    let size {window_size}
    set work_window.window_pos_x (size[0] - 8)
    set tool_window.window_pos_x (size[0] / 2)
    set tool_window.window_pos_y (size[1] - 8)

    update_inspect
    update_work_panel
    if (view_mode != tools_view) do
        set tools_view view_mode
        refresh_tools
    end
    if (view_mode == :embodied) { draw_crosshair }

    for id in selection do
        let p {entity_pos id}
        if (p != nil) {marker_ring p[0] (p[1] + 0.05) p[2] 0.6 0.3 0.9 1.0 0.9}
    end

    # Rubber band while dragging
    if (drag_start != nil) do
        let m {mouse_pos}
        draw_drag_box drag_start m
    end

    draw_placement
    draw_laying
end
