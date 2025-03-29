---@diagnostic disable-next-line: undefined-global
local imguiFns = imguiFns
---@class imgui
imgui = {}

---@param name string
---@param func fun()
function imgui.window(name, func)
    imguiFns.window(name, func)
end

---@param text string
---@return boolean
function imgui.button(text)
    return imguiFns.button(text)
end

---@param text string
function imgui.text(text)
    imguiFns.text(text)
end