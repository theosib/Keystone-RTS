# Expects to be turned away, for the reason in the script's own first line

set outcome nil

on :net do
    log "phase" msg.phase
    if (msg.phase == :failed) do
        log "FAILED_WITH" msg.failure
        bot_exit 0
    end
    if ((msg.phase == :lobby) or (msg.phase == :running)) do
        log "FAIL was let in"
        bot_exit 1
    end
end
