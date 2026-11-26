# The player's own settings (render thread). Kept with pref and set_pref, so they outlast the game
# and belong to this player, not to the game being played. See docs/PLAN.md for what's to come.
#
# Each is a row in SETTINGS: a pref name, a label, and the choices, the first being the default.
# The texture pack is a row of its own: the engine keeps that choice, and the packs it can be are
# whatever's in packs/ (the game's, and the player's own).

set SETTINGS [
    {=key :health_bars =label "settings.health_bars" =choices [:damaged :always :never]}
    {=key :halos =label "settings.halos" =choices [true false]}
    {=key :pins =label "settings.pins" =choices [true false]}
    {=key :carry_marks =label "settings.carry_marks" =choices [false true]}
]

fn setting_choice_label [value] do
    if (value == true) { return {loc "ui.yes"} }
    if (value == false) { return {loc "ui.no"} }
    let name {sym_name value}
    return {loc "settings.choice.{name}"}
end

fn setting_index [setting] do
    let value {pref setting.key setting.choices[0]}
    let i 0
    for choice in setting.choices do
        if (choice == value) { return i }
        set i (i + 1)
    end
    return 0
end

fn setting_row [setting] do
    let labels []
    for choice in setting.choices do
        labels.push {setting_choice_label choice}
    end
    return {ui.combo {loc setting.label} labels {setting_index setting} fn [index] do
        set_pref setting.key setting.choices[index]
    end}
end

# The game's own pictures first, then each pack by its name
fn pack_row [] do
    let ids [""]
    let labels [{loc "settings.pack.none"}]
    let current 0
    let chosen {texture_pack}
    for pack in {texture_packs} do
        if (pack.id == chosen) { set current ids.length }
        ids.push pack.id
        labels.push pack.name
    end
    set PACK_IDS ids
    return {ui.combo {loc "settings.texture_pack"} labels current fn [index] do
        texture_pack PACK_IDS[index]
    end}
end

# on_close puts back whatever this was opened from
fn settings_children [on_close] do
    let children [{ui.text {loc "settings.title"}} {ui.separator}]
    children.push {pack_row}
    for setting in SETTINGS do
        children.push {setting_row setting}
    end
    children.push {ui.separator}
    children.push {ui.button {loc "settings.done"} =width 240 =on_click on_close}
    return children
end
