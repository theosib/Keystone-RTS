# Joins, waits for a game to run, and reports. For tests that care about what happens to it

set started_at nil

on :net do
    log "phase" msg.phase
    if (msg.phase == :failed) do
        log "FAIL could not join:" msg.failure
        bot_exit 1
    end
    if ((msg.phase == :running) and (started_at == nil)) do
        set started_at {now_ms}
        log "RUNNING" started_at
    end
end

on :lobby do
    lobby_ready true
    if ((msg.players.length >= MIN_PLAYERS) and msg.can_launch) { lobby_launch }
end

on :tick do
    if (started_at == nil) { return nil }
    if (({now_ms} - started_at) > RUN_MS) do
        log "END faction" {my_faction} "columns" {columns_known} "entities" {entities}.length "subchunks" {subchunks_received} "root" {content_root}
        bot_exit 0
    end
end
