---@meta

---@class currentLevel
local currentLevel = {}

---@param typeName string?
---@return reflectHashset
function currentLevel:things(typeName)
end

---@class level
---@field current currentLevel
level = {}

---@class reflect
local reflect = {}

---@class reflectList
---@field count integer
local reflectList = {}

---@param func fun(reflect: reflect)
function reflectList:iter(func)
end

---@param index integer
---@return reflect?
function reflectList:get(index)
end

---@class reflectHashset
---@field count integer
local reflectHashset = {}

---@param func fun(reflect: reflect)
function reflectHashset:iter(func)
end

---@class reflectEnumerable
---@field count integer
local reflectEnumerable = {}

---@param func fun(reflect: reflect)
function reflectEnumerable:iter(func)
end

---@return string
function reflect:typeName()
end

---@return number?
function reflect:asNumber()
end

---@return reflectType
function reflect:type()
end

---@param itemTypeName string
---@return reflectList?
function reflect:asList(itemTypeName)
end

---@param itemTypeName string
---@return reflectHashset?
function reflect:asHashset(itemTypeName)
end

---@param itemTypeName string
---@return reflectEnumerable?
function reflect:asEnumerable(itemTypeName)
end

---@return string?
function reflect:asString()
end

---@return boolean?
function reflect:asBoolean()
end

---@param name string
---@return reflect
function reflect:field(name)
end

---@param name string
---@return reflect
function reflect:property(name)
end

---@param typeName DGTypes
---@param position vec2
---@param ignore? duck|reflect
---@return reflect?
function level.nearest(typeName, position, ignore) end

---@param typeName DGTypes
---@param position vec2
---@param radius number
---@param ignore? duck|reflect
---@return reflect?
function level.checkCircle(typeName, position, radius, ignore) end

---@param typeName DGTypes
---@param position vec2
---@param radius number
---@return reflect?
function level.checkCircleAll(typeName, position, radius) end