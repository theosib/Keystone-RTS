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

set PALETTE_WIDTH 176
set PALETTE_ICON 24
set TOOL_WIDTH 74
set TOOL_ICON 32
set FENCE_HEIGHT 3
set TIP_WRAP 250
set KINDS ["timber" "stone" "provisions"]
set TEXT_COLOR [0.95 0.88 0.74 1.0]
set TITLE_COLOR [1.0 0.72 0.3 1.0]
set SHORT_COLOR [1.0 0.45 0.33 1.0]

set amount_timber {ui.text "" =id "a_timber"}
set amount_stone {ui.text "" =id "a_stone"}
set amount_food {ui.text "" =id "a_food"}
set amount_people {ui.text "" =id "a_people"}
set amount_army {ui.text "" =id "a_army"}
set hud_hint {ui.text "" =id "hud_hint"}
set hud_fps {ui.text "" =id "hud_fps"}

for name in ["timber" "stone" "provisions" "population" "army" "shelter" "forager_hut" "sawpit"
             "watchtower" "palisade_wall" "train_hand" "militia" "shortbow" "scout" "menu"] do
    ui_texture name "ui/icons/{name}.png"
end
for tool in BLOCK_TOOLS do
    ui_texture "block_{tool}" "textures/blocks/{tool}.png"
end

fn stat_row [icon amount] do
    return {ui.table_row [{ui.image icon 24 24} {ui.text {loc "ui.stat.{icon}"}} amount]}
end

ui_show {ui.window "##ribbon"
    =window_pos_x 8 =window_pos_y 8
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing]
    =children [
        {ui.text {loc "ui.epoch"}}
        {ui.table "stats" 3 [
            {stat_row "timber" amount_timber}
            {stat_row "stone" amount_stone}
            {stat_row "provisions" amount_food}
            {stat_row "population" amount_people}
            {stat_row "army" amount_army}
        ] =flags [:sizing_fixed_fit :borders_inner_h]}
        hud_fps
    ]
} =immediate true

set hint_window {ui.window "##hint"
    =window_pos_x 0 =window_pos_y 8 =window_pivot_x 0.5
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing :no_inputs]
    =children [hud_hint]
}
ui_show hint_window =immediate true

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

# ---- Tooltips -----------------------------------------------------------------
# What a button does, then what it takes. The game answers with the numbers, and anything the
# settlement can't cover right now turns red.

set catalog nil
set catalog_asked false
set tips []

# Each is a tooltip widget, filled in when the catalog arrives
fn make_tip [key title] do
    let detail {ui.text_wrapped "" =wrap_width TIP_WRAP}
    let widget {ui.item_tooltip [{ui.text_colored TITLE_COLOR title} detail]}
    tips.push {=key key =widget widget =detail detail =amounts [] =bed nil}
    return widget
end

fn add_needs [tip needs] do
    let row tip.widget.children
    row.push {ui.text {loc "tip.needs"}}
    for kind in KINDS do
        let amount {needs.get {to_sym kind}}
        if ((amount != nil) and (amount > 0)) do
            let text {ui.text_colored TEXT_COLOR {to_str amount}}
            row.push {ui.same_line 0}
            row.push {ui.image kind 18 18}
            row.push {ui.same_line 0}
            row.push text
            tip.amounts.push {=kind kind =amount amount =text text}
        end
    end
end

fn fill_tip [tip] do
    let blocks catalog.blocks
    let material {blocks.get {to_sym tip.key}}
    if (material != nil) do
        let needs {=timber 0 =stone 0 =provisions 0}
        needs.set material 1
        set tip.detail.visible false
        add_needs tip needs
        return nil
    end
    let entry {catalog.entries.get {to_sym tip.key}}
    if (entry == nil) do
        set tip.detail.text {loc "tip.{tip.key}"}
        return nil
    end
    set tip.detail.text {loc "tip.{tip.key}" =count {entry.get :count} =percent {entry.get :percent}
        =radius {entry.get :radius} =seconds {entry.get :seconds} =range {entry.get :range}}
    add_needs tip entry.needs
    if ({entry.get :bed} == true) do
        set tip.bed {ui.text_colored TEXT_COLOR ""}
        tip.widget.children.push tip.bed
    end
end

fn stock_of_kind [kind] do
    return {state.stock.get {to_sym kind}}
end

fn refresh_tip [tip] do
    for need in tip.amounts do
        if ({stock_of_kind need.kind} >= need.amount) { set need.text.color TEXT_COLOR } { set need.text.color SHORT_COLOR }
    end
    if (tip.bed != nil) do
        set tip.bed.text {loc "tip.bed" =used state.hands =cap state.housing}
        if (state.hands < state.housing) { set tip.bed.color TEXT_COLOR } { set tip.bed.color SHORT_COLOR }
    end
end

fn refresh_tips [] do
    if (catalog == nil) { return nil }
    for tip in tips do
        refresh_tip tip
    end
end

# ---- Blocks -------------------------------------------------------------------
# One choice of block, for two ways of using it. On foot the Steward places it by hand;
# overhead, a drag marks a run of it for Hands to build.

set tool_index 0
set laying false
set laying_fence false
set fence_button nil
set lay_start nil
set denied nil
set denied_frames 0
set tool_buttons []

fn tool_name [] { return BLOCK_TOOLS[tool_index] }

fn refresh_tools [] do
    let i 0
    while (i < BLOCK_TOOLS.length) do
        let button tool_buttons[i]
        set button.selected ((i == tool_index) and (not laying_fence) and (laying or (view_mode == :embodied)))
        set i (i + 1)
    end
    if (fence_button != nil) { set fence_button.selected laying_fence }
end

fn choose_tool [index] do
    set tool_index index
    if (view_mode != :embodied) do
        set laying true
        set laying_fence false
        set placing nil
        set lay_start nil
    end
    refresh_tools
end

fn stop_laying [] do
    set laying false
    set laying_fence false
    set lay_start nil
    refresh_tools
end

fn start_fence [] do
    set placing nil
    set laying true
    set laying_fence true
    set lay_start nil
    refresh_tools
end

# The cells between two points, each joined to the last straight or on the diagonal
fn line_cells [x0 z0 x1 z1] do
    let dx (x1 - x0)
    let dz (z1 - z0)
    let span {max {abs dx} {abs dz}}
    if (span == 0) { return [[x0 z0]] }
    let cells []
    for i in (0..={min span (LAY_MAX_CELLS - 1)}) do
        let x (x0 + {floor (((i * dx) / (span * 1.0)) + 0.5)})
        let z (z0 + {floor (((i * dz) / (span * 1.0)) + 0.5)})
        cells.push [x z]
    end
    return cells
end

fn tool_button [index] do
    let shown (index + 1)
    let tool BLOCK_TOOLS[index]
    let button {ui.button "{shown}" =icon "block_{tool}" =icon_size TOOL_ICON =width TOOL_WIDTH =on_click fn [] do
        choose_tool index
    end}
    tool_buttons.push button
    return button
end

fn tool_children [] do
    let children [{ui.text {loc "ui.blocks"}}]
    let i 0
    while (i < BLOCK_TOOLS.length) do
        let tool BLOCK_TOOLS[i]
        children.push {ui.same_line 0}
        children.push {tool_button i}
        children.push {make_tip tool {loc "block.{tool}"}}
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
    if (hit != nil) do
        if laying_fence do
            send_command {=name :fence =cells {line_cells lay_start[0] lay_start[2] hit.x hit.z}}
        else do
            send_command {=name :lay =block {to_sym {tool_name}} =x0 lay_start[0] =z0 lay_start[2] =x1 hit.x =z1 hit.z}
        end
    end
    set lay_start nil
end

fn draw_laying [] do
    if ((not laying) or (view_mode == :embodied)) { return nil }
    let m {mouse_pos}
    let hit {pick_ground m[0] m[1]}
    if (hit == nil) { return nil }
    let start lay_start
    if (start == nil) { set start [hit.x hit.y hit.z] }
    if laying_fence do
        for cell in {line_cells start[0] start[2] hit.x hit.z} do
            marker_box cell[0] (start[1] + 1) cell[1] 1 FENCE_HEIGHT 1 0.35 0.75 1.0 0.28
        end
        return nil
    end
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
    predict_block x y z {tool_name}
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

fn palette_button [children label icon tip on_click] do
    let button {ui.button label =icon icon =icon_size PALETTE_ICON =width PALETTE_WIDTH =on_click on_click}
    children.push button
    children.push {make_tip tip label}
    return button
end

# Each its own function, for its own name: a closure made in a loop shares the loop's variable
fn building_button [children name] do
    palette_button children {loc "building.{name}"} name name fn [] do
        start_placing name
    end
end

fn muster_button [children name] do
    palette_button children {loc "unit.{name}"} name name fn [] do
        send_command {=name :muster =unit {to_sym name}}
    end
end

fn palette_children [] do
    let children []
    palette_button children {loc "menu.open"} "menu" "menu" fn [] do
        open_menu
    end
    children.push {ui.separator}
    children.push {ui.text {loc "ui.palette"}}
    for name in ["shelter" "forager_hut" "sawpit" "watchtower"] do
        building_button children name
    end
    set fence_button {palette_button children {loc "building.palisade_wall"} "palisade_wall" "palisade_wall" fn [] do
        start_fence
    end}
    children.push {ui.separator}
    children.push {ui.text {loc "ui.muster"}}
    palette_button children {loc "ui.train_hand"} "train_hand" "hand" fn [] do
        send_command {=name :train}
    end
    for name in ["militia" "shortbow" "scout"] do
        muster_button children name
    end
    return children
end

ui_show {ui.window "##palette"
    =window_pos_x 8 =window_pos_y 240
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing]
    =children {palette_children}
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
    let after nil
    if (catalog != nil) { set after {catalog.breaks.get {to_sym hit.block}} }
    if (after != nil) do
        predict_block hit.x hit.y hit.z after
    else do
        expect_change hit.x hit.y hit.z
    end
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
    let offered {msg.get :catalog}
    if (offered != nil) do
        set catalog offered
        for tip in tips do
            fill_tip tip
        end
        refresh_tips
        return nil
    end
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
    refresh_tips
    if (not catalog_asked) do
        set catalog_asked true
        send_command {=name :catalog}
    end
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
        set amount_timber.text {to_str state.stock.timber}
        set amount_stone.text {to_str state.stock.stone}
        set amount_food.text {to_str state.stock.provisions}
        set amount_people.text {loc "ui.housed" =count state.hands =cap state.housing}
        set amount_army.text {to_str state.army}
    end

    if (denied_frames > 0) { set denied_frames (denied_frames - 1) }

    if ((state != nil) and (state.capture > 0)) do
        set hud_hint.text {loc "ui.keystone_danger"}
    elif (denied_frames > 0) do
        set hud_hint.text {loc "ui.denied" =kind {loc "kind.{denied}"}}
    elif (view_mode == :embodied) do
        set hud_hint.text {loc "ui.hint.embodied"}
    elif laying_fence do
        set hud_hint.text {loc "ui.hint.fence"}
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
    set hint_window.window_pos_x (size[0] / 2)
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
