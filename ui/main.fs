# Keystone UI (render thread)
#
# Reaches the game only through send_command. Hears from it only through on :state.

set selection []
set drag_start nil

set_faction_color 1 0.25 0.45 0.95
set_faction_color 2 0.85 0.28 0.25

set hud_label {ui.text {loc "ui.hint.select"} =id "hud_label"}
set hud_fps {ui.text "" =id "hud_fps"}

ui_show {ui.window "##ribbon"
    =window_pos_x 10 =window_pos_y 10
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings]
    =children [
        {ui.text {loc "ui.title"}}
        hud_label
        hud_fps
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

on :mouse do
    if (mouse_button == :left) do
        if (mouse_event == :down) { begin_drag } { finish_drag }
    elif (mouse_button == :right and mouse_event == :down) do
        order_move
    end
end

on :action do
    match action
        :screenshot { request_screenshot "screenshots/keystone.png" }
        :menu { quit_game }
        _ { nil }
    end
end

on :frame do
    let size selection.length
    if (size > 0) do
        set hud_label.text {loc "ui.selected" =count size}
    else do
        set hud_label.text {loc "ui.hint.select"}
    end
    set hud_fps.text {loc "ui.fps" =fps {fps}}

    for id in selection do
        let p {entity_pos id}
        if (p != nil) {marker_ring p[0] (p[1] + 0.05) p[2] 0.6 0.3 0.9 1.0 0.9}
    end
end
