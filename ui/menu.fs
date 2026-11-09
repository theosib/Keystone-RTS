# Keystone's menus and pre-game (render thread)
#
# The first thing a client runs, before there's any game. Hosting and joining happen here, and
# ends when the world arrives and ui/main.fs takes over.

source "ui/chat"
source "ui/names"

ui_skin "ui/skin/hearth.fgs"

set TITLE_COLOR [1.0 0.72 0.3 1.0]
set TEXT_COLOR [0.95 0.88 0.74 1.0]
set DIM_COLOR [0.7 0.64 0.52 1.0]
set BAD_COLOR [1.0 0.45 0.33 1.0]
set GOOD_COLOR [0.55 0.9 0.5 1.0]
set BROWSE_REFRESH_FRAMES 30
set CHAT_SHOWN 12
set BIG_BUTTON 240

# Screens are windows. Going to one closes the others.
set open_windows []

fn clear_screen [] do
    for id in open_windows do
        ui_hide id
    end
    set open_windows []
end

fn show [window] do
    open_windows.push {ui_show window =immediate true}
end

fn panel [id x y pivot_x pivot_y children] do
    return {ui.window id
        =window_pos_x x =window_pos_y y =window_pivot_x pivot_x =window_pivot_y pivot_y
        =window_flags [:no_title_bar :no_resize :no_move :always_auto_resize :no_saved_settings :no_focus_on_appearing]
        =children children
    }
end

fn centred [id children] do
    let size {window_size}
    return {panel id (size[0] / 2) (size[1] / 2) 0.5 0.5 children}
end

fn big_button [label action] do
    return {ui.button label =width BIG_BUTTON =on_click action}
end

# ---- Title

set error_text ""

fn show_title [] do
    clear_screen
    {net_browse false}
    let children [{ui.text_colored TITLE_COLOR {loc "title.name"}} {ui.text {loc "title.tagline"}} {ui.separator}]
    if {has_save} do
        children.push {big_button {loc "title.continue"} fn [] do
            set screen :starting
            show_status {loc "net.loading"}
            game_start
        end}
    end
    children.push {big_button {loc "title.new"} fn [] do
        start_hosting false
    end}
    children.push {big_button {loc "title.host"} fn [] do
        show_host_form
    end}
    children.push {big_button {loc "title.join"} fn [] do
        show_join
    end}
    children.push {big_button {loc "title.quit"} fn [] do
        quit_game
    end}
    if (error_text != "") do
        children.push {ui.text_colored BAD_COLOR error_text}
    end
    set screen :title
    show {centred "##title" children}
end

fn show_status [text] do
    clear_screen
    show {centred "##status" [{ui.text text}]}
end

# ---- Host

set host_port 7810
set host_password ""

fn start_hosting [on_network] do
    set solo (not on_network)
    set named_lobby false
    set screen :starting
    show_status {loc "net.loading"}
    let problem nil
    if on_network do
        set problem {net_host =new true =listen true =port host_port =password host_password}
    else do
        set problem {net_host =new true}
    end
    if (problem != nil) do
        set error_text {loc "net.host_failed" =why problem}
        show_title
    end
end

fn show_host_form [] do
    clear_screen
    set error_text ""
    set screen :host
    show {centred "##host" [
        {ui.text_colored TITLE_COLOR {loc "host.title"}}
        {ui.input {loc "host.name"} {player_name} fn [value] do
            set_player_name value
        end}
        {ui.input_int {loc "host.port"} host_port fn [value] do
            set host_port value
        end}
        {ui.input {loc "host.password"} host_password fn [value] do
            set host_password value
        end}
        {ui.separator}
        {big_button {loc "host.start"} fn [] do
            start_hosting true
        end}
        {big_button {loc "menu.cancel"} fn [] do
            show_title
        end}
    ]}
end

# ---- Join

set servers []
set join_address ""
set join_password ""
set browse_frames 0
set server_list nil
set server_shown ""

fn server_key [found] do
    let text ""
    for s in found do
        set text "{text}{s.title}{s.players}{s.phase}{s.joinable}"
    end
    return text
end

fn join_addresses [server] do
    let out []
    for a in server.addresses do
        if {a.contains ":"} do
            out.push "[{a}]:{server.port}"
        else do
            out.push "{a}:{server.port}"
        end
    end
    return out
end

fn server_row [server] do
    let phase_name {sym_name server.phase}
    let phase {loc "phase.{phase_name}"}
    let label {loc "join.row" =title server.title =players server.players =max server.max_players =phase phase}
    if server.joinable do
        return {ui.button label =width 400 =on_click fn [] do
            join_game {join_addresses server}
        end}
    end
    return {ui.text_colored DIM_COLOR label}
end

fn fill_server_list [] do
    let rows []
    for server in servers do
        rows.push {server_row server}
    end
    if (rows.length == 0) do
        rows.push {ui.text_colored DIM_COLOR {loc "join.looking"}}
    end
    if {net_blocked} do
        rows.push {ui.text_colored BAD_COLOR {loc "join.blocked"}}
    end
    return rows
end

fn join_game [where] do
    set solo false
    set screen :starting
    show_status {loc "net.connecting"}
    net_join where =password join_password
end

fn show_join [] do
    clear_screen
    set error_text ""
    set screen :join
    {net_browse true}
    set server_shown {server_key servers}
    let size {window_size}
    set server_list {panel "##servers" (size[0] / 2) 60 0.5 0.0 [
        {ui.text_colored TITLE_COLOR {loc "join.title"}}
        {ui.separator}
        {ui.group {fill_server_list}}
    ]}
    show server_list

    let recent []
    for address in {net_recent} do
        recent.push {ui.button address =width 240 =on_click fn_for_address address}
    end

    let children [
        {ui.input {loc "host.name"} {player_name} fn [value] do
            set_player_name value
        end}
        {ui.input {loc "join.address"} join_address fn [value] do
            set join_address value
        end fn [value] do
            set join_address value
            join_game join_address
        end}
        {ui.input {loc "host.password"} join_password fn [value] do
            set join_password value
        end}
        {big_button {loc "join.go"} fn [] do
            if (join_address != "") { join_game join_address }
        end}
    ]
    if (recent.length > 0) do
        children.push {ui.text_colored DIM_COLOR {loc "join.recent"}}
        for button in recent do
            children.push button
        end
    end
    children.push {big_button {loc "menu.cancel"} fn [] do
        show_title
    end}
    if (error_text != "") do
        children.push {ui.text_colored BAD_COLOR error_text}
    end
    show {panel "##join" (size[0] / 2) (size[1] - 40) 0.5 1.0 children}
end

# A closure made in a loop would share its variable, so it's made here
fn fn_for_address [address] do
    return fn [] do
        join_game address
    end
end

# ---- Lobby

set lobby nil
set info nil
set solo false
set ready false
set voted false
set chat_draft ""
set chat_text {ui.text_wrapped "" =id "chat_log" =wrap_width 300}
set chat_shown 0
set lobby_built false
set roster_panel nil
set settings_panel nil

fn me_in [state] do
    for p in state.players do
        if (p.id == {net_me}) { return p }
    end
    return nil
end

fn can_set [state key] do
    let me {me_in state}
    if ((me == nil) or (not me.host)) { return false }
    return (not {state.locked.contains {sym_name key}})
end

fn setting_label [setting] do
    return {loc setting.label}
end

fn choice_index [setting value] do
    let i 0
    for choice in setting.choices do
        if (choice.value == value) { return i }
        set i (i + 1)
    end
    return 0
end

fn choice_labels [setting] do
    let out []
    for choice in setting.choices do
        out.push {loc choice.label}
    end
    return out
end

fn value_text [setting value] do
    match setting.type
        :choice { return {loc setting.choices[{choice_index setting value}].label} }
        :bool do
            if value { return {loc "ui.yes"} }
            return {loc "ui.no"}
        end
        _ { return {to_str value} }
    end
end

fn setting_widget [setting value] do
    let name setting.key
    match setting.type
        :choice do
            return {ui.combo {setting_label setting} {choice_labels setting} {choice_index setting value} fn [index] do
                lobby_set name setting.choices[index].value
            end}
        end
        :bool do
            return {ui.checkbox {setting_label setting} value fn [checked] do
                lobby_set name checked
            end}
        end
        :int do
            return {ui.input_int {setting_label setting} value fn [number] do
                lobby_set name number
            end}
        end
        _ do
            return {ui.input {setting_label setting} value fn [text] do
                lobby_set name text
            end}
        end
    end
end

fn build_settings [] do
    let rows [{ui.text_colored TITLE_COLOR {loc "lobby.settings"}} {ui.separator}]
    for setting in info.settings do
        let value {lobby.settings.get setting.key}
        if {can_set lobby setting.key} do
            rows.push {setting_widget setting value}
        else do
            let text "{setting_label setting}: {value_text setting value}"
            rows.push {ui.text text}
        end
    end
    if (settings_panel != nil) { ui_hide settings_panel }
    set settings_panel {ui_show {panel "##settings" 24 24 0.0 0.0 rows} =immediate true}
    open_windows.push settings_panel
end

fn player_line [p] do
    let text p.name
    if p.host { set text {loc "lobby.host_mark" =name text} }
    if (not p.connected) { set text {loc "lobby.gone_mark" =name text} }
    if (not p.content) { set text {loc "lobby.fetching_mark" =name text} }
    if p.ready { return {ui.text_colored GOOD_COLOR text} }
    return {ui.text text}
end

fn build_roster [] do
    let rows [{ui.text_colored TITLE_COLOR {loc "lobby.players" =count lobby.players.length =max info.max_players}}
              {ui.separator}]
    for p in lobby.players do
        rows.push {player_line p}
    end

    let port {net_port}
    if (port != nil) do
        rows.push {ui.separator}
        rows.push {ui.text_colored DIM_COLOR {loc "lobby.address"}}
        for address in {net_addresses} do
            rows.push {ui.text "{address}:{port}"}
        end
    end

    if (roster_panel != nil) { ui_hide roster_panel }
    let size {window_size}
    set roster_panel {ui_show {panel "##roster" (size[0] - 24) 24 1.0 0.0 rows} =immediate true}
    open_windows.push roster_panel
end

fn build_chat [] do
    let size {window_size}
    show {panel "##chat" (size[0] - 24) (size[1] - 24) 1.0 1.0 [
        {ui.text_colored TITLE_COLOR {loc "lobby.chat"}}
        chat_text
        {ui.input "##say" chat_draft fn [text] do
            set chat_draft text
        end fn [text] do
            if (text != "") { chat_send text }
            set chat_draft ""
        end}
    ]}
end

fn build_controls [] do
    let size {window_size}
    let me {me_in lobby}
    let children []
    let launch info.launch
    if (me != nil) do
        if solo do
            children.push {big_button {loc "lobby.start"} fn [] do
                set screen :starting
                lobby_launch
            end}
        elif ((launch == :host) and me.host) do
            children.push {big_button {loc "lobby.start"} fn [] do
                lobby_launch
            end}
        elif (launch == :vote) do
            children.push {big_button {loc "lobby.vote"} fn [] do
                set voted (not voted)
                lobby_launch voted
            end}
        end
        if (not solo) do
            children.push {ui.checkbox {loc "lobby.ready"} ready fn [checked] do
                set ready checked
                lobby_ready checked
            end}
        end
    end
    children.push {big_button {loc "lobby.leave"} fn [] do
        leave_lobby
    end}
    show {panel "##controls" 24 (size[1] - 24) 0.0 1.0 children}
end

fn leave_lobby [] do
    net_leave
    set lobby nil
    set lobby_built false
    set settings_panel nil
    set roster_panel nil
    show_title
end

fn show_lobby [] do
    clear_screen
    set settings_panel nil
    set roster_panel nil
    set info {lobby_info}
    set lobby_built true
    set screen :lobby
    build_settings
    build_roster
    if (not solo) { build_chat }
    build_controls
end

fn refresh_lobby [] do
    build_settings
    build_roster
end

# A game nobody named gets a made-up name, which the host can still change. It's the lobby's
# title that the network sees.
set named_lobby false

fn name_lobby [] do
    if named_lobby { return nil }
    let me {me_in lobby}
    if ((me == nil) or (not me.host)) { return nil }
    set named_lobby true
    let title {lobby.settings.get :title}
    if ((title == nil) or (title == "")) { lobby_set :title {new_made_up_name} }
end

on :lobby do
    set lobby msg
    name_lobby
    if (screen == :lobby) do
        refresh_lobby
    elif ((screen == :starting) and (msg.phase == :lobby)) do
        show_lobby
    end
end

on :chat do
    chat_hear msg
end

on :net do
    match msg.phase
        :connecting { show_status {loc "net.connecting"} }
        :joining { show_status {loc "net.joining"} }
        :content { show_status {loc "net.content"} }
        :loading { show_status {loc "net.loading"} }
        :failed do
            set lobby nil
            set error_text {loc "net.failed" =why msg.failure}
            if (screen != :title) { show_title }
        end
        :ended do
            set lobby nil
            if (screen == :lobby) { show_title }
        end
        _ {}
    end
end

on :frame do
    if (screen == :join) do
        set browse_frames (browse_frames + 1)
        if (browse_frames >= BROWSE_REFRESH_FRAMES) do
            set browse_frames 0
            set servers {net_servers}
            let key {server_key servers}
            if (key != server_shown) { show_join }
        end
    end
    if (screen == :lobby) do
        if (chat_lines.length != chat_shown) do
            set chat_shown chat_lines.length
            set chat_text.text {chat_tail CHAT_SHOWN}
        end
    end
end

set screen :none
if ({net_phase} == :idle) do
    show_title
else do
    show_status {loc "net.connecting"}
    set screen :starting
end
