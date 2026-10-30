# Keystone UI (render thread)
#
# Reaches the game only through send_command. Hears from it only through on :state.

set selection []
set drag_start nil
set placing nil
set place_turns 0
set state nil

ui_skin "ui/skin/hearth.fgs"

set_faction_color 1 0.25 0.45 0.95
set_faction_color 2 0.85 0.28 0.25

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

fn start_placing [name] do
    set placing {to_sym name}
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
    let hit {pick_block mouse_x mouse_y}
    if (hit == nil) { return nil }
    send_command {=name :move =units selection =x hit.x =y (hit.y + 1) =z hit.z}
end

fn commit_placement [] do
    let hit {pick_block mouse_x mouse_y}
    if (hit == nil) { return nil }
    send_command {=name :build =building placing =x hit.x =z hit.z =turns place_turns}
    set placing nil
end

# Embodied, the Steward works with their own hands
fn steward_click [] do
    let hit {pick_view}
    if (hit == nil) { return nil }
    send_command {=name :steward_harvest =x hit.x =y hit.y =z hit.z}
end

on :mouse do
    if (view_mode == :embodied) do
        if ((mouse_button == :left) and (mouse_event == :down)) { steward_click }
        return nil
    end
    if {gui_has_mouse} { return nil }
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
    if (state == nil) {cam_focus msg.home[0] msg.home[1]}
    if ((msg.match == :over) and (end_screen == nil)) {
        show_end_screen (msg.winner == 1)
    }
    set state msg
end

on :action do
    match action
        :screenshot { request_screenshot "screenshots/keystone.png" }
        :turn { set place_turns ((place_turns + 1) % 4) }
        :menu do
            if (placing != nil) {
                set placing nil
            } {
                quit_game
            }
        end
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

on :frame do
    if (state != nil) do
        set ribbon_timber.text {loc "ui.stock.timber" =count state.stock.timber}
        set ribbon_stone.text {loc "ui.stock.stone" =count state.stock.stone}
        set ribbon_food.text {loc "ui.stock.provisions" =count state.stock.provisions}
        set ribbon_hands.text {loc "ui.stock.hands" =count state.hands =cap state.housing}
        set ribbon_army.text {loc "ui.army" =count state.army}
    end

    if ((state != nil) and (state.capture > 0)) do
        set hud_hint.text {loc "ui.keystone_danger"}
    elif (placing != nil) do
        set hud_hint.text {loc "ui.hint.place"}
    elif (selection.length > 0) do
        set hud_hint.text {loc "ui.selected" =count selection.length}
    else do
        set hud_hint.text {loc "ui.hint.select"}
    end
    set hud_fps.text {loc "ui.fps" =fps {fps}}

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
end
