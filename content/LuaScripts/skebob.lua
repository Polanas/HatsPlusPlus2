local State = {}

function State.load()
end

local fun = require("fun")

function State.init()
    Coroutines = coroutineRunner()
    Blinking = false
    SkebobTeams = loadTeams("C:\\Users\\Polanas\\AppData\\Roaming\\DuckGame\\Mods\\HatsPlusPlus2\\content\\skebob.png", vec2(256))
    SkebobHat = depthHat(SkebobTeams --[[@as teamsBitmap]])
    SkebobHat:setState(depthHatState.regular)
    SkebobHat.sprite.forceCurrentFrame = 0
end

function State.draw(time)
    imgui.window("skebob", function ()
        imgui.text("some text")
    end)
end

---@param time gameTime
---@param hat wearable
function State.update(time, hat)
    SkebobHat:update()
    print("hi")
    SkebobHat.position = mouse.positionScreen
end

return State