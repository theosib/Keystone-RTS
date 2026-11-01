# Second to join. Goes along with what Alice sets up, and tries a few things it shouldn't get away with

set started_at nil
set said false
set tried false
set alice_units []
set first_seen nil

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
end

on :lobby do
    if ((msg.players.length == 2) and (not said)) do
        set said true
        check "bob can't launch" (not {lobby_is_host msg})
        lobby_launch
        chat_send "hi alice"
        lobby_ready true
    end
end

fn lobby_is_host [lobby] do
    for p in lobby.players do
        if ((p.id == {me}) and p.host) { return true }
    end
    return false
end

on :tick do
    if (started_at == nil) { return nil }
    let elapsed ({now_ms} - started_at)

    if ((elapsed > 500) and (not tried)) do
        set tried true
        check "bob is his own faction" ({my_faction} == 2)
        check "bob's units are here" ({count_faction 2} >= 8)
        # Alice's first Hand has id 1. Bob orders it about anyway.
        send_command {=name :move =units [1 2 3] =x 0 =y 70 =z 0}
    end

    if (elapsed > 5000) do
        for e in {entities} do
            if (e.faction == 1) { log "SAW" e.id e.x e.y e.z }
        end
        check "bob sees none of alice's units" ({count_faction 1} == 0)
        finish
    end
end
