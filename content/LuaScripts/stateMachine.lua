---@enum stateMachine.callbackVariant
local callbackVariant = {
    func = 1,
    coroutine = 2,
}

---@class stateMachine.callback
---@field variant stateMachine.callbackVariant
---@field data any?

local callback = {}

---@param func fun():any
---@return stateMachine.callback
function callback.func(func)
    return {
        variant = callbackVariant.func,
        data = func,
    }
end

---@class stateMachine.coroutineData
---@field coroutine fun()
---@field handle? coroutineHandle

---@param coroutine fun()
---@return stateMachine.callback
function callback.coroutine(coroutine)
    return {
        variant = callbackVariant.coroutine,
        ---@type stateMachine.coroutineData
        data = {
            coroutine = coroutine
        },
    }
end

---@class stateMachine.callbacks
---@field updateCallback stateMachine.callback
---@field beginCallback? stateMachine.callback
---@field endCallback? stateMachine.callback

---@class stateMachine.callbacksRaw
---@field updateCallback fun()
---@field beginCallback? fun()
---@field endCallback? fun()

local function dump(o)
   if type(o) == 'table' then
      local s = '{ '
      for k,v in pairs(o) do
         if type(k) ~= 'number' then k = '"'..k..'"' end
         s = s .. '['..k..'] = ' .. dump(v) .. ','
      end
      return s .. '} '
   else
      return tostring(o)
   end
end

local metatable = {
    __call = function()
        ---@class stateMachine
        ---@field state? any
        ---@field previousState? any
        ---@field update_callbacks table<any, stateMachine.callback>
        ---@field begin_callbacks table<any, stateMachine.callback>
        ---@field end_callbacks table<any, stateMachine.callback>
        ---@field coroutines coroutineRunner
        local stateMachine = {
            update_callbacks = {},
            begin_callbacks = {},
            end_callbacks = {},
            coroutines = coroutineRunner(),
        }

        ---@param callbacks stateMachine.callbacks
        function stateMachine:addCallbacks(state, callbacks)
            self.update_callbacks[state] = callbacks.updateCallback
            self.begin_callbacks[state] = callbacks.beginCallback
            self.end_callbacks[state] = callbacks.endCallback
        end

        ---@package
        ---@param callback stateMachine.callback
        function stateMachine:invokeCallback(callback)
            if callback.variant == callbackVariant.func then
                callback.data()
            elseif callback.variant == callbackVariant.coroutine then
                ---@type stateMachine.coroutineData
                local data = callback.data
                local handle = self.coroutines:start(data.coroutine)
                data.handle = handle:clone()
            end
        end

        function stateMachine:setState(state)
            if not self.update_callbacks[state] and not self.end_callbacks[state] and not self.begin_callbacks[state] then
                error("consider adding callbacks for state " .. state .. " before setting it")
            end
            if state == self.state then
                return
            end

            self.state = state
            if self.end_callbacks[self.previousState] then
                self:invokeCallback(self.end_callbacks[self.previousState])
            end
            if self.begin_callbacks[self.state] then
                self:invokeCallback(self.begin_callbacks[self.state])
            end

            local prevUpdateCallback = self.update_callbacks[self.previousState]
            if prevUpdateCallback 
                and prevUpdateCallback.variant == callbackVariant.coroutine  then
                self.coroutines:stop(prevUpdateCallback.data.handle)
            end

            local updateCallback = self.update_callbacks[self.state]
            if updateCallback 
                and updateCallback.variant == callbackVariant.coroutine then
                    local handle = self.coroutines:start(updateCallback.data.coroutine, "update")
                    updateCallback.data.handle = handle:clone()
                end
            
        end

        ---@param deltaTime number
        function stateMachine:update(deltaTime)
            if self.update_callbacks[self.state] then
                local updateCallback = self.update_callbacks[self.state]
                local updateCoroutineState = nil
                if updateCallback.variant == callbackVariant.func then
                    self:setState(updateCallback.data())
                end

                self.coroutines:update(deltaTime, function(handle, state)
                    if updateCallback.variant == callbackVariant.coroutine
                        and updateCallback.data.handle == handle then
                        updateCoroutineState = state
                    end
                end)

                if updateCallback.variant == callbackVariant.coroutine
                    and updateCoroutineState then
                    self:setState(updateCoroutineState)
                end
            end

            self.previousState = self.state
        end

        return stateMachine
    end
}

---@overload fun(): stateMachine
---@diagnostic disable-next-line: lowercase-global
stateMachine = {
    callback = callback
}
setmetatable(stateMachine --[[@as table]], metatable)
