# Keystone UI (render thread)
#
# Reaches the game only through send_command. Hears from it only through on :state.

source "ui/chat"

set selection []
set drag_start nil
set placing nil
set place_turns 0
set place_taken false
set state nil

ui_skin "ui/skin/hearth.fgs"

# Until the game says who's who, which is the first thing it does
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
set ENEMY_PICK_REACHES [0 5 10]
set GHOST_CLEAR [0.35 0.75 1.0 0.28]
set GHOST_TAKEN [1.0 0.2 0.16 0.42]
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
        if (owner == my_faction) { add {loc "inspect.yours"} } { add {loc "inspect.rival"} }
    }
    let broken {info.get :keystone_broken}
    if (broken != nil) do
        if (owner == my_faction) { add {loc "inspect.keystone.hold"} } { add {loc "inspect.keystone.how"} }
        if (broken > 0) { add {loc "inspect.keystone.broken" =percent broken} }
    end
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

# What it's called, however many there are. The plain key is the singular one.
fn noun [group name count] do
    if (count == 1) { return {loc "{group}.{name}"} }
    return {loc "{group}.{name}" =count count}
end

fn frame_text [frame count] do
    let name ""
    let kind ""
    let building {frame.get :building}
    if (building != nil) { set name {noun "building" building count} }
    let material {frame.get :kind}
    if (material != nil) { set kind {loc "kind.{material}"} }
    return {loc "work.{frame.key}" =count count =name name =kind kind =percent {frame.get :percent}}
end

fn queue_row_text [row] do
    if ((row.key == "train") or (row.key == "muster")) do
        let line ""
        if (row.key == "train") do
            set line {loc "work.train" =count row.count}
        else do
            set line {loc "work.muster" =count row.times =name {noun "unit" row.unit row.times}}
        end
        if row.housed { return line }
        let no_room {loc "work.no_room"}
        return "{line} {no_room}"
    end
    let line {frame_text row row.times}
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
        let head {noun "unit" group.who group.count}
        if (group.count > 1) { set head {loc "work.group" =count group.count =name head} }
        if (text == "") { set text head } { set text "{text}\n{head}" }
        let indent "  "
        for frame in group.stack do
            set text "{text}\n{indent}{frame_text frame 1}"
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

# The game has the last word, but it says what it knows of the ground in every state message
fn is_taken [x0 z0 x1 z1] do
    if (state == nil) { return false }
    for p in state.plots do
        if ((x0 < p[2]) and (p[0] < x1) and (z0 < p[3]) and (p[1] < z1)) { return true }
    end
    return false
end

fn ghost_box [x y z sx sy sz color] do
    marker_box x y z sx sy sz color[0] color[1] color[2] color[3]
end

fn cell_box [x y z height] do
    let color GHOST_CLEAR
    if {is_taken x z (x + 1) (z + 1)} { set color GHOST_TAKEN }
    ghost_box x y z 1 height 1 color
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
            cell_box cell[0] (start[1] + 1) cell[1] FENCE_HEIGHT
        end
        return nil
    end
    let dx (hit.x - start[0])
    let dz (hit.z - start[2])
    if ({abs dx} >= {abs dz}) { set dz 0 } { set dx 0 }
    let reach (LAY_MAX_CELLS - 1)
    set dx {clamp dx (0 - reach) reach}
    set dz {clamp dz (0 - reach) reach}
    let step_x {clamp dx -1 1}
    let step_z {clamp dz -1 1}
    for i in (0..=({abs dx} + {abs dz})) do
        cell_box (start[0] + (i * step_x)) (start[1] + 1) (start[2] + (i * step_z)) 1
    end
end

fn steward_place_block [] do
    let hit {pick_view}
    if ({game_paused} or (hit == nil) or (hit.distance > STEWARD_REACH)) { return nil }
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

# How many a click or key queues: a whole batch with Shift held. The game stops at its own limit.
set QUEUE_BATCH 8

fn train_hands [count] do
    send_command {=name :train =count count}
end

fn muster_units [unit count] do
    send_command {=name :muster =unit {to_sym unit} =count count}
end

fn click_count [] do
    if ({key_held "left_shift"} or {key_held "right_shift"}) { return QUEUE_BATCH }
    return 1
end

# Only a game nobody else is in can stop. The game says so too, but the button shouldn't offer it.
fn can_pause [] { return ({net_is_host} and ({net_port} == nil)) }

fn toggle_pause [] do
    if {can_pause} { send_command {=name :pause =paused (not {game_paused})} }
end

# What's named on a palette button is under a key. Keys are for commanding, not for the body or a menu.
fn at_the_palette [] { return ((view_mode != :embodied) and (menu_panel == nil) and (chat_typing == nil)) }

fn titled_button [children label title icon tip on_click] do
    let button {ui.button label =icon icon =icon_size PALETTE_ICON =width PALETTE_WIDTH =on_click on_click}
    children.push button
    children.push {make_tip tip title}
    return button
end

fn palette_button [children label icon tip on_click] do
    return {titled_button children label label icon tip on_click}
end

fn keyed_button [children label icon tip on_click] do
    let title {loc "tip.hotkey" =name label =key {loc "hotkey.{tip}"}}
    return {titled_button children label title icon tip on_click}
end

fn unit_button [children label icon tip on_click] do
    let button {keyed_button children label icon tip on_click}
    let widget children[children.length - 1]
    widget.children.push {ui.text {loc "tip.batch" =count QUEUE_BATCH}}
    return button
end

# Each its own function, for its own name: a closure made in a loop shares the loop's variable
fn building_button [children name] do
    keyed_button children {loc "building.{name}"} name name fn [] do
        start_placing name
    end
end

fn muster_button [children name] do
    unit_button children {loc "unit.{name}"} name name fn [] do
        muster_units name {click_count}
    end
end

set pause_button nil
set pause_tip nil

fn palette_children [] do
    let children []
    # Side by side, or the palette runs off the bottom of a small window
    let half ((PALETTE_WIDTH - 8) / 2)
    children.push {ui.button {loc "menu.open"} =icon "menu" =icon_size PALETTE_ICON =width half =on_click fn [] do
        open_menu
    end}
    children.push {make_tip "menu" {loc "menu.open"}}
    children.push {ui.same_line 0}
    set pause_button {ui.button {loc "menu.pause"} =width half =on_click fn [] do
        toggle_pause
    end}
    children.push pause_button
    set pause_tip {make_tip "pause" {loc "tip.hotkey" =name {loc "menu.pause"} =key {loc "hotkey.pause"}}}
    children.push pause_tip
    children.push {ui.separator}
    children.push {ui.text {loc "ui.palette"}}
    for name in ["shelter" "forager_hut" "sawpit" "watchtower"] do
        building_button children name
    end
    set fence_button {keyed_button children {loc "building.palisade_wall"} "palisade_wall" "palisade_wall" fn [] do
        start_fence
    end}
    children.push {ui.separator}
    children.push {ui.text {loc "ui.muster"}}
    unit_button children {loc "ui.train_hand"} "train_hand" "hand" fn [] do
        train_hands {click_count}
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
    set selection {picked.filter fn [id] ({entity_faction id} == my_faction)}
    set drag_start nil
end

# Whoever's closest to the cursor, within reason: at a distance a body's only a few pixels across,
# and it's on the move
fn enemy_under [x y] do
    for reach in ENEMY_PICK_REACHES do
        for id in {pick_entities (x - reach) (y - reach) (x + reach) (y + reach)} do
            let side {entity_faction id}
            if ((side != nil) and (side != 0) and (side != my_faction)) { return id }
        end
    end
    return nil
end

set attacked nil
set attacked_frames 0

fn order_attack [id] do
    let p {entity_pos id}
    send_command {=name :attack =units selection =target id =x {floor p[0]} =y {floor p[1]} =z {floor p[2]}}
    set attacked id
    set attacked_frames 45
end

fn order_move [] do
    if (selection.length == 0) { return nil }
    let enemy {enemy_under mouse_x mouse_y}
    if (enemy != nil) { return order_attack enemy }
    let hit {pick_ground mouse_x mouse_y}
    if (hit == nil) { return nil }
    send_command {=name :move =units selection =x hit.x =y (hit.y + 1) =z hit.z}
end

# Where a building would stand with its middle at this hit, worked out as the game will
fn placement_at [hit] do
    let size {blueprint_size placing}
    if (size == nil) { return nil }
    let sx size[0]
    let sz size[2]
    if ((place_turns % 2) == 1) do
        set sx size[2]
        set sz size[0]
    end
    let x (hit.x - (sx / 2))
    let z (hit.z - (sz / 2))
    return {=x x =y (hit.y + 1) =z z =sx sx =sy size[1] =sz sz =taken {is_taken x z (x + sx) (z + sz)}}
end

fn commit_placement [] do
    let hit {pick_ground mouse_x mouse_y}
    if (hit == nil) { return nil }
    let spot {placement_at hit}
    if ((spot == nil) or spot.taken) { return nil }
    send_command {=name :build =building placing =x hit.x =z hit.z =turns place_turns}
    set placing nil
end

# Embodied, the Steward works with their own hands
fn steward_click [] do
    let hit {pick_view}
    if ({game_paused} or (hit == nil) or (hit.distance > STEWARD_REACH)) { return nil }
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

fn open_network_panel [problem] do
    let children []
    let port {net_port}
    if (port != nil) do
        children.push {ui.text {loc "menu.open_as"}}
        for address in {net_addresses} do
            children.push {ui.text "{address}:{port}"}
        end
    else do
        children.push {ui.text {loc "menu.open_failed" =why problem}}
    end
    children.push {ui.button {loc "menu.cancel"} =on_click fn [] do
        open_menu
    end}
    show_menu_window children
end

fn open_menu [] do
    let children [{ui.button {loc "menu.resume"} =on_click fn [] do
        close_menu
    end}]
    if {net_is_host} do
        children.push {ui.button {loc "menu.open_game"} =on_click fn [] do
            let problem nil
            if ({net_port} == nil) do
                if {game_paused} { toggle_pause }
                set problem {net_open}
            end
            open_network_panel problem
        end}
        children.push {ui.button {loc "menu.new"} =on_click fn [] do
            open_new_game_confirm
        end}
        children.push {ui.button {loc "menu.to_title"} =on_click fn [] do
            to_menu
        end}
        children.push {ui.button {loc "menu.quit"} =on_click fn [] do
            quit_game
        end}
    else do
        children.push {ui.button {loc "menu.leave"} =on_click fn [] do
            to_menu
        end}
    end
    show_menu_window children
end

# ---- Who else is here, and what they're saying

set sides_rows []
set sides_shown ""

set sides_group {ui.group []}
set sides_window {ui.window "##sides"
    =window_pos_x 0 =window_pos_y 8 =window_pivot_x 1.0
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing :no_inputs]
    =children [sides_group]
}
ui_show sides_window =immediate true

fn side_label [side] do
    if (side.id == my_faction) { return {loc "side.you"} }
    if side.alive do
        if side.rival { return {loc "side.rival" =id side.id} }
        return {loc "side.player" =id side.id}
    end
    return {loc "side.fallen" =id side.id}
end

fn hear_sides [sides] do
    let key ""
    for side in sides do
        set_faction_color side.id side.color[0] side.color[1] side.color[2]
        let label {side_label side}
        set key "{key}{label}"
    end
    if (key == sides_shown) { return nil }
    set sides_shown key

    let rows [{ui.text {loc "side.title"}}]
    for side in sides do
        rows.push {ui.text_colored [side.color[0] side.color[1] side.color[2] 1.0] {side_label side}}
    end
    set sides_group.children rows
end

set CHAT_SHOWN 6
set chat_seen 0
set chat_log {ui.text_wrapped "" =id "chat_log" =wrap_width 340}
set chat_draft ""
set chat_typing nil
set chat_input nil

set chat_window {ui.window "##chat_log"
    =window_pos_x 190 =window_pos_y 0 =window_pivot_y 1.0
    =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing :no_inputs]
    =children [chat_log]
}
set chat_panel nil

fn stop_typing [] do
    if (chat_typing != nil) do
        ui_hide chat_typing
        set chat_typing nil
    end
end

fn start_typing [] do
    if (chat_typing != nil) { return nil }
    let size {window_size}
    set chat_draft ""
    set chat_typing {ui_show {ui.window "##chat_say"
        =window_pos_x 190 =window_pos_y (size[1] - 8) =window_pivot_y 1.0
        =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings]
        =children [{ui.input "##say" "" fn [text] do
            set chat_draft text
        end fn [text] do
            if (text != "") { chat_send text }
            stop_typing
        end =auto_focus true}]
    } =immediate true}
end

fn update_chat [] do
    if (chat_lines.length == chat_seen) { return nil }
    set chat_seen chat_lines.length
    if (chat_panel == nil) { set chat_panel {ui_show chat_window =immediate true} }
    set chat_log.text {chat_tail CHAT_SHOWN}
end

on :chat do
    chat_hear msg
end

# The connection comes and goes without the game stopping
set net_trouble nil

on :net do
    match msg.phase
        :reconnecting { set net_trouble {loc "net.reconnecting"} }
        :running { set net_trouble nil }
        :failed { show_lost_game {loc "net.failed" =why msg.failure} }
        :ended { show_lost_game {loc "net.ended"} }
        _ {}
    end
end

fn show_lost_game [text] do
    set net_trouble nil
    show_menu_window [
        {ui.text text}
        {ui.button {loc "menu.to_title"} =on_click fn [] do
            to_menu
        end}
    ]
end

# DESIGN 7.2: when a match ends, the world is still yours
set end_screen nil
set pacify_rivals false

fn leave_or_quit [] do
    if {net_is_host} do
        quit_game
    else do
        to_menu
    end
end

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
                leave_or_quit
            end}
        ]
    }}
end

on :state do
    let sides {msg.get :sides}
    if (sides != nil) do
        hear_sides sides
        return nil
    end
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
        set denied {loc "ui.denied" =kind {loc "kind.{refused}"}}
        set denied_frames 120
        return nil
    end
    if ({msg.get :taken} != nil) do
        set denied {loc "ui.taken"}
        set denied_frames 120
        return nil
    end
    if ((state == nil) and (not resumed)) {cam_focus msg.home[0] msg.home[1]}
    if ((msg.match == :over) and (end_screen == nil)) {
        show_end_screen (msg.winner == my_faction)
    }
    set state msg
    refresh_tips
    if (not catalog_asked) do
        set catalog_asked true
        send_command {=name :catalog}
    end
end

fn palette_action [] do
    match action
        :place_shelter { start_placing "shelter" }
        :place_forager_hut { start_placing "forager_hut" }
        :place_sawpit { start_placing "sawpit" }
        :place_watchtower { start_placing "watchtower" }
        :place_palisade_wall { start_fence }
        :train_hand { train_hands 1 }
        :train_hand_batch { train_hands QUEUE_BATCH }
        :muster_militia { muster_units "militia" 1 }
        :muster_militia_batch { muster_units "militia" QUEUE_BATCH }
        :muster_shortbow { muster_units "shortbow" 1 }
        :muster_shortbow_batch { muster_units "shortbow" QUEUE_BATCH }
        :muster_scout { muster_units "scout" 1 }
        :muster_scout_batch { muster_units "scout" QUEUE_BATCH }
        :begin_jump { toggle_pause }
        _ { nil }
    end
end

on :action do
    if {at_the_palette} { palette_action }
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
        :chat {
            if (chat_typing == nil) { start_typing }
        }
        _ { nil }
    end
end

fn draw_placement [] do
    set place_taken false
    if (placing == nil) { return nil }
    let m {mouse_pos}
    let hit {pick_ground m[0] m[1]}
    if (hit == nil) { return nil }
    let spot {placement_at hit}
    if (spot == nil) { return nil }
    set place_taken spot.taken
    let color GHOST_CLEAR
    if spot.taken { set color GHOST_TAKEN }
    ghost_box spot.x spot.y spot.z spot.sx spot.sy spot.sz color
end

fn draw_drag_box [a b] do
    ui_rect a[0] a[1] b[0] b[1] 0.4 0.8 1.0 0.9
end

set tools_view :tactical

# The one being broken that matters most: this side's own, if it's that
fn keystone_going [] do
    if (state == nil) { return nil }
    let found nil
    for k in state.keystones do
        if (k.mine or (found == nil)) { set found k }
    end
    return found
end

fn update_pause_button [] do
    set pause_button.visible {can_pause}
    set pause_tip.visible {can_pause}
    if {game_paused} { set pause_button.label {loc "menu.unpause"} } { set pause_button.label {loc "menu.pause"} }
end

on :frame do
    if (state != nil) do
        set amount_timber.text {to_str state.stock.timber}
        set amount_stone.text {to_str state.stock.stone}
        set amount_food.text {to_str state.stock.provisions}
        set amount_people.text {loc "ui.housed" =count state.hands =cap state.housing}
        set amount_army.text {to_str state.army}
    end

    if (denied_frames > 0) { set denied_frames (denied_frames - 1) }

    update_chat
    if (chat_panel != nil) do
        let screen {window_size}
        set chat_window.window_pos_y (screen[1] - 44)
    end

    let going {keystone_going}
    if (net_trouble != nil) do
        set hud_hint.text net_trouble
    elif {game_paused} do
        set hud_hint.text {loc "ui.paused"}
    elif ((going != nil) and going.mine) do
        set hud_hint.text {loc "ui.keystone_danger" =percent going.percent}
    elif (going != nil) do
        set hud_hint.text {loc "ui.keystone_breaking" =percent going.percent}
    elif (denied_frames > 0) do
        set hud_hint.text denied
    elif (view_mode == :embodied) do
        set hud_hint.text {loc "ui.hint.embodied"}
    elif laying_fence do
        set hud_hint.text {loc "ui.hint.fence"}
    elif laying do
        set hud_hint.text {loc "ui.hint.lay"}
    elif place_taken do
        set hud_hint.text {loc "ui.taken"}
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
    set sides_window.window_pos_x (size[0] - 180)
    set hint_window.window_pos_x (size[0] / 2)
    set tool_window.window_pos_x (size[0] / 2)
    set tool_window.window_pos_y (size[1] - 8)

    update_inspect
    update_work_panel
    update_pause_button
    if (view_mode != tools_view) do
        set tools_view view_mode
        refresh_tools
    end
    if (view_mode == :embodied) { draw_crosshair }

    for id in selection do
        let p {entity_pos id}
        if (p != nil) {marker_ring p[0] (p[1] + 0.05) p[2] 0.6 0.3 0.9 1.0 0.9}
    end

    if (state != nil) do
        for k in state.keystones do
            marker_bar (k.x + 0.5) k.y (k.z + 0.5) 5.0 (1.0 - (k.percent / 100.0))
        end
    end

    if (attacked_frames > 0) do
        set attacked_frames (attacked_frames - 1)
        let p {entity_pos attacked}
        if (p != nil) {marker_ring p[0] (p[1] + 0.05) p[2] 0.7 1.0 0.2 0.16 1.0}
    end

    # Rubber band while dragging
    if (drag_start != nil) do
        let m {mouse_pos}
        draw_drag_box drag_start m
    end

    draw_placement
    draw_laying
end
