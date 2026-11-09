# Made-up names, an adjective and a noun, for a game nobody named and for computer rivals
#
# The words are in the language files, so a translation brings its own. A name is a number, which
# the server can choose without knowing the words, and every client reads it the same way.

set name_adjectives nil
set name_nouns nil

fn name_words [] do
    if (name_adjectives == nil) do
        let adjectives {loc "names.adjectives"}
        let nouns {loc "names.nouns"}
        set name_adjectives {adjectives.split " "}
        set name_nouns {nouns.split " "}
    end
end

fn made_up_name [number] do
    name_words
    let n {floor {abs number}}
    let adjective name_adjectives[n % name_adjectives.length]
    let noun name_nouns[{floor (n / name_adjectives.length)} % name_nouns.length]
    return {loc "names.made_up" =adjective adjective =noun noun}
end

fn new_made_up_name [] do
    return {made_up_name {random_range 0 999999}}
end
