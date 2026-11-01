# Shared by the bot scripts: `cat`ed in front of each, since a bot has no game root to source from

set checks_failed 0

fn check [what ok] do
    if ok do
        log "PASS" what
    else do
        log "FAIL" what
        set checks_failed (checks_failed + 1)
    end
end

fn finish [] do
    if (checks_failed > 0) { bot_exit 1 } { bot_exit 0 }
end

fn count_faction [faction] do
    let n 0
    for e in {entities} do
        if (e.faction == faction) { set n (n + 1) }
    end
    return n
end

fn count_not_faction [faction] do
    let n 0
    for e in {entities} do
        if (e.faction != faction) { set n (n + 1) }
    end
    return n
end
