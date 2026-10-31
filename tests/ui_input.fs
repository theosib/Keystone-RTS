# Input through the real listener chain, in a real window: who gets a key or a click when the
# GUI is in the way. tests/run_ui.sh adds this to the end of ui/main.fs in a scratch copy.

set t_frame 0
set t_clicks 0
set t_eye 0
set t_floor 0
set t_rise 0

fn t_to [x y] do
    let m {mouse_pos}
    input_move (x - m[0]) (y - m[1])
end

fn t_check [label ok] do
    if ok do
        print "PASS {label}"
    else do
        print "FAIL {label}"
    end
end

on :mouse do
    if ((view_mode == :embodied) and (mouse_event == :down)) { set t_clicks (t_clicks + 1) }
end

on :frame do
    set t_frame (t_frame + 1)

    # Hold W, press on a panel, and let W go while the GUI has the mouse
    if (t_frame == 58) { t_to 40 126 }
    if (t_frame == 60) { input_key "w" true }
    if (t_frame == 62) { t_check "the panel is under the cursor" {gui_has_mouse} }
    if (t_frame == 66) { input_button 0 true 40 126 }
    if (t_frame == 70) { input_key "w" false }
    if (t_frame == 74) { input_button 0 false 40 126 }
    if (t_frame == 80) do
        t_check "letting go of W during a panel click stops the pan" (not {input_held})
        input_key "w" true
    end

    # A clicked panel doesn't keep the keyboard
    if (t_frame == 84) { t_check "W works straight after clicking a panel" {input_held} }
    if (t_frame == 86) { input_key "w" false }
    if (t_frame == 90) do
        t_check "and so does letting go of it" (not {input_held})
        input_button 0 true 40 126
    end

    # What the GUI took stays with the GUI
    if (t_frame == 94) { input_key "w" true }
    if (t_frame == 98) { t_check "W pressed during a panel click stays with the GUI" (not {input_held}) }
    if (t_frame == 100) { input_button 0 false 40 126 }
    if (t_frame == 104) { input_key "w" false }
    if (t_frame == 108) do
        t_check "and its release does too" (not {input_held})
        t_to 700 400
    end

    # A drag that ends over a panel still ends
    if (t_frame == 114) { input_button 0 true 700 400 }
    if (t_frame == 118) { t_to 40 126 }
    if (t_frame == 122) { t_check "a drag starts in the world" (drag_start != nil) }
    if (t_frame == 124) { input_button 0 false 40 126 }
    if (t_frame == 130) do
        t_check "and ends over a panel" (drag_start == nil)
        do_action "toggle_view"
    end

    # On foot the hidden cursor can't reach the panels
    if (t_frame == 138) { t_check "on foot the panels ignore the cursor" (not {gui_has_mouse}) }
    if (t_frame == 140) do
        input_key "w" true
        input_button 0 true 40 126
    end
    if (t_frame == 144) { input_key "w" false }
    if (t_frame == 148) { input_button 0 false 40 126 }
    if (t_frame == 152) do
        t_check "a click on foot goes to the world" (t_clicks == 1)
        t_check "and W lets go" (not {input_held})
        do_action "toggle_view"
    end

    # And back in the sky they work again
    if (t_frame == 158) { t_to 40 126 }
    if (t_frame == 164) do
        t_check "in the sky the panels take the mouse again" {gui_has_mouse}
        cam_focus 0 0
    end

    # Walk up the stairs in ui_step.fsc to the wall at the top
    if (t_frame == 170) { do_action "toggle_view" }
    if (t_frame == 200) do
        let p {cam_pos}
        set t_eye p[1]
        set t_floor (t_eye - 1.65)
        print "floor {t_floor}"
        input_key "w" true
    end
    if (t_frame > 200) do
        let p {cam_pos}
        set t_rise {max t_rise (p[1] - t_eye)}
        set t_eye p[1]
    end
    if (t_frame == 440) do
        input_key "w" false
        let climbed (t_eye - 1.65 - t_floor)
        print "climbed {climbed}, at most {t_rise} in a frame"
        t_check "walks up one block at a time, and no further" ({abs (climbed - 2.0)} < 0.01)
        t_check "the eye never jumps up a block" (t_rise < 0.4)
        print "UI TEST DONE"
    end
end
