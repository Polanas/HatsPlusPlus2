
---@class vec3
---@field x number
---@field y number
---@field z number
---@operator add(vec3): vec3
---@operator add(number): vec3
---@operator sub(vec3): vec3
---@operator sub(number): vec3
---@operator mul(vec3): vec3
---@operator mul(number): vec3
---@operator div(vec3): vec3
---@operator div(number): vec3
---@operator mod(vec3): vec3
---@operator mod(number): vec3
---@operator unm:vec3
---@operator pow(number): vec3
---@operator pow(vec3): vec3

---@class vec3
local methods = {
    ---@param self vec3
    ---@return vec3
    clone = function(self)
        return vec3(self[1], self[2], self[3])
    end,
    
    ---@param self vec3
    ---@return number
    length = function(self)
        return math.sqrt(self[1] * self[1] + self[2] * self[2] + self[3] * self[3])
    end,

    ---@param self vec3
    ---@return number
    lengthSquared = function(self)
        return self[1] * self[1] + self[2] * self[2] + self[3] * self[3]
    end,

    ---@param self vec3
    ---@return vec3
    normalize = function(self)
        return self / self:length()
    end,

    ---@param self vec3
    ---@return vec3
    abs = function(self)
        return vec3(math.abs(self[1]), math.abs(self[2]), math.abs(self[3]))
    end,

    ---@param self vec3
    ---@return number
    dot = function(self, other)
        return self[1] * other[1] + self[2] * other[2] + self[3] * other[3]
    end,

    ---@param incident vec3
    ---@param normal vec3
    ---@return vec3
    reflect = function(incident, normal)
        return incident - 2 * normal:dot(incident) * normal
    end,

    ---@param self vec3
    ---@param other vec3
    ---@return vec3
    min = function(self, other)
        return vec3(math.min(self[1], other[1]), math.min(self[2], other[2]),
    math.max(self[3], other[3]))
    end,

    ---@param self vec3
    ---@param other vec3
    ---@return vec3
    max = function(self, other)
        return vec3(math.max(self[1], other[1]), math.max(self[2], other[2]),
    math.max(self[3], other[3]))
    end,

    ---@param self vec3
    ---@return number, number, number
    unpack = function(self)
        return self[1], self[2], self[3]
    end,

    type = "vec3",
}

local metatable = {
    __newindex = function(self, key, value)
        if key == "x" then
            rawset(self, 1, value)
        elseif key == "y" then
            rawset(self, 2, value)
        elseif key == "z" then
            rawset(self, 3, value)
        end
    end,
    __index = function(self, key)
        if key == "x" then
            return self[1]
        elseif key == "y" then
            return self[2]
        elseif key == "z" then
            return self[3]
        else
            return rawget(methods, key)
        end
    end,
    __add = function(a, b)
        if type(a) == "number" then
            return vec3(a + b[1], a + b[2], a + b[3])
        else
            return vec3(a[1] + b[1], a[2] + b[2], a[3] + b[3])
        end
    end,
    __sub = function(a, b)
        if type(a) == "number" then
            return vec3(a - b[1], a - b[2], a - b[3])
        else
            return vec3(a[1] - b[1], a[2] - b[2], a[3] - b[3])
        end
    end,

    __mul = function(a, b)
        if type(a) == "number" then
            return vec3(a * b[1], a * b[2], a * b[3])
        else
            return vec3(a[1] * b[1], a[2] * b[2], a[3] * b[3])
        end
    end,
    __div = function(b,a)
        if type(a) == "number" then
            return vec3(a / b[1], a / b[2], a / b[3])
        else
            return vec3(a[1] / b[1], a[2] / b[2], a[3] / b[3])
        end
    end,

    __mod = function(a, b)
        if type(a) == "number" then
            return vec3(a % b[1], a % b[2], a % b[3])
        else
            return vec3(a[1] % b[1], a[2] % b[2], a[3] % b[3])
        end
    end,
    __unm = function(a)
        return vec3(-a[1], -a[2], -a[3])
    end,
    __pow = function(a, b)
        if type(a) == "number" then
            return vec3(a ^ b[1], a ^ b[2], a ^ b[3])
        else
            return vec3(a[1] ^ b[1], a[2] ^ b[2], a[3] ^ b[3])
        end
    end,
    __eq = function(a, b)
        return a[1] == b[1] and a[2] == b[2] and a[3] == b[3]
    end,
    __len = function()
        return 3
    end,
    __tostring = function(self)
        return "{" .. self[1] .. ", " .. self[2] .. ", " .. self[3] .. "}"
    end,
}
---@overload fun(x: number, y: number, z: number): vec3
---@overload fun(value: number): vec3
---@overload fun(): vec3
---@diagnostic disable-next-line: lowercase-global
function vec3(x, y, z)
    if not x then
        return setmetatable({ 0, 0, 0 }, metatable)
    elseif x and not y then
        return setmetatable({ x, x, x }, metatable)
    else
        return setmetatable({ x, y, z }, metatable)
    end
end