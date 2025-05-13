local State = {}

function State.load()
end

local fun = require("fun")

function State.init()
    Coroutines = coroutineRunner()
    Blinking = false
    SkebobTeams = teamsBitmap("C:\\Users\\Polanas\\AppData\\Roaming\\DuckGame\\Mods\\HatsPlusPlus2\\content\\skebob.png", vec2(256))
    WhiteTeams = teamsBitmap("C:\\Users\\Polanas\\AppData\\Roaming\\DuckGame\\Mods\\HatsPlusPlus2\\content\\white.png", vec2(32))
    WhiteHat = depthAnimHat(WhiteTeams --[[@as teamsBitmap]])
    WhiteHat.sprite.forceCurrentFrame = 0
end

function State.draw(time)
    imgui.window("debug", function ()
        imgui.text("x:" .. tostring(WhiteHat.position.x))
        imgui.text("y:" .. tostring(WhiteHat.position.y))
    end)
end

---@param time gameTime
---@param hat wearable
function State.update(time, hat)
    WhiteHat:update()
    WhiteHat.position = mouse.positionScreen
    WhiteHat.depth = -0.81
end

return State