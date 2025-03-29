State = {}
Start = false
function State.load()
end

local fun = require("fun")
function State.init()
    Coroutines = coroutineRunner()
    Blinking = false
    MouthTeams = teamsBitmap("C:\\Users\\Polanas\\AppData\\Roaming\\DuckGame\\Mods\\HatsPlusPlus2\\content\\mouth.png", vec2(32))
    EyeTeams = teamsBitmap("C:\\Users\\Polanas\\AppData\\Roaming\\DuckGame\\Mods\\HatsPlusPlus2\\content\\nikoEye.png", vec2(32))
    Coroutines:start(function ()
        while true do
            local waitTime = (math.random() + 2) * (5/2)
            coroutine.delay(waitTime)
            Blinking = true
            coroutine.delay(0.05)
            Blinking = false
        end
    end)
    MouthHat = depthAnimHat(MouthTeams --[[@as teamsBitmap]])
    MouthHat.depth = ducks.main.depth + 1
    MouthHat.sprite:addAnim(animation("shout", 0.1, false, fun.totable(fun.range(1,4):map(animFrame))))
    MouthHat.sprite:addAnim(animation("normal", 0, true, {animFrame(0)}))
    MouthHat.sprite:addAnim(animation("smirk", 0.1, false, fun.totable(fun.range(5,8):map(animFrame))))
    MouthHat.sprite:setAnim("normal")
    ---@type depthHat[]
    EyeHats = {}
    for i = 1,2 do
        EyeHats[i] = depthHat(EyeTeams --[[@as teamsBitmap]])
        EyeHats[i].sprite.forceCurrentFrame = 0
        EyeHats[i].depth = ducks.main.depth + 1
        EyeHats[i]:setState(depthHatState.depthInactive)
    end
end

---@class wearable
---@field sprite hatSprite
---@field position vec2
---@field angle number
---@field depth number

HatDepth = nil
HoldObjectDepth = nil
function State.draw(time)
    imgui.text("hat: " .. tostring(HatDepth))
    imgui.text("hold object: " .. tostring(HoldObjectDepth))
end
HatAngle = 0
---@param time gameTime
---@param hat wearable
function State.update(time, hat)
    Coroutines:update(time.delta)
    local holdObject = ducks.main.reflect:property("holdObject")
    if not (holdObject == nil) then
        local depth = holdObject:property("depth")
        if not (depth == nil) then
            local value = depth:field("value"):asNumber()
            HoldObjectDepth = value
        end
    end
    local duckHat = ducks.main.reflect:property("hat")
    if not (duckHat == nil) then
        local pos = duckHat:field("position")
        if not (pos == nil) then
            local x = pos:field("x"):asNumber()
            local y = pos:field("y"):asNumber()
            
            if (ducks.main.offdir > 0) then
                EyeHats[1].position = vec2(x-1,y+4)
                EyeHats[2].position = vec2(x + 7,y+4)
            else
                EyeHats[1].position = vec2(x+2,y+4)
                EyeHats[2].position = vec2(x-6,y+4)
            end

            local closestDuck = level.nearest(DGTypes.Duck, ducks.main.position, ducks.main)
            if not (closestDuck == nil) then
                local pos = closestDuck:field("position")
                if not (pos == nil) then
                    local pos = vec2(pos:field("x"):asNumber(), pos:field("y"):asNumber())
                    pos.y = pos.y - 5

                    for _, hat in pairs(EyeHats) do
                        local angle = -maths.pointDirection(pos, hat.position)
                        angle = angle + 180
                        HatAngle = angle
                        angle = math.rad(angle)

                        local offset = vec2(1,0):rotated(angle, vec2(0))
                        hat.position = hat.position + offset
                    end
                end
            end
        end
    end
    hat.sprite.forceCurrentFrame = Blinking and 0 or 1
    for _, hat in pairs(EyeHats) do
        if Blinking then
            hat.position.y = -10000
        end
        hat.sprite.forceCurrentFrame = 0
        hat.depth = 1
        hat:setState(depthHatState.depthInactive)
        hat:update()
    end
    MouthHat.position = ducks.main.position + vec2(0,1);
    if keyboard.pressed(keys.j) then
        MouthHat.sprite:setAnim("shout")
    end
    if keyboard.pressed(keys.k) then
        MouthHat.sprite:setAnim("smirk")
    end
    if keyboard.pressed(keys.l) then
        MouthHat.sprite:setAnim("normal")
    end
    MouthHat.depth = hat.depth + 0.1
    HatDepth = MouthHat.depth
    MouthHat.angle = 0
    -- MouthHat.sprite.forceCurrentFrame = 0
    MouthHat:update()
end

return State