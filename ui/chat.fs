# What's been said, for the lobby and the game alike. Reaches other players through chat_send,
# and hears them through on :chat.

set CHAT_LINES 40
set chat_lines []

fn chat_line_text [line] do
    if (line.from == 0) { return line.text }
    return "{line.name}: {line.text}"
end

# Newest at the bottom, as many as fit. Each caller decides how many that is.
fn chat_tail [count] do
    let text ""
    let first {max 0 (chat_lines.length - count)}
    for i in (first..chat_lines.length) do
        if (i > first) { set text "{text}\n" }
        let line {chat_line_text chat_lines[i]}
        set text "{text}{line}"
    end
    return text
end

fn chat_hear [message] do
    chat_lines.push {=from message.from =name message.name =text message.text}
    if (chat_lines.length > CHAT_LINES) { chat_lines.remove 0 }
end
