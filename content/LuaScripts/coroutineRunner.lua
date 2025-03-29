---@enum coroutineRunner.outputVariant
local outputVariant = {
    next = 1,
    delay = 2,
    yieldBreak = 3,
}

---@class coroutineRunner.output
---@field variant coroutineRunner.outputVariant
---@field data table?
local coroutineOutput = {}

---@return coroutineRunner.output
function coroutineOutput.next()
    return {
        variant = outputVariant.next,
    }
end

---@param data table
---@return coroutineRunner.output
function coroutineOutput.yieldBreak(data)
    return {
        variant = outputVariant.yieldBreak,
        data = data
    }
end

---@param delay number
---@return coroutineRunner.output
function coroutineOutput.delay(delay)
    return {
        variant = outputVariant.delay,
        data = {delay}
    }
end

---@class coroutineHandle
---@field id number
---@field gen number
---@field clone fun(self: coroutineHandle): coroutineHandle

coroutine.next = function ()
    coroutine.yield(coroutineOutput.next())
end
---@param delaySecs number
coroutine.delay = function (delaySecs)
    coroutine.yield(coroutineOutput.delay(delaySecs))
end
coroutine.yieldBreak = function (...)
    local args = {...}
    args = #args == 0 and nil or args
    coroutine.yield(coroutineOutput.yieldBreak(args))
end

---@return coroutineRunner
---@diagnostic disable-next-line: lowercase-global
local function newCoroutineRunner()
    ---@class coroutineRunner.coroutineData
    ---@field luaCoroutine thread
    ---@field delay number
    ---@field args table?
    ---@field handle coroutineHandle

    ---@class coroutineRunner
    ---@field private coroutines coroutineRunner.coroutineData[]
    ---@field private recycledIds coroutineHandle[]
    local coroutineRunner = {
        coroutines = {},
        recycledIds = {},
    }
    
    local handleMetatable
    local handleClone = function (self)
        local handle = {
            id = self.id,
            gen = self.gen
        }
        setmetatable(handle, handleMetatable)
        return handle
    end
    handleMetatable = {
        ---@param self coroutineHandle
        ---@param other coroutineHandle
        __eq = function (self, other)
            return self.id == other.id and self.gen == other.gen
        end,
        __index = function (self, key) 
            if key == "id" then
                return rawget(self, "id")
            elseif key == "gen" then
                return rawget(self, "gen")
            elseif key == "clone" then
                return handleClone
            end
        end
    }

    ---@package
    ---@return coroutineHandle
    function coroutineRunner:coroutineHandle()
        if #self.recycledIds > 0 then
            ---@type coroutineHandle
            local oldId = table.remove(self.recycledIds, #self.recycledIds)
            oldId.gen = oldId.gen + 1
            -- print("returning old handle")
            -- print("id", oldId.id)
            -- print("gen", oldId.gen)
            ---@type coroutineHandle
            return setmetatable({
                id = oldId.id,
                gen = oldId.gen
            }, handleMetatable)
        end
        -- print("returning new id")
        -- print("id", #self.coroutines+1)
        -- print("gen", 0)

        ---@type coroutineHandle
        return setmetatable({
            id = #self.coroutines + 1,
            gen = 0
        }, handleMetatable)
    end

    ---@param func fun(...)
    ---@return coroutineHandle
    function coroutineRunner:start(func, ...)
        local args = {...}
        local handle = self:coroutineHandle()

        ---@type coroutineRunner.coroutineData
        local coroutine = {
            luaCoroutine = coroutine.create(func),
            args = #args == 0 and nil or args,
            delay = 0,
            handle = handle,
        }

        self.coroutines[handle.id] = coroutine
        ---@type coroutineHandle
        return handle
    end

    ---@param deltaTime number
    ---@param on_finish? fun(handle: coroutineHandle, ...)
    function coroutineRunner:update(deltaTime, on_finish)
        local coroutinesToRemove = {}
        for index, coroutineData in pairs(self.coroutines) do
            coroutineData.delay = coroutineData.delay - deltaTime
            if coroutineData.delay <= 0 then
                ---@type boolean, coroutineRunner.output?
                local success, output = coroutine.resume(coroutineData.luaCoroutine, table.unpack(coroutineData.args));
                if output ~= nil then
                    if output.variant == outputVariant.next then
                        coroutineData.delay = 0
                    elseif output.variant == outputVariant.delay then
                        coroutineData.delay = output.data[1]
                    elseif output.variant == outputVariant.yieldBreak then
                        if on_finish ~= nil then
                            on_finish(coroutineData.handle, table.unpack(output.data))
                        end
                        table.insert(coroutinesToRemove, index)
                        goto continue
                    end 
                end
                if coroutine.status(coroutineData.luaCoroutine) == "dead" then
                    if on_finish ~= nil then
                        on_finish(index --[[@as coroutineHandle]])
                    end
                    table.insert(coroutinesToRemove, index)
                end
            end
            ::continue::
        end
        for _, index in pairs(coroutinesToRemove) do
            table.insert(self.recycledIds, self.coroutines[index].handle)
            self.coroutines[index] = nil
        end
    end

    ---@param handle coroutineHandle
    function coroutineRunner:stop(handle)
        if not self:isAlive(handle) then
            return
        end
        local data = self.coroutines[handle.id]
        if data.handle ~= handle then
            return
        end
        table.insert(self.recycledIds, handle)
        self.coroutines[handle.id] = nil
    end

    ---@param handle coroutineHandle
    ---@return boolean
    function coroutineRunner:isAlive(handle)
        local data = self.coroutines[handle.id]
        if not data then
            return false
        end
        return data.handle.gen == handle.gen
    end

    function coroutineRunner:stopAll()
        for k,v in pairs(self.coroutines) do
            table.insert(self.recycledIds, v.handle)
            self.coroutines[k] = nil
        end
    end

    return coroutineRunner
end


local metatable = {
    __call = function ()
        return newCoroutineRunner()
    end
}

---@overload fun(): coroutineRunner
---@diagnostic disable-next-line: lowercase-global
coroutineRunner = {
    outputVariant = outputVariant,
}

setmetatable(coroutineRunner --[[@as table]], metatable)
