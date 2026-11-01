# First to join, so the one who gets to set things up and launch

set heard nil
set configured false
set launched false
set started_at nil
set commanded false
set hands_before 0
set lobby nil

on :net do
    log "phase" msg.phase
    if (msg.phase == :failed) do
        log "FAIL could not join:" msg.failure
        bot_exit 1
    end
    if (msg.phase == :running) do
        set started_at {now_ms}
        log "RUNNING" started_at
    end
end

on :chat do
    log "chat" msg.name msg.text
    if (msg.text == "hi alice") { set heard msg.name }
end

on :lobby do
    set lobby msg
end

# Chat and the lobby travel on different channels, so nothing says which arrives first. Every tick
# looks at both, as a UI would.
fn look_at_lobby [] do
    if (lobby == nil) { return nil }
    let players 0
    let bob_ready false
    for p in lobby.players do
        set players (players + 1)
        if ((p.name == "Bob") and p.ready) { set bob_ready true }
    end
    if ((players == 2) and (not configured)) do
        set configured true
        chat_send "welcome bob"
        lobby_set "rivals" 1
        lobby_set "map" "desert"
        lobby_set "size" "small"
        # None of these are allowed, so none should stick
        lobby_set "rivals" 99
        lobby_set "map" "the moon"
        lobby_set "nonsense" 1
    end
    let wanted ({lobby.settings.get :map} == :desert)
    if (wanted and ({lobby.settings.get :rivals} == 1) and (heard != nil) and bob_ready and lobby.can_launch and (not launched)) do
        set launched true
        check "chat works before the game" (heard == "Bob")
        check "settings reach the lobby" ({lobby.settings.get :size} == :small)
        check "and bad values don't" ({lobby.settings.get :nonsense} == nil)
        lobby_launch
    end
end

on :tick do
    look_at_lobby
    if (started_at == nil) { return nil }
    let elapsed ({now_ms} - started_at)

    if ((elapsed > 500) and (not commanded)) do
        set commanded true
        check "the world arrived" ({columns_known} > 0)
        check "bound to a faction" ({my_faction} == 1)
        check "her own units are here" ({count_faction 1} >= 8)
        set hands_before {count_faction 1}
        send_command {=name :train}
    end

    if (elapsed > 5000) do
        check "desert map: the ground is sand" (found_sand)
        for e in {entities} do
            if (e.faction != 1) { log "SAW" e.id e.faction e.x e.y e.z }
        end
        check "no rival units in the fog" ({count_not_faction 1} == 0)
        let v {vision_counts}
        check "fog: most of the map is unknown" (v.unknown > v.visible)
        check "fog: she holds only part of the map" ({columns_known} < 324)
        log "columns" {columns_known} "subchunks" {subchunks_received} "vision" v.unknown v.explored v.visible
        finish
    end
end

set found_sand false
on :state do
    if ({msg.has :home} and (not found_sand)) do
        let x msg.home[0]
        let z msg.home[1]
        for dx in [20 -20 30 -30 40] do
            for y in (55..95) do
                if ({block_at (x + dx) y z} == "keystone:sand") { set found_sand true }
                if ({block_at (x + dx) y z} == "sand") { set found_sand true }
            end
        end
    end
end
