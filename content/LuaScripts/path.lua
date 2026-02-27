---@class path.defs
path = {
    ---@param ... string
    ---@return string
    join = function (...)
        local items = {...}
        local result = items[1]
        for i = 2, #items do
            result = result .. PATH_DELIMETER .. items[i]
        end
        return result
    end
}