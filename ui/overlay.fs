# What's drawn over the world for the player's eyes only (render thread): health bars, halos on
# units something is hiding, and pins where units were sent.
#
# Each of these is a setting (ui/settings.fs). All of it runs on the client and asks the server for
# nothing: a pin is the order as this player gave it, and it goes when the units get there.

set HEALTH_BAR_WIDTH 0.7
set HEALTH_BAR_HEIGHT 2.05
set HALO_ALPHA 0.55
set HALO_SELECTED [0.85 0.7 1.0 0.75]
set PIN_MOVE [1.0 0.82 0.25 0.95]
set PIN_ATTACK [1.0 0.3 0.2 0.95]
set PIN_DROP_SECONDS 0.35
set PIN_DROP_HEIGHT 4.0
set PIN_RING_SECONDS 0.45
set PIN_STEM 1.1
set PIN_DOT_STEP 1.2
# A pin is drawn at its own size up to this far from the camera, and grows beyond it, to this much
set PIN_NEAR 14.0
set PIN_MOST_SCALE 3.0
# A pin is passed when everyone it's for is this near, plus a little for each extra body
set PIN_REACHED 2.5
# Or when they've all stood still for this long: as near as they could get
set PIN_STALL_SECONDS 3.0

set ui_clock 0.0
set faction_colors {map}

fn note_faction_color [id color] do
    faction_colors.set {to_sym id} color
end

fn halo_color [id faction] do
    if {selection.contains id} { return HALO_SELECTED }
    let c {faction_colors.get {to_sym faction}}
    if (c == nil) { set c [0.4 0.6 1.0] }
    return [c[0] c[1] c[2] HALO_ALPHA]
end

# ---- Health and halos

fn draw_unit_marks [] do
    let bars {pref :health_bars :damaged}
    let halos {pref :halos true}
    if ((bars == :never) and (not halos)) { return nil }
    let size {window_size}
    for id in {pick_entities 0 0 size[0] size[1]} do
        let side {entity_faction id}
        if ((side != nil) and (side != 0)) do
            let p {entity_pos id}
            if (p != nil) do
                if halos do
                    let c {halo_color id side}
                    marker_xray id c[0] c[1] c[2] c[3]
                end
                let health {entity_health id}
                if ((bars == :always) or ((bars == :damaged) and (health < 1.0))) {
                    marker_bar p[0] (p[1] + HEALTH_BAR_HEIGHT) p[2] HEALTH_BAR_WIDTH health
                }
            end
        end
    end
end

# ---- Pins
#
# An order is the units it went to and the pins they're to pass, in turn. A new order takes its
# units out of the old ones; with Shift held, it's another pin at the end of theirs.

set orders []

fn units_key [units] do
    let ids []
    # Whole numbers, so that ids the game sends back make the same key as the ones that went
    for id in units do
        ids.push {to_int id}
    end
    ids.sort
    let key ""
    for id in ids do
        set key "{key},{id}"
    end
    return key
end

fn make_pin [x y z target] do
    return {=x x =y y =z z =target target =born ui_clock}
end

fn without_units [units] do
    let kept []
    for order in orders do
        set order.units {order.units.filter fn [id] (not {units.contains id})}
        if (order.units.length > 0) { kept.push order }
    end
    set orders kept
end

fn drop_pin [units x y z target queued] do
    if (not {pref :pins true}) { return nil }
    let pin {make_pin x y z target}
    if queued do
        let key {units_key units}
        for order in orders do
            if (order.key == key) do
                order.pins.push pin
                return nil
            end
        end
    else do
        without_units units
    end
    let ids []
    for id in units do
        ids.push id
    end
    orders.push {=units ids =key {units_key ids} =pins [pin] =still 0.0 =last nil}
end

# The game says where an order really went, which isn't always where it was dropped: into a
# building rather than onto its roof, say
fn move_pin [units x y z] do
    let key {units_key units}
    for order in orders do
        if (order.key == key) do
            let pin order.pins[order.pins.length - 1]
            if (pin.target == nil) do
                set pin.x (x + 0.5)
                set pin.y y
                set pin.z (z + 0.5)
            end
        end
    end
end

fn pin_pos [pin] do
    if (pin.target == nil) { return [pin.x pin.y pin.z] }
    let p {entity_pos pin.target}
    if (p == nil) { return nil }
    return p
end

# How near its units are to the pin they're making for, and whether they've moved since last frame
fn order_progress [order at dt] do
    let alive []
    let far 0.0
    let moved 0.0
    let sum [0.0 0.0]
    for id in order.units do
        let p {entity_pos id}
        if (p != nil) do
            alive.push id
            set far {max far {sqrt (((p[0] - at[0]) * (p[0] - at[0])) + ((p[2] - at[2]) * (p[2] - at[2])))}}
            set sum [(sum[0] + p[0]) (sum[1] + p[2])]
        end
    end
    set order.units alive
    if (alive.length == 0) { return false }
    let centre [(sum[0] / alive.length) (sum[1] / alive.length)]
    if (order.last != nil) {
        set moved ({abs (centre[0] - order.last[0])} + {abs (centre[1] - order.last[1])})
    }
    set order.last centre
    if (moved < 0.01) { set order.still (order.still + dt) } { set order.still 0.0 }
    return (far <= (PIN_REACHED + {sqrt alive.length}))
end

fn update_orders [dt] do
    let kept []
    for order in orders do
        let keep true
        if (order.pins.length == 0) { set keep false }
        if keep do
            let first order.pins[0]
            let at {pin_pos first}
            if (at == nil) do
                order.pins.remove 0
            elif (first.target == nil) do
                let reached {order_progress order at dt}
                if (reached or (order.still > PIN_STALL_SECONDS)) do
                    order.pins.remove 0
                    set order.still 0.0
                end
            else do
                order_progress order at dt
            end
            if ((order.pins.length == 0) or (order.units.length == 0)) { set keep false }
        end
        if keep { kept.push order }
    end
    set orders kept
end

# Bigger the further off the camera is, so a pin reads at any zoom
fn pin_scale [at] do
    let eye {cam_pos}
    let dx (eye[0] - at[0])
    let dy (eye[1] - at[1])
    let dz (eye[2] - at[2])
    return {clamp ({sqrt ((dx * dx) + (dy * dy) + (dz * dz))} / PIN_NEAR) 1.0 PIN_MOST_SCALE}
end

# Falls from above and bounces once, with a ring going out where it lands
fn draw_pin [pin at] do
    let k {pin_scale at}
    let age (ui_clock - pin.born)
    let color PIN_MOVE
    if (pin.target != nil) { set color PIN_ATTACK }
    let lift 0.0
    if (age < PIN_DROP_SECONDS) do
        let t (age / PIN_DROP_SECONDS)
        set lift (PIN_DROP_HEIGHT * (1.0 - t) * (1.0 - t))
    elif (age < (PIN_DROP_SECONDS * 1.6)) do
        let t ((age - PIN_DROP_SECONDS) / (PIN_DROP_SECONDS * 0.6))
        set lift (0.35 * {sin (t * 3.14159)})
    end
    let base (at[1] + (lift * k))
    if (pin.target != nil) { set base (at[1] + 2.4 + (lift * k)) }
    let stem (0.08 * k)
    let head (0.32 * k)
    marker_box (at[0] - (stem / 2)) base (at[2] - (stem / 2)) stem (PIN_STEM * k) stem color[0] color[1] color[2] color[3]
    marker_box (at[0] - (head / 2)) (base + (PIN_STEM * k)) (at[2] - (head / 2)) head head head color[0] color[1] color[2] color[3]
    if (pin.target != nil) { return nil }
    if ((age > PIN_DROP_SECONDS) and (age < (PIN_DROP_SECONDS + PIN_RING_SECONDS))) do
        let t ((age - PIN_DROP_SECONDS) / PIN_RING_SECONDS)
        marker_ring at[0] (at[1] + 0.05) at[2] ((0.3 + (t * 0.9)) * k) color[0] color[1] color[2] (1.0 - t)
    end
    marker_ring at[0] (at[1] + 0.05) at[2] (0.35 * k) color[0] color[1] color[2] 0.7
end

fn draw_trail [a b color] do
    let dx (b[0] - a[0])
    let dy (b[1] - a[1])
    let dz (b[2] - a[2])
    let length {sqrt ((dx * dx) + (dz * dz))}
    let steps {floor (length / PIN_DOT_STEP)}
    for i in (1..steps) do
        let t (i / steps)
        let x (a[0] + (dx * t))
        let y (a[1] + (dy * t))
        let z (a[2] + (dz * t))
        let k {pin_scale [x y z]}
        let dot (0.12 * k)
        marker_box (x - (dot / 2)) (y + 0.05) (z - (dot / 2)) dot 0.06 dot color[0] color[1] color[2] 0.6
    end
end

fn draw_orders [] do
    for order in orders do
        # From where they are now, so it's plain which pin is whose
        let last nil
        if (order.last != nil) do
            let lead {entity_pos order.units[0]}
            if (lead != nil) { set last [order.last[0] lead[1] order.last[1]] }
        end
        for pin in order.pins do
            let at {pin_pos pin}
            if (at != nil) do
                if (last != nil) { draw_trail last at PIN_MOVE }
                draw_pin pin at
                set last at
            end
        end
    end
end

fn draw_overlay [] do
    let rate {fps}
    let dt (1.0 / {max rate 20})
    set ui_clock (ui_clock + dt)
    update_orders dt
    draw_orders
    draw_unit_marks
end
