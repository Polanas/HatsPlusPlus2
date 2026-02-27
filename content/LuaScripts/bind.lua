---@param f fun(...: any): ...
function bind(f, first)
	return function(...)
		return f(first, ...)
	end
end