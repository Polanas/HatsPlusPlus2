---@class coroutineHandle
---  @field package luaCoroutine thread
---  @field package delaySecs number
---  @field package args table
---  @field package tag string?
---  @field isAlive fun(self: coroutineHandle): boolean

---@alias scheduler.Coroutines table<coroutineHandle, true>

---@class scheduler
---  @field package handles scheduler.Coroutines
---  @field package handlesByTags table<string, coroutineHandle>
---  @field package handlesToAdd scheduler.Coroutines
---  @field package handlesToRemove scheduler.Coroutines
---  @field package isUpdating boolean
---  @field package aboutToCloseAll boolean
---  @field package isAlive fun(handle: coroutineHandle): boolean
local scheduler = {}
---@package
scheduler.__index = scheduler

---@package
---@param handle coroutineHandle
function scheduler:remove(handle)
	self.handles[handle] = nil
	if handle.tag then
		self.handlesByTags:remove(handle.tag)
	end
end

---@param func fun(...: ...): any
---@param ... ...
---@return coroutineHandle
function scheduler:spawn(func, ...)
	return self:spawnInternal(func, nil, 0, ...)
end

---@param func fun(...: ...): any
---@param ... ...
---@param delay number
function scheduler:after(delay, func, ...)
	return self:spawnInternal(func, nil, delay, ...)
end

---@param func fun(...: ...): any
---@param ... ...
---@param delay number
---@param tag string
function scheduler:afterWithTag(delay, func, tag, ...)
	return self:spawnInternal(func, tag, delay, ...)
end

---@param func fun(...: ...): boolean?
---@param ... ...
---@param delay number
---@param start_delay number
---@param tag string
---@param count number?
function scheduler:everyAfterWithTag(delay, func, count, start_delay, tag, ...)
	return self:everyInternal(delay, func, count, start_delay, tag, ...)
end

---@param func fun(...: ...): boolean?
---@param ... ...
---@param delay number
---@param start_delay number
---@param count number?
function scheduler:everyAfter(delay, func, count, start_delay, ...)
	return self:everyInternal(delay, func, count, start_delay, nil, ...)
end

---@param func fun(...: ...): boolean?
---@param ... ...
---@param delay number
---@param count number?
function scheduler:every(delay, func, count, ...)
	return self:everyInternal(delay, func, count, 0, nil, ...)
end

---@param func fun(...: ...): boolean?
---@param ... ...
---@param delay number
---@param tag string
---@param count number?
function scheduler:everyWithTag(delay, func, count, tag, ...)
	return self:everyInternal(delay, func, count, 0, tag, ...)
end

---@param func fun(...: ...): boolean?
---@param ... ...
---@param duration number
---@param tag string
---@param start_delay number
---@param exit? fun()
function scheduler:duringAfterWithTag(duration, func, exit, start_delay, tag, ...)
	return self:duringInternal(duration, func, exit, start_delay, tag, ...)
end

---@param func fun(...: ...): boolean?
---@param ... ...
---@param duration number
---@param start_delay number
---@param exit? fun()
function scheduler:duringAfter(duration, func, exit, start_delay, ...)
	return self:duringInternal(duration, func, exit, start_delay, nil, ...)
end


---@param func fun(...: ...): boolean?
---@param ... ...
---@param duration number
---@param tag string
---@param exit? fun()
function scheduler:duringWithTag(duration, func, exit, tag, ...)
	return self:duringInternal(duration, func, exit, 0, tag, ...)
end

---@param func fun(...: ...): boolean?
---@param ... ...
---@param duration number
---@param exit? fun()
function scheduler:during(duration, func, exit, ...)
	return self:duringInternal(duration, func, exit, 0, nil, ...)
end

---@package
---@param func fun(...: ...): boolean?
---@param ... ...
---@param duration number
---@param start_delay number
---@param exit? fun()
function scheduler:duringInternal(duration, func, exit, start_delay, tag, ...)
	return self:spawnInternal(function(duration, func, exit, dt, ...)
		while true do
			func(...)
			duration = math.max(duration - dt, 0)
			if duration == 0 then
				break
			end
			coroutine.yield()
		end

		if exit then
			exit()
		end
	end, tag, start_delay, duration, func, exit, deltaTime, ...)
end

local every_fn = function(delay, func, count, ...)
	while true do
		local result = func(...)
		if count then
			count = math.max(count - 1, 0)

			if count == 0 then
				break
			end
		elseif result == false then
			break
		end

		coroutine.yield(delay)
	end
end


---@param func fun(...: ...): boolean?
---@param ... ...
---@param delay number
---@param start_delay number
---@param count number?
---@param tag string?
---@package
function scheduler:everyInternal(delay, func, count, start_delay, tag, ...)
	return self:spawnInternal(every_fn, tag, start_delay, delay, func, count, ...)
end

local function repeatFn(func, ...)
	while true do
		local result = func(...)
		if type(result) == "number" then
			coroutine.yield(result)
		elseif result == false then
			break
		else
			coroutine.yield()
		end
	end
end

---@param func fun(...: ...): (number | true)?
---@param tag string?
---@param ... ...
function scheduler:repeatWithTag(func, tag, ...)
	return self:spawnInternal(repeatFn, tag, 0, func, ...)
end


---@param func fun(...: ...): (number | true)?
function scheduler:spawnRepeat(func, ...)
	return self:spawnInternal(repeatFn, nil, 0, func, ...)
end


---@param func fun(...: ...): number?
---@param ... ...
---@param start_delay number
---@param tag string?
---@package
function scheduler:repeat_internal(func, start_delay, tag, ...)
	return self:spawnInternal(repeatFn, tag, start_delay, func, ...)
end


---@param func fun(...: ...): any
---@param ... ...
---@param tag string?
---@return coroutineHandle
function scheduler:spawnWithTag(func, tag, ...)
	return self:spawnInternal(func, tag, 0, ...)
end

-- ---@param duration number
-- ---@param subject table
-- ---@param target table
-- ---@param easing fun(t: number, ...: ...)
-- ---@param exit? fun()
-- ---@param start_delay number
-- ---@param tag string
-- ---@param ... ...
-- ---@return coroutineHanle
-- function Scheduler:tween_after_with_tag(
-- 	duration,
-- 	subject,
-- 	target,
-- 	easing,
-- 	start_delay,
-- 	tag,
-- 	exit,
-- 	...
-- )
-- 	return self:tween_internal(
-- 		duration,
-- 		subject,
-- 		target,
-- 		easing,
-- 		exit,
-- 		start_delay,
-- 		tag,
-- 		...
-- 	)
-- end

-- ---@param duration number
-- ---@param subject table
-- ---@param target table
-- ---@param easing fun(t: number, ...: ...)
-- ---@param start_delay number
-- ---@param exit? fun()
-- ---@param ... ...
-- ---@return coroutineHanle
-- function Scheduler:tween_after(
-- 	duration,
-- 	subject,
-- 	target,
-- 	easing,
-- 	exit,
-- 	start_delay,
-- 	...
-- )
-- 	return self:tween_internal(
-- 		duration,
-- 		subject,
-- 		target,
-- 		easing,
-- 		exit,
-- 		start_delay,
-- 		nil,
-- 		...
-- 	)
-- end

-- ---@param duration number
-- ---@param subject table
-- ---@param target table
-- ---@param easing fun(t: number, ...: ...)
-- ---@param exit? fun()
-- ---@param tag string
-- ---@param ... ...
-- ---@return coroutineHanle
-- function Scheduler:tween_with_tag(duration, subject, target, easing, tag, exit, ...)
-- 	return self:tween_internal(duration, subject, target, easing, exit, 0, tag, ...)
-- end

-- ---@param duration number
-- ---@param subject table
-- ---@param target table
-- ---@param easing fun(t: number, ...: ...)
-- ---@param exit? fun()
-- ---@param ... ...
-- ---@return coroutineHanle
-- function Scheduler:tween(duration, subject, target, easing, exit, ...)
-- 	return self:tween_internal(duration, subject, target, easing, exit, 0, nil, ...)
-- end

-- ---@param duration number
-- ---@param dt number
-- ---@param subject table
-- ---@param target table
-- ---@param easing fun(t: number, ...: any)
-- ---@param exit? fun()
-- ---@param initial_values table
-- local tween_fn = function(
-- 	duration,
-- 	dt,
-- 	subject,
-- 	target,
-- 	easing,
-- 	exit,
-- 	log_error,
-- 	math_lerp,
-- 	vec_lerp,
-- 	initial_values
-- )
-- 	local current_time = dt
-- 	while true do
-- 		for k, v in pairs(initial_values) do
-- 			local t = easing(current_time / duration)
-- 			local subject_key_type = type(subject[k])
-- 			if subject_key_type == "number" then
-- 				subject[k] = math_lerp(v, target[k], t)
-- 			elseif subject_key_type == "vector" then
-- 				subject[k] = vec_lerp(t, v, target[k])
-- 			else
-- 				print("invalid tween type: " .. subject_key_type)
-- 			end
-- 		end
-- 		current_time = current_time + dt
-- 		if current_time >= duration then
-- 			break
-- 		end

-- 		coroutine.yield()
-- 	end

-- 	for k, v in pairs(target) do
-- 		subject[k] = v
-- 	end
-- 	if exit then
-- 		exit()
-- 	end
-- end

-- ---@package
-- ---@param duration number
-- ---@param subject table
-- ---@param target table
-- ---@param easing fun(t: number, ...: ...)
-- ---@param exit? fun()
-- ---@param start_delay number
-- ---@param tag string?
-- ---@param ... ...
-- ---@return coroutineHanle
-- function Scheduler:tween_internal(
-- 	duration,
-- 	subject,
-- 	target,
-- 	easing,
-- 	exit,
-- 	start_delay,
-- 	tag,
-- 	...
-- )
-- 	local initial_values = fun.to_map(fun.iter(target):map(function(k)
-- 		return k, subject[k]
-- 	end))

-- 	return self:spawn_internal(
-- 		tween_fn,
-- 		tag,
-- 		start_delay,
-- 		duration,
-- 		deltaTime,
-- 		subject,
-- 		target,
-- 		easing,
-- 		exit,
-- 		lopa.log.error,
-- 		math.lerp,
-- 		vec4.lerp,
-- 		initial_values
-- 	)
-- end

---@package

---@param func fun(...: ...): any
---@param ... ...
---@param tag string?
---@param delay number?
---@return coroutineHandle
function scheduler:spawnInternal(func, tag, delay, ...)
	---@type coroutineHandle
	local handle = {
		delaySecs = delay or 0,
		luaCoroutine = coroutine.create(func),
		args = { ... },
		tag = tag,
		---@type fun(self: scheduler, handle: coroutineHandle)
		isAlive = self.isAliveCurried,
	}

	local coroutines_table = self.isUpdating and self.handlesToAdd or self.handles

	coroutines_table[handle] = true

	if tag then
		local existing_handle = self.handlesByTags[tag]
		if existing_handle then
			self:close(existing_handle)
		end
		self.handlesByTags[tag] = handle
	end

	return handle
end

---@param delta_time number
function scheduler:update(delta_time)
	self.isUpdating = true
	for handle, _ in pairs(self.handles) do
		handle.delaySecs = math.max(handle.delaySecs - delta_time, 0)

		if handle.delaySecs == 0 then
			local success, delay =
				coroutine.resume(handle.luaCoroutine, table.unpack(handle.args))

			local status = coroutine.status(handle.luaCoroutine)
			if status == "dead" then
				self:close(handle)
				goto continue
			end

			if not success then
				print("error while executing coroutine")
			end

			if delay and type(delay) == "number" then
				handle.delaySecs = delay
			end
		end
	    ::continue::
	end
	self.isUpdating = false

	for handle, _ in pairs(self.handlesToAdd) do
		self.handles[handle] = true
	end
	for handle, _ in pairs(self.handlesToRemove) do
		self:remove(handle)
	end

	if self.aboutToCloseAll then
		self.aboutToCloseAll = false
		self.handles = {}
	end
end

---@param handle coroutineHandle
---@return boolean
function scheduler:isAlive(handle)
	return self.handles[handle] ~= nil or self.handlesToAdd[handle] ~= nil
end

---@package
scheduler.isAliveCurried = bind(scheduler.isAlive, scheduler)

---@param tag string
---@return boolean
function scheduler:close_with_tag(tag)
	local handle = self.handlesByTags[tag]
	if handle then
		self:close(handle)
		return true
	else
		return false
	end
end

---@param handle coroutineHandle
function scheduler:close(handle)
	if not self:isAlive(handle) then
		print("attempt to stop dead coroutine with tag " .. handle.tag)
		return
	end

	coroutine.close(handle.luaCoroutine)

	if self.isUpdating then
		self.handlesToRemove[handle] = true
	else
		self:remove(handle)
	end
end

function scheduler:closeAll()
	for coroutine_data, _ in pairs(self.handles) do
		coroutine.close(coroutine_data.luaCoroutine)
	end

	if self.isUpdating then
		self.aboutToCloseAll = true
		return
	end

	self.handles = {}
	self.handlesByTags = {}
end

---@param ... coroutineHandle
function scheduler:await(...)
	local handles = { ... }

	while true do
		local is_alive = false
		for _, handle in ipairs(handles) do
			if self:isAlive(handle) then
				is_alive = true
				break
			end
		end

		if not is_alive then
			return
		else
			coroutine.yield()
		end
	end
end

---@class scheduler.Defs
Scheduler = {
	---@return scheduler
	new = function()
		local scheduler = setmetatable({
			handles = {},
			handlesByTags = {},
			handlesToAdd = {},
			handlesToRemove = {},
			isUpdating = false,
			aboutToCloseAll = false,
		}, scheduler)
		scheduler.isAliveCurried = bind(scheduler.isAlive, scheduler)

		return scheduler
	end,
}