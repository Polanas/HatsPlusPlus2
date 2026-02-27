---@meta

---@class keyboard
---@field repeat boolean
---@field alt boolean
---@field shift boolean
keyboard = {}

---@return boolean
function keyboard.nothingPressed()
end

---@param key keys
---@param any boolean?
function keyboard.pressed(key, any)
end

---@param key keys
function keyboard.released(key)
end

---@param key keys
function keyboard.down(key)
end
