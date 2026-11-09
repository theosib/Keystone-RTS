# Input through the real listener chain, in a real window: who gets a key or a click when the
# GUI is in the way. tests/run_ui.sh adds this to the end of ui/main.fs in a scratch copy.

set t_frame 0
set t_clicks 0
set t_eye 0
set t_low 0
set t_rise 0
set t_hit nil
set t_menu nil

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

# Other Hands are at home, and the wide sky view reaches them
fn t_near [id] do
    let p {entity_pos id}
    return ({abs p[0]} < 20)
end

fn t_tap [key] do
    input_key key true
    input_key key false
end

fn t_tap_shifted [key] do
    input_key "left_shift" true
    t_tap key
    input_key "left_shift" false
end

# Only neighbours fold into one row, so a kind can be on several
fn t_queued [unit] do
    let total 0
    for row in state.queue do
        if ((row.key == "muster") and (row.unit == unit)) { set total (total + row.times) }
    end
    return total
end

fn t_train [] do
    for row in state.queue do
        if (row.key == "train") { return row.count }
    end
    return 0
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

    # One or many, each line of the queue is a sentence
    if (t_frame == 64) do
        fn says [row wanted] do
            let got {queue_row_text row}
            t_check "the queue says \"{wanted}\", got \"{got}\"" (got == wanted)
        end
        fn building [times percent] do
            return {=key "build" =building "shelter" =percent percent =times times =hands 0 =needs nil}
        end
        fn unit [name times] do
            return {=key "muster" =unit name =housed true =times times}
        end
        says {building 1 24} "Build Shelter 24%"
        says {building 3 24} "Build 3 Shelters 24%"
        says {unit "militia" 1} "Muster Militia"
        says {unit "militia" 8} "Muster 8 Militia"
        says {unit "shortbow" 2} "Muster 2 Shortbows"
        says {unit "scout" 2} "Muster 2 Pathfinders"
        says {=key "train" =count 1 =housed true =times 1} "Train Hand"
        says {=key "train" =count 8 =housed false =times 1} "Train 8 Hands (no housing)"
        says {=key "lay" =percent 40 =times 2 =hands 0 =needs nil} "Lay 2 runs of blocks 40%"
        says {=key "floor" =percent 5 =times 1 =hands 0 =needs nil} "Raise a yard floor 5%"
        says {=key "build" =building "sawpit" =percent 0 =times 2 =hands 3 =needs nil} "Build 2 Sawpits 0% (3 Hands)"

        let groups [{=who "hand" =count 3 =stack [{=key "build" =building "shelter" =percent 24}]}
                     {=who "militia" =count 2 =stack []} {=who "scout" =count 1 =stack []}]
        let got {tasks_text groups}
        t_check "the Selected panel reads, got \"{got}\"" (got == "3 Hands\n  Build Shelter 24%\n2 Militia\nPathfinder")
    end
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
        set t_low p[1]
        input_key "w" true
    end
    if (t_frame > 200) do
        let p {cam_pos}
        set t_rise {max t_rise (p[1] - t_eye)}
        set t_eye p[1]
        # The body may still be coming down after however many frames that was, and walking only
        # goes up from where it lands
        set t_low {min t_low p[1]}
    end
    if (t_frame == 440) do
        input_key "w" false
        let climbed (t_eye - t_low)
        print "climbed {climbed}, at most {t_rise} in a frame"
        t_check "walks up one block at a time, and no further" ({abs (climbed - 2.0)} < 0.01)
        t_check "the eye never jumps up a block" (t_rise < 0.4)
        set t_hit {pick_view}
        input_button 0 true 640 360
        input_button 0 false 640 360
    end

    # What the Steward's own hands do shows at once, and doesn't wait for the server
    if (t_frame == 441) do
        let now {pick_view}
        t_check "a broken block is gone by the next frame" (now.distance > t_hit.distance)
    end
    if (t_frame == 448) do
        let now {pick_view}
        t_check "and the server agrees" (now.distance > t_hit.distance)
        do_action "toggle_view"
    end

    # Left standing, the Steward is no unit to select: a box around it and a Hand finds the Hand
    if (t_frame == 454) { cam_focus 0 3 }
    if (t_frame == 462) do
        let picked {pick_entities 0 0 1280 720}
        let near {picked.filter fn [id] {t_near id}}
        print "picked {near.length} nearby"
        t_check "only the Hand can be selected, not the Steward" (near.length == 1)
    end

    # Keys and palette clicks, one or a batch. There's no room to house anyone, so the queue stays.
    if (t_frame == 470) { t_tap "m" }
    if (t_frame == 474) { t_tap_shifted "b" }
    if (t_frame == 478) { t_tap "h" }
    if (t_frame == 482) { t_tap_shifted "h" }
    if (t_frame == 484) { t_to 100 637 }
    if (t_frame == 486) { input_button 0 true 100 637 }
    if (t_frame == 488) { input_button 0 false 100 637 }
    if (t_frame == 490) { input_key "left_shift" true }
    if (t_frame == 492) { input_button 0 true 100 637 }
    if (t_frame == 494) { input_button 0 false 100 637 }
    if (t_frame == 496) { input_key "left_shift" false }
    if (t_frame == 498) { t_tap "p" }
    if (t_frame == 502) { t_tap "z" }
    if (t_frame == 504) do
        t_check "a key picks up a building" (placing == :shelter)
        let home state.home
        t_check "the game says the Hall's ground is taken" {is_taken home[0] home[1] (home[0] + 1) (home[1] + 1)}
        t_check "and not the ground far from it" (not {is_taken (home[0] - 60) home[1] (home[0] - 59) (home[1] + 1)})
    end
    if (t_frame == 506) { t_tap "escape" }
    if (t_frame == 508) { t_check "and Escape puts it down" (placing == nil) }
    if (t_frame == 510) { t_tap "l" }
    if (t_frame == 512) do
        t_check "the wall has a key too" laying_fence
        t_tap "escape"
    end
    # Shift-click puts one down and keeps another in hand, on ground that reads as taken at once
    if (t_frame == 514) { t_tap "z" }
    if (t_frame == 516) { t_to 640 360 }
    if (t_frame == 518) { input_key "left_shift" true }
    if (t_frame == 520) { input_button 0 true 640 360 }
    if (t_frame == 522) { input_button 0 false 640 360 }
    if (t_frame == 524) { input_key "left_shift" false }
    if (t_frame == 526) do
        t_check "Shift-click keeps a building in hand" (placing == :shelter)
        let hit {pick_ground 640 360}
        t_check "and where it went is taken straight away" ((hit != nil) and {placement_at hit}.taken)
        t_tap "escape"
    end
    if (t_frame == 608) { t_check "and the game took it" (state.sites > 0) }
    if (t_frame == 545) do
        print "queue {state.queue}"
        t_check "M, a click and a Shift-click queue 10 Militia, got {t_queued "militia"}" ({t_queued "militia"} == 10)
        t_check "Shift-B queues 8 Shortbows" ({t_queued "shortbow"} == 8)
        t_check "P queues a Pathfinder" ({t_queued "scout"} == 1)
        t_check "H and Shift-H train 9 Hands" ({t_train} == 9)
    end

    # None of it where the keys mean something else
    if (t_frame == 546) { do_action "toggle_view" }
    if (t_frame == 550) { t_tap "m" }
    if (t_frame == 552) { t_tap "z" }
    if (t_frame == 554) do
        t_check "on foot a key picks up nothing" (placing == nil)
        do_action "toggle_view"
    end
    if (t_frame == 558) { open_menu }
    if (t_frame == 560) { t_tap "m" }
    if (t_frame == 564) { close_menu }
    if (t_frame == 566) { do_action "chat" }
    if (t_frame == 570) { t_tap "m" }
    if (t_frame == 574) { do_action "cancel" }
    if (t_frame == 576) { stop_typing }
    if (t_frame == 610) do
        t_check "keys queue nothing on foot, in the menu or in chat" ({t_queued "militia"} == 10)
    end

    # Space stops the game from overhead, and on foot it's still a jump
    if (t_frame == 612) { t_tap "space" }
    if (t_frame == 622) do
        t_check "Space pauses overhead" {game_paused}
        do_action "toggle_view"
    end
    if (t_frame == 626) { t_tap "space" }
    if (t_frame == 636) do
        t_check "and doesn't, on foot" {game_paused}
        do_action "toggle_view"
    end
    if (t_frame == 640) { t_tap "space" }
    if (t_frame == 650) { t_check "Space carries on" (not {game_paused}) }

    # The menu's buttons, by hand: each one's closure has to outlive the function that made it
    if (t_frame == 652) { open_menu }
    if (t_frame == 656) do
        set t_menu menu_panel
        gui_click 640 297
    end
    if (t_frame == 662) do
        t_check "Settings opens from the menu" ((menu_panel != nil) and (menu_panel != t_menu))
        set t_menu menu_panel
        gui_click 535 436
    end
    if (t_frame == 668) do
        t_check "Done goes back to the menu" ((menu_panel != nil) and (menu_panel != t_menu))
        set t_menu menu_panel
        gui_click 640 378
    end
    if (t_frame == 674) do
        t_check "New Game asks first" ((menu_panel != nil) and (menu_panel != t_menu))
        set t_menu menu_panel
        gui_click 640 404
    end
    if (t_frame == 680) do
        t_check "Cancel goes back to the menu" ((menu_panel != nil) and (menu_panel != t_menu))
        gui_click 640 260
    end
    if (t_frame == 686) { t_check "Resume closes the menu" (menu_panel == nil) }

    # Once shift-clicks have chosen kinds, a box picks only those. Escape lets it pick anything.
    if (t_frame == 688) do
        cam_focus 0 3
        set kind_filter [:militia]
    end
    if (t_frame == 696) { t_to 300 150 }
    if (t_frame == 698) { input_button 0 true 300 150 }
    if (t_frame == 700) { t_to 1000 560 }
    if (t_frame == 702) { input_button 0 false 1000 560 }
    if (t_frame == 704) do
        let boxed {mine_in 300 150 1000 560}
        print "{boxed.length} of ours in the box, {selection.length} picked"
        t_check "the box has some of ours in it" (boxed.length > 0)
        t_check "the client knows what they are" ({kinds_of boxed} != nil)
        t_check "a box picks only the kinds chosen" (selection.length == 0)
        do_action "cancel"
    end
    if (t_frame == 706) { t_to 300 150 }
    if (t_frame == 708) { input_button 0 true 300 150 }
    if (t_frame == 710) { t_to 1000 560 }
    if (t_frame == 712) { input_button 0 false 1000 560 }
    if (t_frame == 714) do
        t_check "and after Escape it picks anything" ((selection.length > 0) and (kind_filter == nil))
        print "UI TEST DONE"
    end
end
