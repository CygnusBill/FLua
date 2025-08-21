
local M = {}

function M.compute(n)
    local result = 0
    for i = 1, n do
        for j = 1, n do
            result = result + i * j
        end
    end
    return result
end

return M
