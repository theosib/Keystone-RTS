# The player's own settings (render thread). Kept with pref and set_pref, so they outlast the game
# and belong to this player, not to the game being played. See docs/PLAN.md for what's to come.
#
# Each is a row in SETTINGS: a pref name, a label, and the choices, the first being the default.

set SETTINGS [
    {=key :health_bars =label "settings.health_bars" =choices [:damaged :always :never]}
    {=key :halos =label "settings.halos" =choices [true false]}
    {=key :pins =label "settings.pins" =choices [true false]}
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

# on_close puts back whatever this was opened from
fn settings_children [on_close] do
    let children [{ui.text {loc "settings.title"}} {ui.separator}]
    for setting in SETTINGS do
        children.push {setting_row setting}
    end
    children.push {ui.separator}
    children.push {ui.button {loc "settings.done"} =width 240 =on_click on_close}
    return children
end
