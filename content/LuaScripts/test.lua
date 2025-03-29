
-- ---@enum states
-- local states = {
--     state1 = 1,
--     state2 = 2,
-- }

-- State = {}

-- local callback = stateMachine.callback
-- local newStateMachine = stateMachine

-- --when hat is loaded
-- function State.load()
-- end

-- local fun = require("fun")
-- local inspect = require("inspect")

-- -- local fun = require("fun")
-- --when hat is first selected
-- function State.init()
--     Coroutines = coroutineRunner()
--     MyTeams = teamsBitmap("C:\\Users\\Polanas\\AppData\\Roaming\\DuckGame\\Mods\\HatsPlusPlus2\\content\\animation.png", vec2(32))
--     Hat = vanillaHat(MyTeams --[[@as teamsBitmap]])
-- end

-- ---@param time gameTime
-- function State.draw(time)
--     imgui.window("lua window", function ()
--         level.current:things("Duck"):iter(function (duck)
--             imgui.text(tostring(duck:typeName()))
--         end)
--         if imgui.button("stuff") then
--         local anim = animation("test", 0.2, true,  fun.totable(fun.range(0,5):map(function (num)
--             return animFrame(num)
--         end)))
--         Hat:setStrappedOn(true)
--         Hat:equip(ducks.main)
--         Hat.sprite:addAnim(anim)
--         Hat.sprite:setAnim("test")
--         Hat:setPosition(vec2(20))
--         end
--         -- local things = level.current:property("things")
--         -- local thingsList = things:field("_bigList"):asHashset("Thing")
--         -- if thingsList ~= nil then
--         --     thingsList:iter(function (reflect)
--         --         imgui.text(reflect:typeName())
--         --     end)
--         -- end
        
--         -- if duck ~= nil then
--         --     local position = duck:field("position")
--         --     if position ~= nil then
--         --         local x = position:field("x")
--         --         local y = position:field("y")
--         --         imgui.text("x: " .. tostring(x:asNumber()))
--         --         imgui.text("y: " .. tostring(y:asNumber()))
--         --     end
--         -- end
--     end)
-- end

-- --when hat is spawned
-- function State.start()
--     -- stateMachine = newStateMachine()
--     -- stateMachine:addCallbacks(states.state1, {
--     --     updateCallback = callback.coroutine(function ()
--     --         coroutine.delay(1)
--     --         print("switching to state 2!")
--     --         coroutine.yieldBreak(states.state2)
--     --     end)
--     -- })
--     -- stateMachine:addCallbacks(states.state2, {
--     --     updateCallback = callback.coroutine(function ()
--     --         coroutine.delay(1)
--     --         print("switching to state 1!")
--     --         coroutine.yieldBreak(states.state1)
--     --     end),
--     -- })
--     -- stateMachine:setState(states.state1)
-- end

-- ---@param time gameTime
-- function State.update(time)
--     -- local hat = Hat
-- end
-- --TODO: account for forceAnimFrame in AnimDepthHat
-- --FIXED: BUG: setting anim on the same frame as creating AnimDepthHat hat fails
-- return State