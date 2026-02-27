LEFT_EYE_OFFSET = vec2(-1,4)
RIGHT_EYE_OFFSET = vec2(7,4)

LEFT_EYE_OFFSET_OFFDIR = vec2(2,4)
RIGHT_EYE_OFFSET_OFFDIR = vec2(-6,4)

---@return vec2?
local function nearestDuckPos()
    local nearestDuck =  level.nearest(DGTypes.Duck, ducks.main.position, ducks.main)
    if nearestDuck == nil then
        return nil
    end
    local pos = nearestDuck:field("position")
    if pos == nil then
        return nil
    end

    return vec2(pos:field("x"):asNumber() --[[@as number]], pos:field("y"):asNumber() --[[@as number]])
end

---@param wearable vanillaHat
local function updateEyes(wearable)
    local duckHat = ducks.main.reflect:property("hat")
    if not duckHat then
        return
    end
    
    local pos = duckHat:field("position")
    if not pos then
        return
    end

    local x = pos:field("x"):asNumber() --[[@as number]]
    local y = pos:field("y"):asNumber() --[[@as number]]
    local hatPos = vec2(x,y)

    local nearestPos = nearestDuckPos()
    local look_offset = vec2()
    if nearestPos then
        look_offset = (nearestPos - vec2(x,y)):normalize()
    end

    local mainDuck = ducks.main
    local ragdoll = mainDuck.ragdoll
    if ragdoll == nil then
        EyeHats[1].angle = 0
        EyeHats[2].angle = 0
        if mainDuck.offdir > 0 then
            EyeHats[1].position = hatPos+LEFT_EYE_OFFSET
            EyeHats[2].position = hatPos+RIGHT_EYE_OFFSET
        else
            EyeHats[1].position = hatPos+LEFT_EYE_OFFSET_OFFDIR
            EyeHats[2].position = hatPos+RIGHT_EYE_OFFSET_OFFDIR
        end
    else
        local wearableAngle = wearable:getAngle()
        
        EyeHats[1].angle = wearableAngle
        EyeHats[2].angle = wearableAngle
        local leftOffset, rightOffset 
        if mainDuck.offdir > 0 then
            leftOffset = LEFT_EYE_OFFSET:clone()
            rightOffset = RIGHT_EYE_OFFSET:clone()
        else
            leftOffset = LEFT_EYE_OFFSET_OFFDIR:clone()
            rightOffset = RIGHT_EYE_OFFSET_OFFDIR:clone()
        end
        leftOffset:rotate(wearableAngle, vec2(0,0))
        rightOffset:rotate(wearableAngle, vec2(0,0))
        EyeHats[1].position = wearable:getPosition() + leftOffset
        EyeHats[2].position = wearable:getPosition() + rightOffset
    end
    for _, hat in pairs(EyeHats) do
        hat.position = hat.position + look_offset
        if Blinking or input.down(inputType.quack) then
            hat.position = vec2(0,-1000)
        end
        hat:update()
    end

    wearable.sprite.forceCurrentFrame = Blinking and 1 or nil
end

---@type state
local state = {
    select = function()
        EyeTeams = teamsBitmap(path.join(imagesPath, "eye.aseprite"), vec2(32))
        MouthTeams = teamsBitmap(path.join(imagesPath, "mouth.aseprite"), vec2(32))
    end,

    spawn = function ()
        ---@type depthHat[]
        EyeHats = {}
        for i = 1,2 do
            EyeHats[i] = depthHat(EyeTeams --[[@as teamsBitmap]])
            EyeHats[i].sprite.forceCurrentFrame = 0
            EyeHats[i].depth = 1
            EyeHats[i]:setState(depthHatState.depthInactive)
        end

        MyScheduler = Scheduler.new()
        Blinking = false

        MyScheduler:spawn(function ()
            while true do
                coroutine.yield(math.random() + 3)
                Blinking = true
                coroutine.yield(0.05)
                Blinking = false
            end
        end)
    end,

    ---@param wearable vanillaHat
    update = function (time, wearable)
        updateEyes(wearable)
        MyScheduler:update(time.delta)
    end
}

return state