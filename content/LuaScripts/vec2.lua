---@class vec2
---@field x number
---@field y number
---@operator add(vec2): vec2
---@operator add(number): vec2
---@operator sub(vec2): vec2
---@operator sub(number): vec2
---@operator mul(vec2): vec2
---@operator mul(number): vec2
---@operator div(vec2): vec2
---@operator div(number): vec2
---@operator mod(vec2): vec2
---@operator mod(number): vec2
---@operator unm:vec2
---@operator pow(vec2): vec2
---@operator pow(number): vec2

---@class vec2
local methods = {
    ---@param self vec2
    ---@return vec2
    clone = function(self)
        return vec2(self[1], self[2])
    end,
    
    ---@param self vec2
    ---@return number
    length = function(self)
        return math.sqrt(self[1] * self[1] + self[2] * self[2])
    end,

    ---@param self vec2
    ---@return number
    lengthSquared = function(self)
        return self[1] * self[1] + self[2] * self[2]
    end,

    ---@param self vec2
    ---@return vec2
    normalize = function(self)
        return self / self:length()
    end,

    ---@param self vec2
    ---@return vec2
    abs = function(self)
        return vec2(math.abs(self[1]), math.abs(self[2]))
    end,

    ---@param self vec2
    ---@return number
    dot = function(self, other)
        return self[1] * other[1] + self[2] * other[2]
    end,

    ---@param incident vec2
    ---@param normal vec2
    ---@return vec2
    reflect = function(incident, normal)
        return incident - 2 * normal:dot(incident) * normal
    end,

    ---@param self vec2
    ---@param angle number]
    ---@param pivot vec2
    rotate = function (self, angle, pivot)
        local cos = math.cos(angle)
        local sin = math.sin(angle)
        local vec = vec2(self.x - pivot.x, self.y - pivot.y)
        self.x = vec.x * cos - vec.y * sin + pivot.x;
        self.y = vec.x * sin + vec.y * cos + pivot.y
        --float num = (float)Math.Cos(radians);
        -- float num2 = (float)Math.Sin(radians);
        -- Vec2 vec = default(Vec2);
        -- vec.x = x - pivot.x;
        -- vec.y = y - pivot.y;
        -- Vec2 result = default(Vec2);
        -- result.x = vec.x * num - vec.y * num2 + pivot.x;
        -- result.y = vec.x * num2 + vec.y * num + pivot.y;
        -- return result;
    end,

    ---@param self vec2
    ---@param angle number]
    ---@param pivot vec2
    ---@return vec2
    rotated = function (self, angle, pivot)
        local cos = math.cos(angle)
        local sin = math.sin(angle)
        local vec = vec2(self.x - pivot.x, self.y - pivot.y)

        local result = vec2()
        result.x = vec.x * cos - vec.y * sin + pivot.x;
        result.y = vec.x * sin + vec.y * cos + pivot.y
        return result
    end,

    ---@param self vec2
    ---@param other vec2
    ---@return vec2
    min = function(self, other)
        return vec2(math.min(self[1], other[1]), math.min(self[2], other[2]))
    end,

    ---@param self vec2
    ---@param other vec2
    ---@return vec2
    max = function(self, other)
        return vec2(math.max(self[1], other[1]), math.max(self[2], other[2]))
    end,

    ---@param self vec2
    ---@return number, number
    unpack = function(self)
        return self[1], self[2]
    end,

    type = "vec2",
}

local metatable = {
    __newindex = function(self, key, value)
        if key == "x" then
            rawset(self, 1, value)
        elseif key == "y" then
            rawset(self, 2, value)
        end
    end,
    __index = function(self, key)
        if key == "x" then
            return self[1]
        elseif key == "y" then
            return self[2]
        else
            return rawget(methods, key)
        end
    end,
    __add = function(a, b)
        if type(a) == "number" then
            return vec2(a + b[1], a + b[2])
        else
            return vec2(a[1] + b[1], a[2] + b[2])
        end
    end,
    __sub = function(a, b)
        if type(a) == "number" then
            return vec2(a - b[1], a - b[2])
        else
            return vec2(a[1] - b[1], a[2] - b[2])
        end
    end,

    __mul = function(a, b)
        if type(a) == "number" then
            return vec2(a * b[1], a * b[2])
        else
            return vec2(a[1] * b[1], a[2] * b[2])
        end
    end,
    __div = function(a, b)
        if type(a) == "number" then
            return vec2(a / b[1], a / b[2])
        else
            return vec2(a[1] / b[1], a[2] / b[2])
        end
    end,

    __mod = function(a, b)
        if type(a) == "number" then
            return vec2(a % b[1], a % b[2])
        else
            return vec2(a[1] % b[1], a[2] % b[2])
        end
    end,
    __unm = function(a)
        return vec2(-a[1], -a[2])
    end,
    __pow = function(a, b)
        if type(a) == "number" then
            return vec2(a ^ b[1], a ^ b[2])
        else
            return vec2(a[1] ^ b[1], a[2] ^ b[2])
        end
    end,
    __eq = function(a, b)
        return a[1] == b[1] and a[2] == b[2]
    end,
    __len = function()
        return 2
    end,
    __tostring = function(self)
        return "{" .. self[1] .. ", " .. self[2] .. "}"
    end,
}
---@return vec2
---@overload fun(x: number, y: number): vec2
---@overload fun(value: number): vec2
---@overload fun(): vec2
---@diagnostic disable-next-line: lowercase-global
function vec2(x, y)
    if not x then
        return setmetatable({ 0, 0 }, metatable)
    elseif x and not y then
        return setmetatable({ x, x }, metatable)
    else
        return setmetatable({ x, y }, metatable)
    end
end